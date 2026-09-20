<template>
  <!-- Corner marker, only when a note exists. Click opens the note and keeps it open. -->
  <button
    v-if="note"
    ref="toggleEl"
    type="button"
    class="note-toggle"
    :class="{ 'note-toggle--pinned': pinned }"
    title="Admin note"
    @click.stop.prevent="pinned = !pinned"
  >
    <v-icon size="16">mdi-note-text</v-icon>
  </button>

  <!--
    The panel floats below the card. It opens after the card has been hovered for a moment,
    stays open while it is clicked or focused, and closes on a click anywhere else.
    v-show keeps the textarea mounted so a draft is not lost while hidden.
  -->
  <Transition name="pop">
    <div
      v-show="hoverOpen || pinned"
      ref="panelEl"
      class="note-popover"
      @click.stop="pinned = true"
      @focusin="pinned = true"
    >
      <div class="note-popover__header">
        <span class="note-popover__title">Admin note</span>
        <span class="note-popover__private">private</span>
        <button
          type="button"
          class="note-popover__pick"
          :class="{ 'note-popover__pick--remove': isPick }"
          :title="
            isPick
              ? 'Remove from admin picks (then Save Changes)'
              : 'Add to admin picks (then Save Changes)'
          "
          @click="emit('toggle-pick')"
        >
          <v-icon size="14">{{ isPick ? 'mdi-account-remove' : 'mdi-account-plus' }}</v-icon>
          {{ isPick ? 'Remove pick' : 'Add pick' }}
        </button>
      </div>
      <textarea
        v-model="draft"
        class="note-popover__input"
        rows="3"
        maxlength="1000"
        placeholder="Enter notes here..."
      />
      <div class="note-popover__footer">
        <span class="note-popover__meta">{{ metaText }}</span>
        <button type="button" class="note-popover__save" :disabled="!dirty || busy" @click="submit">
          {{ buttonLabel }}
        </button>
      </div>
    </div>
  </Transition>
</template>

<script setup>
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue'
import api from '@/services/api'
import { useNotify } from '@/composables/useNotify'

// A private admin note that floats under a moveset card.
// The parent owns the saved note object ({ note, updatedByUserName, updatedAt } or null),
// the unsaved draft (so it survives the card moving between lists), and tells the popover
// when its card is hovered and whether the moveset is currently an admin pick.
const props = defineProps({
  movesetId: { type: Number, required: true },
  visible: { type: Boolean, default: false },
  isPick: { type: Boolean, default: false },
})

const emit = defineEmits(['toggle-pick'])

const note = defineModel('note', { type: Object, default: null })
const draft = defineModel('draft', { type: String, default: '' })

const HOVER_DELAY_MS = 500

const notify = useNotify()
const busy = ref(false)
const pinned = ref(false)
const hoverOpen = ref(false)
const panelEl = ref(null)
const toggleEl = ref(null)
let hoverTimer = null

const dirty = computed(() => (draft.value ?? '').trim() !== (note.value?.note ?? ''))

const metaText = computed(() =>
  note.value
    ? `Last edited by ${note.value.updatedByUserName ?? 'unknown'} on ${formatWhen(note.value.updatedAt)}`
    : 'No note yet'
)

const buttonLabel = computed(() => {
  if (busy.value) return 'Saving…'
  return (draft.value ?? '').trim() || !note.value ? 'Save' : 'Clear'
})

// Open only after the pointer has rested on the card for a moment.
watch(
  () => props.visible,
  (hovered) => {
    clearTimeout(hoverTimer)
    if (hovered) {
      hoverTimer = setTimeout(() => (hoverOpen.value = true), HOVER_DELAY_MS)
    } else {
      hoverOpen.value = false
    }
  }
)

// A saved note arriving from the server replaces the draft.
watch(note, (n) => {
  draft.value = n?.note ?? ''
})

// A click anywhere outside the panel and its corner marker unpins it.
function onDocumentPointerDown(e) {
  if (!pinned.value) return
  if (panelEl.value?.contains(e.target) || toggleEl.value?.contains(e.target)) return
  pinned.value = false
}

onMounted(() => document.addEventListener('pointerdown', onDocumentPointerDown))
onBeforeUnmount(() => {
  document.removeEventListener('pointerdown', onDocumentPointerDown)
  clearTimeout(hoverTimer)
})

function formatWhen(iso) {
  const d = new Date(iso)
  return isNaN(d) ? '' : d.toLocaleString()
}

async function submit() {
  busy.value = true
  try {
    const res = await api.put(`/movesets/${props.movesetId}/admin-note`, {
      note: draft.value ?? '',
    })
    // 204 means the note was cleared.
    note.value = res.status === 204 || !res.data ? null : res.data
    notify.success(note.value ? 'Note saved.' : 'Note cleared.')
  } catch (err) {
    notify.error('Could not save the note.', err)
  } finally {
    busy.value = false
  }
}
</script>

<style scoped>
/* Motion tokens */
.note-toggle,
.note-popover {
  --duration-quick: 150ms;
  --duration-fast: 250ms;
  --ease-smooth-out: cubic-bezier(0.22, 1, 0.36, 1);
}

/* Corner marker on the card */
.note-toggle {
  position: absolute;
  top: 4px;
  right: 6px;
  z-index: 61;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  border: 1px solid #f9a825;
  background-color: rgba(18, 18, 18, 0.85);
  color: #ffd54f;
  cursor: pointer;
  transition:
    color var(--duration-quick) var(--ease-smooth-out),
    border-color var(--duration-quick) var(--ease-smooth-out),
    background-color var(--duration-quick) var(--ease-smooth-out);
}
.note-toggle:hover {
  color: #fff;
  border-color: #ffd54f;
}
.note-toggle--pinned {
  background-color: #ffd54f;
  color: #111;
}

/* Floating panel below the card */
.note-popover {
  position: absolute;
  top: calc(100% - 2px);
  left: 0;
  transform-origin: top center;
  width: 100%;
  z-index: 70;
  padding: 0.5rem 0.6rem 0.6rem;
  background-color: #1a1a1a;
  border: 1px solid #444;
  border-radius: 6px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.6);
  cursor: default;
}

.note-popover__header {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  margin-bottom: 0.35rem;
}
.note-popover__title {
  font-size: 0.85em;
  font-weight: bold;
  color: #ddd;
}
.note-popover__private {
  font-size: 0.7em;
  padding: 0 0.45rem;
  border-radius: 999px;
  background-color: #2e2e2e;
  color: #aaa;
}
.note-popover__pick {
  margin-left: auto;
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  font-size: 0.75em;
  padding: 1px 8px;
  border-radius: 4px;
  border: 1px solid #2e7d32;
  background: #1b3a1b;
  color: #c8e6c9;
  cursor: pointer;
  transition:
    background-color var(--duration-quick) var(--ease-smooth-out),
    border-color var(--duration-quick) var(--ease-smooth-out),
    color var(--duration-quick) var(--ease-smooth-out);
}
.note-popover__pick:hover {
  background: #245224;
}
.note-popover__pick--remove {
  border-color: #c62828;
  background: #3a1010;
  color: #ef9a9a;
}
.note-popover__pick--remove:hover {
  background: #4d1515;
}

.note-popover__input {
  width: 100%;
  resize: vertical;
  font: inherit;
  font-size: 0.85em;
  color: #ddd;
  background-color: #121212;
  border: 1px solid #333;
  border-radius: 4px;
  padding: 0.3rem 0.45rem;
  transition: border-color var(--duration-quick) var(--ease-smooth-out);
}
.note-popover__input:focus {
  outline: none;
  border-color: #64b5f6;
}

.note-popover__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  margin-top: 0.3rem;
}
.note-popover__meta {
  font-size: 0.72em;
  color: #777;
}
.note-popover__save {
  font-size: 0.78em;
  padding: 2px 10px;
  border-radius: 4px;
  border: 1px solid #444;
  background: #1e1e1e;
  color: #ccc;
  cursor: pointer;
  transition:
    background-color var(--duration-quick) var(--ease-smooth-out),
    opacity var(--duration-quick) var(--ease-smooth-out);
}
.note-popover__save:hover:not(:disabled) {
  background: #2a2a2a;
}
.note-popover__save:disabled {
  opacity: 0.4;
  cursor: default;
}

/* Open and close: a short scale-up from the card's edge with a fade */
.pop-enter-active {
  transition:
    transform var(--duration-fast) var(--ease-smooth-out),
    opacity var(--duration-fast) var(--ease-smooth-out);
}
.pop-leave-active {
  transition:
    transform var(--duration-quick) var(--ease-smooth-out),
    opacity var(--duration-quick) var(--ease-smooth-out);
}
.pop-enter-from,
.pop-leave-to {
  transform: scale(0.96);
  opacity: 0;
}

@media (prefers-reduced-motion: reduce) {
  .pop-enter-active,
  .pop-leave-active,
  .note-toggle,
  .note-popover__pick,
  .note-popover__input,
  .note-popover__save {
    transition: none !important;
  }
}
</style>

<template>
  <div class="list-editor">
    <div class="list-editor__head">
      <router-link to="/hooks/add" target="_blank" class="list-editor__hint">
        Don't see your hook? Submit it.
      </router-link>
      <AppButton
        variant="ghost"
        size="sm"
        :icon="showForm ? 'mdi-close' : 'mdi-plus'"
        @click="toggleForm"
      >
        {{ showForm ? 'Cancel' : 'Add a hook' }}
      </AppButton>
    </div>

    <!-- Add / edit -->
    <v-expand-transition>
      <div v-if="showForm" class="list-editor__form">
        <LabeledField label="Hook" required class="list-editor__wide">
          <v-autocomplete
            v-model="draft.hookId"
            :items="hookOptions"
            :item-title="(item) => `${formatOffset(item.offset)} (${item.description})`"
            item-value="hookId"
            density="compact"
            hide-details
          />
        </LabeledField>
        <LabeledField label="What it is used for" class="list-editor__wide">
          <v-text-field
            v-model="draft.description"
            :placeholder="`What does ${characterName || 'the character'} use this for?`"
            density="compact"
            hide-details
          />
        </LabeledField>
        <AppButton size="sm" icon="mdi-check" class="list-editor__commit" @click="commitDraft">
          {{ editingIndex !== null ? 'Update hook' : 'Add hook' }}
        </AppButton>
      </div>
    </v-expand-transition>

    <!-- List -->
    <ul v-if="hooks.length" class="list-editor__list">
      <li v-for="(entry, i) in hooks" :key="i" class="list-editor__row">
        <span class="list-editor__text">
          <strong class="mono">{{ formatOffset(entry.offset) }}</strong>
          <span class="muted">{{ entry.hookDescription }}</span>
          <span v-if="entry.description" class="faint">{{ entry.description }}</span>
        </span>
        <span class="list-editor__actions">
          <button
            type="button"
            class="icon-btn"
            :class="{ 'icon-btn--hidden': i === 0 }"
            aria-label="Move up"
            @click="moveItem(hooks, i, -1)"
          >
            <v-icon size="18">mdi-arrow-up</v-icon>
          </button>
          <button
            type="button"
            class="icon-btn"
            :class="{ 'icon-btn--hidden': i === hooks.length - 1 }"
            aria-label="Move down"
            @click="moveItem(hooks, i, 1)"
          >
            <v-icon size="18">mdi-arrow-down</v-icon>
          </button>
          <button type="button" class="icon-btn" aria-label="Edit" @click="editEntry(i)">
            <v-icon size="18">mdi-pencil</v-icon>
          </button>
          <button
            type="button"
            class="icon-btn icon-btn--danger"
            aria-label="Remove"
            @click="hooks.splice(i, 1)"
          >
            <v-icon size="18">mdi-delete</v-icon>
          </button>
        </span>
      </li>
    </ul>
    <p v-else class="list-editor__empty">No hooks yet.</p>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { moveItem } from '@/services/listUtils'
import { formatOffset } from '@/services/offsets'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'

// Editable list of the hooks a moveset uses: { hookId, offset, hookDescription, description }.
// offset and hookDescription are copied from the chosen hook so the list can render without a lookup.
const props = defineProps({
  hookOptions: { type: Array, default: () => [] },
  characterName: { type: String, default: '' },
})
const hooks = defineModel({ type: Array, default: () => [] })

const emptyDraft = () => ({ hookId: null, description: '' })
const draft = ref(emptyDraft())
const showForm = ref(false)
const editingIndex = ref(null)

// Closing the form also discards any in-progress edit.
const toggleForm = () => {
  showForm.value = !showForm.value
  if (!showForm.value) {
    editingIndex.value = null
    draft.value = emptyDraft()
  }
}

const commitDraft = () => {
  if (!draft.value.hookId) return
  const hook = props.hookOptions.find((h) => h.hookId === draft.value.hookId)
  if (!hook) return

  const entry = {
    hookId: hook.hookId,
    offset: hook.offset,
    hookDescription: hook.description,
    description: draft.value.description,
  }

  if (editingIndex.value !== null) {
    hooks.value[editingIndex.value] = entry
    editingIndex.value = null
  } else {
    hooks.value.push(entry)
  }

  draft.value = emptyDraft()
  showForm.value = false
}

const editEntry = (i) => {
  editingIndex.value = i
  const entry = hooks.value[i]
  draft.value = { hookId: entry.hookId, description: entry.description }
  showForm.value = true
}
</script>

<style scoped>
.list-editor {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.list-editor__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.list-editor__hint {
  font-size: 12px;
  color: var(--tx-2);
}

.list-editor__form {
  display: grid;
  grid-template-columns: 1fr 1fr auto;
  gap: 12px;
  align-items: end;
  padding: 14px;
  border: 1px solid var(--line);
  background: var(--panel-2);
}

.list-editor__commit {
  margin-bottom: 5px;
}

.list-editor__list {
  margin: 0;
  padding: 0;
  list-style: none;
}

.list-editor__row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 8px 0;
  border-bottom: 1px solid var(--line);
  font-size: 14px;
}

.list-editor__row:last-child {
  border-bottom: 0;
}

.list-editor__text {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 8px;
  min-width: 0;
}

.list-editor__actions {
  display: flex;
  gap: 2px;
  flex: none;
}

.list-editor__empty {
  margin: 0;
  color: var(--tx-3);
  font-size: 13px;
}

.icon-btn {
  display: flex;
  padding: 4px;
  border: 0;
  background: none;
  color: var(--tx-2);
  cursor: pointer;
  transition: color var(--dur-fast) var(--ease);
}

.icon-btn:hover {
  color: var(--white);
}

.icon-btn--danger:hover {
  color: var(--err);
}

.icon-btn--hidden {
  visibility: hidden;
  pointer-events: none;
}

@media (max-width: 959px) {
  .list-editor__form {
    grid-template-columns: 1fr;
  }

  .list-editor__commit {
    margin-bottom: 0;
  }
}
</style>

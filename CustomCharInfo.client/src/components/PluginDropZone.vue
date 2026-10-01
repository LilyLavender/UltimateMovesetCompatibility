<template>
  <div class="drop-wrap">
    <div
      class="drop-zone"
      :class="{
        'drop-zone--active': dragging,
        'drop-zone--has-file': file && !duplicate,
        'drop-zone--duplicate': duplicate,
        'drop-zone--large': large,
      }"
      @dragover.prevent="dragging = true"
      @dragleave.prevent="dragging = false"
      @drop.prevent="onDrop"
      @click="openPicker"
    >
      <input ref="inputEl" type="file" :accept="accept" class="hidden-input" @change="onPick" />
      <template v-if="duplicate">
        <v-icon size="24" class="drop-icon">mdi-alert-circle</v-icon>
        <div class="drop-label-group">
          <span class="drop-label">
            Already uploaded. This plugin is
            <router-link
              v-if="duplicate.attachmentType === 'Moveset'"
              :to="{ name: 'MovesetDetail', params: { movesetId: duplicate.movesetId } }"
              @click.stop
              >{{ duplicate.pluginName }}</router-link
            >
            <template v-else>{{ duplicate.pluginName }}</template>
          </span>
          <span class="drop-sub-inline">Version {{ duplicate.matchedVersionLabel }}</span>
        </div>
        <button type="button" class="clear-btn" aria-label="Clear file" @click.stop="clear">
          <v-icon size="18">mdi-close</v-icon>
        </button>
      </template>
      <template v-else-if="!file">
        <v-icon size="28" class="drop-icon">mdi-tray-arrow-up</v-icon>
        <span class="drop-label">{{ label }}</span>
        <span class="drop-sub">Drag and drop, or click to browse</span>
      </template>
      <template v-else>
        <v-icon size="24" class="drop-icon">mdi-file-code</v-icon>
        <div class="drop-label-group">
          <span class="drop-label">{{ file.name }}</span>
          <span v-if="hashing" class="drop-sub-inline">
            <AppLoading size="sm" label="Hashing" />
          </span>
          <span v-else-if="checking" class="drop-sub-inline">
            <AppLoading size="sm" label="Checking for duplicates" />
          </span>
          <span v-else-if="hashValue" class="drop-sub-inline drop-sub-inline--hash mono"
            >SHA-256: {{ hashValue }}</span
          >
        </div>
        <button type="button" class="clear-btn" aria-label="Clear file" @click.stop="clear">
          <v-icon size="18">mdi-close</v-icon>
        </button>
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import api from '@/services/api'
import { hashFile } from '@/services/hashFile'
import AppLoading from '@/components/AppLoading.vue'

const props = defineProps({
  modelValue: { type: [File, null], default: null },
  hash: { type: [String, null], default: null },
  duplicate: { type: [Object, null], default: null },
  label: { type: String, default: 'Upload .nro' },
  accept: { type: String, default: '.nro' },
  checkDuplicate: { type: Boolean, default: true },
  large: { type: Boolean, default: false },
})

const emit = defineEmits(['update:modelValue', 'update:hash', 'update:duplicate'])

const inputEl = ref(null)
const dragging = ref(false)
const hashing = ref(false)
const checking = ref(false)
const file = ref(props.modelValue)
const hashValue = ref(props.hash)
const duplicate = ref(props.duplicate)

function openPicker() {
  inputEl.value?.click()
}

// Resolves duplicate status BEFORE telling the parent the hash is ready,
// so a user gating its own fields on "hash present" never briefly sees a duplicate's fields flash in.
async function resolveDuplicate(h) {
  checking.value = true
  let result
  try {
    const res = await api.get('/plugins/identify', { params: { hash: h } })
    result = res.data
  } catch {
    result = null
  } finally {
    checking.value = false
  }

  if (hashValue.value !== h) return // a newer file was chosen while this was in flight
  duplicate.value = result
  emit('update:hash', h)
  emit('update:duplicate', result)
}

async function setFile(chosen) {
  file.value = chosen ?? null
  emit('update:modelValue', file.value)
  hashValue.value = null
  emit('update:hash', null)
  duplicate.value = null
  emit('update:duplicate', null)

  if (!file.value) return

  hashing.value = true
  let h
  try {
    h = await hashFile(file.value)
    hashValue.value = h
  } finally {
    hashing.value = false
  }

  if (props.checkDuplicate) {
    await resolveDuplicate(h)
  } else {
    emit('update:hash', h)
  }
}

function onDrop(e) {
  dragging.value = false
  const dropped = e.dataTransfer?.files?.[0]
  if (dropped) setFile(dropped)
}

function onPick(e) {
  const chosen = e.target.files?.[0]
  if (chosen) setFile(chosen)
}

function clear() {
  if (inputEl.value) inputEl.value.value = ''
  setFile(null)
}

// A parent resetting its form after a successful submit sets modelValue back to null directly (not via clear()/setFile()),
// so this has to mirror that reset onto our own internal display state too.
// Otherwise the old hash/duplicate card stays stuck on screen with nothing shown.
watch(
  () => props.modelValue,
  (v) => {
    if (v === file.value) return
    file.value = v
    if (v === null) {
      hashValue.value = null
      duplicate.value = null
      if (inputEl.value) inputEl.value.value = ''
    }
  }
)
</script>

<style scoped>
.drop-wrap {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.drop-zone {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 16px;
  border: 1px dashed var(--line-2);
  border-left: 4px solid var(--line-2);
  background: var(--panel);
  cursor: pointer;
  transition:
    border-color var(--dur-fast) var(--ease),
    background-color var(--dur-fast) var(--ease);
}

.drop-zone:hover,
.drop-zone--active {
  border-color: var(--white);
  background: var(--panel-2);
}

.drop-zone--has-file {
  border-style: solid;
  border-left-color: var(--ok);
}

.drop-zone--duplicate {
  border-style: solid;
  border-left-color: var(--warn);
}

.drop-zone--large {
  padding: 24px;
  min-height: 90px;
}

.hidden-input {
  display: none;
}

.drop-icon {
  flex-shrink: 0;
  color: var(--tx-3);
}

.drop-zone--has-file .drop-icon {
  color: var(--ok);
}

.drop-zone--duplicate .drop-icon {
  color: var(--warn);
}

.drop-label {
  font-size: 14px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.drop-label a {
  color: var(--white);
  text-decoration: underline;
}

.drop-label-group {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
  flex: 1;
}

.drop-label-group .drop-label {
  font-weight: 600;
  white-space: normal;
}

.drop-sub-inline {
  font-size: 12px;
  color: var(--tx-2);
  display: flex;
  align-items: center;
  gap: 6px;
}

.drop-sub-inline--hash {
  word-break: break-all;
  white-space: normal;
}

.drop-sub {
  font-size: 12px;
  color: var(--tx-3);
  margin-left: auto;
  flex-shrink: 0;
}

.clear-btn {
  margin-left: auto;
  align-self: flex-start;
  display: flex;
  padding: 2px;
  border: 0;
  background: none;
  color: var(--tx-2);
  cursor: pointer;
}

.clear-btn:hover {
  color: var(--white);
}
</style>

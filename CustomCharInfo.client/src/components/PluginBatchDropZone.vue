<template>
  <div class="drop-wrap">
    <div
      class="drop-zone"
      :class="{
        'drop-zone--active': dragging,
        'drop-zone--has-files': entries.length > 0,
        'drop-zone--large': large,
      }"
      @dragover.prevent="dragging = true"
      @dragleave.prevent="dragging = false"
      @drop.prevent="onDrop"
      @click="openFilePicker"
    >
      <input
        ref="fileInput"
        type="file"
        :accept="extension"
        multiple
        class="hidden-input"
        @change="onPickFiles"
      />
      <input
        ref="folderInput"
        type="file"
        webkitdirectory
        class="hidden-input"
        @change="onPickFiles"
      />

      <template v-if="entries.length === 0">
        <v-icon size="28" class="drop-icon">mdi-tray-arrow-up</v-icon>
        <span class="drop-label">{{ label }}</span>
        <span class="drop-sub">
          Drag and drop files or a folder, or
          <button type="button" class="link-btn" @click.stop="openFilePicker">browse files</button>
          or
          <button type="button" class="link-btn" @click.stop="openFolderPicker">
            browse a folder
          </button>
        </span>
      </template>
      <template v-else>
        <v-icon size="24" class="drop-icon">mdi-file-code</v-icon>
        <div class="drop-label-group">
          <span class="drop-label">{{ selectionLabel }}</span>
          <span v-if="skipped > 0" class="drop-sub-inline">
            {{ skipped }} non-{{ extension }} file{{ skipped === 1 ? '' : 's' }} ignored
          </span>
        </div>
        <button type="button" class="clear-btn" aria-label="Clear files" @click.stop="clear">
          <v-icon size="18">mdi-close</v-icon>
        </button>
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { collectFilesFromDataTransfer, collectFilesFromList } from '@/services/fileEntries'

const props = defineProps({
  // Array of { file, path } from services/fileEntries
  modelValue: { type: Array, default: () => [] },
  label: { type: String, default: 'Upload .nro files or a folder' },
  extension: { type: String, default: '.nro' },
  large: { type: Boolean, default: false },
})

const emit = defineEmits(['update:modelValue'])

const fileInput = ref(null)
const folderInput = ref(null)
const dragging = ref(false)
const entries = ref(props.modelValue)
const skipped = ref(0)

const selectionLabel = computed(() => {
  if (entries.value.length === 1) return entries.value[0].path
  return `${entries.value.length} files selected`
})

function openFilePicker() {
  fileInput.value?.click()
}

function openFolderPicker() {
  folderInput.value?.click()
}

function setEntries(collected) {
  entries.value = collected.files
  skipped.value = collected.skipped
  emit('update:modelValue', entries.value)
}

async function onDrop(e) {
  dragging.value = false
  setEntries(await collectFilesFromDataTransfer(e.dataTransfer, { extension: props.extension }))
}

function onPickFiles(e) {
  setEntries(collectFilesFromList(e.target.files, { extension: props.extension }))
  // Reset so picking the same selection again still fires change.
  e.target.value = ''
}

function clear() {
  setEntries({ files: [], skipped: 0 })
}

watch(
  () => props.modelValue,
  (v) => {
    if (v !== entries.value) {
      entries.value = v ?? []
      if (entries.value.length === 0) skipped.value = 0
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

.drop-zone--has-files {
  border-style: solid;
  border-left-color: var(--ok);
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

.drop-zone--has-files .drop-icon {
  color: var(--ok);
}

.drop-label {
  font-size: 14px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
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
}

.drop-sub {
  font-size: 12px;
  color: var(--tx-3);
  margin-left: auto;
  flex-shrink: 0;
}

.link-btn {
  background: none;
  border: none;
  padding: 0;
  font: inherit;
  color: var(--white);
  text-decoration: underline;
  cursor: pointer;
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

@media (max-width: 599px) {
  .drop-zone {
    flex-wrap: wrap;
  }

  .drop-sub {
    margin-left: 0;
    flex-basis: 100%;
  }
}
</style>

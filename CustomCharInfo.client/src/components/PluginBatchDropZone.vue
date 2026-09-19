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
          Drag &amp; drop files or a folder, or
          <button type="button" class="link-btn" @click.stop="openFilePicker">browse files</button>
          /
          <button type="button" class="link-btn" @click.stop="openFolderPicker">
            browse folder
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
        <v-icon size="18" class="clear-icon" @click.stop="clear">mdi-close</v-icon>
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
  gap: 0.35rem;
}

.drop-zone {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.7rem 1rem;
  border: 2px dashed #444;
  border-radius: 8px;
  cursor: pointer;
  background-color: #1a1a1a;
  transition:
    border-color 0.15s,
    background-color 0.15s;
}

.drop-zone:hover {
  border-color: #666;
}
.drop-zone--active {
  border-color: #64b5f6;
  background-color: #16232b;
}
.drop-zone--has-files {
  border-style: solid;
  border-color: #388e3c;
  background-color: #142614;
}
.drop-zone--large {
  padding: 1.5rem 1.5rem;
  min-height: 90px;
}

.hidden-input {
  display: none;
}

.drop-icon {
  flex-shrink: 0;
  color: #888;
}
.drop-zone--has-files .drop-icon {
  color: #81c784;
}

.drop-label {
  font-size: 0.92em;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.drop-label-group {
  display: flex;
  flex-direction: column;
  min-width: 0;
  flex: 1;
}
.drop-label-group .drop-label {
  font-weight: bold;
  white-space: normal;
  color: #c8e6c9;
}

.drop-sub-inline {
  font-size: 0.8em;
  margin-top: 0.1rem;
  color: #a5d6a7;
}

.drop-sub {
  font-size: 0.8em;
  color: #777;
  margin-left: auto;
  flex-shrink: 0;
}

.link-btn {
  background: none;
  border: none;
  padding: 0;
  font: inherit;
  color: #90caf9;
  cursor: pointer;
}
.link-btn:hover {
  text-decoration: underline;
}

.clear-icon {
  margin-left: auto;
  color: #999;
  flex-shrink: 0;
  align-self: flex-start;
}
.clear-icon:hover {
  color: #fff;
}
</style>

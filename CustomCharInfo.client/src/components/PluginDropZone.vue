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
        <v-icon size="24" class="drop-icon drop-icon--duplicate">mdi-alert-circle</v-icon>
        <div class="drop-label-group">
          <span class="drop-label">
            Already uploaded! This plugin is
            <router-link
              v-if="duplicate.attachmentType === 'Moveset'"
              :to="{ name: 'MovesetDetail', params: { movesetId: duplicate.movesetId } }"
              class="unvisitable"
              @click.stop
              >{{ duplicate.pluginName }}</router-link
            >
            <template v-else>{{ duplicate.pluginName }}</template>
          </span>
          <span class="drop-sub-inline">Version {{ duplicate.matchedVersionLabel }}</span>
        </div>
        <v-icon size="18" class="clear-icon" @click.stop="clear">mdi-close</v-icon>
      </template>
      <template v-else-if="!file">
        <v-icon size="28" class="drop-icon">mdi-tray-arrow-up</v-icon>
        <span class="drop-label">{{ label }}</span>
        <span class="drop-sub">Drag & drop, or click to browse</span>
      </template>
      <template v-else>
        <v-icon size="24" class="drop-icon">mdi-file-code</v-icon>
        <div class="drop-label-group">
          <span class="drop-label">{{ file.name }}</span>
          <span v-if="hashing" class="drop-sub-inline">
            <v-progress-circular indeterminate size="10" width="2" /> Hashing…
          </span>
          <span v-else-if="checking" class="drop-sub-inline">
            <v-progress-circular indeterminate size="10" width="2" /> Checking for duplicates…
          </span>
          <span v-else-if="hashValue" class="drop-sub-inline drop-sub-inline--hash"
            >SHA-256: {{ hashValue }}</span
          >
        </div>
        <v-icon size="18" class="clear-icon" @click.stop="clear">mdi-close</v-icon>
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import api from '@/services/api'
import { hashFile } from '@/services/hashFile'

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
.drop-zone--has-file {
  border-style: solid;
  border-color: #388e3c;
  background-color: #142614;
}
.drop-zone--duplicate {
  border-style: solid;
  border-color: #f9a825;
  background-color: #2e2400;
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
.drop-zone--has-file .drop-icon {
  color: #81c784;
}
.drop-icon--duplicate {
  color: #ffb300;
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
}
.drop-zone--duplicate .drop-label-group .drop-label {
  color: #ffe082;
}
.drop-zone--has-file .drop-label-group .drop-label {
  color: #c8e6c9;
}

.drop-sub-inline {
  font-size: 0.8em;
  margin-top: 0.1rem;
  display: flex;
  align-items: center;
  gap: 0.35rem;
}
.drop-zone--duplicate .drop-sub-inline {
  color: #cbb27a;
}
.drop-zone--has-file .drop-sub-inline {
  color: #a5d6a7;
}
.drop-sub-inline--hash {
  color: #90caf9;
  word-break: break-all;
  white-space: normal;
}

.drop-sub {
  font-size: 0.8em;
  color: #777;
  margin-left: auto;
  flex-shrink: 0;
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

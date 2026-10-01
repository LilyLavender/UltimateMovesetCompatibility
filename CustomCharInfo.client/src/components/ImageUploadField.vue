<template>
  <div class="image-upload-field">
    <div
      class="dropzone"
      :class="{ 'dropzone--dragging': isDragging, 'dropzone--error': !!error }"
      @click="triggerFilePicker"
      @dragover.prevent="isDragging = true"
      @dragleave.prevent="isDragging = false"
      @drop.prevent="handleDrop"
    >
      <input
        ref="fileInput"
        type="file"
        accept="image/*"
        class="hidden-file-input"
        @change="handleFileChange"
      />
      <img v-if="previewSrc" :src="previewSrc" class="preview-img" :style="previewStyle" alt="" />
      <span v-else class="empty-hint">
        <v-icon size="26">mdi-tray-arrow-up</v-icon>
        Click or drop an image here
      </span>

      <div class="dropzone-hint">
        {{ previewSrc ? 'Click or drop to replace' : 'Click or drop an image to upload' }}
      </div>
      <StatusTag v-if="isStagedFile" variant="warn" class="staged-tag"> Uploads on save </StatusTag>
    </div>

    <v-text-field
      density="compact"
      :model-value="urlInputValue"
      placeholder="Or paste an image link"
      hide-details
      clearable
      class="url-input"
      @update:model-value="handleUrlInput"
      @click="$event.stopPropagation()"
    />

    <p v-if="error" class="upload-error">{{ error }}</p>
    <p v-if="hint" class="upload-hint">{{ hint }}</p>
  </div>
</template>

<script setup>
import { ref, computed, onBeforeUnmount } from 'vue'
import StatusTag from '@/components/StatusTag.vue'

const props = defineProps({
  modelValue: { type: [String, File], default: '' },
  requiredWidth: { type: Number, default: null },
  requiredHeight: { type: Number, default: null },
  hint: { type: String, default: '' },
  previewMaxHeight: { type: [String, Number], default: 200 },
})

const emit = defineEmits(['update:modelValue'])

const apiUrl = import.meta.env.VITE_API_URL

const fileInput = ref(null)
const isDragging = ref(false)
const error = ref('')
let objectUrl = null

const isStagedFile = computed(() => props.modelValue instanceof File)

const resolveUrl = (path) => {
  if (!path) return null
  return path.startsWith('/') ? `${apiUrl}${path}` : path
}

const previewSrc = computed(() => {
  if (isStagedFile.value) return objectUrl
  if (typeof props.modelValue === 'string' && props.modelValue) {
    return resolveUrl(props.modelValue)
  }
  return null
})

const urlInputValue = computed(() => (typeof props.modelValue === 'string' ? props.modelValue : ''))

const previewStyle = computed(() => ({
  maxHeight:
    typeof props.previewMaxHeight === 'number'
      ? `${props.previewMaxHeight}px`
      : props.previewMaxHeight,
}))

const revokeObjectUrl = () => {
  if (objectUrl) {
    URL.revokeObjectURL(objectUrl)
    objectUrl = null
  }
}

onBeforeUnmount(revokeObjectUrl)

const triggerFilePicker = () => {
  fileInput.value?.click()
}

const checkDimensions = (file) => {
  return new Promise((resolve, reject) => {
    if (!props.requiredWidth || !props.requiredHeight) {
      resolve()
      return
    }
    const img = new Image()
    const url = URL.createObjectURL(file)
    img.onload = () => {
      URL.revokeObjectURL(url)
      if (img.naturalWidth !== props.requiredWidth || img.naturalHeight !== props.requiredHeight) {
        reject(
          `Image must be exactly ${props.requiredWidth}x${props.requiredHeight}px (got ${img.naturalWidth}x${img.naturalHeight}px).`
        )
      } else {
        resolve()
      }
    }
    img.onerror = () => {
      URL.revokeObjectURL(url)
      reject('Could not read that file as an image.')
    }
    img.src = url
  })
}

const processFile = async (file) => {
  if (!file) return
  if (!file.type.startsWith('image/')) {
    error.value = 'Please select an image file.'
    return
  }

  try {
    await checkDimensions(file)
  } catch (err) {
    error.value = err
    return
  }

  error.value = ''
  revokeObjectUrl()
  objectUrl = URL.createObjectURL(file)
  emit('update:modelValue', file)
}

const handleFileChange = (e) => {
  const file = e.target.files?.[0]
  processFile(file)
  e.target.value = ''
}

const handleDrop = (e) => {
  isDragging.value = false
  const file = e.dataTransfer?.files?.[0]
  processFile(file)
}

const handleUrlInput = (val) => {
  error.value = ''
  revokeObjectUrl()
  emit('update:modelValue', val || '')
}
</script>

<style scoped>
.image-upload-field {
  display: flex;
  flex-direction: column;
  gap: 8px;
  width: 100%;
}

.dropzone {
  position: relative;
  min-height: 120px;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  border: 1px dashed var(--line-2);
  background: var(--panel-2);
  cursor: pointer;
  transition:
    border-color var(--dur-fast) var(--ease),
    background-color var(--dur-fast) var(--ease);
}

.dropzone:hover,
.dropzone--dragging {
  border-color: var(--white);
}

.dropzone--error {
  border-color: var(--err);
}

.preview-img {
  max-width: 100%;
  object-fit: contain;
  display: block;
}

.empty-hint {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  padding: 24px 16px;
  color: var(--tx-3);
  font-size: 13px;
  text-align: center;
}

.dropzone-hint {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  padding: 4px 0;
  background: rgba(0, 0, 0, 0.7);
  color: var(--tx);
  font-size: 12px;
  text-align: center;
  opacity: 0;
  transition: opacity var(--dur-fast) var(--ease);
  pointer-events: none;
}

.dropzone:hover .dropzone-hint {
  opacity: 1;
}

.staged-tag {
  position: absolute;
  top: 6px;
  left: 6px;
}

.hidden-file-input {
  display: none;
}

.upload-error,
.upload-hint {
  margin: 0;
  font-size: 12px;
}

.upload-error {
  color: var(--err);
}

.upload-hint {
  color: var(--tx-3);
}
</style>

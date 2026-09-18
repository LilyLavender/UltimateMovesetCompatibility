<template>
  <div class="image-upload-field">
    <div
      class="dropzone"
      :class="{ dragging: isDragging, 'has-error': !!error }"
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
      <img v-if="previewSrc" :src="previewSrc" class="preview-img" :style="previewStyle" />
      <span v-else class="empty-hint">Click or drop an image here</span>

      <div class="dropzone-hint">
        {{ previewSrc ? 'Click or drop to replace' : 'Click or drop an image to upload' }}
      </div>
      <div v-if="isStagedFile" class="not-uploaded-badge">
        Not uploaded yet — will upload on save
      </div>
    </div>

    <v-text-field
      variant="outlined"
      density="compact"
      :model-value="urlInputValue"
      label="Or paste an image link"
      placeholder="https://example.com/image.png"
      hide-details
      clearable
      class="mt-2 url-input"
      @update:model-value="handleUrlInput"
      @click="$event.stopPropagation()"
    />

    <div v-if="error" class="upload-error">{{ error }}</div>
    <div v-if="hint" class="upload-hint">{{ hint }}</div>
  </div>
</template>

<script setup>
import { ref, computed, onBeforeUnmount } from 'vue'

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
  width: 100%;
}
.dropzone {
  position: relative;
  border: 2px dashed #555;
  border-radius: 6px;
  min-height: 120px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  overflow: hidden;
  background: #141414;
  transition: border-color 150ms ease-in-out;
}
.dropzone:hover {
  border-color: #888;
}
.dropzone.dragging {
  border-color: #8ab4f8;
}
.dropzone.has-error {
  border-color: #cf6679;
}
.preview-img {
  max-width: 100%;
  object-fit: contain;
  display: block;
}
.empty-hint {
  color: #777;
  font-size: 0.85rem;
  padding: 2em 1em;
  text-align: center;
}
.dropzone-hint {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  text-align: center;
  font-size: 0.72rem;
  color: #eee;
  background: rgba(0, 0, 0, 0.6);
  padding: 3px 0;
  opacity: 0;
  transition: opacity 150ms ease-in-out;
  pointer-events: none;
}
.dropzone:hover .dropzone-hint {
  opacity: 1;
}
.not-uploaded-badge {
  position: absolute;
  top: 6px;
  left: 6px;
  background: #b45c1c;
  color: #fff;
  font-size: 0.68rem;
  font-weight: 600;
  padding: 3px 7px;
  border-radius: 4px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.4);
}
.hidden-file-input {
  display: none;
}
.url-input :deep(.v-field) {
  font-size: 0.9rem;
}
.upload-error {
  color: #cf6679;
  font-size: 0.78rem;
  margin-top: 4px;
}
.upload-hint {
  color: #9b9b9b;
  font-size: 0.78rem;
  margin-top: 4px;
}
</style>

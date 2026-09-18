<template>
  <div v-if="(isEditMode && series) || (!isEditMode && form)">
    <v-container max-width="1020px">
      <!-- Header -->
      <h1 v-if="isEditMode">Edit {{ series.seriesName }}</h1>
      <h1 v-else>Add Series</h1>

      <!-- Basic Info -->
      <section>
        <!-- Add mode intro para -->
        <p v-if="!isEditMode" class="mb-3">
          Please do not upload series that are meant to be private. Instead, set the series of your
          private moveset to &quot;Super Smash Bros.&quot;
        </p>
        <p class="mb-3">
          For information on image hosting in UMC, see
          <router-link to="/image-hosting" class="unvisitable" target="_blank">here</router-link>.
        </p>

        <v-row>
          <!-- Series Name -->
          <v-col cols="12" sm="4">
            <v-text-field
              v-model="form.seriesName"
              variant="outlined"
              label="Series Name"
              :error="!!nameError"
              :error-messages="nameError"
            />
          </v-col>

          <!-- Image URL -->
          <v-col cols="12" sm="8">
            <p class="field-label">Series Icon (800x800)</p>
            <ImageUploadField
              v-model="form.seriesIconUrl"
              :required-width="IMAGE_UPLOAD_SPECS.series_icon.width"
              :required-height="IMAGE_UPLOAD_SPECS.series_icon.height"
              hint="Series icons MUST be #333333 on a 800x800 canvas with 100px of padding on each side. See other series icons for reference."
              :preview-max-height="180"
            />
          </v-col>
        </v-row>
      </section>

      <!-- Notes + Submit -->
      <div class="d-flex align-start ga-3 justify-end">
        <v-textarea
          v-model="form.notes"
          variant="outlined"
          density="compact"
          :label="isEditMode ? 'Editing notes' : 'Submission notes'"
          placeholder="Optional, shown to admins only."
          rows="1"
          auto-grow
          hide-details
          class="notes-field"
        />
        <v-btn
          class="btn submit-button mt-1"
          :loading="isSubmitting"
          :disabled="isSubmitting"
          @click="submit"
        >
          {{ uploadStatus || (isEditMode ? 'Save' : 'Add Series') }}
        </v-btn>
      </div>
    </v-container>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import api from '@/services/api'
import ImageUploadField from '@/components/ImageUploadField.vue'
import { IMAGE_UPLOAD_SPECS } from '@/globals'
import { useImageUpload, isStagedFile } from '@/composables/useImageUpload'

const props = defineProps({
  mode: { type: String, default: 'add' },
  seriesId: { type: Number, default: null },
})

const isEditMode = computed(() => props.mode === 'edit')
const router = useRouter()

const series = ref(null)
const form = ref({
  seriesName: '',
  seriesIconUrl: '',
  notes: '',
})

const nameError = ref('')
const isSubmitting = ref(false)
const uploadStatus = ref('')
const allSeries = ref([])
const originalName = ref('')

onMounted(async () => {
  try {
    const res = await api.get('/series')
    allSeries.value = res.data
  } catch (err) {
    console.error('Failed to load series list:', err)
  }

  // Only load series if editing
  if (props.mode === 'edit' && props.seriesId) {
    try {
      const res = await api.get(`/series/${props.seriesId}`)
      series.value = res.data
      Object.assign(form.value, res.data)
      originalName.value = res.data.seriesName
      document.title = `UMC | Editing ${series.value?.seriesName}` // sets page title
    } catch (err) {
      console.error(err)
      router.replace({ name: 'ErrorPage', query: { http: 404, reason: 'Series not found' } })
    }
  }
})

const validateSeriesName = () => {
  nameError.value = ''

  const name = form.value.seriesName?.trim()
  if (!name) return

  if (isEditMode.value && name === originalName.value) return

  const conflict = allSeries.value.find((s) => s.seriesName.toLowerCase() === name.toLowerCase())

  if (conflict) {
    nameError.value = 'A series with this name already exists.'
  }
}

const { uploadIfNeeded } = useImageUpload()

const submit = async () => {
  // Validation
  validateSeriesName()
  if (nameError.value) {
    return
  }

  isSubmitting.value = true

  if (props.mode === 'edit' && props.seriesId) {
    // Edit mode: the series already exists, so upload first (as before) and save in one request.
    uploadStatus.value = 'Uploading image...'
    try {
      form.value.seriesIconUrl = await uploadIfNeeded(form.value.seriesIconUrl, {
        type: 'series_icon',
        itemName: form.value.seriesName,
      })
    } catch (err) {
      console.error('Image upload failed:', JSON.stringify(err.response?.data) || err.message)
      alert(
        'Failed to upload image. Please check the file and try again.\n\n' +
          (JSON.stringify(err.response?.data) || err.message)
      )
      isSubmitting.value = false
      uploadStatus.value = ''
      return
    }

    uploadStatus.value = 'Saving series...'
    try {
      await api.put(`/series/${props.seriesId}`, { ...form.value })
      router.push('/series')
    } catch (err) {
      if (err.response?.status === 409) {
        alert('A series with this name already exists.')
        return
      }
      console.error('Submit failed:', JSON.stringify(err.response?.data) || err.message)
      alert(
        'Failed to save series. Please check the form and try again.\n\n' +
          (JSON.stringify(err.response?.data) || err.message)
      )
    } finally {
      isSubmitting.value = false
      uploadStatus.value = ''
    }
    return
  }

  // Create mode: no series exists yet to attach an image to, so an upload failure or a save
  // failure after upload would otherwise orphan the image in R2 with nothing referencing it.
  // Create the series first (without a staged file), then upload and attach the image after.
  const stagedIcon = isStagedFile(form.value.seriesIconUrl) ? form.value.seriesIconUrl : null
  const payload = { ...form.value }
  if (stagedIcon) payload.seriesIconUrl = null

  uploadStatus.value = 'Saving series...'

  let newId
  try {
    const res = await api.post('/series', payload)
    newId = res.data.seriesId
  } catch (err) {
    if (err.response?.status === 409) {
      alert('A series with this name already exists.')
    } else {
      console.error('Submit failed:', JSON.stringify(err.response?.data) || err.message)
      alert(
        'Failed to save series. Please check the form and try again.\n\n' +
          (JSON.stringify(err.response?.data) || err.message)
      )
    }
    isSubmitting.value = false
    uploadStatus.value = ''
    return
  }

  if (stagedIcon) {
    uploadStatus.value = 'Uploading image...'
    try {
      const seriesIconUrl = await uploadIfNeeded(stagedIcon, {
        type: 'series_icon',
        itemName: form.value.seriesName,
      })
      await api.patch(`/series/${newId}/image`, { seriesIconUrl })
    } catch (err) {
      console.error(
        'Image upload failed after series creation:',
        JSON.stringify(err.response?.data) || err.message
      )
      alert(
        'Series created, but the image failed to upload. You can add it later by editing the series.\n\n' +
          (JSON.stringify(err.response?.data) || err.message)
      )
    }
  }

  isSubmitting.value = false
  uploadStatus.value = ''
  router.push('/series')
}

watch(() => form.value.seriesName, validateSeriesName)
</script>

<style scoped>
/* General display of form */
section {
  margin-bottom: 2rem;
  background-color: #1e1e1e;
  padding: 1em;
  border-radius: 10px;
}
h1 {
  font-size: 3.25em;
}
section h2 {
  font-size: 2.25em;
  margin-bottom: 10px;
}
.submit-button {
  background-color: #2e2e2e;
  color: #e2e2e2;
  text-transform: unset;
}
.field-label {
  font-size: 0.85rem;
  color: #b0b0b0;
  margin-bottom: 4px;
}
.notes-field {
  max-width: 400px;
}
.notes-field :deep(.v-field__input) {
  font-size: 0.85rem;
  padding-top: 6px;
  padding-bottom: 6px;
}
.notes-field :deep(.v-label) {
  font-style: italic;
  color: #6e6e6e !important;
}
</style>

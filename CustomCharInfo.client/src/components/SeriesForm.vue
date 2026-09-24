<template>
  <PageShell
    v-if="(isEditMode && series) || (!isEditMode && form)"
    :title="isEditMode ? `Edit ${series.seriesName}` : 'Submit a series'"
    :head="false"
  >
    <FormLayout>
      <FormSection id="series" title="Series">
        <p v-if="!isEditMode" class="section-note">
          Please do not upload series that are meant to be private. Instead, set the series of your
          private moveset to &quot;Super Smash Bros.&quot;
        </p>
        <p class="section-note">
          For information on image hosting in UMC, see
          <router-link to="/image-hosting" target="_blank">image hosting</router-link>.
        </p>

        <div class="form-grid">
          <LabeledField label="Series name" required :error="nameError">
            <v-text-field v-model="form.seriesName" :error="!!nameError" />
          </LabeledField>

          <LabeledField
            label="Series icon"
            note="800 by 800"
            hint="Series icons must be #333333 on an 800 by 800 canvas with 100px of padding on each side. See other series icons for reference."
            class="span-2"
          >
            <ImageUploadField
              v-model="form.seriesIconUrl"
              :required-width="IMAGE_UPLOAD_SPECS.series_icon.width"
              :required-height="IMAGE_UPLOAD_SPECS.series_icon.height"
              :preview-max-height="180"
            />
          </LabeledField>
        </div>
      </FormSection>

      <template #savebar>
        <LabeledField
          :label="isEditMode ? 'Editing notes' : 'Submission notes'"
          note="admins only"
          class="savebar-notes"
        >
          <v-textarea v-model="form.notes" density="compact" rows="1" auto-grow hide-details />
        </LabeledField>
        <span class="savebar-spacer"></span>
        <AppButton variant="primary" icon="mdi-check" :busy="isSubmitting" @click="submit">
          {{ uploadStatus || (isEditMode ? 'Save changes' : 'Submit series') }}
        </AppButton>
      </template>
    </FormLayout>
  </PageShell>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useHead } from '@unhead/vue'
import api from '@/services/api'
import PageShell from '@/components/PageShell.vue'
import FormLayout from '@/components/FormLayout.vue'
import FormSection from '@/components/FormSection.vue'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'
import ImageUploadField from '@/components/ImageUploadField.vue'
import { IMAGE_UPLOAD_SPECS } from '@/globals'
import { useImageUpload, isStagedFile } from '@/composables/useImageUpload'
import { useNotify } from '@/composables/useNotify'

const notify = useNotify()

const props = defineProps({
  mode: { type: String, default: 'add' },
  seriesId: { type: Number, default: null },
})

const isEditMode = computed(() => props.mode === 'edit')
const router = useRouter()

const series = ref(null)

useHead(
  computed(() => ({
    title: series.value ? `UMC | Editing ${series.value.seriesName}` : 'UMC | Submit a series',
  }))
)

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
    uploadStatus.value = 'Uploading image'
    try {
      form.value.seriesIconUrl = await uploadIfNeeded(form.value.seriesIconUrl, {
        type: 'series_icon',
        itemName: form.value.seriesName,
      })
    } catch (err) {
      console.error('Image upload failed:', JSON.stringify(err.response?.data) || err.message)
      notify.error('Failed to upload image. Please check the file and try again.', err)
      isSubmitting.value = false
      uploadStatus.value = ''
      return
    }

    uploadStatus.value = 'Saving series'
    try {
      await api.put(`/series/${props.seriesId}`, { ...form.value })
      router.push('/series')
    } catch (err) {
      if (err.response?.status === 409) {
        notify.warning('A series with this name already exists.')
        return
      }
      console.error('Submit failed:', JSON.stringify(err.response?.data) || err.message)
      notify.error('Failed to save series. Please check the form and try again.', err)
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

  uploadStatus.value = 'Saving series'

  let newId
  try {
    const res = await api.post('/series', payload)
    newId = res.data.seriesId
  } catch (err) {
    if (err.response?.status === 409) {
      notify.warning('A series with this name already exists.')
    } else {
      console.error('Submit failed:', JSON.stringify(err.response?.data) || err.message)
      notify.error('Failed to save series. Please check the form and try again.', err)
    }
    isSubmitting.value = false
    uploadStatus.value = ''
    return
  }

  if (stagedIcon) {
    uploadStatus.value = 'Uploading image'
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
      notify.error(
        'Series created, but the image failed to upload. You can add it later by editing the series.',
        err
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
.section-note {
  margin: 0 0 4px;
  color: var(--tx-2);
  font-size: 14px;
}

.section-note a {
  color: var(--white);
  text-decoration: underline;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
  align-items: start;
  margin-top: 10px;
}

.span-2 {
  grid-column: span 2;
}

.savebar-notes {
  flex: 1 1 320px;
  max-width: 480px;
}

.savebar-spacer {
  flex: 1;
}

@media (max-width: 959px) {
  .form-grid {
    grid-template-columns: 1fr;
  }

  .span-2 {
    grid-column: span 1;
  }
}
</style>

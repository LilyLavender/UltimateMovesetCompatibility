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
          Please do not upload series that are meant to be private. Instead, set the series of your private moveset to &quot;Super Smash Bros.&quot;
        </p>
        <p class="mb-3">
          For information on image hosting in UMC, see <router-link 
            to="/image-hosting"
            class="unvisitable"
            target="_blank"
            >here</router-link>.
        </p>

        <v-row>
          <!-- Series Name -->
          <v-col cols="12" sm="4">
            <v-text-field
              variant="outlined"
              v-model="form.seriesName"
              label="Series Name"
              :error="!!nameError"
              :error-messages="nameError"
            />
          </v-col>

          <!-- Image URL -->
          <v-col cols="8" sm="6">
            <v-text-field
              variant="outlined"
              v-model="form.seriesIconUrl"
              label="Series Icon URL"
              placeholder="https://example.com/image.png"
              messages="Series icons MUST be #333333 on a 800x800 canvas with 100px of padding on each side. See other series icons for reference."
            />
          </v-col>
          <v-col cols="4" sm="2">
            <v-img
              :src="getFullImageUrl(form.seriesIconUrl) || seriesIconUnknown"
              width="200"
            />
          </v-col>
        </v-row>
      </section>

      <!-- Submit -->
      <div class="d-flex justify-end">
        <v-btn @click="submit" class="btn submit-button">
          {{ isEditMode ? 'Save' : 'Add Series' }}
        </v-btn>
      </div>
    </v-container>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '@/services/api'
import seriesIconUnknown from "@/assets/series_icon_unknown.png"

const props = defineProps({
  mode: { type: String },
  seriesId: { type: Number }
})

const emit = defineEmits(['submitted'])

const isEditMode = computed(() => props.mode === 'edit')
const route = useRoute()
const router = useRouter()

const apiUrl = import.meta.env.VITE_API_URL

const getFullImageUrl = (path) => {
  if (!path) return null
  return path.startsWith('/') ? `${apiUrl}${path}` : path
}

const series = ref(null)
const form = ref({
  seriesName: '',
  seriesIconUrl: '',
})

const showSeparateIds = ref(false)
const nameError = ref('')
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
      document.title = `UMC | Editing ${series.value?.seriesName}`; // sets page title
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

  const conflict = allSeries.value.find(s =>
    s.seriesName.toLowerCase() === name.toLowerCase()
  )

  if (conflict) {
    nameError.value = 'A series with this name already exists.'
  }
}

const submit = async () => {
  // Validation
  validateSeriesName()
  if (nameError.value) {
    return
  }

  const payload = { ...form.value }

  // API
  try {
    if (props.mode == 'edit' && props.seriesId) {
      await api.put(`/series/${props.seriesId}`, payload)
    } else {
      await api.post('/series', payload)
    }

    router.push('/series')
  } catch (err) {
    console.error("Submit failed:", JSON.stringify(err.response?.data) || err.message)
    alert("Failed to save series. Please check the form and try again.\n\n" + (JSON.stringify(err.response?.data) || err.message))
  }
}

watch(
  () => form.value.seriesName,
  validateSeriesName
)
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
  background-color: #1e1e1e;
  color: #e2e2e2;
}
</style>
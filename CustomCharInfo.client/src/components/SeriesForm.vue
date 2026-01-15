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
          Series icons should be #333333 on a 800x800 canvas with 100px of padding on each side.
        </p>
        <!-- Edit mode intro para -->
        <p v-else class="mb-3">
          Series icons should be #333333 on a 800x800 canvas with 100px of padding on each side.
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

          <!-- Image Upload -->
          <v-col cols="8" sm="6">
            <v-file-input
              variant="outlined"
              label="Series Icon (800x800)"
              accept="image/*"
              @change="event => uploadImage(event, 'series_icon')"
              messages="WARNING: Images update automatically, unlike all other data."
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

const uploadImage = async (event, type) => {
  const file = event?.target?.files?.[0];
  const seriesName = form.value.seriesName;

  if (!file || !seriesName) {
    alert("Please select a valid image and ensure Series Name is filled.");
    return;
  }

  if (isEditMode && props.seriesId != null) {
    try {
      const userRes = await api.get('/auth/me');
      const user = userRes.data;

      const movesetsInSeries = (await api.get(`/movesets?seriesId=${props.seriesId}`)).data;

      if (movesetsInSeries.length > 0) {
        // Check if user owns any movesets from this series
        const allModderNames = movesetsInSeries.flatMap(m => m.modders ?? []);

        if (!allModderNames.includes(user.userName)) {
          alert("You do not own any movesets in this series, so you cannot upload an image for it.");
          return;
        }
      } else {
        // No movesets in series
        if (user.userTypeId !== 2 && user.userTypeId !== 3) {
          alert("Only admins or trusted users can add icons for new series.");
          return;
        }
      }
    } catch (err) {
      console.error("Validation failed:", err.response?.data || err.message);
      alert("Failed to validate series permissions.");
      return;
    }
  }

  const formData = new FormData();
  formData.append("file", file);
  formData.append("type", type);
  formData.append("itemName", seriesName);

  try {
    const res = await api.post("/upload/moveset-image", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });

    const timestamp = new Date().getTime();
    const imageUrlWithCacheBust = res.data.url + `?t=${timestamp}`;
    form.value.seriesIconUrl = imageUrlWithCacheBust;
  } catch (err) {
    console.error("Image upload failed:", err.response?.data || err.message);
    alert("Failed to upload image. Please ensure it meets size requirements.");
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
    console.error("Submit failed:", err.response?.data || err.message)
    alert("Failed to save series. Please check the form and try again.")
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
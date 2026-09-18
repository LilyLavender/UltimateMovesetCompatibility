<template>
  <v-container max-width="1300px">
    <h1 class="mb-5 page-title no-select">Banner Image Manager</h1>

    <p class="mb-4 helper-text">
      Manages the images shown in the scrolling banner on the home page.
    </p>

    <v-row class="mb-4" align="center">
      <v-col cols="12" sm="6">
        <v-btn color="primary" class="btn" :loading="uploading" @click="fileInput?.click()">
          <v-icon class="mr-1">mdi-upload</v-icon>
          Upload Image
        </v-btn>
        <input
          ref="fileInput"
          type="file"
          accept="image/png,image/jpeg,image/gif,image/webp"
          class="d-none"
          @change="onFileSelected"
        />
      </v-col>
    </v-row>

    <p v-if="loaded" class="mb-4 summary-text">
      {{ images.length }} {{ pluralize(images.length, 'image') }}.
    </p>

    <div class="tile-grid">
      <div v-for="item in images" :key="item.bannerImageId" class="tile">
        <div class="tile-img-box">
          <a :href="item.imageUrl" target="_blank" rel="noopener">
            <img :src="item.imageUrl" class="tile-img" loading="lazy" alt="" />
          </a>
        </div>
        <v-btn
          class="delete-btn"
          block
          :loading="deletingId === item.bannerImageId"
          @click="deleteImage(item)"
        >
          <v-icon class="mr-1">mdi-delete</v-icon>
          Delete
        </v-btn>
      </div>
    </div>
  </v-container>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'
import { useNotify } from '@/composables/useNotify'

const notify = useNotify()

const images = ref([])
const loaded = ref(false)
const uploading = ref(false)
const deletingId = ref(null)
const fileInput = ref(null)

const pluralize = (count, singular, plural = `${singular}s`) => (count === 1 ? singular : plural)

const loadImages = async () => {
  try {
    const res = await api.get('/banner-images')
    images.value = res.data
    loaded.value = true
  } catch (err) {
    console.error('Failed to load banner images:', err)
    notify.error('Failed to load banner images.')
  }
}

const onFileSelected = async (event) => {
  const file = event.target.files?.[0]
  event.target.value = ''
  if (!file) return

  const formData = new FormData()
  formData.append('File', file)

  uploading.value = true
  try {
    await api.post('/admin/banner-images', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    await loadImages()
  } catch (err) {
    console.error('Failed to upload banner image:', err)
    notify.error('Failed to upload banner image.', err)
  } finally {
    uploading.value = false
  }
}

const deleteImage = async (item) => {
  if (!confirm('Permanently delete this banner image? This cannot be undone.')) return

  deletingId.value = item.bannerImageId
  try {
    await api.delete(`/admin/banner-images/${item.bannerImageId}`)
    images.value = images.value.filter((i) => i.bannerImageId !== item.bannerImageId)
  } catch (err) {
    console.error('Failed to delete banner image:', err)
    notify.error('Failed to delete banner image.')
  } finally {
    deletingId.value = null
  }
}

onMounted(loadImages)
</script>

<style scoped>
.page-title {
  font-size: 2.5rem;
}
.helper-text {
  color: #b0b0b0;
}
.summary-text {
  color: #d0d0d0;
  font-size: 1.05em;
}
.btn {
  text-transform: unset;
  letter-spacing: 0.009375em;
  font-size: medium;
  background-color: #2e2e2e;
  color: #e2e2e2;
}
.delete-btn {
  text-transform: unset;
  background-color: #7a1f1f !important;
  color: #ffffff !important;
}

.tile-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 1em;
}

.tile {
  display: flex;
  flex-direction: column;
  width: 260px;
  background-color: #1e1e1e;
  border-radius: 8px;
  overflow: hidden;
}

.tile-img-box {
  position: relative;
  background-color: #111;
  height: 180px;
  overflow: hidden;
}
.tile-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}
</style>

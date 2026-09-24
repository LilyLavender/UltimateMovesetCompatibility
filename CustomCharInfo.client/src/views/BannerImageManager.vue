<template>
  <PageShell
    title="Banner images"
    :back-to="{ name: 'AdminPortal' }"
    back-label="Admin portal"
    lede="The images in the scrolling banner on the home page."
  >
    <div class="toolbar">
      <AppButton icon="mdi-upload" :busy="uploading" @click="fileInput?.click()">
        Upload an image
      </AppButton>
      <input
        ref="fileInput"
        type="file"
        accept="image/png,image/jpeg,image/gif,image/webp"
        class="hidden-input"
        @change="onFileSelected"
      />
      <p v-if="loaded" class="toolbar__count">
        {{ images.length }} {{ pluralize(images.length, 'image') }}
      </p>
    </div>

    <div v-if="!loaded" class="tile-grid" aria-busy="true">
      <Skeleton v-for="n in 6" :key="n" variant="image" width="260px" height="220px" />
    </div>
    <div v-else-if="images.length" class="tile-grid">
      <div v-for="item in images" :key="item.bannerImageId" class="tile">
        <a :href="item.imageUrl" target="_blank" rel="noopener" class="tile__img-box">
          <img :src="item.imageUrl" class="tile__img" loading="lazy" alt="" />
        </a>
        <AppButton
          variant="danger"
          size="sm"
          icon="mdi-delete"
          block
          :busy="deletingId === item.bannerImageId"
          @click="deleteImage(item)"
        >
          Delete
        </AppButton>
      </div>
    </div>
    <EmptyState v-else message="No banner images yet." icon="mdi-image-multiple" />
  </PageShell>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'
import { useNotify } from '@/composables/useNotify'
import PageShell from '@/components/PageShell.vue'
import AppButton from '@/components/AppButton.vue'
import Skeleton from '@/components/Skeleton.vue'
import EmptyState from '@/components/EmptyState.vue'

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
.toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.toolbar__count {
  margin: 0;
  color: var(--tx-2);
  font-size: 13px;
}

.hidden-input {
  display: none;
}

.tile-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.tile {
  display: flex;
  flex-direction: column;
  width: 260px;
  border: 1px solid var(--line);
  background: var(--panel);
}

.tile__img-box {
  display: block;
  height: 150px;
  background: var(--bg);
  overflow: hidden;
}

.tile__img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

@media (max-width: 599px) {
  .tile {
    width: 100%;
  }
}
</style>

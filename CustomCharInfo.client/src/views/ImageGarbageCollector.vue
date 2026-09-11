<template>
  <v-container max-width="1300px">
    <h1 class="mb-5 page-title no-select">Image Garbage Collector</h1>

    <p class="mb-4 helper-text">
      Lists every image uploaded to R2, showing which ones are still referenced by either a move set, series, blog post, or modder profile.
      Does not show images uploaded in the last 48 hours. 
    </p>

    <!-- Controls -->
    <v-row class="mb-4" align="center">
      <v-col cols="12" sm="6">
        <v-btn
          color="primary"
          class="btn"
          @click="scan"
          :loading="scanning"
        >
          <v-icon class="mr-1">mdi-magnify</v-icon>
          Scan Images
        </v-btn>
      </v-col>
      <v-col cols="12" sm="6">
        <div class="d-flex justify-sm-end">
          <v-btn
            class="btn delete-btn"
            @click="deleteAllUnused"
            :loading="deleting"
            :disabled="deletableCount === 0"
          >
            <v-icon class="mr-1">mdi-delete</v-icon>
            Delete Unused Images ({{ deletableCount }})
          </v-btn>
        </div>
      </v-col>
    </v-row>

    <p v-if="hasScanned" class="mb-4 summary-text" :class="{ 'summary-good': unusedCount === 0 }">
      <template v-if="unusedCount === 0">All images are used. You're good.</template>
      <template v-else>
        {{ images.length }} {{ pluralize(images.length, 'image') }} found, {{ unusedCount }} unused.
        <span v-if="deletableCount < unusedCount">
          ({{ unusedCount - deletableCount }} too recent to delete yet.)
        </span>
      </template>
    </p>

    <div v-if="hasScanned" class="groups">
      <template v-for="group in topGroups" :key="group.name">
        <div v-for="sub in group.subGroups" :key="`${group.name}/${sub.name}`" class="sub-group">
          <div class="sub-group-label">
            <span class="sub-group-path">{{ group.name }}/{{ sub.name ? sub.name + '/' : '' }}</span>
            <span class="sub-group-count">{{ sub.items.length }}{{ sub.unused > 0 ? ` (${sub.unused} unused)` : '' }}</span>
          </div>
          <div class="tile-grid">
            <div
              v-for="item in sub.items"
              :key="item.key"
              class="tile"
              :style="{ width: tileSize(item).w + 'px' }"
            >
              <div class="tile-img-box" :style="{ height: tileSize(item).h + 'px' }">
                <div v-if="!item.inUse" class="tile-warning" :class="{ 'tile-warning--recent': !item.deletable }">
                  <v-icon size="14" class="mr-1">mdi-alert</v-icon>
                  {{ item.deletable ? 'Unused' : 'Unused, too recent' }}
                </div>
                <a :href="item.url" target="_blank" rel="noopener">
                  <img
                    :src="item.url"
                    class="tile-img"
                    loading="lazy"
                    alt=""
                    @load="onImgLoad(item, $event)"
                  />
                </a>
              </div>
              <div class="tile-info">
                <div class="tile-name" :title="item.fileName">{{ item.displayName }}</div>
                <div class="tile-date">
                  <v-icon size="14" class="mr-1">mdi-clock-outline</v-icon>{{ formatDate(item.lastModified) }}
                </div>
                <div class="tile-meta">{{ formatSize(item.sizeBytes) }}</div>
              </div>
            </div>
          </div>
        </div>
      </template>
    </div>
  </v-container>
</template>

<script setup>
import { ref, computed } from 'vue'
import api from '@/services/api'
import { IMAGE_UPLOAD_SPECS } from '@/globals'

const scanning = ref(false)
const deleting = ref(false)
const hasScanned = ref(false)
const images = ref([])

// Tiles are resized to exactly match the images' aspect ratios.
const MAX_TILE_WIDTH = 300
const MAX_TILE_HEIGHT = 220
const naturalSizes = ref({})

const aspectRatioFor = (item) => {
  const spec = item.subFolder && IMAGE_UPLOAD_SPECS[item.subFolder]
  if (spec) return spec.width / spec.height
  const loaded = naturalSizes.value[item.key]
  if (loaded) return loaded.w / loaded.h
  return 1
}

const tileSize = (item) => {
  const ratio = aspectRatioFor(item)
  let w = MAX_TILE_WIDTH
  let h = w / ratio
  if (h > MAX_TILE_HEIGHT) {
    h = MAX_TILE_HEIGHT
    w = h * ratio
  }
  return { w: Math.round(w), h: Math.round(h) }
}

const onImgLoad = (item, event) => {
  if (item.subFolder && IMAGE_UPLOAD_SPECS[item.subFolder]) return
  const { naturalWidth, naturalHeight } = event.target
  if (!naturalWidth || !naturalHeight) return
  naturalSizes.value = { ...naturalSizes.value, [item.key]: { w: naturalWidth, h: naturalHeight } }
}

const pluralize = (count, singular, plural = `${singular}s`) => (count === 1 ? singular : plural)

// e.g. "moveset_hero_b353767d-1a60-4d7a-b968-61723d947d9c.png" -> prefix "moveset_hero",
// rest "b353767d-1a60-4d7a-b968-61723d947d9c.png" (see UploadController.cs's fileName build).
const GUID_SUFFIX_RE = /^(.*?)_?([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\.[a-z0-9]+)$/i

const parseKey = (key) => {
  const withoutUploads = key.startsWith('uploads/') ? key.slice('uploads/'.length) : key
  const slashIdx = withoutUploads.indexOf('/')
  const topFolder = slashIdx === -1 ? '(root)' : withoutUploads.slice(0, slashIdx)
  const fileName = slashIdx === -1 ? withoutUploads : withoutUploads.slice(slashIdx + 1)

  const match = fileName.match(GUID_SUFFIX_RE)
  if (match && match[1]) {
    return { topFolder, subFolder: match[1], fileName, displayName: match[2] }
  }
  return { topFolder, subFolder: null, fileName, displayName: fileName }
}

// Nests images the way they're actually laid out in R2
const topGroups = computed(() => {
  const tops = new Map()
  for (const item of images.value) {
    const { topFolder, subFolder, fileName, displayName } = parseKey(item.key)
    if (!tops.has(topFolder)) tops.set(topFolder, new Map())
    const subs = tops.get(topFolder)
    const subKey = subFolder || ''
    if (!subs.has(subKey)) subs.set(subKey, [])
    subs.get(subKey).push({ ...item, subFolder, fileName, displayName })
  }

  return Array.from(tops.entries())
    .map(([name, subs]) => {
      const subGroups = Array.from(subs.entries())
        .map(([subName, items]) => ({
          name: subName,
          items: items.sort((a, b) => new Date(a.lastModified) - new Date(b.lastModified)),
          unused: items.filter(i => !i.inUse).length,
        }))
        .sort((a, b) => a.name.localeCompare(b.name))
      const total = subGroups.reduce((sum, s) => sum + s.items.length, 0)
      return { name, subGroups, total }
    })
    .sort((a, b) => a.name.localeCompare(b.name))
})

const unusedCount = computed(() => images.value.filter(i => !i.inUse).length)
const deletableCount = computed(() => images.value.filter(i => i.deletable).length)

const formatSize = (bytes) => {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

const formatDate = (iso) => new Date(iso).toLocaleString()

const scan = async () => {
  scanning.value = true
  try {
    const res = await api.get('/admin/image-gc/scan')
    images.value = res.data
    hasScanned.value = true
  } catch (err) {
    console.error('Failed to scan images:', err)
    alert('Failed to scan images.')
  } finally {
    scanning.value = false
  }
}

const deleteAllUnused = async () => {
  const keys = images.value.filter(i => i.deletable).map(i => i.key)
  if (!confirm(`Permanently delete ${keys.length} unused ${pluralize(keys.length, 'image')} from R2? This cannot be undone.`)) return

  deleting.value = true
  try {
    const res = await api.post('/admin/image-gc/execute', keys)
    const { deleted, failed, skipped } = res.data
    console.log(`Deleted ${deleted.length} image(s). Skipped: ${skipped.length}. Failed: ${failed.length}.`)
    if (failed.length) alert(`${failed.length} image(s) failed to delete. Check the console for details.`)
    await scan()
  } catch (err) {
    console.error('Failed to delete images:', err)
    alert('Failed to delete images.')
  } finally {
    deleting.value = false
  }
}
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
.summary-good {
  color: #66bb6a;
  font-weight: 600;
}
.btn {
  text-transform: unset;
  letter-spacing: 0.009375em;
  font-size: medium;
  background-color: #2e2e2e;
  color: #e2e2e2;
}
.btn:disabled {
  background-color: grey !important;
}
.delete-btn {
  background-color: #7a1f1f !important;
  color: #ffffff !important;
}
.delete-btn:disabled {
  background-color: #3a2a2a !important;
  color: #8a8a8a !important;
}

.groups {
  display: flex;
  flex-direction: column;
  gap: 1.5em;
}

.sub-group {
  margin-bottom: 1.5em;
}
.sub-group-label {
  display: flex;
  align-items: baseline;
  gap: 0.6em;
  padding-bottom: 0.4em;
  border-bottom: 1px solid #333;
  margin-bottom: 0.75em;
}
.sub-group-path {
  font-family: monospace;
  font-size: 1em;
  color: #e2e2e2;
}
.sub-group-count {
  color: #8a8a8a;
  font-size: 0.8em;
}

.tile-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 1em;
}

.tile {
  display: flex;
  flex-direction: column;
  background-color: #1e1e1e;
  border-radius: 8px;
  overflow: hidden;
}

.tile-img-box {
  position: relative;
  background-color: #111;
  overflow: hidden;
}
.tile-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}
.tile-warning {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0.3em 0.5em;
  font-size: 0.75em;
  font-weight: 600;
  color: #1a1a1a;
  background-color: rgba(255, 179, 0, 0.9);
}
.tile-warning--recent {
  color: #e2e2e2;
  background-color: rgba(60, 60, 60, 0.85);
}

.tile-info {
  padding: 0.6em 0.75em 0.75em;
}
.tile-name {
  font-family: monospace;
  font-size: 0.82em;
  color: #e2e2e2;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.tile-date {
  display: flex;
  align-items: center;
  font-size: 0.95em;
  font-weight: 600;
  color: #f2f2f2;
  margin-top: 0.4em;
}
.tile-meta {
  font-size: 0.75em;
  color: #8a8a8a;
  margin-top: 0.15em;
}
</style>

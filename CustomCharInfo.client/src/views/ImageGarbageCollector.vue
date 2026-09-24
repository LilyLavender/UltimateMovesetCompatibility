<template>
  <PageShell
    title="Image garbage collector"
    tier="wide"
    :back-to="{ name: 'AdminPortal' }"
    back-label="Admin portal"
    lede="Every image uploaded to R2 and whether a moveset, series, blog post, modder profile, or banner still references it."
  >
    <div class="toolbar">
      <AppButton icon="mdi-magnify" :busy="scanning" @click="scan">Scan images</AppButton>
      <AppButton
        variant="danger"
        icon="mdi-delete"
        :busy="deleting"
        :disabled="unusedCount === 0"
        @click="deleteAllUnused"
      >
        Delete unused images ({{ unusedCount }})
      </AppButton>
      <p
        v-if="hasScanned"
        class="toolbar__summary"
        :class="{ 'toolbar__summary--good': unusedCount === 0 }"
      >
        <template v-if="unusedCount === 0">All images are used.</template>
        <template v-else>
          {{ images.length }} {{ pluralize(images.length, 'image') }} found,
          {{ unusedCount }} unused.
        </template>
      </p>
    </div>

    <EmptyState
      v-if="!hasScanned && !scanning"
      message="Run a scan to list every image and mark the ones nothing references."
      icon="mdi-image-search"
    />

    <div v-if="hasScanned" class="groups">
      <template v-for="group in topGroups" :key="group.name">
        <div v-for="sub in group.subGroups" :key="`${group.name}/${sub.name}`" class="sub-group">
          <button
            type="button"
            class="sub-group__label"
            :aria-expanded="!isCollapsed(`${group.name}/${sub.name}`, sub.unused)"
            @click="toggleGroup(`${group.name}/${sub.name}`, sub.unused)"
          >
            <v-icon size="18">
              {{
                isCollapsed(`${group.name}/${sub.name}`, sub.unused)
                  ? 'mdi-chevron-right'
                  : 'mdi-chevron-down'
              }}
            </v-icon>
            <span class="sub-group__path mono"
              >{{ group.name }}/{{ sub.name ? sub.name + '/' : '' }}</span
            >
            <span class="sub-group__count">{{ sub.items.length }}</span>
            <StatusTag v-if="sub.unused > 0" variant="warn">{{ sub.unused }} unused</StatusTag>
          </button>
          <div v-show="!isCollapsed(`${group.name}/${sub.name}`, sub.unused)" class="tile-grid">
            <div
              v-for="item in sub.items"
              :key="item.key"
              class="tile"
              :class="{ 'tile--unused': !item.inUse }"
              :style="{ width: tileSize(item).w + 'px' }"
            >
              <a
                :href="item.url"
                target="_blank"
                rel="noopener"
                class="tile__img-box"
                :style="{ height: tileSize(item).h + 'px' }"
              >
                <StatusTag v-if="!item.inUse" variant="warn" icon="mdi-alert" class="tile__flag">
                  Unused
                </StatusTag>
                <img
                  :src="item.url"
                  class="tile__img"
                  loading="lazy"
                  alt=""
                  @load="onImgLoad(item, $event)"
                />
              </a>
              <div class="tile__info">
                <div class="tile__name mono" :title="item.fileName">{{ item.displayName }}</div>
                <div class="tile__date">{{ formatDate(item.lastModified) }}</div>
                <div class="tile__meta">{{ formatSize(item.sizeBytes) }}</div>
              </div>
            </div>
          </div>
        </div>
      </template>
    </div>
  </PageShell>
</template>

<script setup>
import { ref, computed } from 'vue'
import api from '@/services/api'
import { IMAGE_UPLOAD_SPECS } from '@/globals'
import { useNotify } from '@/composables/useNotify'
import PageShell from '@/components/PageShell.vue'
import AppButton from '@/components/AppButton.vue'
import StatusTag from '@/components/StatusTag.vue'
import EmptyState from '@/components/EmptyState.vue'

const notify = useNotify()

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
const GUID_SUFFIX_RE =
  /^(.*?)_?([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\.[a-z0-9]+)$/i

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
          unused: items.filter((i) => !i.inUse).length,
        }))
        .sort((a, b) => a.name.localeCompare(b.name))
      const total = subGroups.reduce((sum, s) => sum + s.items.length, 0)
      return { name, subGroups, total }
    })
    .sort((a, b) => a.name.localeCompare(b.name))
})

const unusedCount = computed(() => images.value.filter((i) => !i.inUse).length)
const collapsedOverrides = ref({})

const isCollapsed = (key, unused) => {
  if (key in collapsedOverrides.value) return collapsedOverrides.value[key]
  return unused === 0
}

const toggleGroup = (key, unused) => {
  collapsedOverrides.value = { ...collapsedOverrides.value, [key]: !isCollapsed(key, unused) }
}

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
    notify.error('Failed to scan images.')
  } finally {
    scanning.value = false
  }
}

const deleteAllUnused = async () => {
  const keys = images.value.filter((i) => !i.inUse).map((i) => i.key)
  if (
    !confirm(
      `Permanently delete ${keys.length} unused ${pluralize(keys.length, 'image')} from R2? This cannot be undone.`
    )
  )
    return

  deleting.value = true
  try {
    const res = await api.post('/admin/image-gc/execute', keys)
    const { deleted, failed, skipped } = res.data
    console.log(
      `Deleted ${deleted.length} image(s). Skipped: ${skipped.length}. Failed: ${failed.length}.`
    )
    if (failed.length)
      notify.error(`${failed.length} image(s) failed to delete. Check the console for details.`)
    await scan()
  } catch (err) {
    console.error('Failed to delete images:', err)
    notify.error('Failed to delete images.')
  } finally {
    deleting.value = false
  }
}
</script>

<style scoped>
.toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px 12px;
  margin-bottom: 20px;
}

.toolbar__summary {
  margin: 0 0 0 auto;
  color: var(--tx-2);
  font-size: 14px;
}

.toolbar__summary--good {
  color: var(--ok);
  font-weight: 600;
}

.groups {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.sub-group__label {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  padding: 6px 0;
  margin-bottom: 12px;
  border: 0;
  border-bottom: 1px solid var(--line-2);
  background: none;
  color: var(--tx);
  font: inherit;
  text-align: left;
  cursor: pointer;
}

.sub-group__label:hover {
  border-bottom-color: var(--white);
}

.sub-group__path {
  font-size: 14px;
}

.sub-group__count {
  color: var(--tx-3);
  font-size: 13px;
}

.tile-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.tile {
  display: flex;
  flex-direction: column;
  border: 1px solid var(--line);
  background: var(--panel);
}

.tile--unused {
  border-color: color-mix(in srgb, var(--warn) 50%, var(--line));
}

.tile__img-box {
  position: relative;
  display: block;
  background: var(--bg);
  overflow: hidden;
}

.tile__img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.tile__flag {
  position: absolute;
  top: 6px;
  left: 6px;
  z-index: 1;
}

.tile__info {
  padding: 8px 10px 10px;
}

.tile__name {
  font-size: 12px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.tile__date {
  margin-top: 4px;
  font-size: 13px;
  font-weight: 600;
}

.tile__meta {
  margin-top: 2px;
  font-size: 12px;
  color: var(--tx-3);
}
</style>

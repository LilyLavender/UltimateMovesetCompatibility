<template>
  <v-container max-width="1200px">
    <h1 class="mb-5 page-title no-select">All Plugins</h1>

    <v-data-table
      class="dark-table"
      :items="versionRows"
      :headers="headers"
      item-key="pluginVersionId"
      density="comfortable"
    >
      <template #item.lastCheckedAt="{ item }">
        {{ item.lastCheckedAt ? new Date(item.lastCheckedAt).toLocaleString() : 'Never' }}
      </template>
    </v-data-table>

    <h2 class="mt-8 mb-3">Unmatched Hashes</h2>
    <p class="mb-4 helper-text">
      Hashes people have checked that don't match any registered plugin version.
    </p>

    <v-data-table
      class="dark-table"
      :items="unknownHashes"
      :headers="unknownHeaders"
      item-key="hash"
      density="comfortable"
    >
      <template #item.firstCheckedAt="{ item }">
        {{ new Date(item.firstCheckedAt).toLocaleString() }}
      </template>
      <template #item.lastCheckedAt="{ item }">
        {{ new Date(item.lastCheckedAt).toLocaleString() }}
      </template>
    </v-data-table>
  </v-container>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/services/api'

const plugins = ref([])
const unknownHashes = ref([])

const headers = [
  { title: 'Plugin', key: 'pluginName' },
  { title: 'Attached To', key: 'attachedTo' },
  { title: 'Version', key: 'versionLabel' },
  { title: 'Current', key: 'isCurrent' },
  { title: 'Check Count', key: 'checkCount' },
  { title: 'Last Checked', key: 'lastCheckedAt' },
]

const unknownHeaders = [
  { title: 'Hash', key: 'hash' },
  { title: 'Check Count', key: 'checkCount' },
  { title: 'First Checked', key: 'firstCheckedAt' },
  { title: 'Last Checked', key: 'lastCheckedAt' },
]

// Flatten to one row per version so check counts/dates are visible per-version
const versionRows = computed(() =>
  plugins.value.flatMap(p =>
    p.versions.map(v => ({
      pluginVersionId: v.pluginVersionId,
      pluginName: p.name,
      attachedTo: p.movesetName ?? p.dependencyName ?? 'Other',
      versionLabel: v.versionLabel,
      isCurrent: v.isCurrent,
      checkCount: v.checkCount,
      lastCheckedAt: v.lastCheckedAt,
    }))
  )
)

onMounted(async () => {
  try {
    const [pluginsRes, unknownRes] = await Promise.all([
      api.get('/plugins/all'),
      api.get('/plugins/unknown-hashes'),
    ])
    plugins.value = pluginsRes.data
    unknownHashes.value = unknownRes.data
  } catch (err) {
    console.error('Failed to fetch plugins:', err)
  }
})
</script>

<style scoped>
.page-title {
  font-size: 2.5rem;
}
.helper-text {
  color: #b0b0b0;
}
</style>

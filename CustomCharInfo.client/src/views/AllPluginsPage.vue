<template>
  <PageShell
    title="All plugins"
    tier="wide"
    :back-to="{ name: 'AdminPortal' }"
    back-label="Admin portal"
  >
    <SectionHeading title="Registered versions" :count="versionRows.length" />
    <SkeletonTable v-if="loading" :headers="headers.map((h) => h.title)" :rows="8" />
    <TableScroll v-else min-width="900px" class="reveal">
      <v-data-table
        :items="versionRows"
        :headers="headers"
        item-key="pluginVersionId"
        density="comfortable"
      >
        <template #item.isCurrent="{ value }">
          <StatusTag v-if="value" variant="ok">Current</StatusTag>
          <span v-else class="faint">No</span>
        </template>
        <template #item.firstCheckedAt="{ item }">
          {{ item.firstCheckedAt ? new Date(item.firstCheckedAt).toLocaleString() : 'Never' }}
        </template>
        <template #item.lastCheckedAt="{ item }">
          {{ item.lastCheckedAt ? new Date(item.lastCheckedAt).toLocaleString() : 'Never' }}
        </template>
      </v-data-table>
    </TableScroll>

    <SectionHeading title="Unmatched hashes" :count="unknownHashes.length" />
    <p class="hint">Hashes people have checked that don't match any registered plugin version.</p>
    <SkeletonTable v-if="loading" :headers="unknownHeaders.map((h) => h.title)" :rows="4" />
    <TableScroll v-else min-width="720px" class="reveal">
      <v-data-table
        :items="unknownHashes"
        :headers="unknownHeaders"
        item-key="hash"
        density="comfortable"
      >
        <template #item.hash="{ value }">
          <span class="mono">{{ value }}</span>
        </template>
        <template #item.firstCheckedAt="{ item }">
          {{ new Date(item.firstCheckedAt).toLocaleString() }}
        </template>
        <template #item.lastCheckedAt="{ item }">
          {{ new Date(item.lastCheckedAt).toLocaleString() }}
        </template>
      </v-data-table>
    </TableScroll>
  </PageShell>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/services/api'
import PageShell from '@/components/PageShell.vue'
import SectionHeading from '@/components/SectionHeading.vue'
import StatusTag from '@/components/StatusTag.vue'
import TableScroll from '@/components/TableScroll.vue'
import SkeletonTable from '@/components/SkeletonTable.vue'

const plugins = ref([])
const unknownHashes = ref([])
const loading = ref(true)

const headers = [
  { title: 'Plugin', key: 'pluginName' },
  { title: 'Attached to', key: 'attachedTo' },
  { title: 'Version', key: 'versionLabel' },
  { title: 'Current', key: 'isCurrent' },
  { title: 'Checks', key: 'checkCount', align: 'end' },
  { title: 'First checked', key: 'firstCheckedAt' },
  { title: 'Last checked', key: 'lastCheckedAt' },
]

const unknownHeaders = [
  { title: 'Hash', key: 'hash' },
  { title: 'Checks', key: 'checkCount', align: 'end' },
  { title: 'First checked', key: 'firstCheckedAt' },
  { title: 'Last checked', key: 'lastCheckedAt' },
]

// Flatten to one row per version so check counts/dates are visible per-version
const versionRows = computed(() =>
  plugins.value.flatMap((p) =>
    p.versions.map((v) => ({
      pluginVersionId: v.pluginVersionId,
      pluginName: p.name,
      attachedTo: p.movesetName ?? p.dependencyName ?? 'Other',
      versionLabel: v.versionLabel,
      isCurrent: v.isCurrent,
      checkCount: v.checkCount,
      firstCheckedAt: v.firstCheckedAt,
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
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.hint {
  margin: -6px 0 12px;
  color: var(--tx-3);
  font-size: 13px;
}
</style>

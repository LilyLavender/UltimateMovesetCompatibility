<template>
  <PageShell
    title="All plugins"
    tier="wide"
    :back-to="{ name: 'AdminPortal' }"
    back-label="Admin portal"
  >
    <LabeledField label="Search" class="search-field">
      <v-text-field
        v-model="search"
        placeholder="Search..."
        density="compact"
        hide-details
        clearable
        prepend-inner-icon="mdi-magnify"
      />
    </LabeledField>

    <SectionHeading title="Registered versions" :count="filteredVersionRows.length" />
    <SkeletonTable v-if="loading" :headers="headers.map((h) => h.title)" :rows="8" />
    <TableScroll v-else min-width="1100px" class="reveal">
      <v-data-table
        :items="filteredVersionRows"
        :headers="headers"
        item-value="pluginVersionId"
        density="comfortable"
      >
        <template #item.isCurrent="{ value }">
          <StatusTag v-if="value" variant="ok">Current</StatusTag>
          <span v-else class="faint">No</span>
        </template>
        <template #item.hash="{ item }">
          <span class="hash-cell mono" :title="item.hash">
            {{ item.hash.slice(0, 12) }}&hellip;
            <button
              type="button"
              class="copy-btn"
              aria-label="Copy hash"
              @click="copyHash(item.hash)"
            >
              <v-icon size="14">mdi-content-copy</v-icon>
            </button>
          </span>
        </template>
        <template #item.firstCheckedAt="{ item }">
          {{ item.firstCheckedAt ? new Date(item.firstCheckedAt).toLocaleString() : 'Never' }}
        </template>
        <template #item.lastCheckedAt="{ item }">
          {{ item.lastCheckedAt ? new Date(item.lastCheckedAt).toLocaleString() : 'Never' }}
        </template>
        <template #item.actions="{ item }">
          <span class="row-actions">
            <AppButton
              v-if="item.movesetId == null"
              size="sm"
              variant="ghost"
              icon="mdi-pencil"
              @click="openEditVersion(item)"
            >
              Edit
            </AppButton>
            <AppButton
              v-if="item.movesetId == null"
              size="sm"
              variant="ghost"
              icon="mdi-delete"
              aria-label="Delete version"
              @click="confirmDelete(item)"
            />
          </span>
        </template>
      </v-data-table>
    </TableScroll>

    <SectionHeading title="Unmatched hashes" :count="filteredUnknownHashes.length" />
    <p class="hint">Hashes people have checked that don't match any registered plugin version.</p>
    <SkeletonTable v-if="loading" :headers="unknownHeaders.map((h) => h.title)" :rows="4" />
    <TableScroll v-else min-width="720px" class="reveal">
      <v-data-table
        :items="filteredUnknownHashes"
        :headers="unknownHeaders"
        item-value="hash"
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

    <PluginVersionEditDialog v-model="editVersionOpen" :version="editTarget" @saved="load" />

    <!-- Delete confirmation -->
    <v-dialog v-bind="dialogProps" v-model="deleteOpen" max-width="480px">
      <v-card>
        <v-card-title class="dialog-title">Delete this version?</v-card-title>
        <v-card-text>
          {{ deleteTarget?.pluginName }} v{{ deleteTarget?.versionLabel }} will be removed and its
          hash will no longer identify anything.
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <AppButton variant="ghost" @click="deleteOpen = false">Cancel</AppButton>
          <AppButton variant="danger" icon="mdi-delete" :busy="deleting" @click="deleteVersion">
            Delete
          </AppButton>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </PageShell>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/services/api'
import { useNotify } from '@/composables/useNotify'
import { useDialogProps } from '@/composables/useDialogProps'
import PageShell from '@/components/PageShell.vue'
import SectionHeading from '@/components/SectionHeading.vue'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'
import StatusTag from '@/components/StatusTag.vue'
import TableScroll from '@/components/TableScroll.vue'
import SkeletonTable from '@/components/SkeletonTable.vue'
import PluginVersionEditDialog from '@/components/PluginVersionEditDialog.vue'

const notify = useNotify()
const dialogProps = useDialogProps()

const plugins = ref([])
const unknownHashes = ref([])
const loading = ref(true)
const search = ref('')

const editTarget = ref(null)
const editVersionOpen = ref(false)
const deleteTarget = ref(null)
const deleteOpen = ref(false)
const deleting = ref(false)

const headers = [
  { title: 'Plugin', key: 'pluginName' },
  { title: 'Attached to', key: 'attachedTo' },
  { title: 'Version', key: 'versionLabel' },
  { title: 'Current', key: 'isCurrent' },
  { title: 'Hash', key: 'hash', sortable: false },
  { title: 'Checks', key: 'checkCount', align: 'end' },
  { title: 'First checked', key: 'firstCheckedAt' },
  { title: 'Last checked', key: 'lastCheckedAt' },
  { title: '', key: 'actions', sortable: false, align: 'end' },
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
      pluginId: p.pluginId,
      pluginName: p.name,
      movesetId: p.movesetId,
      dependencyId: p.dependencyId,
      attachedTo: p.movesetName ?? p.dependencyName ?? 'Other',
      versionLabel: v.versionLabel,
      hash: v.hash,
      isCurrent: v.isCurrent,
      checkCount: v.checkCount,
      firstCheckedAt: v.firstCheckedAt,
      lastCheckedAt: v.lastCheckedAt,
    }))
  )
)

const query = computed(() => (search.value ?? '').trim().toLowerCase())

function matchesSearch(fields) {
  if (!query.value) return true
  return fields.some((f) => (f ?? '').toLowerCase().includes(query.value))
}

const filteredVersionRows = computed(() =>
  versionRows.value.filter((r) =>
    matchesSearch([r.pluginName, r.attachedTo, r.versionLabel, r.hash])
  )
)

const filteredUnknownHashes = computed(() =>
  unknownHashes.value.filter((u) => matchesSearch([u.hash]))
)

async function load() {
  try {
    const [pluginsRes, unknownRes] = await Promise.all([
      api.get('/plugins/all'),
      api.get('/plugins/unknown-hashes'),
    ])
    plugins.value = pluginsRes.data
    unknownHashes.value = unknownRes.data
  } catch (err) {
    notify.error('Failed to load plugins.', err)
  } finally {
    loading.value = false
  }
}

function openEditVersion(row) {
  editTarget.value = row
  editVersionOpen.value = true
}

function confirmDelete(row) {
  deleteTarget.value = row
  deleteOpen.value = true
}

async function deleteVersion() {
  const row = deleteTarget.value
  if (!row) return
  deleting.value = true
  try {
    await api.delete(`/plugins/${row.pluginId}/versions/${row.pluginVersionId}`)
    notify.success('Version deleted.')
    deleteOpen.value = false
    await load()
  } catch (err) {
    notify.error('Could not delete the version.', err)
  } finally {
    deleting.value = false
  }
}

async function copyHash(hash) {
  try {
    await navigator.clipboard.writeText(hash)
    notify.info('Hash copied.')
  } catch {
    notify.warning('Could not copy to the clipboard.')
  }
}

onMounted(load)
</script>

<style scoped>
.search-field {
  max-width: 480px;
}

.hint {
  margin: -6px 0 12px;
  color: var(--tx-3);
  font-size: 13px;
}

.hash-cell {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  color: var(--tx-2);
  white-space: nowrap;
}

.copy-btn {
  display: inline-flex;
  padding: 2px;
  border: 0;
  background: none;
  color: var(--tx-2);
  cursor: pointer;
}

.copy-btn:hover {
  color: var(--white);
}

.row-actions {
  display: inline-flex;
  gap: 4px;
  white-space: nowrap;
}

.dialog-title {
  font-family: var(--font-condensed);
  font-weight: 700;
  text-transform: uppercase;
}
</style>

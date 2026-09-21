<template>
  <v-container max-width="1200px">
    <h1 class="mb-2 page-title no-select">Game Versions</h1>
    <p class="helper-text mb-5">
      Every hook keeps one offset per game version. Adding a version takes the update's shift table
      and works out every hook's new offset, marked unverified until a modder confirms it.
    </p>

    <!-- Existing versions -->
    <h2 class="mb-2">Versions</h2>
    <v-table class="dark-table mb-8" density="comfortable">
      <thead>
        <tr>
          <th>Version</th>
          <th>Added</th>
          <th>Confirmed</th>
          <th>Generated</th>
          <th>Carried forward</th>
          <th>No offset</th>
          <th class="text-right"></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="version in versions" :key="version.gameVersionId">
          <td>
            <strong>{{ version.name }}</strong>
            <span v-if="version.isLatest" class="latest-marker">current</span>
          </td>
          <td class="helper-text">{{ formatDate(version.createdAt) }}</td>
          <td>{{ countFor(version, OffsetState.Confirmed) }}</td>
          <td>{{ countFor(version, OffsetState.Generated) }}</td>
          <td>{{ countFor(version, OffsetState.CarriedForward) }}</td>
          <td>{{ missingFor(version) }}</td>
          <td class="text-right">
            <v-btn
              v-if="version.isLatest && versions.length > 1"
              size="small"
              variant="text"
              icon="mdi-delete"
              title="Delete this version and its offsets"
              @click="confirmDelete(version)"
            />
          </td>
        </tr>
      </tbody>
    </v-table>

    <!-- Add a version -->
    <h2 class="mb-2">Add a version</h2>
    <p class="helper-text mb-3">
      One range per line. First and last address in the old version, then how far that range moved
      between old and new. Addresses outside every range are copied unchanged.
    </p>
    <v-row dense>
      <v-col cols="12" sm="3">
        <v-text-field
          v-model="name"
          variant="outlined"
          density="comfortable"
          label="Version"
          placeholder="XX.X.X"
          hide-details
        />
      </v-col>
    </v-row>
    <v-textarea
      v-model="shiftTable"
      variant="outlined"
      class="shift-table mt-3"
      label="Shift table"
      placeholder="0x0          0x169c3bf    +0x0&#10;0x169ef70    0x178b563    -0x1a0&#10;0x52a9000    0x7446100    +0x1000"
      rows="8"
      auto-grow
      hide-details
    />
    <div class="d-flex ga-3 mt-3">
      <v-btn
        class="btn action-button"
        :disabled="!canPreview || previewing"
        :loading="previewing"
        @click="preview"
      >
        Preview
      </v-btn>
      <v-btn
        class="btn action-button"
        :disabled="!canApply || applying"
        :loading="applying"
        @click="apply"
      >
        Apply to every hook
      </v-btn>
    </div>

    <!-- Preview -->
    <template v-if="previewResult">
      <h2 class="mt-8 mb-2">Preview for {{ previewedName }}</h2>
      <div class="d-flex ga-2 mb-3 flex-wrap">
        <span class="status-pill offset-state-2">{{ previewResult.generated }} generated</span>
        <span class="status-pill offset-state-3">
          {{ previewResult.carriedForward }} carried forward
        </span>
        <span v-if="!previewCurrent" class="helper-text align-self-center">
          Inputs changed since this preview. Preview again before applying.
        </span>
      </div>
      <v-data-table
        class="dark-table"
        :items="previewRows"
        :headers="previewHeaders"
        :sort-by="previewSortBy"
        item-value="hookId"
        density="compact"
        :items-per-page="50"
      >
        <template #item.oldOffset="{ item }">
          <span class="mono">0x{{ item.oldOffset }}</span>
        </template>
        <template #item.newOffset="{ item }">
          <span class="mono" :class="{ changed: item.oldOffset !== item.newOffset }">
            0x{{ item.newOffset }}
          </span>
        </template>
        <template #item.offsetStateId="{ item }">
          <span class="status-pill" :class="`offset-state-${item.offsetStateId}`">
            {{ OFFSET_STATE_NAMES[item.offsetStateId] }}
          </span>
        </template>
      </v-data-table>
    </template>

    <!-- Delete confirmation -->
    <v-dialog v-model="deleteOpen" max-width="480px">
      <v-card color="#1e1e1e">
        <v-card-title>Delete {{ deleteTarget?.name }}?</v-card-title>
        <v-card-text>
          Every hook's offset for {{ deleteTarget?.name }} and the shift table that produced them
          will be removed. Offsets shown across the site go back to the previous version.
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="deleteOpen = false">Cancel</v-btn>
          <v-btn variant="text" color="#ef5350" :loading="deleting" @click="deleteVersion">
            Delete
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { format } from 'date-fns'
import api from '@/services/api'
import { useNotify } from '@/composables/useNotify'
import { OffsetState, OFFSET_STATE_NAMES } from '@/globals'

const notify = useNotify()

const versions = ref([])
const hooks = ref([])

const name = ref('')
const shiftTable = ref('')
const previewing = ref(false)
const applying = ref(false)
const previewResult = ref(null)
const previewedName = ref('')
// What the last preview was computed from, so Apply is only offered for the same input.
const previewedKey = ref('')

const deleteOpen = ref(false)
const deleteTarget = ref(null)
const deleting = ref(false)

const previewHeaders = [
  { title: 'Hook', key: 'description' },
  { title: 'Old offset', key: 'oldOffset', width: 140 },
  { title: 'New offset', key: 'newOffset', width: 140 },
  { title: 'State', key: 'offsetStateId', width: 160 },
]

const inputKey = computed(() => JSON.stringify([name.value.trim(), shiftTable.value.trim()]))
const canPreview = computed(
  () => name.value.trim().length > 0 && shiftTable.value.trim().length > 0
)
const previewCurrent = computed(() => previewResult.value && previewedKey.value === inputKey.value)
const canApply = computed(() => canPreview.value && previewCurrent.value)

// Zero-padded to a fixed width so the column lines up and a plain string sort is a numeric sort.
const pad = (offset) => offset.padStart(8, '0')
const previewRows = computed(() =>
  (previewResult.value?.rows ?? [])
    .map((r) => ({ ...r, oldOffset: pad(r.oldOffset), newOffset: pad(r.newOffset) }))
    .sort((a, b) => a.oldOffset.localeCompare(b.oldOffset))
)
const previewSortBy = [{ key: 'oldOffset', order: 'asc' }]

const formatDate = (date) => (date ? format(new Date(date), 'PP') : '')

const entriesFor = (version) =>
  hooks.value
    .map((h) => h.offsets?.find((o) => o.gameVersionId === version.gameVersionId))
    .filter(Boolean)

const countFor = (version, stateId) =>
  entriesFor(version).filter((o) => o.offsetStateId === stateId).length

const missingFor = (version) => hooks.value.length - entriesFor(version).length

const load = async () => {
  try {
    const [versionRes, hookRes] = await Promise.all([api.get('/game-versions'), api.get('/hooks')])
    versions.value = versionRes.data
    hooks.value = hookRes.data
  } catch (err) {
    notify.error('Failed to load game versions.', err)
  }
}

const requestFailed = (err, fallback) => {
  const status = err.response?.status
  if (status === 400 || status === 409) {
    notify.warning(err.response.data)
  } else {
    notify.error(fallback, err)
  }
}

const preview = async () => {
  previewing.value = true
  const key = inputKey.value
  try {
    const res = await api.post('/game-versions/preview', {
      name: name.value.trim(),
      shiftTable: shiftTable.value,
    })
    previewResult.value = res.data
    previewedName.value = name.value.trim()
    previewedKey.value = key
  } catch (err) {
    previewResult.value = null
    requestFailed(err, 'Failed to preview the shift table.')
  } finally {
    previewing.value = false
  }
}

const apply = async () => {
  applying.value = true
  try {
    const res = await api.post('/game-versions', {
      name: name.value.trim(),
      shiftTable: shiftTable.value,
    })
    notify.success(
      `${res.data.version.name} added: ${res.data.generated} generated, ${res.data.carriedForward} carried forward.`
    )
    name.value = ''
    shiftTable.value = ''
    previewResult.value = null
    previewedKey.value = ''
    await load()
  } catch (err) {
    requestFailed(err, 'Failed to add the game version.')
  } finally {
    applying.value = false
  }
}

const confirmDelete = (version) => {
  deleteTarget.value = version
  deleteOpen.value = true
}

const deleteVersion = async () => {
  if (!deleteTarget.value) return
  deleting.value = true
  try {
    await api.delete(`/game-versions/${deleteTarget.value.gameVersionId}`)
    notify.success(`${deleteTarget.value.name} deleted.`)
    deleteOpen.value = false
    deleteTarget.value = null
    await load()
  } catch (err) {
    requestFailed(err, 'Failed to delete the game version.')
  } finally {
    deleting.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.page-title {
  font-size: 4em;
}

.helper-text {
  opacity: 0.7;
  font-size: 0.9rem;
}

.latest-marker {
  margin-left: 8px;
  padding: 1px 7px;
  border-radius: 10px;
  font-size: 0.7rem;
  background-color: #2e7d32;
  color: white;
}

.shift-table :deep(textarea) {
  font-family: monospace;
  font-size: 0.85rem;
}

.action-button {
  background-color: #2e2e2e;
  color: #e2e2e2;
  box-shadow: none;
}

.mono {
  font-family: monospace;
}

.changed {
  color: #ffd54f;
}

.status-pill {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 0.8rem;
  font-weight: 500;
  white-space: nowrap;
}
.offset-state-1 {
  background-color: #2e7d32;
  color: white;
}
.offset-state-2 {
  background-color: #fbc02d;
  color: black;
}
.offset-state-3 {
  background-color: #ef6c00;
  color: white;
}
</style>

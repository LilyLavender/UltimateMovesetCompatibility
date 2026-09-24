<template>
  <PageShell
    title="Game versions"
    tier="wide"
    :back-to="{ name: 'AdminPortal' }"
    back-label="Admin portal"
    lede="Every hook keeps one offset per game version. Adding a version takes the update's shift table and works out every hook's new offset, marked unverified until a modder confirms it."
  >
    <!-- Existing versions -->
    <SectionHeading title="Versions" :count="versions.length" />
    <TableScroll min-width="720px">
      <v-table density="comfortable">
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
              <span class="mono version-name">{{ version.name }}</span>
              <StatusTag v-if="version.isLatest" variant="ok" class="ml-2">Current</StatusTag>
            </td>
            <td class="muted">{{ formatDate(version.createdAt) }}</td>
            <td>{{ countFor(version, OffsetState.Confirmed) }}</td>
            <td>{{ countFor(version, OffsetState.Generated) }}</td>
            <td>{{ countFor(version, OffsetState.CarriedForward) }}</td>
            <td>{{ missingFor(version) }}</td>
            <td class="text-right">
              <AppButton
                v-if="version.isLatest && versions.length > 1"
                variant="danger"
                size="sm"
                icon="mdi-delete"
                aria-label="Delete this version and its offsets"
                @click="confirmDelete(version)"
              />
            </td>
          </tr>
        </tbody>
      </v-table>
    </TableScroll>

    <!-- Add a version -->
    <SectionHeading title="Add a version" />
    <section class="panel add-version">
      <p class="hint">
        One range per line. First and last address in the old version, then how far that range moved
        between old and new. Addresses outside every range are copied unchanged.
      </p>
      <LabeledField label="Version" required class="add-version__name">
        <v-text-field v-model="name" placeholder="XX.X.X" density="comfortable" hide-details />
      </LabeledField>
      <LabeledField label="Shift table" required>
        <v-textarea
          v-model="shiftTable"
          class="shift-table"
          placeholder="0x0          0x169c3bf    +0x0&#10;0x169ef70    0x178b563    -0x1a0&#10;0x52a9000    0x7446100    +0x1000"
          rows="8"
          auto-grow
          hide-details
        />
      </LabeledField>
      <div class="add-version__actions">
        <AppButton icon="mdi-eye" :disabled="!canPreview" :busy="previewing" @click="preview">
          Preview
        </AppButton>
        <AppButton
          variant="primary"
          icon="mdi-check"
          :disabled="!canApply"
          :busy="applying"
          @click="apply"
        >
          Apply to every hook
        </AppButton>
      </div>
    </section>

    <!-- Preview -->
    <template v-if="previewResult">
      <SectionHeading :title="`Preview for ${previewedName}`" />
      <div class="preview-summary">
        <StatusTag
          :offset-state="OffsetState.Generated"
          :label="`${previewResult.generated} generated`"
        />
        <StatusTag
          :offset-state="OffsetState.CarriedForward"
          :label="`${previewResult.carriedForward} carried forward`"
        />
        <span v-if="!previewCurrent" class="preview-summary__stale">
          Inputs changed since this preview. Preview again before applying.
        </span>
      </div>
      <TableScroll min-width="720px">
        <v-data-table
          :items="previewRows"
          :headers="previewHeaders"
          :sort-by="previewSortBy"
          item-value="hookId"
          density="compact"
          :items-per-page="50"
        >
          <template #item.oldOffset="{ item }">
            <span class="mono">{{ formatOffset(item.oldOffset) }}</span>
          </template>
          <template #item.newOffset="{ item }">
            <span class="mono" :class="{ changed: item.oldOffset !== item.newOffset }">
              {{ formatOffset(item.newOffset) }}
            </span>
          </template>
          <template #item.offsetStateId="{ item }">
            <StatusTag :offset-state="item.offsetStateId" />
          </template>
        </v-data-table>
      </TableScroll>
    </template>

    <!-- Delete confirmation -->
    <v-dialog v-bind="dialogProps" v-model="deleteOpen" max-width="480px">
      <v-card>
        <v-card-title class="dialog-title">Delete {{ deleteTarget?.name }}?</v-card-title>
        <v-card-text>
          Every hook's offset for {{ deleteTarget?.name }} and the shift table that produced them
          will be removed. Offsets shown across the site go back to the previous version.
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
import { format } from 'date-fns'
import api from '@/services/api'
import { useNotify } from '@/composables/useNotify'
import { OffsetState } from '@/globals'
import { useDialogProps } from '@/composables/useDialogProps'
import { formatOffset } from '@/services/offsets'
import PageShell from '@/components/PageShell.vue'
import SectionHeading from '@/components/SectionHeading.vue'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'
import StatusTag from '@/components/StatusTag.vue'
import TableScroll from '@/components/TableScroll.vue'
const dialogProps = useDialogProps()

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
.version-name {
  font-weight: 600;
}

.add-version {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.hint {
  margin: 0;
  color: var(--tx-2);
  font-size: 14px;
}

.add-version__name {
  max-width: 240px;
}

.shift-table :deep(textarea) {
  font-family: var(--font-mono);
  font-size: 13px;
}

.add-version__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.preview-summary {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  margin-bottom: 12px;
}

.preview-summary__stale {
  color: var(--warn);
  font-size: 13px;
}

.changed {
  color: var(--warn);
}

.dialog-title {
  font-family: var(--font-condensed);
  font-weight: 700;
  text-transform: uppercase;
}
</style>

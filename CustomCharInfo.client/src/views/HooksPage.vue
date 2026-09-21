<template>
  <v-container max-width="1080px">
    <!-- Page title -->
    <h1 class="mb-4 page-title">Hooks</h1>

    <!-- Add hook button -->
    <div v-if="canConfirm" class="mb-5 pb-5">
      <router-link :to="{ name: 'AddHook' }" class="unvisitable text-decoration-none">
        <v-icon>mdi-plus</v-icon>
        Add Hook
      </router-link>
    </div>

    <!-- Hooks Table -->
    <v-data-table
      v-if="hooks.length"
      v-model:expanded="expanded"
      :headers="headers"
      :items="visibleHooks"
      :sort-by="defaultSort"
      item-value="hookId"
      show-expand
      class="dark-table"
      dense
    >
      <!-- Offset header doubles as the game version picker -->
      <template #header.offset>
        <span class="offset-header">
          <span>Offset</span>
          <v-select
            v-model="selectedVersionId"
            :items="gameVersions"
            item-title="name"
            item-value="gameVersionId"
            density="compact"
            variant="plain"
            hide-details
            class="version-select"
            @click.stop
          />
        </span>
      </template>

      <!-- Offset with 0x and a pill when nobody has confirmed it for the selected version -->
      <template #item.offset="{ item }">
        <template v-if="item.entry">
          <span class="mono">0x{{ item.entry.offset }}</span>
          <OffsetStatePill
            v-if="isUnverified(item.entry.offsetStateId)"
            class="ml-2"
            :entry="item.entry"
            :hook-id="item.hookId"
            :can-confirm="canConfirm"
            @confirmed="(updated) => replaceEntry(item.hookId, updated)"
          />
        </template>
        <span v-else class="no-offset" :title="`No offset recorded for ${selectedVersionName}`">
          none
        </span>
      </template>

      <!-- Hookable? column -->
      <template #item.hookableStatusId="{ item }">
        <span class="hookable-pill" :class="`status-${item.hookableStatusId}`">
          {{ hookableStatusMap[item.hookableStatusId] || 'Unknown' }}
        </span>
      </template>

      <!-- Actions -->
      <template #item.actions="{ item }">
        <router-link
          :to="{ name: 'EditHook', params: { hookId: item.hookId } }"
          class="text-decoration-none unvisitable"
        >
          <v-icon small>mdi-pencil</v-icon>
        </router-link>
      </template>

      <!-- Every version's offset for the hook -->
      <template #expanded-row="{ columns, item }">
        <tr class="expanded-row">
          <td :colspan="columns.length" class="expanded-cell">
            <table class="version-table">
              <tbody>
                <tr v-for="entry in item.offsets" :key="entry.gameVersionId">
                  <td class="version-name">{{ entry.gameVersion }}</td>
                  <td class="mono">0x{{ entry.offset }}</td>
                  <td>
                    <OffsetStatePill
                      :entry="entry"
                      :hook-id="item.hookId"
                      :can-confirm="canConfirm"
                      @confirmed="(updated) => replaceEntry(item.hookId, updated)"
                    />
                  </td>
                  <td class="version-date">{{ formatDate(entry.updatedAt) }}</td>
                </tr>
              </tbody>
            </table>
          </td>
        </tr>
      </template>
    </v-data-table>

    <p v-else>No hooks found.</p>
  </v-container>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { format } from 'date-fns'
import api from '@/services/api'
import { UserType, UNVERIFIED_OFFSET_STATES } from '@/globals'
import OffsetStatePill from '@/components/OffsetStatePill.vue'

const user = ref(null)
const hooks = ref([])
const hookableStatuses = ref([])
const hookableStatusMap = ref({})
const gameVersions = ref([])
const selectedVersionId = ref(null)
const expanded = ref([])

// Offsets are hex strings of varying length, so the column sorts by value rather than text.
const compareHex = (a, b) => parseInt(a || '0', 16) - parseInt(b || '0', 16)

const headers = [
  {
    title: 'Offset',
    key: 'offset',
    width: '1%',
    sort: compareHex,
    cellProps: { class: 'offset-cell' },
  },
  { title: 'Description', key: 'description' },
  { title: 'Hookable?', key: 'hookableStatusId', width: '20%' },
  { title: 'Actions', key: 'actions', width: '1%', sortable: false },
]
const defaultSort = [{ key: 'offset', order: 'asc' }]

const canConfirm = computed(() => !!user.value && user.value.userTypeId >= UserType.Modder)

const selectedVersionName = computed(
  () => gameVersions.value.find((v) => v.gameVersionId === selectedVersionId.value)?.name ?? ''
)

// Rows carry the entry for the selected version, so the column and its sort follow the picker.
const visibleHooks = computed(() =>
  hooks.value.map((h) => {
    const entry = h.offsets?.find((o) => o.gameVersionId === selectedVersionId.value) ?? null
    return { ...h, entry, offset: entry?.offset ?? '' }
  })
)

const isUnverified = (stateId) => UNVERIFIED_OFFSET_STATES.includes(stateId)

const replaceEntry = (hookId, updated) => {
  const hook = hooks.value.find((h) => h.hookId === hookId)
  if (!hook) return
  hook.offsets = hook.offsets.map((o) => (o.gameVersionId === updated.gameVersionId ? updated : o))
}

const formatDate = (date) => (date ? format(new Date(date), 'PP') : '')

const fetchUser = async () => {
  try {
    const res = await api.get('/auth/me')
    user.value = res.data
  } catch (err) {
    console.error('Failed to fetch user info:', err)
  }
}

const fetchHooks = async () => {
  try {
    const res = await api.get('/hooks')
    hooks.value = res.data
  } catch (err) {
    console.error('Failed to fetch hooks:', err)
  }
}

const fetchGameVersions = async () => {
  try {
    const res = await api.get('/game-versions')
    gameVersions.value = res.data
    selectedVersionId.value = res.data.find((v) => v.isLatest)?.gameVersionId ?? null
  } catch (err) {
    console.error('Failed to fetch game versions:', err)
  }
}

const fetchHookableStatuses = async () => {
  try {
    const res = await api.get('/hookablestatuses')
    hookableStatuses.value = res.data
    hookableStatusMap.value = hookableStatuses.value.reduce((acc, status) => {
      acc[status.hookableStatusId] = status.name
      return acc
    }, {})
  } catch (err) {
    console.error('Failed to fetch hookable statuses:', err)
  }
}

onMounted(async () => {
  await fetchUser()
  await Promise.all([fetchHookableStatuses(), fetchGameVersions(), fetchHooks()])
})
</script>

<style scoped>
.page-title {
  font-weight: bold;
}

.col-fit {
  width: fit-content;
}

.mono {
  font-family: monospace;
}

/* App.vue caps dark-table cells at 150px with an ellipsis; the offset plus its pill needs the room. */
:deep(.dark-table .offset-cell),
:deep(.dark-table .expanded-cell) {
  max-width: none;
  overflow: visible;
  text-overflow: clip;
}

/* The picker sits on the same baseline as the "Offset" label. */
.offset-header {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}
.version-select {
  width: 80px;
  font-size: 0.85rem;
  flex: 0 0 auto;
}
.version-select :deep(.v-input__control),
.version-select :deep(.v-field),
.version-select :deep(.v-field__field),
.version-select :deep(.v-field__input) {
  min-height: 0;
  height: auto;
  padding-top: 0;
  padding-bottom: 0;
  align-items: center;
}
.version-select :deep(.v-field__append-inner) {
  padding-top: 0;
  align-items: center;
}

.no-offset {
  opacity: 0.5;
  font-style: italic;
}

/* Hookable pill styling */
.hookable-pill {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 0.8rem;
  color: white;
  font-weight: 500;
  text-align: center;
}
.status-1 {
  background-color: #fbc02d;
  color: black;
}
.status-2 {
  background-color: #c62828;
}
.status-3 {
  background-color: #2e7d32;
}

.expanded-row td {
  background-color: #181818;
  padding: 8px 16px 8px 48px;
  white-space: normal;
}
.version-table {
  border-collapse: collapse;
}
.version-table td {
  padding: 2px 18px 2px 0;
  background: none;
  font-size: 0.85rem;
}
.version-name {
  font-weight: bold;
}
.version-date {
  opacity: 0.6;
}
</style>

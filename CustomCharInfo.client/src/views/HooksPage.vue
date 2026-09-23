<template>
  <PageShell title="Hooks" tier="wide">
    <template #subnav>
      <SubNav section="movesets" label="Movesets">
        <template #actions>
          <AppButton v-if="canConfirm" :to="{ name: 'AddHook' }" icon="mdi-plus" size="sm">
            Submit a hook
          </AppButton>
        </template>
      </SubNav>
    </template>

    <div class="hooks-toolbar">
      <LabeledField label="Search" class="hooks-toolbar__search">
        <v-text-field
          v-model="search"
          placeholder="Offset or description"
          density="compact"
          hide-details
          clearable
          prepend-inner-icon="mdi-magnify"
        />
      </LabeledField>
      <LabeledField label="Game version">
        <v-select
          v-model="selectedVersionId"
          :items="gameVersions"
          item-title="name"
          item-value="gameVersionId"
          density="compact"
          hide-details
        />
      </LabeledField>
      <LabeledField label="Hookable">
        <v-select
          v-model="hookableFilter"
          :items="hookableStatuses"
          item-title="name"
          item-value="hookableStatusId"
          placeholder="Any"
          clearable
          density="compact"
          hide-details
        />
      </LabeledField>
      <p class="hooks-toolbar__count">{{ filteredHooks.length }} hooks</p>
    </div>

    <SkeletonTable
      v-if="loading"
      :headers="['Offset', 'State', 'Description', 'Hookable', 'Used by', '']"
      :columns="[1.2, 1, 3, 1.2, 0.8, 0.4]"
      :rows="8"
    />

    <template v-else>
      <TableScroll v-if="hooks.length" min-width="820px">
        <v-data-table
          v-model:expanded="expanded"
          :headers="headers"
          :items="filteredHooks"
          :search="search"
          :sort-by="defaultSort"
          :items-per-page="25"
          item-value="hookId"
          show-expand
          class="hooks-table"
          @update:expanded="loadUsedBy"
        >
          <template #header.offset>
            <span class="offset-header">
              Offset
              <HudReadout label="v" :value="selectedVersionName" tone="info" />
            </span>
          </template>

          <template #item.offset="{ item }">
            <span v-if="item.entry" class="mono">{{ formatOffset(item.entry.offset) }}</span>
            <span v-else class="faint" :title="`No offset recorded for ${selectedVersionName}`">
              none
            </span>
          </template>

          <template #item.offsetStateId="{ item }">
            <OffsetStatePill
              v-if="item.entry"
              :entry="item.entry"
              :hook-id="item.hookId"
              :can-confirm="canConfirm"
              @confirmed="(updated) => replaceEntry(item.hookId, updated)"
            />
          </template>

          <template #item.hookableStatusId="{ item }">
            <StatusTag :variant="hookableTone[item.hookableStatusId] ?? 'neutral'">
              {{ hookableStatusMap[item.hookableStatusId] || 'Unknown' }}
            </StatusTag>
          </template>

          <template #item.usedByCount="{ item }">
            <button
              type="button"
              class="usedby"
              :title="`Show the movesets using ${formatOffset(item.offset)}`"
              @click="toggleExpanded(item.hookId)"
            >
              {{ item.usedByCount }}
            </button>
          </template>

          <template #item.actions="{ item }">
            <AppButton
              v-if="canConfirm"
              :to="{ name: 'EditHook', params: { hookId: item.hookId } }"
              variant="ghost"
              size="sm"
              icon="mdi-pencil"
              aria-label="Edit hook"
            />
          </template>

          <template #expanded-row="{ columns, item }">
            <tr class="expanded-row">
              <td :colspan="columns.length" class="expanded-cell">
                <div class="expanded">
                  <div class="expanded__block">
                    <h3 class="expanded__title">Every version</h3>
                    <table class="version-table">
                      <tbody>
                        <tr v-for="entry in item.offsets" :key="entry.gameVersionId">
                          <td class="version-name">{{ entry.gameVersion }}</td>
                          <td class="mono">{{ formatOffset(entry.offset) }}</td>
                          <td>
                            <OffsetStatePill
                              :entry="entry"
                              :hook-id="item.hookId"
                              :can-confirm="canConfirm"
                              @confirmed="(updated) => replaceEntry(item.hookId, updated)"
                            />
                          </td>
                          <td class="faint">{{ formatDate(entry.updatedAt) }}</td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                  <div class="expanded__block">
                    <h3 class="expanded__title">Used by</h3>
                    <AppLoading
                      v-if="usedBy[item.hookId] === undefined"
                      size="sm"
                      label="Loading"
                    />
                    <p v-else-if="!usedBy[item.hookId].length" class="faint">
                      No moveset you can see uses this hook.
                    </p>
                    <ul v-else class="usedby-list">
                      <li v-for="m in usedBy[item.hookId]" :key="m.movesetId">
                        <router-link
                          :to="{ name: 'MovesetDetail', params: { movesetId: m.movesetId } }"
                        >
                          {{ m.moddedCharName }}
                        </router-link>
                        <span class="mono faint">{{ m.slottedId }}</span>
                      </li>
                    </ul>
                  </div>
                </div>
              </td>
            </tr>
          </template>
        </v-data-table>
      </TableScroll>
      <EmptyState v-else message="No hooks have been submitted yet." />
    </template>
  </PageShell>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { format } from 'date-fns'
import api from '@/services/api'
import { UserType, HookableStatus } from '@/globals'
import { formatOffset } from '@/services/offsets'
import PageShell from '@/components/PageShell.vue'
import SubNav from '@/components/SubNav.vue'
import AppButton from '@/components/AppButton.vue'
import AppLoading from '@/components/AppLoading.vue'
import LabeledField from '@/components/LabeledField.vue'
import HudReadout from '@/components/HudReadout.vue'
import StatusTag from '@/components/StatusTag.vue'
import TableScroll from '@/components/TableScroll.vue'
import SkeletonTable from '@/components/SkeletonTable.vue'
import EmptyState from '@/components/EmptyState.vue'
import OffsetStatePill from '@/components/OffsetStatePill.vue'

const user = ref(null)
const hooks = ref([])
const hookableStatuses = ref([])
const hookableStatusMap = ref({})
const gameVersions = ref([])
const selectedVersionId = ref(null)
const hookableFilter = ref(null)
const search = ref('')
const expanded = ref([])
const usedBy = ref({})
const loading = ref(true)

const hookableTone = {
  [HookableStatus.Untested]: 'warn',
  [HookableStatus.OnlyOnce]: 'err',
  [HookableStatus.MoreThanOnce]: 'ok',
}

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
  { title: 'State', key: 'offsetStateId', width: '1%', sortable: false },
  { title: 'Description', key: 'description' },
  { title: 'Hookable', key: 'hookableStatusId', width: '14%' },
  { title: 'Used by', key: 'usedByCount', width: '1%', align: 'end' },
  { title: '', key: 'actions', width: '1%', sortable: false },
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

const filteredHooks = computed(() =>
  hookableFilter.value == null
    ? visibleHooks.value
    : visibleHooks.value.filter((h) => h.hookableStatusId === hookableFilter.value)
)

const replaceEntry = (hookId, updated) => {
  const hook = hooks.value.find((h) => h.hookId === hookId)
  if (!hook) return
  hook.offsets = hook.offsets.map((o) => (o.gameVersionId === updated.gameVersionId ? updated : o))
}

const toggleExpanded = (hookId) => {
  expanded.value = expanded.value.includes(hookId)
    ? expanded.value.filter((id) => id !== hookId)
    : [...expanded.value, hookId]
  loadUsedBy(expanded.value)
}

// The moveset list behind a count is fetched the first time its row opens.
const loadUsedBy = async (ids) => {
  for (const hookId of ids) {
    if (usedBy.value[hookId] !== undefined) continue
    try {
      const res = await api.get(`/hooks/${hookId}`)
      usedBy.value[hookId] = res.data.usedBy ?? []
    } catch {
      usedBy.value[hookId] = []
    }
  }
}

const formatDate = (date) => (date ? format(new Date(date), 'PP') : '')

const fetchUser = async () => {
  try {
    const res = await api.get('/auth/me')
    user.value = res.data
  } catch {
    user.value = null
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
  loading.value = false
})
</script>

<style scoped>
.hooks-toolbar {
  display: grid;
  grid-template-columns: 2fr 1fr 1fr auto;
  gap: 12px 16px;
  align-items: end;
  margin-bottom: 16px;
}

.hooks-toolbar__count {
  margin: 0 0 10px;
  color: var(--tx-2);
  font-size: 13px;
  white-space: nowrap;
}

.offset-header {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

.hooks-table :deep(.offset-cell) {
  white-space: nowrap;
}

.usedby {
  min-width: 34px;
  padding: 2px 8px;
  border: 1px solid var(--line-2);
  background: transparent;
  color: var(--tx);
  font: inherit;
  font-family: var(--font-mono);
  font-size: 13px;
  cursor: pointer;
  transition:
    background-color var(--dur-fast) var(--ease),
    color var(--dur-fast) var(--ease);
}

.usedby:hover {
  background: var(--white);
  color: #000;
}

.expanded-row td {
  background: var(--panel-2) !important;
  padding: 14px 16px 14px 56px;
  white-space: normal;
}

.expanded {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 24px;
}

.expanded__title {
  margin: 0 0 8px;
  font-size: 15px;
}

.version-table {
  border-collapse: collapse;
}

.version-table td {
  padding: 3px 18px 3px 0;
  background: none;
  font-size: 13px;
  border: 0 !important;
}

.version-name {
  font-weight: 600;
}

.usedby-list {
  margin: 0;
  padding: 0;
  list-style: none;
  display: flex;
  flex-direction: column;
  gap: 4px;
  font-size: 14px;
}

.usedby-list li {
  display: flex;
  gap: 10px;
  align-items: baseline;
}

@media (max-width: 959px) {
  .hooks-toolbar {
    grid-template-columns: 1fr 1fr;
  }

  .hooks-toolbar__search {
    grid-column: span 2;
  }

  .expanded {
    grid-template-columns: 1fr;
  }

  .expanded-row td {
    padding-left: 16px;
  }
}
</style>

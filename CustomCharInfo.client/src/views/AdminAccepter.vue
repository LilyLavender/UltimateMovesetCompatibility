<template>
  <v-container max-width="1200px">
    <h1 class="mb-4 page-title no-select">Action Log Manager</h1>

    <!-- Pending Admin Items -->
    <div class="mb-6">
      <h2 class="mb-2">Pending Admin Action</h2>
      <p v-if="!pendingAdminLogs.length" class="text-medium-emphasis">No items pending admin action.</p>
      <v-row v-else>
        <v-col
          v-for="log in pendingAdminLogs"
          :key="log.actionLogId"
          cols="12" sm="6" md="3"
        >
          <v-card class="pending-card pa-3 h-100 d-flex flex-column" color="#2e2e2e">
            <div class="card-info-row mb-3">
              <span class="type-chip">
                <v-icon size="16">{{ itemTypeIcon(log.itemType.itemTypeId) }}</v-icon>
                {{ itemTypeLabel(log.itemType.itemTypeId) }}
              </span>
              <span class="item-name">{{ getItemName(log) }}</span>
              <v-tooltip :text="log.acceptanceState.acceptanceStateName" location="top">
                <template #activator="{ props: tooltipProps }">
                  <span v-bind="tooltipProps" class="state-dot" :style="stateDotStyle(log.acceptanceState.acceptanceStateId)" />
                </template>
              </v-tooltip>
            </div>
            <div class="d-flex ga-2">
              <v-btn
                variant="flat"
                :class="['action-btn', pendingUserTargetState(log) === 4 ? 'pending-btn-hard' : 'pending-btn-soft']"
                style="width: 50%"
                @click="prefillForm(log, pendingUserTargetState(log))"
              >
                Pending User
              </v-btn>
              <v-btn
                variant="flat"
                class="action-btn accept-btn"
                style="width: 50%"
                @click="prefillForm(log, 5)"
              >
                Accepted
              </v-btn>
            </div>
          </v-card>
        </v-col>
      </v-row>
    </div>

    <v-divider class="mb-6" />

    <v-form @submit.prevent="submitLog">
      <v-row>
        <!-- Left column -->
        <v-col cols="12" md="5">
          <!-- Row 1: Select item -->
          <v-row>
            <!-- Title -->
            <v-col cols="12" md="4">
              <h2 class="center-entire">
                Select Item
              </h2>
            </v-col>

            <!-- Item Type -->
            <v-col cols="12" md="4">
              <v-select
                variant="outlined"
                v-model="form.itemTypeId"
                :items="itemTypes"
                item-title="label"
                item-value="value"
                label="Item Type"
                required
                hide-details
                @update:modelValue="fetchItems"
              >
                <template #selection="{ item }">
                  <v-icon size="20" class="mr-1">{{ itemTypeIcon(item.raw.value) }}</v-icon>
                  {{ item.raw.label }}
                </template>
                <template #item="{ item, props }">
                  <v-list-item v-bind="props" :title="undefined">
                    <template #title>
                      <div class="d-flex align-center ga-2">
                        <v-icon size="20">{{ itemTypeIcon(item.raw.value) }}</v-icon>
                        {{ item.raw.label }}
                      </div>
                    </template>
                  </v-list-item>
                </template>
              </v-select>
            </v-col>

            <!-- Item ID -->
            <v-col cols="12" md="4">
              <v-autocomplete
                variant="outlined"
                v-model="form.itemId"
                :items="items"
                item-title="name"
                item-value="id"
                label="Item"
                :disabled="!form.itemTypeId"
                required
                hide-details
                auto-select-first
              />
            </v-col>
          </v-row>

          <v-divider class="my-6" />

          <!-- Row 2: Acceptance -->
          <v-row>
            <!-- Title -->
            <v-col cols="12" md="4">
              <h2 class="center-entire">
                Create Log
              </h2>
            </v-col>

            <!-- Acceptance State -->
            <v-col cols="12" md="8">
              <v-select
                variant="outlined"
                v-model="form.acceptanceStateId"
                :items="acceptanceStates"
                item-title="name"
                item-value="id"
                label="Acceptance State"
                required
                hide-details
              >
                <template #selection="{ item }">
                  <span class="state-dot mr-2" :style="acceptanceStateDotStyle(item.raw.id)" />
                  {{ item.raw.name }}
                </template>
                <template #item="{ item, props }">
                  <v-list-item v-bind="props">
                    <template #prepend>
                      <span class="state-dot mr-3" :style="acceptanceStateDotStyle(item.raw.id)" />
                    </template>
                  </v-list-item>
                </template>
              </v-select>
            </v-col>
          </v-row>

          <!-- Row 3: Notes -->
          <v-row>
            <v-col cols="12">
              <v-textarea
                variant="outlined"
                v-model="form.notes"
                label="Notes"
                rows="3"
                auto-grow
              />
            </v-col>
          </v-row>

          <!-- Row 4: Submit -->
          <v-row>
            <!-- Submit -->
            <v-col cols="12" md="5">
              <v-btn type="submit" class="btn">
                <v-icon class="mr-1">mdi-file-document-plus</v-icon>
                Create Action Log
              </v-btn>
            </v-col>

            <!-- Feedback -->
            <v-col cols="12" md="7">
              <p v-if="success" class="text-green mt-3">Action Log {{ success }} submitted at {{ (new Date()).toLocaleTimeString("en-US") }}</p>
              <p v-if="error" class="text-red mt-3">{{ error }}</p>
            </v-col>
          </v-row>
        </v-col>

        <!-- Right column -->
        <v-col cols="12" md="7">
          <!-- Moveset preview -->
          <div v-if="selectedFull && form.itemTypeId === 1" class="mb-3">
            <h2 class="mb-1">Preview</h2>
            <MovesetCard :moveset="selectedFull" :canView="true" />
          </div>

          <!-- Series preview: 3x3 icon grid -->
          <div v-if="selectedFull && form.itemTypeId === 3" class="mb-3">
            <h2 class="mb-1">Preview</h2>
            <div class="series-grid-preview">
              <img
                v-for="(cell, i) in seriesGridCells"
                :key="i"
                :src="resolveIconUrl(cell?.seriesIconUrl)"
                :alt="cell?.seriesName ?? ''"
                class="series-grid-cell"
              />
            </div>
          </div>

          <!-- Action log display -->
          <h2>Action Logs</h2>
          <ActionLogGroup
            v-if="itemLogs.length"
            :logs="itemLogs"
            :isAdmin="false"
            :defaultOpen="true"
          />
        </v-col>
      </v-row>
    </v-form>
  </v-container>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import api from '@/services/api'
import ActionLogGroup from '@/components/ActionLogGroup.vue'
import MovesetCard from '@/components/MovesetCard.vue'
import seriesIconUnknown from '@/assets/series_icon_unknown.png'

const apiUrl = import.meta.env.VITE_API_URL
const resolveIconUrl = (path) => path?.startsWith('/') ? `${apiUrl}${path}` : (path ?? seriesIconUnknown)

const form = ref({
  userId: null,
  itemTypeId: null,
  itemId: null,
  acceptanceStateId: null,
  notes: ''
})

const itemTypes = [
  { label: 'Moveset', value: 1 },
  { label: 'Modder', value: 2 },
  { label: 'Series', value: 3 },
]

const acceptanceStates = [
  { id: 1, name: 'Pending Admin Action (Soft)' },
  { id: 2, name: 'Pending Admin Action (Hard)' },
  { id: 3, name: 'Pending User Action (Soft)' },
  { id: 4, name: 'Pending User Action (Hard)' },
  { id: 5, name: 'Accepted' },
  { id: 6, name: 'Rejected' },
]

const items = ref([])
const fullItemsById = ref({})
const success = ref(null)
const error = ref(null)
const itemLogs = ref([])
const loadingLogs = ref(false)
const pendingAdminLogs = ref([])

const itemTypeLabel = (id) => ({ 1: 'Moveset', 2: 'Modder', 3: 'Series' })[id] ?? '?'

const itemTypeIcon = (id) => ({ 1: 'mdi-sword', 2: 'mdi-account', 3: 'mdi-view-list' })[id] ?? 'mdi-help'

const stateDotStyle = (id) => ({
  backgroundColor: id === 2 ? 'rgb(52, 194, 241)' : 'rgb(187, 224, 236)',
})

const acceptanceStateDotStyle = (id) => ({
  backgroundColor: {
    1: 'rgb(187, 224, 236)',
    2: 'rgb(52, 194, 241)',
    3: 'rgb(241, 241, 142)',
    4: 'rgb(241, 241, 52)',
    5: 'rgb(52, 241, 52)',
    6: 'rgb(241, 52, 52)',
    7: 'rgb(52, 241, 52)',
  }[id] ?? '#888',
})

const getItemId = (log) => log.item?.movesetId ?? log.item?.modderId ?? log.item?.seriesId

const getItemName = (log) => log.item?.moddedCharName ?? log.item?.name ?? log.item?.seriesName ?? '(deleted)'

const pendingUserTargetState = (log) => log.acceptanceState.acceptanceStateId === 2 ? 4 : 3


const fetchUser = async () => {
  const res = await api.get('/auth/me')
  form.value.userId = res.data.id
}

const fetchPendingAdminLogs = async () => {
  try {
    const res = await api.get('/logs', {
      params: { acceptanceStates: [1, 2], itemTypes: [1, 2, 3], viewAll: true }
    })
    const latestMap = new Map()
    for (const log of res.data) {
      const key = `${log.itemType.itemTypeId}-${getItemId(log)}`
      const existing = latestMap.get(key)
      if (!existing || new Date(log.createdAt) > new Date(existing.createdAt)) {
        latestMap.set(key, log)
      }
    }
    pendingAdminLogs.value = Array.from(latestMap.values())
      .filter(log => [1, 2].includes(log.acceptanceState.acceptanceStateId))
  } catch (err) {
    console.error('Failed to fetch pending admin logs:', err)
  }
}

const fetchItems = async () => {
  try {
    if (form.value.itemTypeId === 1) {
      const res = await api.get('/movesets')
      const sorted = res.data.sort((a, b) => a.moddedCharName.localeCompare(b.moddedCharName))
      fullItemsById.value = Object.fromEntries(sorted.map(m => [m.movesetId, m]))
      items.value = sorted.map(m => ({ id: m.movesetId, name: m.moddedCharName }))
    } else if (form.value.itemTypeId === 2) {
      const res = await api.get('/modders')
      fullItemsById.value = {}
      items.value = res.data.sort((a, b) => a.name.localeCompare(b.name))
        .map(m => ({ id: m.modderId, name: m.name }))
    } else if (form.value.itemTypeId === 3) {
      const res = await api.get('/series')
      const sorted = res.data.sort((a, b) => a.seriesName.localeCompare(b.seriesName))
      fullItemsById.value = Object.fromEntries(sorted.map(s => [s.seriesId, s]))
      items.value = sorted.map(s => ({ id: s.seriesId, name: s.seriesName }))
    } else {
      fullItemsById.value = {}
      items.value = []
    }
  } catch (err) {
    console.error('Failed to fetch items:', err)
  }
}

const selectedFull = computed(() =>
  form.value.itemId ? (fullItemsById.value[form.value.itemId] ?? null) : null
)

const SURROUNDING_SERIES_IDS = [6, 39, 4, 11, 1, 2, 34, 20]

const seriesGridCells = computed(() => {
  if (!selectedFull.value || form.value.itemTypeId !== 3) return []
  const surrounding = SURROUNDING_SERIES_IDS.map(id => fullItemsById.value[id] ?? null)
  const cells = Array(9).fill(null)
  cells[4] = selectedFull.value
  let j = 0
  for (let i = 0; i < 9; i++) {
    if (i !== 4) cells[i] = surrounding[j++] ?? null
  }
  return cells
})

const prefillForm = async (log, targetStateId) => {
  form.value.itemTypeId = log.itemType.itemTypeId
  form.value.itemId = null
  await fetchItems()
  form.value.itemId = getItemId(log)
  form.value.acceptanceStateId = targetStateId
}

const fetchItemLogs = async () => {
  if (!form.value.itemTypeId || !form.value.itemId) {
    itemLogs.value = []
    return
  }

  loadingLogs.value = true

  try {
    const res = await api.get(`/logs/${form.value.itemTypeId}-${form.value.itemId}`)
    itemLogs.value = res.data
  } catch (err) {
    console.error('Failed to fetch item logs:', err)
    itemLogs.value = []
  } finally {
    loadingLogs.value = false
  }
}

const submitLog = async () => {
  try {
    const res = await api.post('/logs', form.value)
    success.value = res.data.actionLogId
    error.value = null
    fetchItemLogs()
    fetchPendingAdminLogs()
  } catch (err) {
    success.value = false
    error.value = 'Failed to submit action log.'
    console.error(err)
  }
}

// Fetch item logs when itemId changes
watch(
  () => [form.value.itemTypeId, form.value.itemId],
  fetchItemLogs
)

onMounted(async () => {
  await fetchUser()
  fetchPendingAdminLogs()
})
</script>

<style scoped>
.page-title {
  font-size: 5em;
  margin-top: 0.5em;
}

.center-entire {
  text-align: center;
}

div:has(>.center-entire) {
  align-content: center;
}

.btn {
  text-transform: unset;
  letter-spacing: 0.009375em;
  font-size: medium;
  background-color: #2e2e2e;
  color: #e2e2e2;
}

.pending-card {
  border-radius: 12px !important;
}

.card-info-row {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.type-chip {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 0.7rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: #aaa;
  background: rgba(255,255,255,0.07);
  padding: 2px 8px 2px 6px;
  border-radius: 999px;
  white-space: nowrap;
  flex-shrink: 0;
}

.item-name {
  font-weight: 600;
  font-size: 0.95rem;
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.state-dot {
  display: inline-block;
  width: 14px;
  height: 14px;
  border-radius: 50%;
  flex-shrink: 0;
  cursor: default;
}

.action-btn {
  text-transform: none;
  font-size: 0.8rem;
  font-weight: 600;
  letter-spacing: 0.01em;
  border-radius: 8px !important;
  height: 32px !important;
}

.pending-btn-soft {
  background-color: rgba(255, 193, 7, 0.10) !important;
  color: #ffe082 !important;
}

.pending-btn-soft:hover {
  background-color: rgba(255, 193, 7, 0.20) !important;
}

.pending-btn-hard {
  background-color: rgba(255, 160, 0, 0.22) !important;
  color: #ffb300 !important;
}

.pending-btn-hard:hover {
  background-color: rgba(255, 160, 0, 0.35) !important;
}

.accept-btn {
  background-color: rgba(76, 175, 80, 0.15) !important;
  color: #81c784 !important;
}

.accept-btn:hover {
  background-color: rgba(76, 175, 80, 0.28) !important;
}

.series-grid-preview {
  display: grid;
  grid-template-columns: repeat(3, 56px);
  grid-template-rows: repeat(3, 56px);
  gap: 4px;
}

.series-grid-cell {
  width: 56px;
  height: 56px;
  object-fit: contain;
}
</style>

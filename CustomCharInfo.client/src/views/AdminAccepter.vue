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
                v-if="![ItemType.Hook, ItemType.Plugin].includes(log.itemType.itemTypeId)"
                variant="flat"
                :class="['action-btn', pendingUserTargetState(log) === AcceptanceState.PendingUserHard ? 'pending-btn-hard' : 'pending-btn-soft']"
                style="width: 50%"
                @click="prefillForm(log, pendingUserTargetState(log))"
              >
                Pending User
              </v-btn>
              <v-btn
                variant="flat"
                class="action-btn accept-btn"
                :style="{ width: [ItemType.Hook, ItemType.Plugin].includes(log.itemType.itemTypeId) ? '100%' : '50%' }"
                @click="prefillForm(log, AcceptanceState.Accepted)"
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
          <div v-if="selectedFull && form.itemTypeId === ItemType.Moveset" class="mb-3">
            <h2 class="mb-1">Preview</h2>
            <MovesetCard :moveset="selectedFull" :canView="true" />
          </div>

          <!-- Modder preview -->
          <div v-if="selectedFull && form.itemTypeId === ItemType.Modder" class="mb-3">
            <h2 class="mb-1">Preview</h2>
            <div class="modder-preview">
              <div class="modder-preview-pfp-wrap">
                <img
                  v-if="modderPfpPreview"
                  :src="modderPfpPreview"
                  class="modder-preview-pfp"
                  alt=""
                />
                <v-icon v-else size="48" style="color: #555">mdi-account</v-icon>
              </div>
              <div class="modder-preview-info">
                <span class="modder-preview-name">{{ selectedFull.name }}</span>
                <span v-if="selectedFull.bio" class="modder-preview-bio">{{ selectedFull.bio }}</span>
                <span v-if="selectedFull.discordUsername" class="modder-preview-discord">@{{ selectedFull.discordUsername }}</span>
              </div>
            </div>
          </div>

          <!-- Series preview: 3x3 icon grid -->
          <div v-if="selectedFull && form.itemTypeId === ItemType.Series" class="mb-3">
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

          <!-- Hook preview -->
          <div v-if="selectedFull && form.itemTypeId === ItemType.Hook" class="mb-3">
            <h2 class="mb-1">Preview</h2>
            <div class="hook-preview">
              <div><strong>Offset:</strong> 0x{{ selectedFull.offset }}</div>
              <div><strong>Description:</strong> {{ selectedFull.description }}</div>
              <div><strong>Status:</strong> {{ selectedFull.hookableStatus }}</div>
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
import axios from 'axios'
import api from '@/services/api'
import ActionLogGroup from '@/components/ActionLogGroup.vue'
import MovesetCard from '@/components/MovesetCard.vue'
import seriesIconUnknown from '@/assets/series_icon_unknown.png'
import { ItemType, AcceptanceState } from '@/globals'

const apiUrl = import.meta.env.VITE_API_URL
const resolveIconUrl = (path) => path?.startsWith('/') ? `${apiUrl}${path}` : (path ?? seriesIconUnknown)

const form = ref({
  userId: null,
  itemTypeId: null,
  itemId: null,
  acceptanceStateId: null,
  notes: ''
})

const itemTypes = ref([])
const acceptanceStates = ref([])

const fetchItemTypes = async () => {
  try {
    const res = await api.get('/itemtypes')
    itemTypes.value = res.data.map(t => ({ label: t.itemTypeName, value: t.itemTypeId }))
  } catch (err) {
    console.error('Failed to fetch item types:', err)
  }
}

const fetchAcceptanceStates = async () => {
  try {
    const res = await api.get('/acceptancestates')
    acceptanceStates.value = res.data
      .filter(s => s.acceptanceStateId !== AcceptanceState.AutoAccepted)
      .map(s => ({ id: s.acceptanceStateId, name: s.acceptanceStateName }))
  } catch (err) {
    console.error('Failed to fetch acceptance states:', err)
  }
}

const items = ref([])
const fullItemsById = ref({})
const success = ref(null)
const error = ref(null)
const itemLogs = ref([])
const loadingLogs = ref(false)
const pendingAdminLogs = ref([])
const modderGbPfp = ref(null)

const itemTypeLabel = (id) => ({
  [ItemType.Moveset]: 'Moveset',
  [ItemType.Modder]: 'Modder',
  [ItemType.Series]: 'Series',
  [ItemType.Hook]: 'Hook',
  [ItemType.Plugin]: 'Plugin',
})[id] ?? '?'

const itemTypeIcon = (id) => ({
  [ItemType.Moveset]: 'mdi-sword',
  [ItemType.Modder]: 'mdi-account',
  [ItemType.Series]: 'mdi-view-list',
  [ItemType.Hook]: 'mdi-hook',
  [ItemType.Plugin]: 'mdi-file-code',
})[id] ?? 'mdi-help'

const stateDotStyle = (id) => ({
  backgroundColor: id === AcceptanceState.PendingAdminHard ? 'rgb(52, 194, 241)' : 'rgb(187, 224, 236)',
})

const acceptanceStateDotStyle = (id) => ({
  backgroundColor: {
    [AcceptanceState.PendingAdminSoft]: 'rgb(187, 224, 236)',
    [AcceptanceState.PendingAdminHard]: 'rgb(52, 194, 241)',
    [AcceptanceState.PendingUserSoft]: 'rgb(241, 241, 142)',
    [AcceptanceState.PendingUserHard]: 'rgb(241, 241, 52)',
    [AcceptanceState.Accepted]: 'rgb(52, 241, 52)',
    [AcceptanceState.Rejected]: 'rgb(241, 52, 52)',
    [AcceptanceState.AutoAccepted]: 'rgb(52, 241, 52)',
  }[id] ?? '#888',
})

const getItemId = (log) => log.item?.movesetId ?? log.item?.modderId ?? log.item?.seriesId ?? log.item?.hookId ?? log.item?.pluginVersionId

const getItemName = (log) => log.item?.moddedCharName ?? log.item?.name ?? log.item?.seriesName ?? (log.item?.offset ? `0x${log.item.offset}` : undefined) ?? log.item?.label ?? '(deleted)'

const pendingUserTargetState = (log) =>
  log.acceptanceState.acceptanceStateId === AcceptanceState.PendingAdminHard
    ? AcceptanceState.PendingUserHard
    : AcceptanceState.PendingUserSoft


const fetchUser = async () => {
  const res = await api.get('/auth/me')
  form.value.userId = res.data.id
}

const fetchPendingAdminLogs = async () => {
  try {
    const res = await api.get('/logs', {
      params: {
        acceptanceStates: [AcceptanceState.PendingAdminSoft, AcceptanceState.PendingAdminHard],
        itemTypes: [ItemType.Moveset, ItemType.Modder, ItemType.Series, ItemType.Hook, ItemType.Plugin],
        viewAll: true
      }
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
      .filter(log => [AcceptanceState.PendingAdminSoft, AcceptanceState.PendingAdminHard].includes(log.acceptanceState.acceptanceStateId))
  } catch (err) {
    console.error('Failed to fetch pending admin logs:', err)
  }
}

const fetchItems = async () => {
  try {
    if (form.value.itemTypeId === ItemType.Moveset) {
      const res = await api.get('/movesets')
      const sorted = res.data.sort((a, b) => a.moddedCharName.localeCompare(b.moddedCharName))
      fullItemsById.value = Object.fromEntries(sorted.map(m => [m.movesetId, m]))
      items.value = sorted.map(m => ({ id: m.movesetId, name: m.moddedCharName }))
    } else if (form.value.itemTypeId === ItemType.Modder) {
      const res = await api.get('/modders')
      const sorted = res.data.sort((a, b) => a.name.localeCompare(b.name))
      fullItemsById.value = Object.fromEntries(sorted.map(m => [m.modderId, m]))
      items.value = sorted.map(m => ({ id: m.modderId, name: m.name }))
    } else if (form.value.itemTypeId === ItemType.Series) {
      const res = await api.get('/series')
      const sorted = res.data.sort((a, b) => a.seriesName.localeCompare(b.seriesName))
      fullItemsById.value = Object.fromEntries(sorted.map(s => [s.seriesId, s]))
      items.value = sorted.map(s => ({ id: s.seriesId, name: s.seriesName }))
    } else if (form.value.itemTypeId === ItemType.Hook) {
      const res = await api.get('/hooks')
      const sorted = res.data.sort((a, b) => a.offset.localeCompare(b.offset))
      fullItemsById.value = Object.fromEntries(sorted.map(h => [h.hookId, h]))
      items.value = sorted.map(h => ({ id: h.hookId, name: `${h.offset} - ${h.description}` }))
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

const modderPfpPreview = computed(() =>
  selectedFull.value?.pfpUrl || modderGbPfp.value || null
)

const SURROUNDING_SERIES_IDS = [6, 39, 4, 11, 1, 2, 34, 20]

const seriesGridCells = computed(() => {
  if (!selectedFull.value || form.value.itemTypeId !== ItemType.Series) return []
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

// Fetch GB pfp when a modder without a custom pfpUrl is selected
watch(selectedFull, async (modder) => {
  modderGbPfp.value = null
  if (!modder || form.value.itemTypeId !== ItemType.Modder || modder.pfpUrl) return
  if (!modder.gamebananaId) return
  try {
    const res = await axios.get(
      `https://api.gamebanana.com/Core/Item/Data?itemtype=Member&itemid=${modder.gamebananaId}&fields=Url().sHdAvatarUrl(),Url().sAvatarUrl()`
    )
    modderGbPfp.value = res.data[0] || res.data[1] || null
  } catch {
    modderGbPfp.value = null
  }
})

onMounted(async () => {
  await fetchUser()
  fetchPendingAdminLogs()
  fetchItemTypes()
  fetchAcceptanceStates()
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

.modder-preview {
  display: flex;
  align-items: center;
  gap: 12px;
  background-color: #1e1e1e;
  border-radius: 10px;
  padding: 12px;
}

.modder-preview-pfp-wrap {
  flex-shrink: 0;
  width: 72px;
  height: 72px;
  border-radius: 8px;
  overflow: hidden;
  background-color: #2e2e2e;
  display: flex;
  align-items: center;
  justify-content: center;
}

.modder-preview-pfp {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.modder-preview-info {
  display: flex;
  flex-direction: column;
  gap: 3px;
  min-width: 0;
}

.modder-preview-name {
  font-size: 1.1rem;
  font-weight: 600;
}

.modder-preview-bio {
  font-size: 0.82rem;
  color: #aaa;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.modder-preview-discord {
  font-size: 0.82rem;
  color: #7289da;
}

.hook-preview {
  display: flex;
  flex-direction: column;
  gap: 4px;
  background-color: #1e1e1e;
  border-radius: 10px;
  padding: 12px;
  font-size: 0.9rem;
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

<template>
  <PageShell
    title="Action log manager"
    tier="wide"
    :back-to="{ name: 'AdminPortal' }"
    back-label="Admin portal"
  >
    <!-- Pending admin items -->
    <SectionHeading title="Pending admin action" :count="pendingAdminLogs.length" />
    <EmptyState
      v-if="!pendingAdminLogs.length"
      message="Nothing is waiting on an admin."
      icon="mdi-check-circle-outline"
    />
    <div v-else class="pending-grid">
      <article v-for="log in pendingAdminLogs" :key="log.actionLogId" class="panel pending">
        <div class="pending__head">
          <span class="pending__kind">
            <v-icon size="15">{{ itemTypeIcon(log.itemType.itemTypeId) }}</v-icon>
            {{ itemTypeLabel(log.itemType.itemTypeId) }}
          </span>
          <StatusTag :state="log.acceptanceState.acceptanceStateId" :label="stateShort(log)" />
        </div>
        <span class="pending__name" :title="getItemName(log)">{{ getItemName(log) }}</span>
        <div class="pending__actions">
          <AppButton
            v-if="![ItemType.Hook, ItemType.Plugin].includes(log.itemType.itemTypeId)"
            size="sm"
            variant="ghost"
            icon="mdi-account-clock"
            @click="prefillForm(log, pendingUserTargetState(log))"
          >
            Pending user
          </AppButton>
          <AppButton
            size="sm"
            icon="mdi-check"
            class="pending__accept"
            @click="prefillForm(log, AcceptanceState.Accepted)"
          >
            Accept
          </AppButton>
        </div>
      </article>
    </div>

    <div class="manager">
      <!-- Left column: the form -->
      <form class="manager__form" @submit.prevent="submitLog">
        <FormSection id="select-item" title="Select item">
          <div class="form-grid">
            <LabeledField label="Item type" required>
              <v-select
                v-model="form.itemTypeId"
                :items="itemTypes"
                item-title="label"
                item-value="value"
                hide-details
                @update:model-value="fetchItems"
              >
                <template #selection="{ item }">
                  <v-icon size="18" class="mr-2">{{ itemTypeIcon(item.raw.value) }}</v-icon>
                  {{ item.raw.label }}
                </template>
                <template #item="{ item, props }">
                  <v-list-item v-bind="props" :title="undefined">
                    <template #title>
                      <div class="option-row">
                        <v-icon size="18">{{ itemTypeIcon(item.raw.value) }}</v-icon>
                        {{ item.raw.label }}
                      </div>
                    </template>
                  </v-list-item>
                </template>
              </v-select>
            </LabeledField>

            <LabeledField label="Item" required>
              <v-autocomplete
                v-model="form.itemId"
                :items="items"
                item-title="name"
                item-value="id"
                :disabled="!form.itemTypeId"
                hide-details
                auto-select-first
              />
            </LabeledField>
          </div>
        </FormSection>

        <FormSection id="create-log" title="Create log">
          <LabeledField label="Acceptance state" required>
            <v-select
              v-model="form.acceptanceStateId"
              :items="acceptanceStates"
              item-title="name"
              item-value="id"
              hide-details
            >
              <template #selection="{ item }">
                <span class="state-swatch mr-2" :style="acceptanceStateSwatch(item.raw.id)" />
                {{ item.raw.name }}
              </template>
              <template #item="{ item, props }">
                <v-list-item v-bind="props">
                  <template #prepend>
                    <span class="state-swatch mr-3" :style="acceptanceStateSwatch(item.raw.id)" />
                  </template>
                </v-list-item>
              </template>
            </v-select>
          </LabeledField>

          <LabeledField label="Notes">
            <v-textarea v-model="form.notes" rows="3" auto-grow hide-details />
          </LabeledField>

          <div class="manager__submit">
            <AppButton type="submit" variant="primary" icon="mdi-file-document-plus">
              Create action log
            </AppButton>
            <p v-if="success" class="note note--ok">
              Action log {{ success }} submitted at {{ new Date().toLocaleTimeString('en-US') }}
            </p>
            <p v-if="error" class="note note--err">{{ error }}</p>
          </div>
        </FormSection>
      </form>

      <!-- Right column: preview and history -->
      <div class="manager__side">
        <template v-if="selectedFull">
          <SectionHeading title="Preview" class="side-heading" />

          <MovesetCard
            v-if="form.itemTypeId === ItemType.Moveset"
            :moveset="selectedFull"
            :can-view="true"
          />

          <div v-else-if="form.itemTypeId === ItemType.Modder" class="panel modder-preview">
            <div class="modder-preview__pfp">
              <img v-if="modderPfpPreview" :src="modderPfpPreview" alt="" />
              <v-icon v-else size="40">mdi-account</v-icon>
            </div>
            <div class="modder-preview__info">
              <span class="modder-preview__name">{{ selectedFull.name }}</span>
              <span v-if="selectedFull.bio" class="modder-preview__bio">{{
                selectedFull.bio
              }}</span>
              <span v-if="selectedFull.discordUsername" class="modder-preview__discord"
                >@{{ selectedFull.discordUsername }}</span
              >
            </div>
          </div>

          <div v-else-if="form.itemTypeId === ItemType.Series" class="series-grid-preview">
            <img
              v-for="(cell, i) in seriesGridCells"
              :key="i"
              :src="resolveIconUrl(cell?.seriesIconUrl)"
              :alt="cell?.seriesName ?? ''"
              class="series-grid-cell"
            />
          </div>

          <dl v-else-if="form.itemTypeId === ItemType.Hook" class="panel hook-preview">
            <dt>Offset</dt>
            <dd class="mono">{{ formatOffset(selectedFull.offset) }}</dd>
            <dt>Description</dt>
            <dd>{{ selectedFull.description }}</dd>
            <dt>Status</dt>
            <dd>{{ selectedFull.hookableStatus }}</dd>
          </dl>
        </template>

        <SectionHeading title="Action logs" class="side-heading" />
        <ActionLogGroup
          v-if="itemLogs.length"
          :logs="itemLogs"
          :is-admin="false"
          :default-open="true"
        />
        <p v-else class="faint">
          {{ form.itemId ? 'No logs for this item yet.' : 'Pick an item to see its history.' }}
        </p>
      </div>
    </div>
  </PageShell>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import axios from 'axios'
import api from '@/services/api'
import PageShell from '@/components/PageShell.vue'
import SectionHeading from '@/components/SectionHeading.vue'
import FormSection from '@/components/FormSection.vue'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'
import StatusTag from '@/components/StatusTag.vue'
import EmptyState from '@/components/EmptyState.vue'
import ActionLogGroup from '@/components/ActionLogGroup.vue'
import MovesetCard from '@/components/MovesetCard.vue'
import seriesIconUnknown from '@/assets/series_icon_unknown.png'
import { ItemType, AcceptanceState } from '@/globals'
import { PILL_COLORS } from '@/services/acceptanceStateDisplay'
import { formatOffset } from '@/services/offsets'

const apiUrl = import.meta.env.VITE_API_URL
const resolveIconUrl = (path) =>
  path?.startsWith('/') ? `${apiUrl}${path}` : (path ?? seriesIconUnknown)

const form = ref({
  userId: null,
  itemTypeId: null,
  itemId: null,
  acceptanceStateId: null,
  notes: '',
})

const itemTypes = ref([])
const acceptanceStates = ref([])

const fetchItemTypes = async () => {
  try {
    const res = await api.get('/itemtypes')
    itemTypes.value = res.data.map((t) => ({
      label: itemTypeLabel(t.itemTypeId),
      value: t.itemTypeId,
    }))
  } catch (err) {
    console.error('Failed to fetch item types:', err)
  }
}

const fetchAcceptanceStates = async () => {
  try {
    const res = await api.get('/acceptancestates')
    acceptanceStates.value = res.data
      .filter((s) => s.acceptanceStateId !== AcceptanceState.AutoAccepted)
      .map((s) => ({ id: s.acceptanceStateId, name: s.acceptanceStateName }))
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

const itemTypeLabel = (id) =>
  ({
    [ItemType.Moveset]: 'Moveset',
    [ItemType.Modder]: 'Modder',
    [ItemType.Series]: 'Series',
    [ItemType.Hook]: 'Hook',
    [ItemType.Plugin]: 'Plugin',
  })[id] ?? '?'

const itemTypeIcon = (id) =>
  ({
    [ItemType.Moveset]: 'mdi-sword',
    [ItemType.Modder]: 'mdi-account',
    [ItemType.Series]: 'mdi-view-list',
    [ItemType.Hook]: 'mdi-hook',
    [ItemType.Plugin]: 'mdi-file-code',
  })[id] ?? 'mdi-help'

// The card's tag says only how hard the hold is. Kind and name carry the rest
const stateShort = (log) =>
  log.acceptanceState.acceptanceStateId === AcceptanceState.PendingAdminHard ? 'Hard' : 'Soft'

const acceptanceStateSwatch = (id) => ({ backgroundColor: PILL_COLORS[id] ?? 'var(--tx-3)' })

const getItemId = (log) =>
  log.item?.movesetId ??
  log.item?.modderId ??
  log.item?.seriesId ??
  log.item?.hookId ??
  log.item?.pluginVersionId

const getItemName = (log) =>
  log.item?.moddedCharName ??
  log.item?.name ??
  log.item?.seriesName ??
  (log.item?.offset ? formatOffset(log.item.offset) : undefined) ??
  log.item?.label ??
  '(deleted)'

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
    // Every state is needed. Newest log per item decides whether it is still pending, so the filter runs below.
    const res = await api.get('/logs', {
      params: {
        itemTypes: [
          ItemType.Moveset,
          ItemType.Modder,
          ItemType.Series,
          ItemType.Hook,
          ItemType.Plugin,
        ],
        viewAll: true,
      },
    })
    const latestMap = new Map()
    for (const log of res.data) {
      const key = `${log.itemType.itemTypeId}-${getItemId(log)}`
      const existing = latestMap.get(key)
      if (!existing || new Date(log.createdAt) > new Date(existing.createdAt)) {
        latestMap.set(key, log)
      }
    }
    pendingAdminLogs.value = Array.from(latestMap.values()).filter((log) =>
      [AcceptanceState.PendingAdminSoft, AcceptanceState.PendingAdminHard].includes(
        log.acceptanceState.acceptanceStateId
      )
    )
  } catch (err) {
    console.error('Failed to fetch pending admin logs:', err)
  }
}

const fetchItems = async () => {
  try {
    if (form.value.itemTypeId === ItemType.Moveset) {
      const res = await api.get('/movesets', { params: { includeHidden: true } })
      const sorted = res.data.sort((a, b) => a.moddedCharName.localeCompare(b.moddedCharName))
      fullItemsById.value = Object.fromEntries(sorted.map((m) => [m.movesetId, m]))
      items.value = sorted.map((m) => ({ id: m.movesetId, name: m.moddedCharName }))
    } else if (form.value.itemTypeId === ItemType.Modder) {
      const res = await api.get('/modders', { params: { includeHidden: true } })
      const sorted = res.data.sort((a, b) => a.name.localeCompare(b.name))
      fullItemsById.value = Object.fromEntries(sorted.map((m) => [m.modderId, m]))
      items.value = sorted.map((m) => ({ id: m.modderId, name: m.name }))
    } else if (form.value.itemTypeId === ItemType.Series) {
      const res = await api.get('/series', { params: { includeHidden: true } })
      const sorted = res.data.sort((a, b) => a.seriesName.localeCompare(b.seriesName))
      fullItemsById.value = Object.fromEntries(sorted.map((s) => [s.seriesId, s]))
      items.value = sorted.map((s) => ({ id: s.seriesId, name: s.seriesName }))
    } else if (form.value.itemTypeId === ItemType.Hook) {
      const res = await api.get('/hooks')
      const sorted = res.data.sort((a, b) => a.offset.localeCompare(b.offset))
      fullItemsById.value = Object.fromEntries(sorted.map((h) => [h.hookId, h]))
      items.value = sorted.map((h) => ({
        id: h.hookId,
        name: `${formatOffset(h.offset)} - ${h.description}`,
      }))
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

const modderPfpPreview = computed(() => selectedFull.value?.pfpUrl || modderGbPfp.value || null)

const SURROUNDING_SERIES_IDS = [6, 39, 4, 11, 1, 2, 34, 20]

const seriesGridCells = computed(() => {
  if (!selectedFull.value || form.value.itemTypeId !== ItemType.Series) return []
  const surrounding = SURROUNDING_SERIES_IDS.map((id) => fullItemsById.value[id] ?? null)
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
watch(() => [form.value.itemTypeId, form.value.itemId], fetchItemLogs)

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
.pending-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
}

.pending {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 12px 14px;
}

.pending__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.pending__kind {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  font-weight: 600;
  color: var(--tx-3);
  letter-spacing: 0.03em;
  text-transform: uppercase;
}

.pending__name {
  font-family: var(--font-condensed);
  font-size: 17px;
  font-weight: 700;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.pending__actions {
  display: flex;
  gap: 6px;
  margin-top: 4px;
}

.pending__actions > * {
  flex: 1;
}

.pending__accept {
  border-color: var(--ok);
  color: var(--ok);
}

.pending__accept:hover {
  background: var(--ok) !important;
  border-color: var(--ok) !important;
  color: #000 !important;
}

.manager {
  display: grid;
  grid-template-columns: minmax(0, 5fr) minmax(0, 7fr);
  gap: 28px;
  margin-top: 36px;
  align-items: start;
}

.manager__form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}

.option-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.state-swatch {
  display: inline-block;
  width: 12px;
  height: 12px;
  flex: none;
}

.manager__submit {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 10px;
}

.note {
  margin: 0;
  padding: 8px 12px;
  border: 1px solid var(--line-2);
  border-left: 4px solid var(--tx-3);
  background: var(--panel-2);
  font-size: 13px;
}

.note--ok {
  border-left-color: var(--ok);
}

.note--err {
  border-left-color: var(--err);
}

.manager__side {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
}

.side-heading {
  margin-top: 0;
}

.side-heading + * {
  margin-bottom: 12px;
}

.modder-preview {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 12px 14px;
}

.modder-preview__pfp {
  flex: none;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 72px;
  height: 72px;
  overflow: hidden;
  background: var(--panel-2);
  color: var(--tx-3);
}

.modder-preview__pfp img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.modder-preview__info {
  display: flex;
  flex-direction: column;
  gap: 3px;
  min-width: 0;
}

.modder-preview__name {
  font-size: 17px;
  font-weight: 600;
}

.modder-preview__bio {
  font-size: 13px;
  color: var(--tx-2);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.modder-preview__discord {
  font-size: 13px;
  color: var(--info);
}

.hook-preview {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 6px 16px;
  margin: 0;
  padding: 12px 14px;
  font-size: 14px;
}

.hook-preview dt {
  color: var(--tx-3);
}

.hook-preview dd {
  margin: 0;
}

.series-grid-preview {
  display: grid;
  grid-template-columns: repeat(3, 56px);
  grid-template-rows: repeat(3, 56px);
  gap: 4px;
  padding: 8px;
  border: 1px solid var(--line);
  background: var(--panel);
  width: fit-content;
}

.series-grid-cell {
  width: 56px;
  height: 56px;
  object-fit: contain;
}

@media (max-width: 959px) {
  .pending-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .manager {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 599px) {
  .pending-grid,
  .form-grid {
    grid-template-columns: 1fr;
  }
}
</style>

<template>
  <div class="action-log">
    <!-- Header -->
    <div class="action-log__head">
      <div class="action-log__title-row">
        <h3 class="action-log__title">Notifications</h3>
        <div class="action-log__presets">
          <AppButton variant="ghost" size="sm" @click="selectAllFilters">Enable All</AppButton>
          <AppButton variant="ghost" size="sm" @click="selectOnlyRelevant">Only Relevant</AppButton>
        </div>
      </div>

      <div class="action-log__filters">
        <LabeledField label="Acceptance states">
          <v-select
            v-model="selectedAcceptanceStates"
            :items="acceptanceStateOptions"
            item-title="name"
            item-value="id"
            density="compact"
            multiple
            chips
            clearable
            hide-details
          />
        </LabeledField>
        <LabeledField label="Item types">
          <v-select
            v-model="selectedItemTypes"
            :items="itemTypeOptions"
            item-title="name"
            item-value="id"
            density="compact"
            multiple
            chips
            clearable
            hide-details
          />
        </LabeledField>
      </div>
    </div>

    <!-- Logs -->
    <div v-if="filteredGroups.length" class="action-log__groups">
      <ActionLogGroup
        v-for="group in filteredGroups"
        :key="group.key"
        :logs="group.logs"
        :is-admin="isAdmin && !userId"
      />
    </div>
    <EmptyState v-else message="No logs found." icon="mdi-bell-outline" />
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import api from '@/services/api'
import ActionLogGroup from '@/components/ActionLogGroup.vue'
import AppButton from '@/components/AppButton.vue'
import LabeledField from '@/components/LabeledField.vue'
import EmptyState from '@/components/EmptyState.vue'
import {
  ItemType,
  AcceptanceState,
  PENDING_ADMIN_STATES,
  PENDING_USER_STATES,
  ALL_ACCEPTANCE_STATES,
} from '@/globals'
import { isAdmin as isAdminUser } from '@/navigation'

const props = defineProps({
  viewAll: {
    type: Boolean,
    default: false,
  },
  userId: {
    type: String,
    default: null,
  },
})

const logs = ref([])
const filteredGroups = ref([])
const user = ref(null)
const isAdmin = ref(false)
const filterEnabled = ref(true)

const selectedAcceptanceStates = ref([...PENDING_ADMIN_STATES, ...PENDING_USER_STATES])
const selectedItemTypes = ref(Object.values(ItemType))

const acceptanceStateOptions = [
  { id: AcceptanceState.PendingAdminSoft, name: 'Pending Admin (Soft)' },
  { id: AcceptanceState.PendingAdminHard, name: 'Pending Admin (Hard)' },
  { id: AcceptanceState.PendingUserSoft, name: 'Pending User (Soft)' },
  { id: AcceptanceState.PendingUserHard, name: 'Pending User (Hard)' },
  { id: AcceptanceState.Accepted, name: 'Accepted' },
  { id: AcceptanceState.Rejected, name: 'Rejected' },
  { id: AcceptanceState.AutoAccepted, name: 'Auto-Accepted' },
]

const itemTypeOptions = [
  { id: ItemType.Moveset, name: 'Movesets' },
  { id: ItemType.Modder, name: 'User' },
  { id: ItemType.Series, name: 'Series' },
  { id: ItemType.Hook, name: 'Hooks' },
  { id: ItemType.Plugin, name: 'Plugins' },
]

const fetchUser = async () => {
  try {
    const res = await api.get('/auth/me')
    user.value = res.data
    isAdmin.value = isAdminUser(user.value)
  } catch (err) {
    console.error('Failed to fetch user info:', err)
  }
}

const fetchLogs = async () => {
  try {
    const params = {
      acceptanceStates: ALL_ACCEPTANCE_STATES,
      itemTypes: selectedItemTypes.value,
    }

    if (props.userId) {
      params.targetUserId = props.userId
      params.viewAll = false
    } else if (props.viewAll) {
      params.viewAll = true
    } else {
      params.viewAll = false
    }

    const res = await api.get('/logs', { params })
    logs.value = res.data
    filterLogs()
  } catch (err) {
    console.error('Failed to fetch logs:', err)
  }
}

const filterLogs = () => {
  const enabledStates = selectedAcceptanceStates.value
  const enabledItemTypes = selectedItemTypes.value

  const groupMap = new Map()
  for (const log of logs.value) {
    if (!enabledItemTypes.includes(log.itemType.itemTypeId)) continue
    const key = `${log.itemType.itemTypeId}-${log.item?.movesetId ?? log.item?.modderId ?? log.item?.seriesId ?? log.item?.hookId ?? log.item?.pluginVersionId ?? log.itemId}`
    if (!groupMap.has(key)) groupMap.set(key, [])
    groupMap.get(key).push(log)
  }

  const groups = []
  for (const [key, groupLogs] of groupMap) {
    groupLogs.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))
    const latest = groupLogs[0]
    if (filterEnabled.value && !enabledStates.includes(latest.acceptanceState.acceptanceStateId))
      continue
    groups.push({ key, logs: groupLogs })
  }

  filteredGroups.value = groups
}

// Filter helpers
const selectAllFilters = () => {
  selectedAcceptanceStates.value = acceptanceStateOptions.map((s) => s.id)
  selectedItemTypes.value = itemTypeOptions.map((t) => t.id)
}
const selectOnlyRelevant = () => {
  selectedAcceptanceStates.value = [...PENDING_ADMIN_STATES, ...PENDING_USER_STATES]
  selectedItemTypes.value = isAdmin.value
    ? [ItemType.Moveset, ItemType.Modder, ItemType.Series, ItemType.Hook, ItemType.Plugin]
    : [ItemType.Moveset, ItemType.Modder, ItemType.Series, ItemType.Plugin]
}

watch(
  () => props.userId,
  () => {
    if (props.userId) fetchLogs()
  }
)
watch(selectedAcceptanceStates, filterLogs, { deep: true })
watch(selectedItemTypes, fetchLogs, { deep: true })

onMounted(async () => {
  await fetchUser()
  await fetchLogs()
})
</script>

<style scoped>
.action-log {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.action-log__head {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.action-log__title-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 8px;
}

.action-log__title {
  margin: 0;
  font-size: 20px;
  text-transform: uppercase;
  letter-spacing: 0.01em;
}

.action-log__presets {
  display: flex;
  gap: 6px;
}

.action-log__filters {
  display: grid;
  grid-template-columns: 3fr 2fr;
  gap: 12px 16px;
}

.action-log__groups {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

@media (max-width: 599px) {
  .action-log__filters {
    grid-template-columns: 1fr;
  }
}
</style>

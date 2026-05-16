<template>
  <div>
    <v-container>
      <!-- Header -->
      <v-row class="mb--1">
        <!-- Title -->
        <v-col cols="12" sm="4">
          <div class="d-flex align-center flex-column">
            <h1>Notification Dashboard</h1>

            <div class="d-flex ga-2">
              <v-btn
                size="small"
                variant="tonal"
                @click="selectAllFilters"
              >
                Enable All
              </v-btn>

              <v-btn
                size="small"
                variant="tonal"
                @click="selectOnlyRelevant"
              >
                Only Relevant
              </v-btn>
            </div>
          </div>
        </v-col>

        <!-- Acceptance States -->
        <v-col cols="12" sm="5">
          <v-select
            variant="outlined"
            v-model="selectedAcceptanceStates"
            :items="acceptanceStateOptions"
            item-title="name"
            item-value="id"
            label="Acceptance States"
            multiple
            chips
            clearable
          />
        </v-col>

        <!-- Item Types -->
        <v-col cols="12" sm="3">
          <v-select
            variant="outlined"
            v-model="selectedItemTypes"
            :items="itemTypeOptions"
            item-title="name"
            item-value="id"
            label="Item Types"
            multiple
            chips
            clearable
          />
        </v-col>
      </v-row>

      <!-- Logs -->
      <v-row v-if="filteredGroups.length">
        <v-col
          v-for="group in filteredGroups"
          :key="group.key"
          cols="12"
        >
          <ActionLogGroup :logs="group.logs" :isAdmin="isAdmin && !userId" />
        </v-col>
      </v-row>
      <p v-else>No logs found.</p>
    </v-container>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import api from '@/services/api'
import ActionLogGroup from '@/components/ActionLogGroup.vue'

const props = defineProps({
  viewAll: {
    type: Boolean,
    default: false
  },
  userId: {
    type: String,
    default: null
  }
})

const logs = ref([])
const filteredGroups = ref([])
const user = ref(null)
const isAdmin = ref(false)
const filterEnabled = ref(true)

const selectedAcceptanceStates = ref([1, 2, 3, 4])
const selectedItemTypes = ref([1, 2, 3])

const acceptanceStateOptions = [
  { id: 1, name: 'Pending Admin (Soft)' },
  { id: 2, name: 'Pending Admin (Hard)' },
  { id: 3, name: 'Pending User (Soft)' },
  { id: 4, name: 'Pending User (Hard)' },
  { id: 5, name: 'Accepted' },
  { id: 6, name: 'Rejected' },
  { id: 7, name: 'Auto-Accepted' }
]

const itemTypeOptions = [
  { id: 1, name: 'Movesets' },
  { id: 2, name: 'User' },
  { id: 3, name: 'Series' }
]

const fetchUser = async () => {
  try {
    const res = await api.get('/auth/me')
    user.value = res.data
    isAdmin.value = user.value.userTypeId === 3
  } catch (err) {
    console.error('Failed to fetch user info:', err)
  }
}

const fetchLogs = async () => {
  try {
    const params = {
      acceptanceStates: [1, 2, 3, 4, 5, 6, 7],
      itemTypes: selectedItemTypes.value
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
    const key = `${log.itemType.itemTypeId}-${log.item?.movesetId ?? log.item?.modderId ?? log.item?.seriesId ?? log.itemId}`
    if (!groupMap.has(key)) groupMap.set(key, [])
    groupMap.get(key).push(log)
  }

  const groups = []
  for (const [key, groupLogs] of groupMap) {
    groupLogs.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))
    const latest = groupLogs[0]
    if (filterEnabled.value && !enabledStates.includes(latest.acceptanceState.acceptanceStateId)) continue
    groups.push({ key, logs: groupLogs })
  }

  filteredGroups.value = groups
}

// Filter helpers
const selectAllFilters = () => {
  selectedAcceptanceStates.value = acceptanceStateOptions.map(s => s.id)
  selectedItemTypes.value = itemTypeOptions.map(t => t.id)
}
const selectOnlyRelevant = () => {
  selectedAcceptanceStates.value = [1, 2, 3, 4]
  // selectedItemTypes.value = [1, 2, 3]
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
.admin-link {
  background-color: rgba(255, 255, 255, 0.2);
  padding: 3px;
  height: 32px;
  margin-top: 3px;
  border-radius: 4px;
}

.mb--1 {
  margin-bottom: -2em;
}
</style>

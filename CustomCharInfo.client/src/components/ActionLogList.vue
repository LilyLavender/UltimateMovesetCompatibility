<template>
  <div>
    <v-container>
      <!-- Checkbox -->
      <v-checkbox
        v-model="filterEnabled"
        label="Only show relevant"
        inset
        hide-details
        color="primary"
      />

      <!-- Logs -->
      <v-row v-if="filteredLogs.length">
        <v-col
          v-for="log in filteredLogs"
          :key="log.actionLogId"
          cols="12"
        >
          <ActionLogItem :log="log" :isAdmin="isAdmin && !userId" />
        </v-col>
      </v-row>
      <p v-else>No logs found.</p>
    </v-container>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import api from '@/services/api'
import ActionLogItem from '@/components/ActionLogItem.vue'

const props = defineProps({
  userId: {
    type: String,
    default: null
  }
})

const logs = ref([])
const filteredLogs = ref([])
const user = ref(null)
const isAdmin = ref(false)
const filterEnabled = ref(true)

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
    const res = await api.get('/logs', {
      params: props.userId ? { userId: props.userId } : {}
    })
    logs.value = res.data
    filterLogs()
  } catch (err) {
    console.error('Failed to fetch logs:', err)
  }
}

const filterLogs = () => {
  const latestMap = new Map()
  for (const log of logs.value) {
    const key = `${log.itemType.itemTypeId}-${log.item?.movesetId ?? log.item?.modderId ?? log.item?.seriesId ?? log.itemId}`
    const existing = latestMap.get(key)
    if (!existing || new Date(log.createdAt) > new Date(existing.createdAt)) {
      latestMap.set(key, log)
    }
  }

  const latestLogs = Array.from(latestMap.values())
  filteredLogs.value = filterEnabled.value
    ? latestLogs.filter(log => [1, 2, 3, 4].includes(log.acceptanceState.acceptanceStateId))
    : latestLogs
}

watch(filterEnabled, filterLogs)
watch(() => props.userId, fetchLogs)

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
</style>

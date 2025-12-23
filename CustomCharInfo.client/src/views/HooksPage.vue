<template>
  <v-container max-width="1080px">
    <!-- Page title -->
    <h1 class="mb-4 page-title">Hooks</h1>

    <!-- Add hook button -->
    <div 
      v-if="user && user.userTypeId >= 2"
      class="mb-5 pb-5"
    >
      <router-link
        :to="{ name: 'AddHook' }"
        class="unvisitable text-decoration-none"
      >
        <v-icon>mdi-plus</v-icon>
        Add Hook
      </router-link>
    </div>

    <!-- Hooks Table -->
    <v-data-table
      v-if="hooks.length"
      :headers="headers"
      :items="hooks"
      item-key="hookId"
      class="dark-table"
      dense
    >
      <!-- Offset with 0x -->
      <template v-slot:item.offset="{ item }">
        0x{{ item.offset.toString(16).toUpperCase() }}
      </template>

      <!-- Hookable? column -->
      <template v-slot:item.hookableStatusId="{ item }">
        <span class="hookable-pill" :class="`status-${item.hookableStatusId}`">
          {{ hookableStatusMap[item.hookableStatusId] || 'Unknown' }}
        </span>
      </template>

      <!-- Actions -->
      <template v-slot:item.actions="{ item }">
        <router-link
          :to="{ name: 'EditHook', params: { hookId: item.hookId } }"
          class="text-decoration-none unvisitable"
        >
          <v-icon small>mdi-pencil</v-icon>
        </router-link>
      </template>
    </v-data-table>

    <p v-else>No hooks found.</p>
  </v-container>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'

const user = ref(null)
const hooks = ref([])
const hookableStatuses = ref([])
const hookableStatusMap = ref({})

const headers = [
  { title: 'Offset', key: 'offset', width: '1%' },
  { title: 'Description', key: 'description' },
  { title: 'Hookable?', key: 'hookableStatusId', width: '20%' },
  { title: 'Actions', key: 'actions', width: '1%', sortable: false }
]

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
  await fetchHookableStatuses()
  await fetchHooks()
})
</script>

<style scoped>
.page-title {
  font-weight: bold;
}

.col-fit {
  width: fit-content;
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
.status-1 { background-color: #fbc02d; color: black; }
.status-2 { background-color: #c62828; }
.status-3 { background-color: #2e7d32; }
</style>

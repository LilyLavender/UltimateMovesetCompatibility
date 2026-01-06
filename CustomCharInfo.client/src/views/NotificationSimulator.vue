<template>
  <v-container max-width="1200px">
    <h1 class="mb-4 page-title no-select">Notification Simulator</h1>

    <!-- User selector -->
    <v-row>
      <v-col cols="12" sm="3">
        <v-select
          v-model="selectedUserId"
          :items="users"
          item-title="user.userName"
          item-value="user.id"
          label="Select User"
          :loading="loadingUsers"
        />
      </v-col>
    </v-row>

    <!-- Logs -->
    <ActionLogList
      v-if="selectedUserId"
      :user-id="selectedUserId"
    />

    <p v-else class="text-medium-emphasis">
      Select a user to view their action logs.
    </p>
  </v-container>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'
import ActionLogList from '@/components/ActionLogList.vue'

const users = ref([])
const selectedUserId = ref(null)
const loadingUsers = ref(false)

const fetchUsers = async () => {
  loadingUsers.value = true
  try {
    const res = await api.get('/users')
    users.value = res.data.inBoth
      .slice()
      .sort((a, b) => (a.user.userName).localeCompare(b.user.userName))
  } catch (err) {
    console.error('Failed to fetch users:', err)
  } finally {
    loadingUsers.value = false
  }
}

onMounted(fetchUsers)
</script>

<style scoped>
.page-title {
  font-size: 5em;
  margin-top: 0.5em;
}  
</style>
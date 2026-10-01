<template>
  <PageShell
    title="Notification simulator"
    :back-to="{ name: 'AdminPortal' }"
    back-label="Admin portal"
    lede="See the notifications exactly as one user sees them."
  >
    <LabeledField label="User" class="user-picker">
      <v-select
        v-model="selectedUserId"
        :items="users"
        item-title="user.userName"
        item-value="user.id"
        placeholder="Pick a user"
        :loading="loadingUsers"
      />
    </LabeledField>

    <section v-if="selectedUserId" class="panel">
      <ActionLogList :user-id="selectedUserId" />
    </section>
    <EmptyState
      v-else
      message="Select a user to view their notifications."
      icon="mdi-account-search"
    />
  </PageShell>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'
import PageShell from '@/components/PageShell.vue'
import LabeledField from '@/components/LabeledField.vue'
import EmptyState from '@/components/EmptyState.vue'
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
      .sort((a, b) => a.user.userName.localeCompare(b.user.userName))
  } catch (err) {
    console.error('Failed to fetch users:', err)
  } finally {
    loadingUsers.value = false
  }
}

onMounted(fetchUsers)
</script>

<style scoped>
.user-picker {
  max-width: 360px;
  margin-bottom: 20px;
}
</style>

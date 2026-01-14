<template>
  <v-container max-width="1020px">
    <!-- Header -->
    <h1 class="mb-4 page-title no-select">Password Resets</h1>

    <!-- Error -->
    <v-alert v-if="error" type="error" class="mb-5">{{ error }}</v-alert>

    <!-- Table -->
    <v-data-table
      :items="users"
      :headers="headers"
      item-key="id"
      class="dark-table"
    >
      <template #item.actions="{ item }">
        <v-btn
          size="small"
          color="primary"
          @click="generate(item)"
        >
          Generate Reset
        </v-btn>
      </template>
    </v-data-table>

    <!-- Dialog -->
    <v-dialog v-model="dialog" max-width="600px">
      <v-card color="#2e2e2e">
        <v-card-title>Password Reset Token</v-card-title>

        <v-card-text>
          <p>Send this link to the user:</p>

          <v-text-field
            readonly
            :model-value="resetLink"
            append-inner-icon="mdi-content-copy"
            @click:append-inner="copy"
          />
        </v-card-text>

        <v-card-actions>
          <v-btn @click="dialog=false">Close</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'

const users = ref([])
const error = ref('')
const dialog = ref(false)
const resetLink = ref('')

const headers = [
  { title: 'Username', key: 'userName' },
  { title: 'Email', key: 'email' },
  { title: 'User Type', key: 'userTypeId' },
  { title: 'Actions', key: 'actions' }
]

onMounted(async () => {
  try {
    const res = await api.get('/users')
    const onlyUsers = res.data.onlyUsers ?? []
    const inBothUsers = (res.data.inBoth ?? []).map(x => x.user)
    users.value = [...onlyUsers, ...inBothUsers].sort((a, b) =>
      a.userName.localeCompare(b.userName, undefined, { sensitivity: 'base' })
    )
  } catch {
    error.value = 'Failed to load users'
  }
})

const generate = async (user) => {
  try {
    const res = await api.post('/auth/generate-password-reset', {
      userId: user.id
    })

    resetLink.value = `${window.location.origin}/UltimateMovesetCompatibility/#/reset-password?userId=${res.data.userId}&token=${encodeURIComponent(res.data.token)}`

    dialog.value = true
  } catch {
    error.value = 'Failed to generate token'
  }
}

const copy = async () => {
  await navigator.clipboard.writeText(resetLink.value)
}
</script>

<style scoped>
.page-title {
  font-size: 4em;
  margin-top: 0.5em;
}
</style>
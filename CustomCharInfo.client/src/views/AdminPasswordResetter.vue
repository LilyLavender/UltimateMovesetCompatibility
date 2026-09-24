<template>
  <PageShell
    title="Password resets"
    :back-to="{ name: 'AdminPortal' }"
    back-label="Admin portal"
    lede="Generate a one-time reset link and send it to the user yourself."
  >
    <p v-if="error" class="note note--err">{{ error }}</p>

    <SkeletonTable v-if="loading" :headers="['Username', 'Email', 'Role', '']" :rows="6" />
    <TableScroll v-else min-width="640px">
      <v-data-table :items="users" :headers="headers" item-key="id">
        <template #item.userTypeId="{ value }">
          <StatusTag :variant="roleTone(value)">{{ roleName(value) }}</StatusTag>
        </template>
        <template #item.actions="{ item }">
          <AppButton size="sm" variant="ghost" icon="mdi-lock-reset" @click="generate(item)">
            Generate reset link
          </AppButton>
        </template>
      </v-data-table>
    </TableScroll>

    <v-dialog v-bind="dialogProps" v-model="dialog" max-width="600px">
      <v-card>
        <v-card-title class="dialog-title">Password reset link</v-card-title>
        <v-card-text>
          <LabeledField label="Send this link to the user">
            <v-text-field
              :model-value="resetLink"
              append-inner-icon="mdi-content-copy"
              hide-details
              readonly
              class="mono-input"
              @click:append-inner="copy"
            />
          </LabeledField>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <AppButton variant="ghost" @click="dialog = false">Close</AppButton>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </PageShell>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'
import { UserType } from '@/globals'
import { useDialogProps } from '@/composables/useDialogProps'
import { useNotify } from '@/composables/useNotify'
import PageShell from '@/components/PageShell.vue'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'
import StatusTag from '@/components/StatusTag.vue'
import TableScroll from '@/components/TableScroll.vue'
import SkeletonTable from '@/components/SkeletonTable.vue'
const dialogProps = useDialogProps()
const notify = useNotify()

const users = ref([])
const error = ref('')
const dialog = ref(false)
const resetLink = ref('')
const loading = ref(true)

const headers = [
  { title: 'Username', key: 'userName' },
  { title: 'Email', key: 'email' },
  { title: 'Role', key: 'userTypeId' },
  { title: '', key: 'actions', sortable: false, align: 'end' },
]

const roleName = (id) =>
  ({ [UserType.User]: 'User', [UserType.Modder]: 'Modder', [UserType.Admin]: 'Admin' })[id] ?? id
const roleTone = (id) =>
  id === UserType.Admin ? 'info' : id === UserType.Modder ? 'ok' : 'neutral'

onMounted(async () => {
  try {
    const res = await api.get('/users')
    const onlyUsers = res.data.onlyUsers ?? []
    const inBothUsers = (res.data.inBoth ?? []).map((x) => x.user)
    users.value = [...onlyUsers, ...inBothUsers].sort((a, b) =>
      a.userName.localeCompare(b.userName, undefined, { sensitivity: 'base' })
    )
  } catch {
    error.value = 'Failed to load users'
  } finally {
    loading.value = false
  }
})

const generate = async (user) => {
  try {
    const res = await api.post('/auth/generate-password-reset', {
      userId: user.id,
    })

    resetLink.value = `${window.location.origin}/UltimateMovesetCompatibility/#/reset-password?userId=${res.data.userId}&token=${encodeURIComponent(res.data.token)}`

    dialog.value = true
  } catch {
    error.value = 'Failed to generate token'
  }
}

const copy = async () => {
  try {
    await navigator.clipboard.writeText(resetLink.value)
    notify.info('Link copied.')
  } catch {
    notify.warning('Could not copy to the clipboard.')
  }
}
</script>

<style scoped>
.note {
  margin: 0 0 16px;
  padding: 10px 14px;
  border: 1px solid var(--line-2);
  border-left: 4px solid var(--err);
  background: var(--panel);
  font-size: 14px;
}

.dialog-title {
  font-family: var(--font-condensed);
  font-weight: 700;
  text-transform: uppercase;
}

.mono-input :deep(input) {
  font-family: var(--font-mono);
  font-size: 13px;
}
</style>

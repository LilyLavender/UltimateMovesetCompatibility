<template>
  <PageShell
    title="All users"
    tier="wide"
    :back-to="{ name: 'AdminPortal' }"
    back-label="Admin portal"
  >
    <SectionHeading title="Full users" :count="inBoth.length" />
    <SkeletonTable
      v-if="loading"
      :headers="['Username', 'Email', 'Modder', 'Last active']"
      :rows="6"
    />
    <TableScroll v-else min-width="1200px">
      <v-data-table :items="inBoth" :headers="bothHeaders" item-key="user.id" density="comfortable">
        <template #item.user.id="{ value }">
          <span class="clamp mono" :title="value">{{ value }}</span>
        </template>
        <template #item.user.email="{ value }">
          <span class="clamp" :title="value">{{ value }}</span>
        </template>
        <template #item.modder.bio="{ value }">
          <span class="clamp" :title="value">{{ value }}</span>
        </template>
        <template #item.user.userTypeId="{ value }">
          <StatusTag :variant="roleTone(value)">{{ roleName(value) }}</StatusTag>
        </template>
      </v-data-table>
    </TableScroll>

    <SectionHeading title="Users without a modder profile" :count="onlyUsers.length" />
    <SkeletonTable v-if="loading" :headers="['Username', 'Email', 'Last active']" :rows="4" />
    <TableScroll v-else min-width="900px">
      <v-data-table :items="onlyUsers" :headers="userHeaders" item-key="id" density="comfortable">
        <template #item.id="{ value }">
          <span class="clamp mono" :title="value">{{ value }}</span>
        </template>
        <template #item.email="{ value }">
          <span class="clamp" :title="value">{{ value }}</span>
        </template>
        <template #item.userTypeId="{ value }">
          <StatusTag :variant="roleTone(value)">{{ roleName(value) }}</StatusTag>
        </template>
      </v-data-table>
    </TableScroll>

    <SectionHeading title="Modders without a user account" :count="onlyModders.length" />
    <SkeletonTable v-if="loading" :headers="['Name', 'Bio', 'GameBanana']" :rows="3" />
    <TableScroll v-else min-width="900px">
      <v-data-table
        :items="onlyModders"
        :headers="modderHeaders"
        item-key="modderId"
        density="comfortable"
      >
        <template #item.bio="{ value }">
          <span class="clamp" :title="value">{{ value }}</span>
        </template>
      </v-data-table>
    </TableScroll>
  </PageShell>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'
import { UserType } from '@/globals'
import PageShell from '@/components/PageShell.vue'
import SectionHeading from '@/components/SectionHeading.vue'
import StatusTag from '@/components/StatusTag.vue'
import TableScroll from '@/components/TableScroll.vue'
import SkeletonTable from '@/components/SkeletonTable.vue'

const onlyUsers = ref([])
const onlyModders = ref([])
const inBoth = ref([])
const loading = ref(true)

const roleName = (id) =>
  ({ [UserType.User]: 'User', [UserType.Modder]: 'Modder', [UserType.Admin]: 'Admin' })[id] ?? id
const roleTone = (id) =>
  id === UserType.Admin ? 'info' : id === UserType.Modder ? 'ok' : 'neutral'

// Table headers
const userHeaders = [
  { title: 'ID', key: 'id' },
  { title: 'Username', key: 'userName' },
  { title: 'Email', key: 'email' },
  { title: 'Role', key: 'userTypeId' },
  { title: 'Modder ID', key: 'modderId' },
  { title: 'Last active', key: 'lastActiveAt' },
  { title: 'Last IP', key: 'lastIp' },
  { title: 'IPs', key: 'ipCount', align: 'end' },
]

const modderHeaders = [
  { title: 'Modder ID', key: 'modderId' },
  { title: 'Name', key: 'name' },
  { title: 'Bio', key: 'bio' },
  { title: 'GameBanana ID', key: 'gamebananaId' },
  { title: 'User ID', key: 'userId' },
  { title: 'Discord', key: 'discordUsername' },
]

const bothHeaders = [
  { title: 'User ID', key: 'user.id' },
  { title: 'Username', key: 'user.userName' },
  { title: 'Email', key: 'user.email' },
  { title: 'Role', key: 'user.userTypeId' },
  { title: 'Modder ID', key: 'user.modderId' },
  { title: 'Modder name', key: 'modder.name' },
  { title: 'Bio', key: 'modder.bio' },
  { title: 'GameBanana ID', key: 'modder.gamebananaId' },
  { title: 'Discord', key: 'modder.discordUsername' },
  { title: 'Last active', key: 'user.lastActiveAt' },
  { title: 'Last IP', key: 'user.lastIp' },
  { title: 'IPs', key: 'user.ipCount', align: 'end' },
]

// Fetch all users on mount
onMounted(async () => {
  try {
    const res = await api.get('/users')
    onlyUsers.value = res.data.onlyUsers
    onlyModders.value = res.data.onlyModders
    inBoth.value = res.data.inBoth
  } catch (err) {
    console.error('Failed to fetch users:', err)
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.clamp {
  display: inline-block;
  max-width: 220px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  vertical-align: bottom;
}

.clamp.mono {
  max-width: 120px;
  font-size: 12px;
}
</style>

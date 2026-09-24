<template>
  <PageShell title="Account" :head="false">
    <template #subnav>
      <SubNav v-if="isLoggedIn" section="account" label="Account" />
    </template>

    <!-- Dashboard: where a modder's own things live -->
    <div v-if="isLoggedIn && user" class="dashboard">
      <DashboardTile
        v-if="user.modderId"
        :to="{ name: 'MyContent' }"
        icon="mdi-view-list"
        label="My content"
        :description="contentSummary"
        :badge="pendingCount || ''"
        large
      />
      <DashboardTile
        :to="{ name: 'MyLikes' }"
        icon="mdi-heart"
        label="My likes"
        description="Movesets you have liked"
      />
      <DashboardTile
        v-if="user.modderId"
        :to="{ name: 'ModderDetail', params: { id: user.modderId } }"
        icon="mdi-account-eye"
        label="My profile"
        description="View or edit your modder page"
      />
      <DashboardTile
        v-if="isAdmin"
        :to="{ name: 'AdminPortal' }"
        icon="mdi-shield-account"
        label="Admin portal"
        description="Review queue and site tools"
      />
    </div>

    <div class="account-grid" :class="{ 'account-grid--single': !isLoggedIn }">
      <div class="account-grid__side">
        <AuthPanel />
        <blockquote class="community-note">
          <strong>A reminder:</strong> someone's contributions to this community, no matter how
          celebrated, don't reflect their value as a person. Please treat people accordingly.
        </blockquote>
      </div>

      <section v-if="isLoggedIn" class="panel account-grid__log">
        <ActionLogList />
      </section>
    </div>
  </PageShell>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { useHead } from '@unhead/vue'
import api from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { UserType, ItemType, ALL_ACCEPTANCE_STATES, PENDING_USER_STATES } from '@/globals'
import { latestLogsByItem } from '@/services/acceptanceStateDisplay'
import PageShell from '@/components/PageShell.vue'
import SubNav from '@/components/SubNav.vue'
import DashboardTile from '@/components/DashboardTile.vue'
import AuthPanel from '@/components/AuthPanel.vue'
import ActionLogList from '@/components/ActionLogList.vue'

useHead({ title: 'UMC | Account' })

const authStore = useAuthStore()
const isLoggedIn = computed(() => authStore.isLoggedIn)
const user = computed(() => authStore.user)
const isAdmin = computed(() => user.value?.userTypeId === UserType.Admin)

const movesetCount = ref(null)
const seriesCount = ref(null)
const pendingCount = ref(0)

const contentSummary = computed(() => {
  if (movesetCount.value == null) return 'Your movesets, series, and plugins'
  const parts = [`${movesetCount.value} moveset${movesetCount.value === 1 ? '' : 's'}`]
  if (seriesCount.value) parts.push(`${seriesCount.value} series`)
  const summary = parts.join(', ')
  if (pendingCount.value) {
    return `${summary}. ${pendingCount.value} item${pendingCount.value === 1 ? '' : 's'} need${pendingCount.value === 1 ? 's' : ''} your action.`
  }
  return summary
})

// Counts for the My Content tile
async function loadCounts() {
  if (!user.value?.modderId) return
  try {
    const [movesetsRes, logsRes] = await Promise.all([
      api.get('/movesets', { params: { modderId: user.value.modderId } }),
      api.get('/logs', {
        params: {
          acceptanceStates: ALL_ACCEPTANCE_STATES,
          itemTypes: [ItemType.Moveset, ItemType.Series, ItemType.Hook, ItemType.Plugin],
        },
      }),
    ])
    movesetCount.value = movesetsRes.data.length
    const logs = logsRes.data
    seriesCount.value = latestLogsByItem(logs, ItemType.Series, (item) => item?.seriesId).size
    let pending = 0
    for (const typeId of [ItemType.Moveset, ItemType.Series, ItemType.Hook, ItemType.Plugin]) {
      for (const log of latestLogsByItem(
        logs,
        typeId,
        (item) => item?.movesetId ?? item?.seriesId ?? item?.hookId ?? item?.pluginVersionId
      ).values()) {
        if (PENDING_USER_STATES.includes(log.acceptanceState?.acceptanceStateId)) pending++
      }
    }
    pendingCount.value = pending
  } catch {
    movesetCount.value = null
  }
}

watch(
  () => user.value?.modderId,
  () => {
    loadCounts()
  },
  { immediate: true }
)
</script>

<style scoped>
.dashboard {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
  margin-bottom: 28px;
}

.account-grid {
  display: grid;
  grid-template-columns: 460px minmax(0, 1fr);
  gap: 28px;
  align-items: start;
}

.account-grid--single {
  grid-template-columns: minmax(0, 480px);
  justify-content: center;
}

.account-grid__side {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.account-grid__log {
  min-width: 0;
}

.community-note {
  margin: 0;
  padding: 10px 14px;
  border: 1px solid var(--line);
  border-left: 4px solid var(--line-2);
  background: var(--panel);
  color: var(--tx-3);
  font-size: 13px;
  line-height: 1.6;
}

.community-note strong {
  color: var(--tx-2);
}

@media (max-width: 959px) {
  .dashboard {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .account-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 599px) {
  .dashboard {
    grid-template-columns: 1fr;
  }
}
</style>

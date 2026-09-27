<template>
  <PageShell title="Account" :head="false">
    <!-- Row 1: the signed-in panel (or the login form) on the left, my content and the reminder stacked on the right -->
    <div
      class="account-top"
      :class="{ 'account-top--single': !isLoggedIn, 'account-top--no-tile': !user?.modderId }"
    >
      <AuthPanel class="account-top__panel" />
      <DashboardTile
        v-if="isLoggedIn && user?.modderId"
        :to="{ name: 'MyContent' }"
        icon="mdi-view-list"
        label="My content"
        :description="contentSummary"
        :badge="pendingCount || ''"
        large
      />
      <blockquote class="community-note">
        <strong>A reminder:</strong> someone's contributions to this community, no matter how
        celebrated, don't reflect their value as a person. Please treat people accordingly.
      </blockquote>
    </div>

    <template v-if="isLoggedIn && user">
      <!-- Row 2: the rest of the tiles -->
      <div class="dashboard">
        <DashboardTile
          v-if="user.modderId"
          :to="{ name: 'ModderDetail', params: { id: user.modderId } }"
          icon="mdi-account-eye"
          label="My profile"
          description="Your modder page as visitors see it"
        />
        <DashboardTile
          v-if="user.modderId"
          :to="{ name: 'EditModder', params: { id: user.modderId } }"
          icon="mdi-account-edit"
          label="Edit profile"
          description="Bio, picture, and links"
        />
        <DashboardTile
          :to="{ name: 'MyLikes' }"
          icon="mdi-heart"
          label="My likes"
          description="Movesets you have liked"
        />
        <DashboardTile
          v-if="isAdmin"
          :to="{ name: 'AdminPortal' }"
          icon="mdi-shield-account"
          label="Admin portal"
          description="Review queue and site tools"
        />
      </div>

      <!-- Row 3: notifications -->
      <SectionHeading title="Notifications" />
      <section class="panel">
        <ActionLogList />
      </section>
    </template>
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
import SectionHeading from '@/components/SectionHeading.vue'
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
  if (movesetCount.value == null) return 'Your movesets and series'
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
.account-top {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  grid-template-rows: auto 1fr;
  gap: 12px;
  align-items: stretch;
  margin-bottom: 12px;
}

/* The panel takes the left column for both rows, tile and reminder stack on the right */
.account-top__panel {
  grid-row: 1 / 3;
}

.account-top > .dashboard-tile {
  grid-column: 2;
  grid-row: 1;
}

.account-top > .community-note {
  grid-column: 2;
  grid-row: 2;
  align-self: start;
}

/* Without a My content tile the reminder moves up beside the panel */
.account-top--no-tile > .community-note {
  grid-row: 1;
}

.account-top--single {
  grid-template-columns: minmax(0, 480px);
  grid-template-rows: auto;
  justify-content: center;
}

.account-top--single .account-top__panel,
.account-top--single .community-note {
  grid-column: 1;
  grid-row: auto;
}

.dashboard {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
  margin-bottom: 20px;
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

  .account-top {
    grid-template-columns: 1fr;
    grid-template-rows: auto;
  }

  .account-top__panel,
  .account-top > .dashboard-tile,
  .account-top > .community-note {
    grid-column: 1;
    grid-row: auto;
  }
}

@media (max-width: 599px) {
  .dashboard {
    grid-template-columns: 1fr;
  }
}
</style>

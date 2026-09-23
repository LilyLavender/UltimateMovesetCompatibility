<template>
  <header class="site-header" :class="`site-header--${variant}`">
    <router-link to="/" class="site-logo" aria-label="UMC home">
      <img :src="umcLogo" alt="" />
    </router-link>

    <div class="nav-band">
      <nav class="nav nav--desktop" aria-label="Main">
        <template v-for="item in headerItems" :key="item.key">
          <NavDropdown
            v-if="item.menu && (!item.menuWhen || item.menuWhen(user))"
            :items="visibleItems(menus[item.menu], user)"
            :label="item.icon ? '' : item.label"
            :icon="item.icon"
            :active="section === item.key"
            :align="item.key === 'account' ? 'right' : 'left'"
          >
            <template v-if="item.key === 'account'" #trigger>
              <span class="account-icon">
                <span v-if="userPendingCount > 0" class="badge badge--user">
                  {{ userPendingCount }}
                </span>
                <v-icon>mdi-account</v-icon>
                <span v-if="adminPendingCount > 0" class="badge badge--admin">
                  {{ adminPendingCount }}
                </span>
              </span>
            </template>
          </NavDropdown>
          <router-link
            v-else
            :to="item.to"
            class="nav__item"
            :class="{ 'nav__item--active': section === item.key }"
            :aria-label="item.icon ? item.label : undefined"
          >
            <v-icon v-if="item.icon">{{ item.icon }}</v-icon>
            <template v-else>{{ item.label }}</template>
          </router-link>
        </template>
      </nav>

      <div class="nav nav--mobile">
        <router-link :to="{ name: 'UserActions' }" class="nav__item" aria-label="Account">
          <span class="account-icon">
            <span v-if="userPendingCount > 0" class="badge badge--user">{{
              userPendingCount
            }}</span>
            <v-icon>mdi-account</v-icon>
            <span v-if="adminPendingCount > 0" class="badge badge--admin">
              {{ adminPendingCount }}
            </span>
          </span>
        </router-link>
        <button type="button" class="nav__item" aria-label="Open menu" @click="drawerOpen = true">
          <v-icon>mdi-menu</v-icon>
        </button>
      </div>
    </div>

    <MobileNavDrawer
      v-model="drawerOpen"
      :user-pending="userPendingCount"
      :admin-pending="adminPendingCount"
    />
  </header>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import api from '@/services/api'
import { useAuthStore } from '@/stores/auth'
import { AcceptanceState, ALL_ACCEPTANCE_STATES } from '@/globals'
import { headerItems, menus, visibleItems, sectionOf } from '@/navigation'
import NavDropdown from '@/components/NavDropdown.vue'
import MobileNavDrawer from '@/components/MobileNavDrawer.vue'
import umcLogo from '@/assets/umc-logo.svg'

/*
  The header. `hero` keeps the transparent look over the home page collage;
  `solid` is the black bar with a hairline used on every other page.
*/
defineProps({
  variant: { type: String, default: 'solid' },
})

const route = useRoute()
const authStore = useAuthStore()
const user = computed(() => authStore.user)
const section = computed(() => sectionOf(route.name))
const drawerOpen = ref(false)

// Notification counts
const userPendingCount = ref(0)
const adminPendingCount = ref(0)

const fetchNotifications = async () => {
  if (!authStore.isLoggedIn) {
    userPendingCount.value = 0
    adminPendingCount.value = 0
    return
  }
  try {
    const res = await api.get('/logs', { params: { acceptanceStates: ALL_ACCEPTANCE_STATES } })
    const logs = res.data

    const groupMap = new Map()
    for (const log of logs) {
      const key = `${log.itemType.itemTypeId}-${log.item?.movesetId ?? log.item?.modderId ?? log.item?.seriesId ?? log.itemId}`
      if (!groupMap.has(key)) groupMap.set(key, [])
      groupMap.get(key).push(log)
    }

    let userPending = 0
    let adminPending = 0
    for (const [, groupLogs] of groupMap) {
      groupLogs.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))
      const stateId = groupLogs[0].acceptanceState.acceptanceStateId
      if (
        stateId === AcceptanceState.PendingUserSoft ||
        stateId === AcceptanceState.PendingUserHard
      ) {
        userPending++
      } else if (
        stateId === AcceptanceState.PendingAdminSoft ||
        stateId === AcceptanceState.PendingAdminHard
      ) {
        adminPending++
      }
    }

    userPendingCount.value = userPending
    adminPendingCount.value = adminPending
  } catch {
    userPendingCount.value = 0
    adminPendingCount.value = 0
  }
}

// Refetch when login state changes so the badges never wait for a reload.
watch(
  () => authStore.isLoggedIn,
  () => {
    fetchNotifications()
  }
)

onMounted(async () => {
  await fetchNotifications()
})
</script>

<style scoped>
.site-header {
  position: relative;
  z-index: 20;
  height: 56px;
}

.site-header--hero {
  height: 0;
}

.site-header--solid {
  background: var(--bg);
  border-bottom: 1px solid var(--line);
}

.site-logo {
  position: absolute;
  left: 0;
  top: 0;
  display: block;
  padding: 3px 8px;
  line-height: 0;
}

.site-logo img {
  width: 50px;
  height: 50px;
  display: block;
}

/* Skewed band on the nav. Its right offset compensates for the skew at the band's center. */
.nav-band {
  position: absolute;
  right: 0;
  top: 0;
  height: 44px;
  display: flex;
  align-items: stretch;
  padding-left: 40px;
}

.nav-band::before {
  content: '';
  position: absolute;
  top: 0;
  bottom: 0;
  left: 0;
  right: -19px;
  background: rgba(0, 0, 0, 0.8);
  transform: skewX(29deg);
  transform-origin: top left;
}

.site-header--solid .nav-band::before {
  background: var(--panel-2);
  border-bottom: 1px solid var(--line-2);
}

.nav {
  position: relative;
  display: flex;
  align-items: stretch;
  margin-right: 8px;
}

.nav--mobile {
  display: none;
}

.nav__item {
  position: relative;
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 0 14px;
  border: 0;
  background: none;
  color: var(--tx-2);
  font: inherit;
  font-weight: 500;
  font-size: 15px;
  text-decoration: none;
  cursor: pointer;
  transition: color var(--dur-fast) var(--ease);
}

.nav__item:hover,
.nav__item--active {
  color: var(--white);
}

.nav__item--active::after {
  content: '';
  position: absolute;
  left: 12px;
  right: 12px;
  bottom: 0;
  height: 3px;
  background: var(--white);
}

.account-icon {
  display: flex;
  align-items: center;
  gap: 3px;
}

.badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 18px;
  height: 18px;
  padding: 0 5px;
  font-size: 11px;
  font-weight: 700;
  line-height: 1;
  color: #141414;
}

.badge--user {
  background: var(--state-pending-user-hard);
}

.badge--admin {
  background: var(--state-pending-admin-hard);
}

@media (max-width: 959px) {
  .nav--desktop {
    display: none;
  }

  .nav--mobile {
    display: flex;
  }

  .nav-band {
    padding-left: 34px;
  }
}

@media (max-width: 599px) {
  .site-header {
    height: 52px;
  }

  .site-header--hero {
    height: 0;
  }

  .site-logo img {
    width: 44px;
    height: 44px;
  }

  .nav-band {
    height: 40px;
  }
}
</style>

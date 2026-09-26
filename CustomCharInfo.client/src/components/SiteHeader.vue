<template>
  <header class="site-header" :class="`site-header--${variant}`">
    <router-link to="/" class="site-logo" aria-label="UMC home">
      <img :src="umcLogo" alt="" />
    </router-link>

    <div class="nav-band">
      <nav ref="desktopNav" class="nav nav--desktop" aria-label="Main">
        <span ref="underline" class="nav__underline" aria-hidden="true"></span>
        <template v-for="item in headerItems" :key="item.key">
          <NavDropdown
            v-if="item.menu && (!item.menuWhen || item.menuWhen(user))"
            :items="visibleItems(menus[item.menu], user)"
            :label="item.icon ? '' : item.label"
            :icon="item.icon"
            :active="section === item.key"
            :align="item.key === 'account' ? 'right' : 'left'"
            :data-active="section === item.key || undefined"
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
            :data-active="section === item.key || undefined"
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
import { ref, computed, onMounted, onBeforeUnmount, nextTick, watch } from 'vue'
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

// The active underline is one element that slides to the current item on route change.
// It snaps into place on mount, on resize, and when the fonts finish loading.
const desktopNav = ref(null)
const underline = ref(null)
const UNDERLINE_INSET = 12

const placeUnderline = (animate) => {
  const bar = underline.value
  if (!bar) return
  const target = desktopNav.value?.querySelector('[data-active]')
  if (!animate) bar.style.transition = 'none'
  if (target) {
    bar.style.transform = `translateX(${target.offsetLeft + UNDERLINE_INSET}px)`
    bar.style.width = `${Math.max(target.offsetWidth - UNDERLINE_INSET * 2, 0)}px`
    bar.style.opacity = '1'
  } else {
    bar.style.opacity = '0'
  }
  if (!animate) {
    void bar.offsetWidth
    bar.style.transition = ''
  }
}

const snapUnderline = () => placeUnderline(false)
let navObserver = null

watch(section, async () => {
  await nextTick()
  placeUnderline(true)
})

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
    const res = await api.get('/logs/latest', {
      params: { acceptanceStates: ALL_ACCEPTANCE_STATES },
    })

    let userPending = 0
    let adminPending = 0
    for (const { acceptanceStateId: stateId } of res.data) {
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
  snapUnderline()
  window.addEventListener('resize', snapUnderline)
  if (typeof ResizeObserver !== 'undefined' && desktopNav.value) {
    navObserver = new ResizeObserver(snapUnderline)
    navObserver.observe(desktopNav.value)
  }
  document.fonts?.ready?.then(snapUnderline)
  await fetchNotifications()
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', snapUnderline)
  navObserver?.disconnect()
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

.nav__underline {
  position: absolute;
  left: 0;
  bottom: 0;
  width: 0;
  height: 3px;
  background: var(--white);
  opacity: 0;
  pointer-events: none;
  transition:
    transform var(--dur-base) var(--ease-out),
    width var(--dur-base) var(--ease-out),
    opacity var(--dur-fast) var(--ease);
  will-change: transform, width;
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

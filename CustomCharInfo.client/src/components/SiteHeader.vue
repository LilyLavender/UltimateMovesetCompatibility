<template>
  <!-- <div v-if="$vuetify.display.mobile"> -->
  <header class="umc-header">
    <div class="umc-logo-container">
      <router-link to="/">
        <v-img :src="umcLogo" alt="UMC Logo" class="umc-logo" />
      </router-link>
    </div>
    <div class="nav-bg-wrapper">
      <v-tabs v-model="activeTab" class="globalheader-util mb-0">
        <v-tab
          v-for="link in links"
          :key="link.title"
          :to="link.route"
          :value="link.route"
          class="globalheader-util__item"
        >
          <template v-if="link.isAccount">
            <div class="account-icon-wrapper">
              <span v-if="userPendingCount > 0" class="notif-badge" :style="userBadgeStyle">
                {{ userPendingCount }}
              </span>
              <v-icon>mdi-account</v-icon>
              <span v-if="adminPendingCount > 0" class="notif-badge" :style="adminBadgeStyle">
                {{ adminPendingCount }}
              </span>
            </div>
          </template>
          <template v-else-if="link.title.startsWith('mdi-')">
            <v-icon>{{ link.title }}</v-icon>
          </template>
          <template v-else>
            {{ link.title }}
          </template>
        </v-tab>
      </v-tabs>
    </div>
  </header>
</template>


<script setup>
import { useRoute } from 'vue-router';
import { ref, computed, onMounted, onUnmounted } from 'vue'
import api from '@/services/api'
import umcLogo from "@/assets/umc-logo.svg"

const links = [
  { title: "Home", route: "/" },
  { title: "Movesets", route: "/movesets" },
  { title: "Series", route: "/series" },
  { title: "Modders", route: "/modders" },
  { title: "Blog", route: "/blog" },
  { title: "mdi-account", route: "/user-actions", isAccount: true },
]

const route = useRoute();

const activeTab = computed(() => {
  const tabRoutes = links.map(link => link.route);
  return tabRoutes.includes(route.path) ? route.path : null;
});

// Notification counts
const userPendingCount = ref(0)
const adminPendingCount = ref(0)
const hasUserHard = ref(false)
const hasAdminHard = ref(false)

const userBadgeStyle = computed(() => ({
  backgroundColor: hasUserHard.value ? 'rgb(241, 241, 52)' : 'rgb(241, 241, 142)',
  color: 'rgb(20, 20, 20)',
}))

const adminBadgeStyle = computed(() => ({
  backgroundColor: hasAdminHard.value ? 'rgb(52, 194, 241)' : 'rgb(187, 224, 236)',
  color: 'rgb(20, 20, 20)',
}))

const fetchNotifications = async () => {
  const token = localStorage.getItem('token')
  if (!token) {
    userPendingCount.value = 0
    adminPendingCount.value = 0
    return
  }
  try {
    const res = await api.get('/logs', { params: { acceptanceStates: [1, 2, 3, 4, 5, 6, 7] } })
    const logs = res.data

    const groupMap = new Map()
    for (const log of logs) {
      const key = `${log.itemType.itemTypeId}-${log.item?.movesetId ?? log.item?.modderId ?? log.item?.seriesId ?? log.itemId}`
      if (!groupMap.has(key)) groupMap.set(key, [])
      groupMap.get(key).push(log)
    }

    let userSoft = 0, userHard = 0, adminSoft = 0, adminHard = 0
    for (const [, groupLogs] of groupMap) {
      groupLogs.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))
      const stateId = groupLogs[0].acceptanceState.acceptanceStateId
      if (stateId === 3) userSoft++
      else if (stateId === 4) userHard++
      else if (stateId === 1) adminSoft++
      else if (stateId === 2) adminHard++
    }

    userPendingCount.value = userSoft + userHard
    adminPendingCount.value = adminSoft + adminHard
    hasUserHard.value = userHard > 0
    hasAdminHard.value = adminHard > 0
  } catch {
    userPendingCount.value = 0
    adminPendingCount.value = 0
  }
}

const handleAuthExpired = () => {
  userPendingCount.value = 0
  adminPendingCount.value = 0
}

onMounted(async () => {
  window.addEventListener('auth:expired', handleAuthExpired)
  await fetchNotifications()
})

onUnmounted(() => {
  window.removeEventListener('auth:expired', handleAuthExpired)
})
</script>

<style scoped>
.umc-header {
  z-index: 10;
}

.umc-logo-container {
  background-color: rgba(200, 200, 200, 0.75);
  position: absolute;
  left: 0;
  padding: 4px 6px 6px 4px;
  border-bottom-right-radius: 16px;
}

.umc-logo {
  height: 60px;
  width: 60px;
}

/* Plain div wrapper — no Vuetify overflow:hidden — so ::before can bleed right freely */
.nav-bg-wrapper {
  position: absolute;
  right: 0;
  top: 0;
  height: 40px;
  display: flex;
  align-items: center;
}

/*
  The skewed background.
  `right` only needs to compensate for the skew shift at the element's center:
    shift = (height/2) * tan(skewAngle) = 20 * tan(29°) ≈ 11px
  Adding a 4px buffer → right: -15px.
  This value is purely geometric and never needs updating when nav width changes.
*/
.nav-bg-wrapper::before {
  all: unset;
  content: '';
  display: block;
  width: calc(100% + 30px);
  height: 40px;
  -webkit-transform: skewX(29deg);
  transform: skewX(29deg);
  position: absolute;
  background: rgba(0, 0, 0, 0.8);
  right: -19px;
  top: 0px;
}

.globalheader-util {
  all: unset;
  display: flex;
  -webkit-box-align: center;
  -ms-flex-align: center;
  align-items: center;
}

.globalheader-util__item {
  text-align: center;
  position: relative;
  height: 40px !important;
  padding: 0 12px;
  min-width: fit-content !important;
  font-size: 1.05rem;
  text-transform: unset;
}

.globalheader-util__item:not(:has(.mdi)) {
  width: 80px;
}

.account-icon-wrapper {
  display: flex;
  align-items: center;
  gap: 2px;
}

.notif-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  font-size: 0.8rem;
  font-weight: bold;
  line-height: 1;
  flex-shrink: 0;
}
</style>

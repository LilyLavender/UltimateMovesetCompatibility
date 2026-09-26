<template>
  <Teleport to="body">
    <transition name="drawer">
      <div v-if="modelValue" class="drawer-root">
        <div class="drawer-scrim" @click="close"></div>
        <aside
          ref="panel"
          class="drawer"
          role="dialog"
          aria-modal="true"
          aria-label="Site menu"
          tabindex="-1"
          @keydown.esc.prevent="close"
        >
          <div class="drawer__texture" aria-hidden="true"></div>
          <div class="drawer__top">
            <span class="drawer__heading">Menu</span>
            <button
              ref="closeButton"
              type="button"
              class="drawer__close"
              aria-label="Close menu"
              @click="close"
            >
              <v-icon>mdi-close</v-icon>
            </button>
          </div>

          <nav class="drawer__nav" aria-label="Main">
            <template
              v-for="item in headerItems.filter((i) => i.key !== 'account')"
              :key="item.key"
            >
              <router-link
                :to="item.to"
                class="drawer__section"
                :class="{ 'drawer__section--on': section === item.key }"
              >
                {{ item.label }}
              </router-link>
              <div v-if="item.menu" class="drawer__sub">
                <router-link
                  v-for="sub in visibleItems(menus[item.menu], user).filter((i) => i.label)"
                  :key="sub.label"
                  :to="resolveTo(sub, user)"
                  class="drawer__link"
                >
                  <v-icon v-if="sub.icon" size="14">{{ sub.icon }}</v-icon>
                  {{ sub.label }}
                </router-link>
              </div>
            </template>

            <template v-if="user">
              <span class="drawer__section drawer__section--static">Account</span>
              <div class="drawer__sub">
                <template v-for="(sub, i) in accountItems" :key="i">
                  <span v-if="sub.note" class="drawer__link drawer__link--note">
                    <v-icon v-if="sub.icon" size="14">{{ sub.icon }}</v-icon>
                    {{ sub.note }}
                  </span>
                  <button
                    v-else-if="sub.action"
                    type="button"
                    class="drawer__link"
                    @click="runAction(sub)"
                  >
                    <v-icon v-if="sub.icon" size="14">{{ sub.icon }}</v-icon>
                    {{ sub.label }}
                  </button>
                  <router-link v-else :to="resolveTo(sub, user)" class="drawer__link">
                    <v-icon v-if="sub.icon" size="14">{{ sub.icon }}</v-icon>
                    {{ sub.label }}
                    <span v-if="sub.strong && userPending" class="badge badge--user">{{
                      userPending
                    }}</span>
                    <span
                      v-if="sub.to?.name === 'AdminPortal' && adminPending"
                      class="badge badge--admin"
                      >{{ adminPending }}</span
                    >
                  </router-link>
                </template>
              </div>
            </template>
            <router-link v-else :to="{ name: 'UserActions' }" class="drawer__section">
              Log in
            </router-link>
          </nav>

          <div v-if="user" class="drawer__account">
            <v-icon size="18">mdi-account</v-icon>
            <span>{{ user.userName }}</span>
            <span class="faint">{{ roleName }}</span>
          </div>
        </aside>
      </div>
    </transition>
  </Teleport>
</template>

<script setup>
import { ref, computed, watch, nextTick, onBeforeUnmount } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { UserType } from '@/globals'
import { headerItems, menus, visibleItems, resolveTo, sectionOf } from '@/navigation'

/*
  The phone navigation: a right-side drawer over a scrim listing every section, the Movesets pages,
  and the account items. Closes on Escape, on the scrim, and after any navigation.
*/
const props = defineProps({
  modelValue: { type: Boolean, default: false },
  userPending: { type: Number, default: 0 },
  adminPending: { type: Number, default: 0 },
})
const emit = defineEmits(['update:modelValue'])

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const user = computed(() => authStore.user)
const section = computed(() => sectionOf(route.name))
const accountItems = computed(() => visibleItems(menus.account, user.value))

const roleName = computed(() => {
  if (!user.value) return ''
  if (user.value.userTypeId === UserType.Admin) return 'Admin'
  if (user.value.userTypeId === UserType.Modder) return 'Modder'
  return 'User'
})

const panel = ref(null)
const closeButton = ref(null)

const close = () => emit('update:modelValue', false)

const runAction = async (item) => {
  close()
  if (item.action === 'logout') {
    await authStore.logout()
    router.push({ name: 'Home' })
  }
}

watch(
  () => props.modelValue,
  async (open) => {
    document.body.style.overflow = open ? 'hidden' : ''
    if (open) {
      await nextTick()
      closeButton.value?.focus()
    }
  }
)

watch(
  () => route.fullPath,
  () => {
    if (props.modelValue) close()
  }
)

onBeforeUnmount(() => {
  document.body.style.overflow = ''
})
</script>

<style scoped>
.drawer-root {
  position: fixed;
  inset: 0;
  z-index: 100;
}

.drawer-scrim {
  position: absolute;
  inset: 0;
  background: rgba(0, 0, 0, 0.55);
}

.drawer {
  position: absolute;
  top: 0;
  right: 0;
  bottom: 0;
  width: min(300px, 86vw);
  display: flex;
  flex-direction: column;
  background: var(--menu);
  border-left: 1px solid var(--line-2);
  overflow-y: auto;
  outline: none;
}

.drawer__texture {
  position: absolute;
  inset: 0;
  pointer-events: none;
  background-image:
    linear-gradient(rgba(255, 255, 255, 0.07) 1px, transparent 1px),
    linear-gradient(90deg, rgba(255, 255, 255, 0.07) 1px, transparent 1px);
  background-size: 28px 28px;
  -webkit-mask-image: linear-gradient(180deg, transparent 0, #000 40%);
  mask-image: linear-gradient(180deg, transparent 0, #000 40%);
}

.drawer__top {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 12px 10px 18px;
  border-bottom: 1px solid var(--line);
}

.drawer__heading {
  font-family: var(--font-condensed);
  font-weight: 700;
  font-size: 18px;
  text-transform: uppercase;
}

.drawer__close {
  display: flex;
  padding: 6px;
  border: 0;
  background: none;
  color: var(--white);
  cursor: pointer;
}

.drawer__nav {
  position: relative;
  display: flex;
  flex-direction: column;
  padding: 8px 0;
}

.drawer__section {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 18px;
  border-left: 4px solid transparent;
  color: var(--white);
  font-family: var(--font-condensed);
  font-weight: 700;
  font-size: 24px;
  line-height: 1;
  text-transform: uppercase;
  text-decoration: none;
}

.drawer__section--on {
  border-left-color: var(--white);
  background: var(--panel-2);
}

.drawer__section--static {
  color: var(--tx-2);
  font-size: 18px;
  margin-top: 8px;
}

.drawer__sub {
  display: flex;
  flex-direction: column;
  padding: 2px 0 8px 32px;
}

.drawer__link {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 7px 0;
  border: 0;
  background: none;
  color: var(--tx-2);
  font: inherit;
  font-size: 14px;
  font-weight: 500;
  text-align: left;
  text-decoration: none;
  cursor: pointer;
}

.drawer__link .v-icon {
  color: var(--tx-3);
}

.drawer__link:hover {
  color: var(--white);
}

.drawer__link--note {
  color: var(--tx-3);
  cursor: default;
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
  color: var(--ink);
}

.badge--user {
  background: var(--state-pending-user-hard);
}

.badge--admin {
  background: var(--state-pending-admin-hard);
}

.drawer__account {
  position: relative;
  margin-top: auto;
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 18px;
  border-top: 1px solid var(--line);
  font-size: 14px;
}

.drawer-enter-active,
.drawer-leave-active {
  transition: opacity var(--dur-slow) var(--ease);
}

.drawer-enter-active .drawer,
.drawer-leave-active .drawer {
  transition: transform var(--dur-slow) var(--ease-out);
}

.drawer-enter-from,
.drawer-leave-to {
  opacity: 0;
}

.drawer-enter-from .drawer,
.drawer-leave-to .drawer {
  transform: translateX(100%);
}
</style>

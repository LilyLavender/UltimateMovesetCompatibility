<template>
  <div
    ref="root"
    class="navdrop"
    @mouseenter="hoverOpen"
    @mouseleave="hoverClose"
    @keydown.esc.prevent="close(true)"
  >
    <button
      ref="trigger"
      type="button"
      class="navdrop__trigger"
      :class="{ 'navdrop__trigger--active': active, 'navdrop__trigger--open': open }"
      aria-haspopup="menu"
      :aria-expanded="open"
      @click="toggle"
      @keydown.down.prevent="openAndFocus(0)"
    >
      <slot name="trigger">
        <v-icon v-if="icon">{{ icon }}</v-icon>
        <span v-if="label">{{ label }}</span>
      </slot>
      <v-icon size="14" class="navdrop__caret">mdi-chevron-down</v-icon>
    </button>

    <transition name="navmenu">
      <div
        v-show="open"
        class="navmenu"
        :class="`navmenu--${align}`"
        role="menu"
        @keydown.down.prevent="move(1)"
        @keydown.up.prevent="move(-1)"
      >
        <template v-for="(item, i) in items" :key="i">
          <hr v-if="item.divider" class="navmenu__divider" />
          <span v-else-if="item.note" class="navmenu__note">
            <v-icon v-if="item.icon" size="16">{{ item.icon }}</v-icon>
            {{ item.note }}
          </span>
          <button
            v-else-if="item.action"
            type="button"
            role="menuitem"
            class="navmenu__item"
            @click="runAction(item)"
          >
            <v-icon v-if="item.icon" size="16">{{ item.icon }}</v-icon>
            {{ item.label }}
          </button>
          <router-link
            v-else
            role="menuitem"
            class="navmenu__item"
            :class="{ 'navmenu__item--strong': item.strong }"
            :to="resolveTo(item, user)"
            @click="close()"
          >
            <v-icon v-if="item.icon" size="16">{{ item.icon }}</v-icon>
            {{ item.label }}
          </router-link>
        </template>
      </div>
    </transition>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { resolveTo } from '@/navigation'

/*
  Header dropdown. Opens on hover after a short delay, on click, and from the keyboard;
  closes on Escape, on an outside click, and after choosing an item. Items come from navigation.js.
*/
defineProps({
  items: { type: Array, required: true },
  label: { type: String, default: '' },
  icon: { type: String, default: '' },
  active: { type: Boolean, default: false },
  align: { type: String, default: 'left' },
})

const HOVER_OPEN_DELAY = 120
const HOVER_CLOSE_DELAY = 150

const router = useRouter()
const authStore = useAuthStore()
const user = computed(() => authStore.user)

const root = ref(null)
const trigger = ref(null)
const open = ref(false)
let openTimer = null
let closeTimer = null

const clearTimers = () => {
  clearTimeout(openTimer)
  clearTimeout(closeTimer)
}

const close = (refocus = false) => {
  clearTimers()
  open.value = false
  if (refocus) trigger.value?.focus()
}

const toggle = () => {
  clearTimers()
  open.value = !open.value
}

const hoverOpen = () => {
  clearTimers()
  openTimer = setTimeout(() => {
    open.value = true
  }, HOVER_OPEN_DELAY)
}

const hoverClose = () => {
  clearTimers()
  closeTimer = setTimeout(() => {
    open.value = false
  }, HOVER_CLOSE_DELAY)
}

const menuItems = () => Array.from(root.value?.querySelectorAll('[role="menuitem"]') ?? [])

const openAndFocus = async (index) => {
  clearTimers()
  open.value = true
  await Promise.resolve()
  const list = menuItems()
  list[index]?.focus()
}

const move = (delta) => {
  const list = menuItems()
  if (!list.length) return
  const current = list.indexOf(document.activeElement)
  const next = (current + delta + list.length) % list.length
  list[next].focus()
}

const runAction = async (item) => {
  close()
  if (item.action === 'logout') {
    await authStore.logout()
    router.push({ name: 'Home' })
  }
}

const onDocumentClick = (event) => {
  if (open.value && root.value && !root.value.contains(event.target)) close()
}

onMounted(() => document.addEventListener('click', onDocumentClick))
onBeforeUnmount(() => {
  clearTimers()
  document.removeEventListener('click', onDocumentClick)
})
</script>

<style scoped>
.navdrop {
  position: relative;
  display: flex;
  align-items: stretch;
}

.navdrop__trigger {
  position: relative;
  display: flex;
  align-items: center;
  gap: 6px;
  height: 100%;
  padding: 0 14px;
  border: 0;
  background: none;
  color: var(--tx-2);
  font: inherit;
  font-weight: 500;
  font-size: 15px;
  cursor: pointer;
  transition: color var(--dur-fast) var(--ease);
}

.navdrop__trigger:hover,
.navdrop__trigger--open,
.navdrop__trigger--active {
  color: var(--white);
}

.navdrop__trigger--active::after {
  content: '';
  position: absolute;
  left: 12px;
  right: 12px;
  bottom: 0;
  height: 3px;
  background: var(--white);
}

.navdrop__caret {
  opacity: 0.7;
  transition: transform var(--dur-fast) var(--ease);
}

.navdrop__trigger--open .navdrop__caret {
  transform: rotate(180deg);
}

.navmenu {
  position: absolute;
  top: 100%;
  min-width: 240px;
  padding: 6px 0;
  background: #050505;
  border: 1px solid var(--line-2);
  z-index: 30;
}

.navmenu--left {
  left: 0;
}

.navmenu--right {
  right: 0;
}

.navmenu__item {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
  padding: 9px 16px;
  border: 0;
  background: none;
  color: var(--tx);
  font: inherit;
  font-size: 14px;
  font-weight: 500;
  text-align: left;
  text-decoration: none;
  white-space: nowrap;
  cursor: pointer;
  transition:
    background-color var(--dur-fast) var(--ease),
    color var(--dur-fast) var(--ease);
}

.navmenu__item .v-icon {
  color: var(--tx-2);
}

.navmenu__item:hover,
.navmenu__item:focus-visible {
  background: var(--white);
  color: #000;
  outline: none;
}

.navmenu__item:hover .v-icon,
.navmenu__item:focus-visible .v-icon {
  color: #000;
}

.navmenu__item--strong {
  font-weight: 700;
  color: var(--white);
}

.navmenu__note {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 9px 16px;
  color: var(--tx-3);
  font-size: 13px;
  white-space: nowrap;
}

.navmenu__divider {
  border: 0;
  border-top: 1px solid var(--line);
  margin: 6px 0;
}

.navmenu-enter-active,
.navmenu-leave-active {
  transition:
    opacity var(--dur-base) var(--ease),
    transform var(--dur-base) var(--ease);
}

.navmenu-enter-from,
.navmenu-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}
</style>

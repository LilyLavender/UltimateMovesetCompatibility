<template>
  <nav class="subnav" :aria-label="label">
    <div ref="itemsEl" class="subnav__items">
      <span ref="fill" class="subnav__fill" aria-hidden="true"></span>
      <router-link
        v-for="item in items"
        :key="item.label"
        :to="resolveTo(item, user)"
        class="subnav__item"
        :class="{ 'subnav__item--on': isActive(item) }"
        :aria-current="isActive(item) ? 'page' : undefined"
        :data-active="isActive(item) || undefined"
      >
        <v-icon v-if="item.icon" size="16">{{ item.icon }}</v-icon>
        {{ item.label }}
      </router-link>
    </div>
    <div v-if="actions.length || $slots.actions" class="subnav__actions">
      <AppButton
        v-for="action in actions"
        :key="action.label"
        :to="resolveTo(action, user)"
        :icon="action.icon"
        size="sm"
      >
        {{ action.label }}
      </AppButton>
      <slot name="actions" />
    </div>
  </nav>
</template>

<script setup>
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { subnavs, visibleItems, resolveTo } from '@/navigation'
import AppButton from '@/components/AppButton.vue'

/*
  The strip of sibling pages under a hub title, from one entry in navigation.js.
  Items and actions are filtered by the signed-in user's role; the current route is filled white.
  The fill is one element that slides to the active item. Each page mounts its own SubNav,
  so the last fill position per section is remembered here and the new one animates from it.
*/
const lastFillBySection = new Map()

const props = defineProps({
  section: { type: String, required: true },
  label: { type: String, default: 'Section' },
})

const route = useRoute()
const authStore = useAuthStore()
const user = computed(() => authStore.user)

const config = computed(() => subnavs[props.section] ?? { items: [], actions: [] })
const items = computed(() => visibleItems(config.value.items, user.value))
const actions = computed(() =>
  visibleItems(config.value.actions, user.value).filter(
    (action) => !action.on || action.on.includes(route.name)
  )
)

const isActive = (item) => resolveTo(item, user.value)?.name === route.name

const itemsEl = ref(null)
const fill = ref(null)

const measureActive = () => {
  const target = itemsEl.value?.querySelector('[data-active]')
  if (!target) return null
  return {
    x: target.offsetLeft,
    y: target.offsetTop,
    w: target.offsetWidth,
    h: target.offsetHeight,
  }
}

const applyRect = (rect, animate) => {
  const el = fill.value
  if (!el) return
  if (!animate) el.style.transition = 'none'
  if (rect) {
    el.style.transform = `translate(${rect.x}px, ${rect.y}px)`
    el.style.width = `${rect.w}px`
    el.style.height = `${rect.h}px`
    el.style.opacity = '1'
  } else {
    el.style.opacity = '0'
  }
  if (!animate) {
    void el.offsetWidth
    el.style.transition = ''
  }
}

const placeFill = (animate) => {
  const rect = measureActive()
  applyRect(rect, animate)
  if (rect) lastFillBySection.set(props.section, rect)
}

const snapFill = () => placeFill(false)

onMounted(() => {
  const previous = lastFillBySection.get(props.section)
  const current = measureActive()
  if (previous && current && (previous.x !== current.x || previous.w !== current.w)) {
    applyRect(previous, false)
    placeFill(true)
  } else {
    snapFill()
  }
  window.addEventListener('resize', snapFill)
  document.fonts?.ready?.then(snapFill)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', snapFill)
})

watch(
  () => route.name,
  () => placeFill(true),
  { flush: 'post' }
)
watch(user, () => placeFill(false), { flush: 'post' })
</script>

<style scoped>
.subnav {
  display: flex;
  align-items: flex-end;
  gap: 16px;
  margin: 22px 0 0;
  border-bottom: 1px solid var(--line-2);
}

.subnav__items {
  position: relative;
  display: flex;
  flex: 1;
  min-width: 0;
  overflow-x: auto;
  scrollbar-width: none;
}

.subnav__fill {
  position: absolute;
  top: 0;
  left: 0;
  width: 0;
  height: 0;
  background: var(--white);
  opacity: 0;
  pointer-events: none;
  transition:
    transform var(--dur-base) var(--ease-out),
    width var(--dur-base) var(--ease-out),
    height var(--dur-base) var(--ease-out),
    opacity var(--dur-fast) var(--ease);
  will-change: transform, width;
}

.subnav__items::-webkit-scrollbar {
  display: none;
}

.subnav__item {
  position: relative;
  z-index: 1;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 9px 16px;
  margin-bottom: -1px;
  border: 1px solid transparent;
  border-bottom: 0;
  color: var(--tx-2);
  font-size: 14px;
  font-weight: 500;
  white-space: nowrap;
  text-decoration: none;
  transition:
    color var(--dur-fast) var(--ease),
    background-color var(--dur-fast) var(--ease);
}

.subnav__item:hover {
  color: var(--white);
  border-color: var(--line-2);
}

.subnav__item--on,
.subnav__item--on:hover {
  color: #000;
  border-color: transparent;
}

.subnav__actions {
  display: flex;
  gap: 8px;
  padding-bottom: 8px;
  flex: none;
}

@media (max-width: 599px) {
  .subnav {
    flex-wrap: wrap;
    gap: 8px;
  }

  .subnav__items {
    flex-basis: 100%;
  }

  .subnav__item {
    padding: 8px 12px;
    font-size: 13px;
  }

  .subnav__actions {
    order: -1;
    padding: 0 0 8px;
  }
}
</style>

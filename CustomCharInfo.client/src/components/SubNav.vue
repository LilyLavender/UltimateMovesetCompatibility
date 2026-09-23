<template>
  <nav class="subnav" :aria-label="label">
    <div class="subnav__items">
      <router-link
        v-for="item in items"
        :key="item.label"
        :to="resolveTo(item, user)"
        class="subnav__item"
        :class="{ 'subnav__item--on': isActive(item) }"
        :aria-current="isActive(item) ? 'page' : undefined"
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
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { subnavs, visibleItems, resolveTo } from '@/navigation'
import AppButton from '@/components/AppButton.vue'

/*
  The strip of sibling pages under a hub title, from one entry in navigation.js.
  Items and actions are filtered by the signed-in user's role; the current route is filled white.
*/
const props = defineProps({
  section: { type: String, required: true },
  label: { type: String, default: 'Section' },
})

const route = useRoute()
const authStore = useAuthStore()
const user = computed(() => authStore.user)

const config = computed(() => subnavs[props.section] ?? { items: [], actions: [] })
const items = computed(() => visibleItems(config.value.items, user.value))
const actions = computed(() => visibleItems(config.value.actions, user.value))

const isActive = (item) => resolveTo(item, user.value)?.name === route.name
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
  display: flex;
  flex: 1;
  min-width: 0;
  overflow-x: auto;
  scrollbar-width: none;
}

.subnav__items::-webkit-scrollbar {
  display: none;
}

.subnav__item {
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
  background: var(--white);
  color: #000;
  border-color: var(--white);
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

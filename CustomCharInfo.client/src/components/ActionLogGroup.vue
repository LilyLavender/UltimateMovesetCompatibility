<template>
  <div class="log-group">
    <div class="latest-wrapper">
      <ActionLogItem :log="latest" :is-admin="isAdmin" />
      <button
        v-if="history.length"
        type="button"
        class="history-toggle"
        :title="open ? 'Hide history' : 'Show history'"
        :aria-expanded="open"
        @click="open = !open"
      >
        <v-icon size="18">{{ open ? 'mdi-chevron-up' : 'mdi-chevron-down' }}</v-icon>
        <span class="history-toggle__count">{{ history.length }}</span>
      </button>
    </div>

    <div v-if="open && history.length" class="history-list">
      <ActionLogItem v-for="log in history" :key="log.actionLogId" :log="log" :is-admin="isAdmin" />
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import ActionLogItem from './ActionLogItem.vue'

const props = defineProps({
  logs: { type: Array, required: true },
  isAdmin: Boolean,
  defaultOpen: { type: Boolean, default: false },
})

const open = ref(props.defaultOpen)

const sorted = computed(() =>
  [...props.logs].sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))
)
const latest = computed(() => sorted.value[0])
const history = computed(() => sorted.value.slice(1))
</script>

<style scoped>
.latest-wrapper {
  position: relative;
}

.history-toggle {
  position: absolute;
  top: 8px;
  right: 8px;
  display: inline-flex;
  align-items: center;
  gap: 2px;
  padding: 2px 6px 2px 2px;
  border: 1px solid var(--line-2);
  background: var(--panel);
  color: var(--tx-2);
  font: inherit;
  font-size: 12px;
  cursor: pointer;
  transition:
    background-color var(--dur-fast) var(--ease),
    color var(--dur-fast) var(--ease);
}

.history-toggle:hover {
  background: var(--white);
  color: #000;
}

.history-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin: 6px 0 0 16px;
  padding-left: 12px;
  border-left: 2px solid var(--line-2);
  opacity: 0.85;
}
</style>

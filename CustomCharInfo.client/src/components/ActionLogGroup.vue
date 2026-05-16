<template>
  <div class="log-group">
    <div class="latest-wrapper">
      <ActionLogItem :log="latest" :isAdmin="isAdmin" />
      <v-btn
        v-if="history.length"
        icon
        size="x-small"
        variant="text"
        class="history-toggle"
        :title="open ? 'Hide history' : 'Show history'"
        @click="open = !open"
      >
        <v-icon>{{ open ? 'mdi-chevron-up' : 'mdi-chevron-down' }}</v-icon>
      </v-btn>
    </div>

    <div v-if="open && history.length" class="history-list">
      <ActionLogItem
        v-for="log in history"
        :key="log.actionLogId"
        :log="log"
        :isAdmin="isAdmin"
      />
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import ActionLogItem from './ActionLogItem.vue'

const props = defineProps({
  logs: { type: Array, required: true },
  isAdmin: Boolean,
  defaultOpen: { type: Boolean, default: false }
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
  top: 4px;
  right: 4px;
}

.history-list {
  display: flex;
  flex-direction: column;
  gap: 4px;
  margin-top: 4px;
  margin-left: 16px;
  opacity: 0.8;
}
</style>

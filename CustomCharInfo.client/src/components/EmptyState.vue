<template>
  <div class="empty">
    <v-icon v-if="icon" size="28" class="empty__icon">{{ icon }}</v-icon>
    <p class="empty__message">
      <slot>{{ message }}</slot>
    </p>
    <div v-if="$slots.action" class="empty__action"><slot name="action" /></div>
  </div>
</template>

<script setup>
/* One sentence for a list with nothing in it, with room for the action that would fill it. */
defineProps({
  message: { type: String, default: 'Nothing here yet.' },
  icon: { type: String, default: '' },
})
</script>

<style scoped>
.empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  padding: 36px 16px;
  border: 1px dashed var(--line-2);
  color: var(--tx-2);
  text-align: center;
}

.empty__icon {
  color: var(--tx-3);
}

.empty__message {
  margin: 0;
}
</style>

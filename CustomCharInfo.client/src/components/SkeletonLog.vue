<template>
  <div class="skeleton-log" aria-busy="true">
    <div v-for="r in rows" :key="r" class="skeleton-log__row">
      <Skeleton variant="line" width="55%" />
      <Skeleton variant="tag" />
      <Skeleton variant="line" width="40%" height="10px" />
      <Skeleton variant="line" width="60px" height="10px" />
    </div>
  </div>
</template>

<script setup>
import Skeleton from '@/components/Skeleton.vue'

/* Action log rows while they load: a title, a status tag, a note, and a time. */
defineProps({
  rows: { type: Number, default: 3 },
})
</script>

<style scoped>
.skeleton-log__row {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 8px 16px;
  align-items: center;
  padding: 12px 0;
  border-bottom: 1px solid var(--line);
}

.skeleton-log__row:last-child {
  border-bottom: 0;
}
</style>

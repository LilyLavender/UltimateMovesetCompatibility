<template>
  <div class="panel skeleton-panel" aria-busy="true">
    <Skeleton v-if="title" variant="title" width="45%" />
    <Skeleton variant="text" :count="lines" />
  </div>
</template>

<script setup>
import Skeleton from '@/components/Skeleton.vue'

/* A panel of text while its content loads. */
defineProps({
  title: { type: Boolean, default: true },
  lines: { type: Number, default: 3 },
})
</script>

<style scoped>
.skeleton-panel {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
</style>

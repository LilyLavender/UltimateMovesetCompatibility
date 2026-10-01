<template>
  <div class="skeleton-list" aria-busy="true">
    <Skeleton variant="card" :count="count" />
  </div>
</template>

<script setup>
import Skeleton from '@/components/Skeleton.vue'

/* The moveset card grid while its movesets load. Same grid rule as MovesetList so nothing shifts. */
defineProps({
  count: { type: Number, default: 6 },
})
</script>

<style scoped>
.skeleton-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, 340px);
  justify-content: center;
  gap: 0;
}
</style>

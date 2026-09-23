<template>
  <ToolTile
    :to="to"
    :icon="icon"
    :label="label"
    :description="description"
    :badge="badge"
    badge-tone="user"
    :large="large"
    class="dashboard-tile"
    :class="{ 'dashboard-tile--wide': large }"
  />
</template>

<script setup>
import ToolTile from '@/components/ToolTile.vue'

/* The account page's tiles. The large one is My Content and spans two columns of the row. */
defineProps({
  to: { type: [Object, String], required: true },
  icon: { type: String, default: 'mdi-arrow-right' },
  label: { type: String, required: true },
  description: { type: String, default: '' },
  badge: { type: [Number, String], default: '' },
  large: { type: Boolean, default: false },
})
</script>

<style scoped>
.dashboard-tile--wide {
  grid-column: span 2;
}

@media (max-width: 599px) {
  .dashboard-tile--wide {
    grid-column: span 1;
  }
}
</style>

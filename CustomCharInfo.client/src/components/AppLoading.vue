<template>
  <span
    class="loading"
    :class="[`loading--${size}`, { 'loading--inverted': inverted }]"
    role="status"
    :aria-label="label || 'Loading'"
  >
    <span class="loading__chevrons" aria-hidden="true"> <i></i><i></i><i></i><i></i><i></i> </span>
    <span v-if="label" class="loading__label">{{ label }}</span>
  </span>
</template>

<script setup>
/*
  The one loading indicator: five skewed blocks at the nav band's angle, lighting in sequence.
  Sizes sm (inside buttons), md, lg. `inverted` draws black chevrons for use on a white surface.
*/
defineProps({
  size: { type: String, default: 'md' },
  label: { type: String, default: '' },
  inverted: { type: Boolean, default: false },
})
</script>

<style scoped>
.loading {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  color: var(--tx-2);
  font-size: 14px;
  vertical-align: middle;
}

.loading__chevrons {
  display: inline-flex;
  gap: 4px;
}

.loading__chevrons i {
  display: block;
  width: 12px;
  height: 18px;
  background: var(--white);
  transform: skewX(-29deg);
  animation: loading-chevron 1.2s ease-in-out infinite;
}

.loading__chevrons i:nth-child(2) {
  animation-delay: 0.15s;
}

.loading__chevrons i:nth-child(3) {
  animation-delay: 0.3s;
}

.loading__chevrons i:nth-child(4) {
  animation-delay: 0.45s;
}

.loading__chevrons i:nth-child(5) {
  animation-delay: 0.6s;
}

.loading--sm .loading__chevrons {
  gap: 3px;
}

.loading--sm .loading__chevrons i {
  width: 7px;
  height: 12px;
}

.loading--sm {
  font-size: 13px;
}

.loading--lg .loading__chevrons {
  gap: 6px;
}

.loading--lg .loading__chevrons i {
  width: 18px;
  height: 28px;
}

.loading--lg {
  font-size: 15px;
}

.loading--inverted .loading__chevrons i {
  background: #000;
}

.loading--inverted {
  color: #000;
}

@keyframes loading-chevron {
  0%,
  100% {
    opacity: 0.15;
  }

  30% {
    opacity: 1;
  }
}

@media (prefers-reduced-motion: reduce) {
  .loading__chevrons i {
    animation: none;
    opacity: 0.7;
  }
}
</style>

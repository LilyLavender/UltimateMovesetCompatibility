<template>
  <span
    v-for="n in count"
    :key="n"
    class="skeleton"
    :class="`skeleton--${variant}`"
    :style="style"
    aria-hidden="true"
  ></span>
</template>

<script setup>
import { computed } from 'vue'

/*
  Skeleton loader.
  Variants: line, title, text, tag, button, card (a moveset card), image, circle.
  SkeletonList, SkeletonTable, SkeletonPanel, and SkeletonLog compose these for whole blocks.
*/
const props = defineProps({
  variant: { type: String, default: 'line' },
  count: { type: Number, default: 1 },
  width: { type: String, default: '' },
  height: { type: String, default: '' },
})

const style = computed(() => ({
  width: props.width || undefined,
  height: props.height || undefined,
}))
</script>

<style scoped>
.skeleton {
  position: relative;
  display: block;
  overflow: hidden;
  background: var(--panel-2);
}

.skeleton::after {
  content: '';
  position: absolute;
  inset: 0;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.07), transparent);
  animation: skeleton-shimmer 1.4s ease-in-out infinite;
}

.skeleton--line {
  height: 12px;
  width: 100%;
}

.skeleton--title {
  height: 26px;
  width: 60%;
}

.skeleton--text {
  height: 12px;
  width: 90%;
}

.skeleton--text + .skeleton--text {
  margin-top: 8px;
  width: 75%;
}

.skeleton--tag {
  display: inline-block;
  height: 22px;
  width: 110px;
}

.skeleton--button {
  display: inline-block;
  height: 38px;
  width: 120px;
}

.skeleton--card {
  width: 340px;
  height: 82px;
  background: #111;
  border: 1px solid var(--line);
}

.skeleton--image {
  height: 200px;
  width: 100%;
}

.skeleton--circle {
  width: 36px;
  height: 36px;
}

@keyframes skeleton-shimmer {
  0% {
    transform: translateX(-100%);
  }

  100% {
    transform: translateX(100%);
  }
}

@media (prefers-reduced-motion: reduce) {
  .skeleton::after {
    animation: none;
  }
}
</style>

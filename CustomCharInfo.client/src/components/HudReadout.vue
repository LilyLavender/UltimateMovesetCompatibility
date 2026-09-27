<template>
  <span class="hud" :style="{ '--hud-color': color }">
    <span v-if="label" class="hud__label">{{ label }}</span>
    <span class="hud__value"
      ><slot>{{ value }}</slot></span
    >
  </span>
</template>

<script setup>
import { computed } from 'vue'

/* A key and value readout with a colored left rail, for things like the game version or a like count. Without a label it is a plain rail chip. */
const props = defineProps({
  label: { type: String, default: '' },
  value: { type: [String, Number], default: '' },
  tone: { type: String, default: 'info' },
})

const color = computed(() => `var(--${props.tone})`)
</script>

<style scoped>
.hud {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  height: 24px;
  padding: 0 10px 0 8px;
  border-left: 3px solid var(--hud-color);
  background: var(--panel-2);
  font-size: 12px;
  line-height: 1;
  white-space: nowrap;
  vertical-align: middle;
}

.hud__label {
  color: var(--hud-color);
  font-weight: 700;
}

.hud__value {
  color: var(--white);
  font-weight: 600;
  font-family: var(--font-mono);
}
</style>

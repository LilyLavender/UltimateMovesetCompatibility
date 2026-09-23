<template>
  <span class="status-tag" :class="classes" :style="style">
    <v-icon v-if="icon" size="14">{{ icon }}</v-icon>
    <slot>{{ text }}</slot>
  </span>
</template>

<script setup>
import { computed } from 'vue'
import { PILL_COLORS, PILL_LABELS } from '@/services/acceptanceStateDisplay'
import { OFFSET_STATE_NAMES, OffsetState } from '@/globals'

/*
  A square status tag. Three ways to use it:
  <StatusTag :state="acceptanceStateId" /> for an acceptance state, colored and labeled from the shared display service.
  <StatusTag :offset-state="offsetStateId" /> for an offset state.
  <StatusTag variant="ok">Released</StatusTag> for any other meaning: ok, info, warn, err, neutral, outline.
*/
const props = defineProps({
  state: { type: Number, default: null },
  offsetState: { type: Number, default: null },
  variant: { type: String, default: 'neutral' },
  label: { type: String, default: '' },
  icon: { type: String, default: '' },
})

const OFFSET_CLASS = {
  [OffsetState.Confirmed]: 'status-tag--offset-confirmed',
  [OffsetState.Generated]: 'status-tag--offset-generated',
  [OffsetState.CarriedForward]: 'status-tag--offset-carried',
}

const text = computed(() => {
  if (props.label) return props.label
  if (props.state != null) return PILL_LABELS[props.state] ?? ''
  if (props.offsetState != null) return OFFSET_STATE_NAMES[props.offsetState] ?? ''
  return ''
})

const classes = computed(() => {
  if (props.state != null) return 'status-tag--state'
  if (props.offsetState != null) return OFFSET_CLASS[props.offsetState] ?? 'status-tag--neutral'
  return `status-tag--${props.variant}`
})

const style = computed(() =>
  props.state != null ? { backgroundColor: PILL_COLORS[props.state] } : undefined
)
</script>

<style scoped>
.status-tag {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  height: 22px;
  padding: 0 8px;
  font-size: 12px;
  font-weight: 600;
  line-height: 1;
  white-space: nowrap;
  color: #000;
  vertical-align: middle;
}

.status-tag--state {
  background: var(--panel-2);
}

.status-tag--ok {
  background: var(--ok);
  color: #000;
}

.status-tag--info {
  background: var(--info);
  color: #000;
}

.status-tag--warn {
  background: var(--warn);
}

.status-tag--err {
  background: var(--err);
  color: #fff;
}

.status-tag--neutral {
  background: var(--panel-2);
  color: var(--tx);
}

.status-tag--outline {
  background: transparent;
  border: 1px solid var(--line-2);
  color: var(--tx-2);
}

.status-tag--offset-confirmed {
  background: var(--offset-confirmed);
  color: #000;
}

.status-tag--offset-generated {
  background: var(--offset-generated);
}

.status-tag--offset-carried {
  background: var(--offset-carried);
  color: #000;
}
</style>

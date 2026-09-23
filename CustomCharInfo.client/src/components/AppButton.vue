<template>
  <component
    :is="tag"
    class="btn"
    :class="[`btn--${variant}`, `btn--${size}`, { 'btn--block': block, 'btn--busy': busy }]"
    v-bind="attrs"
    :aria-busy="busy || undefined"
    :aria-disabled="isDisabled || undefined"
    @click="onClick"
  >
    <AppLoading v-if="busy" size="sm" :inverted="variant === 'primary'" />
    <v-icon v-else-if="icon" :size="iconSize">{{ icon }}</v-icon>
    <span v-if="$slots.default" class="btn__label"><slot /></span>
  </component>
</template>

<script setup>
import { computed } from 'vue'
import AppLoading from '@/components/AppLoading.vue'

/*
  The site's button. Renders a router-link when `to` is set, an anchor when `href` is set, otherwise a button.
  Variants: default (white outline), primary (white fill), ghost (hairline), danger (red outline).
*/
const props = defineProps({
  variant: { type: String, default: 'default' },
  size: { type: String, default: 'md' },
  to: { type: [Object, String], default: null },
  href: { type: String, default: '' },
  type: { type: String, default: 'button' },
  icon: { type: String, default: '' },
  busy: { type: Boolean, default: false },
  disabled: { type: Boolean, default: false },
  block: { type: Boolean, default: false },
})

const emit = defineEmits(['click'])

const isDisabled = computed(() => props.disabled || props.busy)

const tag = computed(() => {
  if (props.to && !isDisabled.value) return 'router-link'
  if (props.href && !isDisabled.value) return 'a'
  return 'button'
})

const attrs = computed(() => {
  if (tag.value === 'router-link') return { to: props.to }
  if (tag.value === 'a') return { href: props.href, target: '_blank', rel: 'noopener' }
  return { type: props.type, disabled: isDisabled.value }
})

const iconSize = computed(() => (props.size === 'sm' ? 16 : props.size === 'lg' ? 22 : 18))

const onClick = (event) => {
  if (isDisabled.value) {
    event.preventDefault()
    return
  }
  emit('click', event)
}
</script>

<style scoped>
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  height: 38px;
  padding: 0 16px;
  border: 1px solid var(--white);
  background: transparent;
  color: var(--white);
  font: inherit;
  font-weight: 600;
  font-size: 14px;
  line-height: 1;
  text-decoration: none;
  white-space: nowrap;
  cursor: pointer;
  transition:
    background-color var(--dur-fast) var(--ease),
    color var(--dur-fast) var(--ease),
    border-color var(--dur-fast) var(--ease);
}

.btn:hover,
.btn:focus-visible {
  background: var(--white);
  color: #000;
}

.btn--primary {
  background: var(--white);
  color: #000;
}

.btn--primary:hover,
.btn--primary:focus-visible {
  background: var(--tx-2);
  border-color: var(--tx-2);
  color: #000;
}

.btn--ghost {
  border-color: var(--line-2);
  color: var(--tx);
}

.btn--ghost:hover,
.btn--ghost:focus-visible {
  background: var(--panel-2);
  border-color: var(--white);
  color: var(--white);
}

.btn--danger {
  border-color: var(--err);
  color: var(--err);
}

.btn--danger:hover,
.btn--danger:focus-visible {
  background: var(--err);
  color: #000;
}

.btn--sm {
  height: 30px;
  padding: 0 10px;
  font-size: 13px;
}

.btn--lg {
  height: 46px;
  padding: 0 22px;
  font-size: 15px;
}

.btn--block {
  width: 100%;
}

.btn[disabled],
.btn[aria-disabled='true'] {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn[disabled]:hover,
.btn[aria-disabled='true']:hover {
  background: transparent;
  color: var(--white);
}

.btn--primary[aria-disabled='true']:hover {
  background: var(--white);
  color: #000;
}

.btn__label {
  display: inline-block;
}
</style>

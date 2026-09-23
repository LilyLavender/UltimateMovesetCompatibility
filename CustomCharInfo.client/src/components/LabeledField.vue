<template>
  <div class="field" :class="{ 'field--error': !!error }">
    <label v-if="label" class="field__label" :for="forId">
      {{ label }}<i v-if="required" class="field__required" aria-hidden="true">*</i>
      <span v-if="note" class="field__note">{{ note }}</span>
    </label>
    <slot />
    <p v-if="error" class="field__hint field__hint--error">{{ error }}</p>
    <p v-else-if="hint" class="field__hint">{{ hint }}</p>
  </div>
</template>

<script setup>
/*
  A label above an input, with an optional required mark, a small note beside the label,
  and a hint or error line beneath. Vuetify inputs inside it should not pass their own `label`.
*/
defineProps({
  label: { type: String, default: '' },
  forId: { type: String, default: undefined },
  required: { type: Boolean, default: false },
  note: { type: String, default: '' },
  hint: { type: String, default: '' },
  error: { type: String, default: '' },
})
</script>

<style scoped>
.field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 0;
}

.field__label {
  font-size: 12px;
  font-weight: 600;
  color: var(--tx-2);
  letter-spacing: 0.02em;
}

.field__required {
  font-style: normal;
  color: var(--err);
  margin-left: 2px;
}

.field__note {
  margin-left: 8px;
  font-weight: 400;
  color: var(--tx-3);
}

.field__hint {
  margin: 0;
  font-size: 12px;
  color: var(--tx-3);
}

.field__hint--error {
  color: var(--err);
}
</style>

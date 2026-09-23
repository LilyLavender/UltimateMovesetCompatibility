<template>
  <section :id="id" class="panel form-section">
    <h3 class="form-section__title">{{ title }}</h3>
    <p v-if="description" class="form-section__description">{{ description }}</p>
    <div class="form-section__body">
      <slot />
    </div>
  </section>
</template>

<script setup>
/* One panel of a form, with the id the FormLayout's section list jumps to. */
defineProps({
  id: { type: String, required: true },
  title: { type: String, required: true },
  description: { type: String, default: '' },
})
</script>

<style scoped>
.form-section {
  scroll-margin-top: 20px;
}

.form-section__title {
  margin: 0 0 12px;
  font-family: var(--font-condensed);
  font-weight: 700;
  font-size: 20px;
  text-transform: uppercase;
  letter-spacing: 0.01em;
}

.form-section__description {
  margin: -6px 0 14px;
  color: var(--tx-2);
  font-size: 14px;
}

.form-section__body {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
</style>

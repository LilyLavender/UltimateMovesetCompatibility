<template>
  <section
    :id="id"
    class="panel form-section"
    :class="{ 'form-section--collapsible': collapsible }"
  >
    <button
      v-if="collapsible"
      type="button"
      class="form-section__toggle"
      :aria-expanded="open"
      @click="open = !open"
    >
      <h3 class="form-section__title">{{ title }}</h3>
      <v-icon
        size="20"
        class="form-section__chevron"
        :class="{ 'form-section__chevron--open': open }"
      >
        mdi-chevron-down
      </v-icon>
    </button>
    <h3 v-else class="form-section__title">{{ title }}</h3>
    <p v-if="description" class="form-section__description">{{ description }}</p>
    <!-- The animated element carries no padding; the gap sits on the inner body so the height tween has nothing to jump over. -->
    <v-expand-transition>
      <div v-show="!collapsible || open" class="form-section__reveal">
        <div class="form-section__body">
          <slot />
        </div>
      </div>
    </v-expand-transition>
  </section>
</template>

<script setup>
/* One panel of a form, with the id the FormLayout's section list jumps to. Collapsible sections bind v-model:open. */
defineProps({
  id: { type: String, required: true },
  title: { type: String, required: true },
  description: { type: String, default: '' },
  collapsible: { type: Boolean, default: false },
})

const open = defineModel('open', { type: Boolean, default: false })
</script>

<style scoped>
.form-section {
  scroll-margin-top: 20px;
}

.form-section__toggle {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: 0;
  border: 0;
  background: none;
  color: inherit;
  cursor: pointer;
  text-align: left;
}

.form-section__toggle .form-section__title {
  margin-bottom: 0;
}

.form-section--collapsible .form-section__body {
  padding-top: 12px;
}

.form-section__chevron {
  color: var(--tx-2);
  transition: transform var(--dur-base) var(--ease);
}

.form-section__chevron--open {
  transform: rotate(180deg);
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

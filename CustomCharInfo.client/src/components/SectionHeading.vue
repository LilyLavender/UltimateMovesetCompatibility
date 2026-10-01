<template>
  <div class="section-heading">
    <h2 class="section-heading__title">{{ title }}</h2>
    <span v-if="count != null" class="section-heading__count">{{ count }}</span>
    <slot />
    <router-link v-if="to" :to="to" class="section-heading__link">
      {{ linkLabel }}
      <v-icon size="16">mdi-arrow-right</v-icon>
    </router-link>
  </div>
</template>

<script setup>
/* A section heading in Roboto Condensed with the white bar, an optional count, and an optional link on the right. */
defineProps({
  title: { type: String, required: true },
  count: { type: [Number, String], default: null },
  to: { type: [Object, String], default: null },
  linkLabel: { type: String, default: 'View all' },
})
</script>

<style scoped>
.section-heading {
  display: flex;
  align-items: baseline;
  gap: 14px;
  margin: 36px 0 14px;
  padding-left: 12px;
  border-left: 6px solid var(--white);
}

.section-heading__title {
  margin: 0;
  font-family: var(--font-condensed);
  font-weight: 700;
  font-size: 26px;
  line-height: 1;
  text-transform: uppercase;
  letter-spacing: 0.01em;
}

.section-heading__count {
  color: var(--tx-3);
  font-size: 13px;
}

.section-heading__link {
  margin-left: auto;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  color: var(--tx-2);
  font-size: 14px;
  text-decoration: none;
  white-space: nowrap;
}

.section-heading__link:hover {
  color: var(--white);
}
</style>

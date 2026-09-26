<template>
  <div class="page" :class="{ 'page--over-hero': overHero }">
    <div class="tier" :class="`tier-${tier}`">
      <router-link v-if="backTo" :to="backTo" class="page-back">
        <v-icon size="16">mdi-arrow-left</v-icon>
        {{ backLabel }}
      </router-link>
      <div class="page-head">
        <h1 class="page-title no-select" :class="`page-title--${size}`">{{ title }}</h1>
        <span class="page-tail" aria-hidden="true"></span>
      </div>
      <p v-if="lede" class="page-lede">{{ lede }}</p>
      <div v-if="$slots.subnav" class="page-subnav">
        <slot name="subnav" />
      </div>
      <div class="page-body">
        <slot />
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useHead } from '@unhead/vue'

/*
  Every view renders inside one of these. It sets the width tier, draws the one DFHeiGB title
  per page with its striped tail, hosts the sub-nav, and registers the tab title.
  Tiers: narrow (prose), standard (hubs, details, forms, account), wide (tables and tools).
  Mounting plays the page-enter reveal; the shell mounts once per route, so in-page state changes never replay it.
*/
const props = defineProps({
  title: { type: String, required: true },
  tier: { type: String, default: 'standard' },
  size: { type: String, default: 'md' },
  lede: { type: String, default: '' },
  backTo: { type: [Object, String], default: null },
  backLabel: { type: String, default: 'Back' },
  overHero: { type: Boolean, default: false },
  head: { type: Boolean, default: true },
})

if (props.head) {
  useHead({ title: computed(() => `UMC | ${props.title}`) })
}
</script>

<style scoped>
.page {
  padding: 34px 0 40px;
}

.page--over-hero {
  position: relative;
  z-index: 2;
  margin-top: -150px;
  padding-top: 0;
}

.page-back {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  margin-bottom: 10px;
  color: var(--tx-2);
  font-size: 13px;
  font-weight: 500;
  text-decoration: none;
}

.page-back:hover {
  color: var(--white);
}

.page-head {
  display: flex;
  align-items: center;
  gap: 22px;
}

.page-title {
  margin: 0;
  font-family: var(--font-display);
  font-weight: 400;
  font-size: 64px;
  line-height: 1;
  text-transform: uppercase;
  letter-spacing: 0.01em;
  white-space: nowrap;
  animation: page-enter-title var(--dur-slow) var(--ease-out) backwards;
}

.page-title--lg {
  font-size: 88px;
}

.page-tail {
  flex: 1;
  height: 12px;
  min-width: 40px;
  opacity: 0.4;
  background: url('@/assets/ptn_diagonal_12.png') repeat;
  background-size: 12px 12px;
  animation:
    page-tail-drift 1.2s linear infinite,
    page-enter-tail var(--dur-slow) var(--ease-out) var(--stagger) backwards;
}

.page-lede {
  margin: 8px 0 0;
  max-width: 720px;
  color: var(--tx-2);
}

.page-lede,
.page-subnav {
  animation: page-enter-rise var(--dur-base) var(--ease-out) calc(var(--stagger) * 2) backwards;
}

.page-body {
  margin-top: 22px;
  animation: page-enter-rise var(--dur-base) var(--ease-out) calc(var(--stagger) * 3) backwards;
}

/* 
  Reveal:
  1. title slides in
  2. tail extends after it
  3. sub-nav and body rise
  about 360ms total
*/
@keyframes page-enter-title {
  from {
    opacity: 0;
    transform: translateX(calc(var(--shift) * -2));
  }
}

@keyframes page-enter-tail {
  from {
    clip-path: inset(0 100% 0 0);
  }

  to {
    clip-path: inset(0 0 0 0);
  }
}

@keyframes page-enter-rise {
  from {
    opacity: 0;
    transform: translateY(var(--shift));
  }
}

@keyframes page-tail-drift {
  from {
    background-position: 0 0;
  }

  to {
    background-position: -12px 0;
  }
}

@media (max-width: 599px) {
  .page {
    padding-top: 22px;
  }

  .page--over-hero {
    margin-top: -110px;
  }

  .page-title,
  .page-title--lg {
    font-size: 40px;
    white-space: normal;
  }

  .page-tail {
    height: 8px;
  }
}

@media (min-width: 600px) and (max-width: 959px) {
  .page-title--lg {
    font-size: 64px;
  }
}

@media (prefers-reduced-motion: reduce) {
  .page-title,
  .page-tail,
  .page-lede,
  .page-subnav,
  .page-body {
    animation: none;
  }
}
</style>

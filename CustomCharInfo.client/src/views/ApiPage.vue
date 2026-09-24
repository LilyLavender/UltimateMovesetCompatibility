<template>
  <PageShell title="Public API" tier="narrow">
    <div class="prose">
      <p>
        UMC exposes a read-only public API for looking up movesets, series, hooks, and plugin
        versions, and for checking predicted compatibility between movesets. It's free to use and
        doesn't require an API key.
      </p>

      <p>Movesets can be looked up by their SlottedId or UMC's internal database ID.</p>

      <p>
        Documentation:
        <a :href="swaggerUrl" target="_blank" rel="noopener" class="mono">{{ swaggerUrl }}</a>
      </p>
    </div>
  </PageShell>
</template>

<script setup>
import PageShell from '@/components/PageShell.vue'

const apiBaseUrl = import.meta.env.VITE_API_URL.replace(/\/+$/, '')
const swaggerUrl = `${apiBaseUrl}/swagger`
</script>

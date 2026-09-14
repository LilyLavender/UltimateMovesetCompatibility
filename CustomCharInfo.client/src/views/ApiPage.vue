<template>
  <v-container max-width="700px">
    <!-- Page title -->
    <h1 class="mb-1 page-title no-select">Public API</h1>

    <!-- Main content -->
    <div class="space-between-ps">
      <p>UMC exposes a read-only public API for looking up movesets, series, hooks, and plugin versions, and for checking predicted compatibility between movesets. It's free to use and doesn't require an API key.</p>

      <p>Movesets can be looked up by their SlottedId or UMC's internal database ID.</p>

      <p>
        Documentation: <a :href="swaggerUrl" target="_blank" rel="noopener" class="unvisitable">{{ swaggerUrl }}</a>
      </p>
    </div>
  </v-container>
</template>

<script setup>
const apiBaseUrl = import.meta.env.VITE_API_URL.replace(/\/+$/, '')
const swaggerUrl = `${apiBaseUrl}/swagger`
</script>

<style scoped>
.space-between-ps > p:not(:last-child) {
  margin-bottom: 2em;
}

.page-title {
  font-size: 5em;
}
</style>

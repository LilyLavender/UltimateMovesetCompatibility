<template>
  <v-app>
    <div
      class="page-texture"
      :class="{ 'page-texture--hero': route.name === 'Home' }"
      aria-hidden="true"
    ></div>
    <Header :variant="route.name === 'Home' ? 'hero' : 'solid'" />
    <main class="site-main">
      <router-view />
    </main>
    <Footer />

    <AppSnackbar />
  </v-app>
</template>

<script setup>
import Header from '@/components/SiteHeader.vue'
import Footer from '@/components/SiteFooter.vue'
import AppSnackbar from '@/components/AppSnackbar.vue'

import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useHead } from '@unhead/vue'

const route = useRoute()

// Route-level fallback title.
// Pages that know their subject register their own useHead title that wins while mounted.
useHead({
  title: computed(() => (route.meta.title ? `UMC | ${route.meta.title}` : 'UMC')),
})
</script>

<style scoped>
/* v-app's wrap is a flex column at viewport height. The page fills it so the footer stays at the bottom. */
.site-main {
  flex: 1 0 auto;
}
</style>

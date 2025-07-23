<template>
  <v-app>
    <Header />
    <router-view />
    <Footer />
  </v-app>
</template>

<script setup>
import Header from '@/components/SiteHeader.vue'
import Footer from '@/components/SiteFooter.vue'
</script>

<style scoped>
/* Fonts */
@font-face {
  font-family: AsiaKGDU;
  src: url('@/assets/fonts/asia_kgdu.woff') format("woff");
}

@font-face {
  font-family: DFHeiGB;
  src: url('@/assets/fonts/df_hei_gb-w12.otf') format("opentype");
}

@font-face {
  font-family: "Roboto Condensed";
  src: url('@/assets/fonts/Roboto_Condensed-normal-700.woff') format("woff");
}
</style>

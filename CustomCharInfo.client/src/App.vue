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

import { watch } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();

watch(
  () => route.fullPath,
  () => {
    document.title = route.meta.title
      ? `UMC | ${route.meta.title}`
      : 'UMC';
  },
  { immediate: true }
);
</script>

<style scoped>
/* Also see style.css for more common styling */

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

/* Table */
:deep(.dark-table .v-data-table) {
  background-color: #121212;
  color: #eee;
}

:deep(.dark-table .v-data-table__th) {
  background-color: #1e1e1e;
  color: #ddd;
}

:deep(.dark-table .v-data-table__td) {
  background-color: #1e1e1e;
  color: #ccc;
  max-width: 150px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  cursor: pointer;
}

:deep(.dark-table),
:deep(.dark-table .v-data-table-footer),
:deep(.dark-table .v-table__wrapper),
:deep(.dark-table table),
:deep(.dark-table thead),
:deep(.dark-table tr),
:deep(.dark-table tbody) {
  background-color: #1e1e1e;
  color: #ccc;
}
</style>

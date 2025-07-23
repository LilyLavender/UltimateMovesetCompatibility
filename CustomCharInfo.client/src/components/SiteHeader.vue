<template>
  <!-- <div v-if="$vuetify.display.mobile"> -->
  <header class="umc-header">
    <div class="umc-logo-container">
      <router-link to="/">
        <v-img :src="umcLogo" alt="UMC Logo" class="umc-logo" />
      </router-link>
    </div>
    <v-tabs v-model="activeTab" class="globalheader-util mb-0">
      <v-tab
        v-for="link in links"
        :key="link.title"
        :to="link.route"
        :value="link.route"
        class="globalheader-util__item"
      >
        <template v-if="link.title.startsWith('mdi-')">
          <v-icon>{{ link.title }}</v-icon>
        </template>
        <template v-else>
          {{ link.title }}
        </template>
      </v-tab>
    </v-tabs>
  </header>
</template>


<script setup>
import { useRoute } from 'vue-router';
import { ref, inject, computed } from 'vue'
import umcLogo from "@/assets/umc-logo.svg"

const drawer = ref(false);

// const isAdmin = authStore.isAdmin

const links = [
  { title: "Home", route: "/" },
  { title: "Movesets", route: "/movesets" },
  { title: "Blog", route: "/blog" },
  { title: "mdi-account", route: "/user-actions" },
]

// const adminLinks = [
//   { title: "Admin Portal", route: "/admin" }
// ]

const route = useRoute();

const activeTab = computed(() => {
  const tabRoutes = links.map(link => link.route);
  return tabRoutes.includes(route.path) ? route.path : null;
});
</script>

<style scoped>
.umc-header {
  z-index: 10;
}

.umc-logo-container {
  background-color: rgba(200, 200, 200, 0.75);
  position: absolute;
  left: 0;
  padding: 4px 6px 6px 4px;
  border-bottom-right-radius: 16px;
}

.umc-logo {
  height: 60px;
  width: 60px;
}

.globalheader-util {
  all: unset;
  /* background: rgba(0, 0, 0, 0.8); */
  position: absolute;
  right: 0;
  display: flex;
  -webkit-box-align: center;
  -ms-flex-align: center;
  align-items: center;
}

.globalheader-util__item {
  text-align: center;
  position: relative;
  height: 40px !important;
  padding: 0 12px;
  min-width: fit-content !important;
  font-size: medium;
  text-transform: unset;
}

.globalheader-util__item:not(:has(.mdi)) {
  width: 80px; 
}

.globalheader-util::before {
  all: unset;
  content: '';
  display: block;
  width: 107%;
  height: 40px;
  -webkit-transform: skewX(29deg);
  transform: skewX(29deg);
  position: absolute;
  background: rgba(0, 0, 0, 0.8);
  right: -14px;
  top: 0px;
}
</style>
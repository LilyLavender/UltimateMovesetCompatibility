<template>
  <v-container>
    <!-- Page title -->
    <h1 class="mb-4 page-title">Series</h1>

    <!-- Add series button -->
    <div 
      v-if="user && user.userTypeId >= 2"
      class="mb-5 pb-5"
    >
      <router-link
        :to="{ name: 'AddSeries' }"
        class="unvisitable text-decoration-none"
      >
        <v-icon>mdi-plus</v-icon>
        Add series
      </router-link>
    </div>
    
    <!-- Series List -->
    <SeriesList />
  </v-container>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'
import SeriesList from '@/components/SeriesList.vue'

const user = ref(null)

onMounted(async () => {
  user.value = (await api.get('/auth/me')).data
})
</script>

<style scoped>

</style>

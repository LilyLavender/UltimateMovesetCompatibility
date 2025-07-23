<template>
  <div class="p-6 mx-auto">
    <!-- Page title -->
    <h1 class="mb-1 title-font page-title no-select">All Movesets</h1>

    <!-- Add moveset button -->
    <div 
      v-if="user && user.userTypeId >= 2"
      class="mb-5 pb-5"
    >
      <router-link
        :to="{ name: 'AddMoveset' }"
        class="unvisitable text-decoration-none"
      >
        <v-icon>mdi-plus</v-icon>
        Submit Moveset
      </router-link>
    </div>

    <!-- Moveset List -->
    <MovesetList />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'
import MovesetList from '@/components/MovesetList.vue'

const user = ref(null)

onMounted(async () => {
  user.value = (await api.get('/auth/me')).data
})
</script>

<style scoped>

</style>
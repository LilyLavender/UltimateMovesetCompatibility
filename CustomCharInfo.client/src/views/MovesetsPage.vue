<template>
  <div class="p-6 mx-auto">
    <!-- Page title -->
    <h1 class="mb-1 title-font page-title no-select">All Movesets</h1>

    <!-- Actions -->
    <div class="mb-5 pb-5 d-flex justify-center">
      <!-- View table -->
      <router-link
        :to="{ name: 'MovesetsList' }"
        class="unvisitable text-decoration-none"
      >
        <i class="mdi mdi-arrow-right-bottom"></i>
        View all movesets as a table
      </router-link>

      <!-- Add moveset -->
      <router-link
        v-if="user && user.userTypeId >= 2"
        :to="{ name: 'AddMoveset' }"
        class="unvisitable text-decoration-none ml-5"
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
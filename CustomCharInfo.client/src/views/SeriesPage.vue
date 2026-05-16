<template>
  <v-container>
    <!-- Page title -->
    <h1 class="mb-4 page-title">Series</h1>

    <!-- Modder actions -->
    <div
      v-if="user && user.userTypeId >= 2"
      class="d-flex ga-3 mb-5 pb-3 flex-wrap"
    >
      <v-btn
        :to="{ name: 'AddSeries' }"
        variant="tonal"
        prepend-icon="mdi-plus"
        class="action-btn"
      >
        Add Series
      </v-btn>
      <v-btn
        :to="{ name: 'RequestEditSeries' }"
        variant="tonal"
        prepend-icon="mdi-pencil"
        class="action-btn"
      >
        Edit Series
      </v-btn>
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
  try {
    user.value = (await api.get('/auth/me')).data
  } catch {
    user.value = null
  }
})
</script>

<style scoped>
.action-btn {
  text-transform: none;
  letter-spacing: normal;
  background-color: #2e2e2e;
  color: #e2e2e2;
}
</style>

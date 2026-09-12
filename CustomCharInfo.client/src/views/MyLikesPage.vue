<template>
  <div class="my-likes-page">
    <h1 class="page-title no-select">My Likes</h1>

    <div v-if="loading" class="text-center mt-8">
      <v-progress-circular indeterminate />
    </div>

    <template v-else>
      <p v-if="movesets.length === 0" class="empty-msg">You haven't liked any movesets yet.</p>
      <div v-else class="moveset-grid">
        <MovesetCard
          v-for="moveset in movesets"
          :key="moveset.movesetId"
          :moveset="moveset"
        />
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'
import MovesetCard from '@/components/MovesetCard.vue'

const loading = ref(true)
const movesets = ref([])

onMounted(async () => {
  try {
    const user = (await api.get('/auth/me')).data
    const movesetsRes = await api.get('/movesets', {
      params: { likedByUserId: user.id, sort: 'alpha' },
    })
    movesets.value = movesetsRes.data
  } catch (err) {
    console.error('Failed to load liked movesets:', err)
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.my-likes-page {
  max-width: 1060px;
  margin: 0 auto;
  padding: 2rem 1rem;
}

.page-title {
  margin-bottom: 2rem;
}

.empty-msg {
  color: #666;
  font-style: italic;
}

.moveset-grid {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
}
</style>

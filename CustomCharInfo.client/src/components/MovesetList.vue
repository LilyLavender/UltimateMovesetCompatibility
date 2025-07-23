<template>
  <div class="moveset-list-all no-select">
    <!-- Search & Filter -->
    <!-- <div class="flex flex-wrap gap-4 mb-6">
      <input
        v-model="search"
        type="text"
        placeholder="Search modded character name..."
        class="border px-3 py-2 rounded w-full"
        @input="fetchMovesets"
      />

      <select v-model="selectedSeries" @change="fetchMovesets" class="border px-3 py-2 rounded">
        <option :value="null">All Series</option>
        <option v-for="series in seriesList" :key="series.seriesId" :value="series.seriesId">
          {{ series.seriesName }}
        </option>
      </select>
    </div> -->

    <!-- Moveset List (Grid Layout) -->
    <div class="moveset-grid">
      <MovesetCard v-for="m in displayedMovesets" :key="m.movesetId" :moveset="m" />
    </div>

    <!-- Pagination -->
    <!-- <div class="flex justify-between items-center mt-6">
      <button @click="prevPage" :disabled="page === 1" class="px-3 py-2 border rounded">Prev</button>
      <span>Page {{ page }}</span>
      <button @click="nextPage" :disabled="movesets.length < pageSize" class="px-3 py-2 border rounded">Next</button>
    </div> -->
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import MovesetCard from './MovesetCard.vue'
import { getMovesets } from '@/services/movesetService'
import api from '@/services/api'

const props = defineProps({
  movesets: {
    type: Array,
    default: null,
  },
})

const search = ref('')
const selectedSeries = ref(null)
const page = ref(1)
const pageSize = 100
const fetchedMovesets = ref([])
const seriesList = ref([])

const displayedMovesets = computed(() =>
  props.movesets ?? fetchedMovesets.value
)

const fetchMovesets = async () => {
  const response = await getMovesets({
    search: search.value,
    seriesId: selectedSeries.value,
    page: page.value,
    pageSize
  })
  fetchedMovesets.value = response.data
}

const fetchFilters = async () => {
  const seriesRes = await api.get('/series')
  seriesList.value = seriesRes.data
}

const nextPage = () => {
  page.value++
  fetchMovesets()
}

const prevPage = () => {
  if (page.value > 1) {
    page.value--
    fetchMovesets()
  }
}

onMounted(() => {
  fetchFilters()
  if (!props.movesets) {
    fetchMovesets()
  }
})
</script>

<style scoped>
.moveset-grid {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  margin: 0 auto;
  max-width: 1020px;
}

.moveset-card {
  flex: 0 1 33.33%;
  box-sizing: border-box;
}

@media (max-width: 768px) {
  .moveset-card {
    flex: 0 1 50%;
  }
}

@media (max-width: 480px) {
  .moveset-card {
    flex: 0 1 100%;
  }
}

.moveset-list-all {
  margin-bottom: 4em;
}
</style>
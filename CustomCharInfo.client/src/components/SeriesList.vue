<template>
  <v-container>
    <v-row class="mb-4">
      <v-col cols="12" sm="6">
        <v-checkbox
          v-model="showOnlyWithMovesets"
          label="Only show series with movesets"
          density="compact"
          hide-details
        />
      </v-col>
      <v-col cols="12" sm="6">
        <v-select
          variant="outlined"
          v-model="sortBy"
          :items="['Alphabetical', 'Most Movesets']"
          label="Sort by"
          density="compact"
          hide-details
        />
      </v-col>
    </v-row>

    <v-row>
      <v-col
        v-for="s in filteredAndSortedSeries"
        :key="s.seriesId"
        cols="6"
        sm="3"
      >
        <SeriesCard :series="s" :apiUrl="apiUrl" />
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/services/api'
import SeriesCard from './SeriesCard.vue'

const series = ref([])
const showOnlyWithMovesets = ref(true)
const sortBy = ref('Alphabetical')
const apiUrl = import.meta.env.VITE_API_URL

onMounted(async () => {
  const res = await api.get('/series')
  series.value = res.data
})

const filteredAndSortedSeries = computed(() => {
  let result = [...series.value]

  if (showOnlyWithMovesets.value) {
    result = result.filter(s => s.movesetCount > 0)
  }

  if (sortBy.value === 'Alphabetical') {
    result.sort((a, b) => a.seriesName.localeCompare(b.seriesName))
  } else if (sortBy.value === 'Most Movesets') {
    result.sort((a, b) => b.movesetCount - a.movesetCount)
  }

  return result
})
</script>

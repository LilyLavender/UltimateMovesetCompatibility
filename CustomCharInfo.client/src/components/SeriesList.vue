<template>
  <v-container>
    <v-row class="mb-4" align="center">
      <v-col cols="12" sm="auto">
        <v-checkbox
          v-model="showOnlyWithMovesets"
          label="Only show series with movesets"
          density="compact"
          hide-details
        />
      </v-col>
      <v-col class="d-flex justify-center ga-2">
        <template v-if="user && user.userTypeId >= 2">
          <v-btn
            :to="{ name: 'AddSeries' }"
            variant="outlined"
            prepend-icon="mdi-plus"
            class="action-btn"
            size="small"
          >
            Add Series
          </v-btn>
          <v-btn
            :to="{ name: 'RequestEditSeries' }"
            variant="outlined"
            prepend-icon="mdi-pencil"
            class="action-btn"
            size="small"
          >
            Edit Series
          </v-btn>
        </template>
      </v-col>
      <v-col cols="12" sm="3">
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
const blockedSeriesIds = ref(new Set())
const showOnlyWithMovesets = ref(true)
const sortBy = ref('Alphabetical')
const apiUrl = import.meta.env.VITE_API_URL
const user = ref(null)

onMounted(async () => {
  const [seriesRes] = await Promise.all([
    api.get('/series', { params: { inSeriesList: true } }),
    api.get('/auth/me').then(r => { user.value = r.data }).catch(() => {}),
    api.get('/logs', { params: { acceptanceStates: [1, 2, 3, 4, 5, 6, 7], itemTypes: [3] } })
      .then(r => {
        const latestPerSeries = new Map()
        for (const log of r.data) {
          const id = log.item?.seriesId
          if (id == null) continue
          const cur = latestPerSeries.get(id)
          if (!cur || new Date(log.createdAt) > new Date(cur.createdAt)) {
            latestPerSeries.set(id, log)
          }
        }
        const blocked = new Set()
        for (const [id, log] of latestPerSeries) {
          if ([2, 4].includes(log.acceptanceState?.acceptanceStateId)) blocked.add(id)
        }
        blockedSeriesIds.value = blocked
      })
      .catch(() => {}),
  ])
  series.value = seriesRes.data
})

const filteredAndSortedSeries = computed(() => {
  let result = series.value.filter(s => !blockedSeriesIds.value.has(s.seriesId))

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

<style scoped>
.action-btn {
  text-transform: none;
  letter-spacing: normal;
  color: #b0b0b0;
  border-color: #4a4a4a;
}
</style>

<template>
  <div class="series-list">
    <div class="series-list__controls">
      <LabeledField label="Sort">
        <v-select
          v-model="sortBy"
          :items="['Alphabetical', 'Most movesets']"
          density="compact"
          hide-details
        />
      </LabeledField>
      <div class="series-list__check">
        <v-checkbox
          v-model="showOnlyWithMovesets"
          label="Only series with movesets"
          density="compact"
          hide-details
        />
      </div>
      <p class="series-list__count">{{ filteredAndSortedSeries.length }} series</p>
    </div>

    <div v-if="loading" class="series-grid" aria-busy="true">
      <Skeleton v-for="n in 8" :key="n" variant="line" height="72px" />
    </div>
    <div v-else-if="filteredAndSortedSeries.length" class="series-grid">
      <SeriesCard
        v-for="s in filteredAndSortedSeries"
        :key="s.seriesId"
        :series="s"
        :api-url="apiUrl"
      />
    </div>
    <EmptyState v-else message="No series match this filter." />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/services/api'
import SeriesCard from './SeriesCard.vue'
import LabeledField from '@/components/LabeledField.vue'
import Skeleton from '@/components/Skeleton.vue'
import EmptyState from '@/components/EmptyState.vue'
import { ItemType, ALL_ACCEPTANCE_STATES, BLOCKED_ACCEPTANCE_STATES } from '@/globals'

const series = ref([])
const blockedSeriesIds = ref(new Set())
const showOnlyWithMovesets = ref(true)
const sortBy = ref('Alphabetical')
const loading = ref(true)
const apiUrl = import.meta.env.VITE_API_URL

onMounted(async () => {
  const [seriesRes] = await Promise.all([
    api.get('/series', { params: { inSeriesList: true } }),
    api
      .get('/logs', {
        params: { acceptanceStates: ALL_ACCEPTANCE_STATES, itemTypes: [ItemType.Series] },
      })
      .then((r) => {
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
          if (BLOCKED_ACCEPTANCE_STATES.includes(log.acceptanceState?.acceptanceStateId)) {
            blocked.add(id)
          }
        }
        blockedSeriesIds.value = blocked
      })
      .catch(() => {}),
  ])
  series.value = seriesRes.data
  loading.value = false
})

const filteredAndSortedSeries = computed(() => {
  let result = series.value.filter((s) => !blockedSeriesIds.value.has(s.seriesId))

  if (showOnlyWithMovesets.value) {
    result = result.filter((s) => s.movesetCount > 0)
  }

  if (sortBy.value === 'Alphabetical') {
    result.sort((a, b) => a.seriesName.localeCompare(b.seriesName))
  } else {
    result.sort((a, b) => b.movesetCount - a.movesetCount)
  }

  return result
})
</script>

<style scoped>
.series-list__controls {
  display: grid;
  grid-template-columns: 220px auto 1fr;
  gap: 12px 20px;
  align-items: end;
  margin-bottom: 18px;
}

.series-list__check {
  padding-bottom: 2px;
}

.series-list__count {
  margin: 0 0 10px;
  justify-self: end;
  color: var(--tx-2);
  font-size: 13px;
}

.series-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 8px;
}

@media (max-width: 599px) {
  .series-list__controls {
    grid-template-columns: 1fr;
  }

  .series-list__count {
    justify-self: start;
  }
}
</style>

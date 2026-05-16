<template>
  <div class="my-content-page">
    <h1 class="page-title no-select">My Content</h1>

    <div v-if="loading" class="text-center mt-8">
      <v-progress-circular indeterminate />
    </div>

    <template v-else>
      <!-- Movesets -->
      <section class="content-section">
        <h2 class="section-title">Movesets</h2>
        <p v-if="movesets.length === 0" class="empty-msg">No movesets yet.</p>
        <div v-else class="moveset-grid">
          <div
            v-for="moveset in movesets"
            :key="moveset.movesetId"
            class="moveset-wrapper"
          >
            <MovesetCard :moveset="moveset" />
            <div
              class="pill-overlay"
              v-if="statusPillFor(movesetStates[moveset.movesetId]) || moveset.privateMoveset"
            >
              <span
                v-if="statusPillFor(movesetStates[moveset.movesetId])"
                class="state-pill"
                :style="{ backgroundColor: statusPillFor(movesetStates[moveset.movesetId]).color }"
              >{{ statusPillFor(movesetStates[moveset.movesetId]).label }}</span>
              <span
                v-if="moveset.privateMoveset"
                class="state-pill"
                :style="{ backgroundColor: PRIVATE_COLOR }"
              >Private</span>
            </div>
          </div>
        </div>
      </section>

      <!-- Series -->
      <section class="content-section">
        <h2 class="section-title">Series</h2>
        <p v-if="userSeries.length === 0" class="empty-msg">No series yet.</p>
        <v-row v-else>
          <v-col
            v-for="s in userSeries"
            :key="s.seriesId"
            cols="6"
            sm="4"
          >
            <SeriesCard :series="s" :apiUrl="apiUrl">
              <template #subtitle>
                <div class="series-pills">
                  <span
                    v-for="pill in pillsFor(seriesStates[s.seriesId])"
                    :key="pill.label"
                    class="state-pill"
                    :style="{ backgroundColor: pill.color }"
                  >{{ pill.label }}</span>
                  <span v-if="!pillsFor(seriesStates[s.seriesId]).length" class="series-pill-spacer" />
                </div>
              </template>
            </SeriesCard>
          </v-col>
        </v-row>
      </section>
    </template>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'
import MovesetCard from '@/components/MovesetCard.vue'
import SeriesCard from '@/components/SeriesCard.vue'

const apiUrl = import.meta.env.VITE_API_URL

const loading = ref(true)
const movesets = ref([])
const userSeries = ref([])
const movesetStates = ref({})
const seriesStates = ref({})

const PILL_COLORS = {
  1: 'rgb(187, 224, 236)',
  2: 'rgb(52, 194, 241)',
  3: 'rgb(241, 241, 142)',
  4: 'rgb(241, 241, 52)',
  6: 'rgb(241, 52, 52)',
}

const PILL_LABELS = {
  1: 'Pending Admin (Soft)',
  2: 'Pending Admin (Hard)',
  3: 'Pending User (Soft)',
  4: 'Pending User (Hard)',
  6: 'Rejected',
}

const PRIVATE_COLOR = 'rgb(241, 52, 52)'

function statusPillFor(stateId) {
  if (!PILL_LABELS[stateId]) return null
  return { label: PILL_LABELS[stateId], color: PILL_COLORS[stateId] }
}

function pillsFor(stateId) {
  const pill = statusPillFor(stateId)
  return pill ? [pill] : []
}

onMounted(async () => {
  try {
    const user = (await api.get('/auth/me')).data

    const [logsRes, movesetsRes] = await Promise.all([
      api.get('/logs', {
        params: {
          acceptanceStates: [1, 2, 3, 4, 5, 6, 7],
          itemTypes: [1, 3],
        },
      }),
      user.modderId
        ? api.get('/movesets', { params: { modderId: user.modderId } })
        : Promise.resolve({ data: [] }),
    ])

    movesets.value = movesetsRes.data

    const logs = logsRes.data
    const movesetLogMap = new Map()
    const seriesLogMap = new Map()

    for (const log of logs) {
      const typeId = log.itemType?.itemTypeId
      if (typeId === 1 && log.item?.movesetId != null) {
        const id = log.item.movesetId
        const cur = movesetLogMap.get(id)
        if (!cur || new Date(log.createdAt) > new Date(cur.createdAt)) {
          movesetLogMap.set(id, log)
        }
      } else if (typeId === 3 && log.item?.seriesId != null) {
        const id = log.item.seriesId
        const cur = seriesLogMap.get(id)
        if (!cur || new Date(log.createdAt) > new Date(cur.createdAt)) {
          seriesLogMap.set(id, log)
        }
      }
    }

    for (const [id, log] of movesetLogMap) {
      movesetStates.value[id] = log.acceptanceState?.acceptanceStateId
    }

    const seriesIds = [...seriesLogMap.keys()]
    if (seriesIds.length > 0) {
      const results = await Promise.all(
        seriesIds.map(id => api.get(`/series/${id}`).catch(() => null))
      )
      userSeries.value = results.filter(r => r?.data).map(r => r.data)
      for (const [id, log] of seriesLogMap) {
        seriesStates.value[id] = log.acceptanceState?.acceptanceStateId
      }
    }
  } catch (err) {
    console.error('Failed to load content:', err)
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.my-content-page {
  max-width: 1060px;
  margin: 0 auto;
  padding: 2rem 1rem;
}

.page-title {
  margin-bottom: 2rem;
}

.content-section {
  margin-bottom: 2.5rem;
}

.section-title {
  font-size: 1.2rem;
  font-weight: 600;
  margin-bottom: 0.75rem;
  color: #b0b0b0;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.empty-msg {
  color: #666;
  font-style: italic;
}

/* Moveset grid */
.moveset-grid {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
}

.moveset-wrapper {
  position: relative;
  flex: 0 1 33.333%;
}

.pill-overlay {
  position: absolute;
  bottom: 4px;
  right: 6px;
  display: flex;
  gap: 4px;
  pointer-events: none;
  z-index: 60;
}

/* Pill styling matching ActionLogItem, but fully rounded */
.state-pill {
  padding: 2px 8px;
  border-radius: 9999px;
  color: rgb(20, 20, 20);
  font-weight: bold;
  font-size: 0.7rem;
  white-space: nowrap;
}

/* Series pills */
.series-pills {
  display: flex;
  gap: 4px;
  padding: 2px 0;
  min-height: 20px;
}

.series-pill-spacer {
  display: inline-block;
  height: 20px;
}

@media (max-width: 768px) {
  .moveset-wrapper {
    flex: 0 1 50%;
  }
}

@media (max-width: 480px) {
  .moveset-wrapper {
    flex: 0 1 100%;
  }
}
</style>

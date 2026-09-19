<template>
  <div class="hidden-content-page">
    <h1 class="page-title no-select">Hidden Content</h1>
    <p class="subtitle">
      Everything the public cannot see right now: submissions held for review, rejected items, and
      private movesets.
    </p>

    <div v-if="loading" class="text-center mt-8">
      <v-progress-circular indeterminate />
    </div>

    <template v-else>
      <!-- Movesets grouped by hidden reason -->
      <section v-for="group in movesetGroups" :key="group.key" class="content-section">
        <h2 class="section-title">
          {{ group.title }}
          <span class="section-count">{{ group.items.length }}</span>
        </h2>
        <p v-if="group.items.length === 0" class="empty-msg">None.</p>
        <div v-else class="moveset-grid">
          <div v-for="moveset in group.items" :key="moveset.movesetId" class="moveset-wrapper">
            <MovesetCard :moveset="moveset" />
            <div class="pill-overlay">
              <span
                v-if="statusPillFor(movesetStates[moveset.movesetId])"
                class="state-pill"
                :style="{ backgroundColor: statusPillFor(movesetStates[moveset.movesetId]).color }"
                >{{ statusPillFor(movesetStates[moveset.movesetId]).label }}</span
              >
              <span
                v-if="moveset.privateMoveset"
                class="state-pill"
                :style="{ backgroundColor: PRIVATE_COLOR }"
                >Private</span
              >
            </div>
          </div>
        </div>
      </section>

      <!-- Series -->
      <section class="content-section">
        <h2 class="section-title">
          Held or rejected series
          <span class="section-count">{{ blockedSeries.length }}</span>
        </h2>
        <p v-if="blockedSeries.length === 0" class="empty-msg">None.</p>
        <v-row v-else>
          <v-col v-for="s in blockedSeries" :key="s.seriesId" cols="6" sm="4" md="3">
            <SeriesCard :series="s" :api-url="apiUrl">
              <template #subtitle>
                <div class="series-pills">
                  <span
                    v-for="pill in pillsFor(seriesStates[s.seriesId])"
                    :key="pill.label"
                    class="state-pill"
                    :style="{ backgroundColor: pill.color }"
                    >{{ pill.label }}</span
                  >
                </div>
              </template>
            </SeriesCard>
          </v-col>
        </v-row>
      </section>

      <!-- Modders -->
      <section class="content-section">
        <h2 class="section-title">
          Held or rejected modder profiles
          <span class="section-count">{{ blockedModders.length }}</span>
        </h2>
        <p v-if="blockedModders.length === 0" class="empty-msg">None.</p>
        <ul v-else class="modder-list">
          <li v-for="m in blockedModders" :key="m.modderId" class="modder-row">
            <router-link
              :to="{ name: 'ModderDetail', params: { id: m.modderId } }"
              class="unvisitable modder-link"
              >{{ m.name }}</router-link
            >
            <span
              v-for="pill in pillsFor(modderStates[m.modderId])"
              :key="pill.label"
              class="state-pill"
              :style="{ backgroundColor: pill.color }"
              >{{ pill.label }}</span
            >
          </li>
        </ul>
      </section>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/services/api'
import {
  AcceptanceState,
  ItemType,
  ALL_ACCEPTANCE_STATES,
  BLOCKED_ACCEPTANCE_STATES,
} from '@/globals'
import MovesetCard from '@/components/MovesetCard.vue'
import SeriesCard from '@/components/SeriesCard.vue'
import {
  PRIVATE_COLOR,
  statusPillFor,
  pillsFor,
  latestLogsByItem,
} from '@/services/acceptanceStateDisplay'

const apiUrl = import.meta.env.VITE_API_URL

const loading = ref(true)
const movesets = ref([])
const series = ref([])
const modders = ref([])
const movesetStates = ref({})
const seriesStates = ref({})
const modderStates = ref({})

const isBlocked = (stateId) => BLOCKED_ACCEPTANCE_STATES.includes(stateId)

const movesetGroups = computed(() => {
  const byState = (stateId) =>
    movesets.value.filter((m) => movesetStates.value[m.movesetId] === stateId)
  return [
    {
      key: 'pending-admin',
      title: 'Pending admin action (hard)',
      items: byState(AcceptanceState.PendingAdminHard),
    },
    {
      key: 'pending-user',
      title: 'Pending user action (hard)',
      items: byState(AcceptanceState.PendingUserHard),
    },
    { key: 'rejected', title: 'Rejected', items: byState(AcceptanceState.Rejected) },
    {
      key: 'private',
      title: 'Private',
      items: movesets.value.filter(
        (m) => m.privateMoveset && !isBlocked(movesetStates.value[m.movesetId])
      ),
    },
  ]
})

const blockedSeries = computed(() =>
  series.value.filter((s) => isBlocked(seriesStates.value[s.seriesId]))
)

const blockedModders = computed(() =>
  modders.value.filter((m) => isBlocked(modderStates.value[m.modderId]))
)

onMounted(async () => {
  try {
    const [movesetsRes, seriesRes, moddersRes, logsRes] = await Promise.all([
      api.get('/movesets', { params: { includeHidden: true } }),
      api.get('/series', { params: { includeHidden: true } }),
      api.get('/modders', { params: { includeHidden: true } }),
      api.get('/logs', {
        params: {
          viewAll: true,
          acceptanceStates: ALL_ACCEPTANCE_STATES,
          itemTypes: [ItemType.Moveset, ItemType.Series, ItemType.Modder],
        },
      }),
    ])

    const logs = logsRes.data
    const toStates = (map) =>
      Object.fromEntries([...map].map(([id, log]) => [id, log.acceptanceState?.acceptanceStateId]))
    movesetStates.value = toStates(latestLogsByItem(logs, ItemType.Moveset, (i) => i?.movesetId))
    seriesStates.value = toStates(latestLogsByItem(logs, ItemType.Series, (i) => i?.seriesId))
    modderStates.value = toStates(latestLogsByItem(logs, ItemType.Modder, (i) => i?.modderId))

    // Only movesets that are hidden for some reason belong here.
    movesets.value = movesetsRes.data.filter(
      (m) => m.privateMoveset || isBlocked(movesetStates.value[m.movesetId])
    )
    series.value = seriesRes.data
    modders.value = moddersRes.data
  } catch (err) {
    console.error('Failed to load hidden content:', err)
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.hidden-content-page {
  max-width: 1060px;
  margin: 0 auto;
  padding: 2rem 1rem;
}

.page-title {
  margin-bottom: 0.5rem;
}
.subtitle {
  color: #aaa;
  margin-bottom: 2rem;
  line-height: 1.5;
}

.content-section {
  margin-bottom: 2.5rem;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 1.2rem;
  font-weight: 600;
  margin-bottom: 0.75rem;
  color: #b0b0b0;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}
.section-count {
  font-size: 0.8rem;
  padding: 0 0.5rem;
  border-radius: 999px;
  background-color: #2e2e2e;
  color: #ddd;
  letter-spacing: normal;
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

.state-pill {
  padding: 2px 8px;
  border-radius: 9999px;
  color: rgb(20, 20, 20);
  font-weight: bold;
  font-size: 0.7rem;
  white-space: nowrap;
}

.series-pills {
  display: flex;
  gap: 4px;
  padding: 2px 0;
  min-height: 20px;
}

.modder-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}
.modder-row {
  display: flex;
  align-items: center;
  gap: 0.6rem;
}
.modder-link {
  font-weight: 600;
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

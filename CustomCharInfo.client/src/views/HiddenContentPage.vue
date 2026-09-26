<template>
  <PageShell
    title="Hidden content"
    :back-to="{ name: 'AdminPortal' }"
    back-label="Admin portal"
    lede="Everything the public cannot see right now: submissions held for review, rejected items, and private movesets."
  >
    <template v-if="loading">
      <SectionHeading title="Pending admin action (hard)" />
      <SkeletonList :count="3" />
      <SectionHeading title="Series" />
      <SkeletonPanel :title="false" :lines="2" />
    </template>

    <div v-else class="reveal">
      <!-- Movesets grouped by hidden reason -->
      <template v-for="group in movesetGroups" :key="group.key">
        <SectionHeading :title="group.title" :count="group.items.length" />
        <p v-if="group.items.length === 0" class="empty">None.</p>
        <div v-else class="moveset-grid">
          <div v-for="moveset in group.items" :key="moveset.movesetId" class="moveset-wrapper">
            <MovesetCard :moveset="moveset" />
            <div class="tag-overlay">
              <StatusTag
                v-if="statusPillFor(movesetStates[moveset.movesetId])"
                :state="movesetStates[moveset.movesetId]"
              />
              <StatusTag v-if="moveset.privateMoveset" variant="err">Private</StatusTag>
            </div>
          </div>
        </div>
      </template>

      <!-- Series -->
      <SectionHeading title="Series" :count="blockedSeries.length" />
      <p v-if="blockedSeries.length === 0" class="empty">None.</p>
      <div v-else class="series-grid">
        <SeriesCard v-for="s in blockedSeries" :key="s.seriesId" :series="s" :api-url="apiUrl">
          <template #subtitle>
            <span class="series-tags">
              <StatusTag v-for="stateId in seriesTagStates(s)" :key="stateId" :state="stateId" />
            </span>
          </template>
        </SeriesCard>
      </div>

      <!-- Modders -->
      <SectionHeading title="Modders" :count="blockedModders.length" />
      <p v-if="blockedModders.length === 0" class="empty">None.</p>
      <ul v-else class="modder-list">
        <li v-for="m in blockedModders" :key="m.modderId" class="modder-row">
          <router-link
            :to="{ name: 'ModderDetail', params: { id: m.modderId } }"
            class="modder-link"
          >
            {{ m.name }}
          </router-link>
          <StatusTag v-for="stateId in modderTagStates(m)" :key="stateId" :state="stateId" />
        </li>
      </ul>
    </div>
  </PageShell>
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
import PageShell from '@/components/PageShell.vue'
import SectionHeading from '@/components/SectionHeading.vue'
import SkeletonList from '@/components/SkeletonList.vue'
import SkeletonPanel from '@/components/SkeletonPanel.vue'
import StatusTag from '@/components/StatusTag.vue'
import MovesetCard from '@/components/MovesetCard.vue'
import SeriesCard from '@/components/SeriesCard.vue'
import { statusPillFor, latestStatesByItem } from '@/services/acceptanceStateDisplay'

const apiUrl = import.meta.env.VITE_API_URL

const loading = ref(true)
const movesets = ref([])
const series = ref([])
const modders = ref([])
const movesetStates = ref({})
const seriesStates = ref({})
const modderStates = ref({})

const isBlocked = (stateId) => BLOCKED_ACCEPTANCE_STATES.includes(stateId)

const seriesTagStates = (s) => {
  const stateId = seriesStates.value[s.seriesId]
  return statusPillFor(stateId) ? [stateId] : []
}

const modderTagStates = (m) => {
  const stateId = modderStates.value[m.modderId]
  return statusPillFor(stateId) ? [stateId] : []
}

const movesetGroups = computed(() => {
  const byState = (stateId) =>
    movesets.value.filter((m) => movesetStates.value[m.movesetId] === stateId)
  return [
    {
      key: 'pending-admin',
      title: 'Pending Admin',
      items: byState(AcceptanceState.PendingAdminHard),
    },
    {
      key: 'pending-user',
      title: 'Pending User',
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
      api.get('/logs/latest', {
        params: {
          viewAll: true,
          acceptanceStates: ALL_ACCEPTANCE_STATES,
          itemTypes: [ItemType.Moveset, ItemType.Series, ItemType.Modder],
        },
      }),
    ])

    const rows = logsRes.data
    const toStates = (map) => Object.fromEntries(map)
    movesetStates.value = toStates(latestStatesByItem(rows, ItemType.Moveset))
    seriesStates.value = toStates(latestStatesByItem(rows, ItemType.Series))
    modderStates.value = toStates(latestStatesByItem(rows, ItemType.Modder))

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
.empty {
  margin: 0;
  color: var(--tx-3);
  font-size: 13px;
}

.moveset-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, 340px);
  justify-content: center;
  gap: 0;
}

.moveset-wrapper {
  position: relative;
  width: 340px;
}

.tag-overlay {
  position: absolute;
  bottom: 4px;
  right: 6px;
  display: flex;
  gap: 4px;
  pointer-events: none;
  z-index: 60;
}

.series-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 8px;
}

.series-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  margin-top: 2px;
  min-height: 22px;
}

.modder-list {
  list-style: none;
  padding: 0;
  margin: 0;
  border: 1px solid var(--line);
  background: var(--panel);
}

.modder-row {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  padding: 10px 14px;
  border-bottom: 1px solid var(--line);
}

.modder-row:last-child {
  border-bottom: 0;
}

.modder-link {
  font-weight: 600;
  text-decoration: none;
}

.modder-link:hover {
  text-decoration: underline;
}
</style>

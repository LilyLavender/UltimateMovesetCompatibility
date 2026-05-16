<template>
  <div class="moveset-list-all no-select">
    <!-- Controls -->
    <v-row v-if="showControls" class="controls mb-2">
      <!-- Header -->
      <v-col cols="12" sm="2">
        <h2 class="center-entire">Filters</h2>
      </v-col>
      
      <!-- Sort -->
      <v-col cols="12" sm="5">
        <v-select
          label="Sort"
          v-model="sortMode"
          variant="outlined"
          hide-details
          :items="[
            { title: 'Alphabetical', value: 'alpha' },
            { title: 'Release Date', value: 'releaseDate' },
            { title: 'Most Popular', value: 'popularity' }
          ]"
        />
      </v-col>

      <!-- Release State -->
      <v-col cols="12" sm="5">
        <v-select
          label="Release State"
          v-model="filterReleaseState"
          clearable
          variant="outlined"
          hide-details
          :items="releaseStates"
          item-title="releaseStateName"
          item-value="releaseStateName"
        />
      </v-col>

    </v-row>

    <!-- Moveset List -->
    <div class="moveset-grid">
      <MovesetCard
        v-for="m in processedMovesets"
        :key="m.movesetId"
        :moveset="m"
        :canView="canViewMoveset(m)"
        :blockedSeriesIconUrls="blockedSeriesIconUrls"
      />
    </div>
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
  showControls: {
    type: Boolean,
    default: false,
  },
})

const fetchedMovesets = ref([])
const releaseStates = ref([])
const user = ref(null)
const blockedSeriesIconUrls = ref(new Set())
const hardHeldMovesetIds = ref(new Set())

// sort/filter
const sortMode = ref('alpha')
const filterReleaseState = ref(null)

const displayedMovesets = computed(() => props.movesets ?? fetchedMovesets.value)

const canViewMoveset = (moveset) => {
  if (!moveset.privateMoveset) return true
  if (!user.value) return false

  const isAdmin = user.value.userTypeId === 3
  const isModder =
    user.value.userName &&
    moveset.modders.includes(user.value.userName) // This should absolutely not be done by username but there's security on the moveset itself so it's whatever lol

  return isAdmin || isModder
}

const processedMovesets = computed(() => {
  let list = displayedMovesets.value.filter(m => !hardHeldMovesetIds.value.has(m.movesetId))

  // Filter
  if (filterReleaseState.value != null) {
    list = list.filter(m => m.releaseState === filterReleaseState.value)
  }

  // Sort
  if (sortMode.value === 'releaseDate') {
    list.sort((a, b) => {
      const aHasDate = !!a.releaseDate
      const bHasDate = !!b.releaseDate

      // Newest first
      if (aHasDate && bHasDate) {
        return new Date(b.releaseDate) - new Date(a.releaseDate)
      }

      // With date first
      if (aHasDate) return -1
      if (bHasDate) return 1

      // Public first
      if (a.privateMoveset !== b.privateMoveset) {
        return a.privateMoveset ? 1 : -1
      }

      // Alphabetical fallback if public
      if (!a.privateMoveset) {
        return a.moddedCharName.localeCompare(b.moddedCharName)
      }

      // Modder name if private
      const modderA = (a.modders[0] || '').toLowerCase()
      const modderB = (b.modders[0] || '').toLowerCase()
      return modderA.localeCompare(modderB)
    })
  } else if (sortMode.value === 'popularity') {
    list = list.filter(m => !m.privateMoveset)
    list.sort((a, b) => (b.likeCount ?? 0) - (a.likeCount ?? 0) || a.moddedCharName.localeCompare(b.moddedCharName))
  } else if (!props.movesets) {
    list.sort((a, b) => {
      if (a.privateMoveset !== b.privateMoveset) return a.privateMoveset ? 1 : -1
      if (!a.privateMoveset) return a.moddedCharName.localeCompare(b.moddedCharName)
      const modderA = (a.modders[0] || '').toLowerCase()
      const modderB = (b.modders[0] || '').toLowerCase()
      return modderA.localeCompare(modderB)
    })
  }

  return list
})

const fetchMovesets = async () => {
  const res = await getMovesets()
  fetchedMovesets.value = res.data
}

const fetchReleaseStates = async () => {
  try {
    const res = await api.get('/releasestates')
    releaseStates.value = res.data
  } catch (err) {
    console.error('Failed to fetch release states:', err)
  }
}

const fetchUser = async () => {
  try {
    const res = await api.get('/auth/me')
    user.value = res.data
  } catch {
    user.value = null
  }
}

const fetchBlockedIds = async () => {
  try {
    const res = await api.get('/logs', {
      params: { acceptanceStates: [1, 2, 3, 4, 5, 6, 7], itemTypes: [1, 3] },
    })
    const latestPerMoveset = new Map()
    const latestPerSeries = new Map()
    for (const log of res.data) {
      const typeId = log.itemType?.itemTypeId
      if (typeId === 1) {
        const id = log.item?.movesetId
        if (id == null) continue
        const cur = latestPerMoveset.get(id)
        if (!cur || new Date(log.createdAt) > new Date(cur.createdAt)) latestPerMoveset.set(id, log)
      } else if (typeId === 3) {
        const id = log.item?.seriesId
        if (id == null) continue
        const cur = latestPerSeries.get(id)
        if (!cur || new Date(log.createdAt) > new Date(cur.createdAt)) latestPerSeries.set(id, log)
      }
    }
    const hardMovesets = new Set()
    for (const [id, log] of latestPerMoveset) {
      if ([2, 4].includes(log.acceptanceState?.acceptanceStateId)) hardMovesets.add(id)
    }
    hardHeldMovesetIds.value = hardMovesets

    const blockedUrls = new Set()
    for (const [, log] of latestPerSeries) {
      if ([2, 4].includes(log.acceptanceState?.acceptanceStateId) && log.item?.seriesIconUrl) {
        blockedUrls.add(log.item.seriesIconUrl)
      }
    }
    blockedSeriesIconUrls.value = blockedUrls
  } catch {
    // fail open
  }
}

onMounted(async () => {
  if (!props.movesets) await fetchMovesets()
  await Promise.all([fetchUser(), fetchReleaseStates(), fetchBlockedIds()])
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

/* Center text */
.center-entire {
  text-align: center;
}

div:has(>.center-entire) {
  align-content: center;
}
</style>
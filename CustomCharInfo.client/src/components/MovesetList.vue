<template>
  <div class="moveset-list-all no-select">
    <!-- Controls -->
    <v-row v-if="showControls" dense align="center" class="controls mb-3">

      <!-- Search -->
      <v-col cols="12" sm="12" md="4">
        <v-text-field
          v-model="searchQuery"
          label="Search"
          placeholder="Name, series, creator…"
          variant="outlined"
          density="compact"
          hide-details
          clearable
          prepend-inner-icon="mdi-magnify"
        />
      </v-col>

      <!-- Sort -->
      <v-col cols="6" sm="4" md="2">
        <v-select
          label="Sort"
          v-model="sortMode"
          variant="outlined"
          density="compact"
          hide-details
          :items="[
            { title: 'Alphabetical', value: 'alpha' },
            { title: 'Release Date', value: 'releaseDate' },
            { title: 'Most Popular', value: 'popularity' },
          ]"
        />
      </v-col>

      <!-- Release State -->
      <v-col cols="6" sm="4" md="2">
        <v-select
          label="Release State"
          v-model="filterReleaseState"
          clearable
          variant="outlined"
          density="compact"
          hide-details
          :items="releaseStates"
          item-title="releaseStateName"
          item-value="releaseStateName"
        />
      </v-col>

      <!-- Privacy -->
      <v-col cols="6" sm="4" md="2">
        <v-select
          label="Privacy"
          v-model="filterPrivacy"
          variant="outlined"
          density="compact"
          hide-details
          :items="[
            { title: 'All', value: 'all' },
            { title: 'Public', value: 'public' },
            { title: 'Private', value: 'private' },
          ]"
        />
      </v-col>

      <!-- Show Joke Movesets -->
      <v-col cols="auto" class="joke-toggle-col d-flex align-center">
        <v-checkbox
          v-model="showJokeMovesets"
          hide-details
          density="compact"
          label="Joke Movesets"
        />
      </v-col>

      <!-- Vanilla Character -->
      <v-col cols="12" sm="6" md="4">
        <v-autocomplete
          label="Vanilla Character"
          v-model="filterVanillaChar"
          clearable
          variant="outlined"
          density="compact"
          hide-details
          :items="vanillaChars"
          item-title="displayName"
          item-value="internalName"
          :custom-filter="vanillaCharFilter"
          auto-select-first
        >
          <template #item="{ props, item }">
            <v-list-item v-bind="props" class="vc-remove-title">
              <div class="vc-filter-option">
                <img
                  :src="`/UltimateMovesetCompatibility/vanilla-stock-icons/chara_2_${item.raw.internalName}.png`"
                  class="vc-stock-icon"
                />
                <span>{{ item.raw.displayName }}</span>
              </div>
            </v-list-item>
          </template>
          <template #selection="{ item }">
            <div class="vc-filter-option">
              <img
                :src="`/UltimateMovesetCompatibility/vanilla-stock-icons/chara_2_${item.raw.internalName}.png`"
                class="vc-stock-icon"
              />
              <span>{{ item.raw.displayName }}</span>
            </div>
          </template>
        </v-autocomplete>
      </v-col>

      <!-- Vanilla Article -->
      <v-col cols="12" sm="6" md="4">
        <v-autocomplete
          label="Vanilla Article"
          v-model="filterArticle"
          clearable
          variant="outlined"
          density="compact"
          hide-details
          :items="articleNames"
          auto-select-first
        />
      </v-col>

      <!-- Open Source -->
      <v-col cols="6" sm="3" md="2">
        <v-select
          label="Source"
          v-model="filterOpenSource"
          variant="outlined"
          density="compact"
          hide-details
          :items="[
            { title: 'All', value: 'all' },
            { title: 'Open Source', value: 'yes' },
            { title: 'Closed Source', value: 'no' },
          ]"
        />
      </v-col>

      <!-- On Mods Wiki -->
      <v-col cols="6" sm="3" md="2">
        <v-select
          label="Mods Wiki"
          v-model="filterOnModsWiki"
          variant="outlined"
          density="compact"
          hide-details
          :items="[
            { title: 'All', value: 'all' },
            { title: 'On Mods Wiki', value: 'yes' },
            { title: 'Not on Mods Wiki', value: 'no' },
          ]"
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
const filterPrivacy = ref('all')
const filterVanillaChar = ref(null)
const filterArticle = ref(null)
const searchQuery = ref('')
const showJokeMovesets = ref(false)
const filterOpenSource = ref('all')
const filterOnModsWiki = ref('all')

const vanillaCharFilter = (_, query, item) => {
  const q = query.toLowerCase()
  return item.raw.displayName.toLowerCase().includes(q) ||
         item.raw.internalName.toLowerCase().includes(q)
}

const displayedMovesets = computed(() => props.movesets ?? fetchedMovesets.value)

const vanillaChars = computed(() => {
  const map = new Map()
  displayedMovesets.value.forEach(m => {
    if (m.vanillaCharName) map.set(m.vanillaCharName, m.vanillaCharDisplayName ?? m.vanillaCharName)
  })
  return [...map.entries()]
    .sort((a, b) => a[1].localeCompare(b[1]))
    .map(([internalName, displayName]) => ({ internalName, displayName }))
})

const articleNames = computed(() => {
  const set = new Set()
  displayedMovesets.value.forEach(m => {
    m.articleNames?.forEach(a => { if (a) set.add(a) })
  })
  return [...set].sort()
})

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

  // Filter joke movesets (only when controls are shown — user profiles/series always show all)
  if (props.showControls && !showJokeMovesets.value) {
    list = list.filter(m => !m.isJokeMoveset)
  }

  // Text search
  if (searchQuery.value?.trim()) {
    const q = searchQuery.value.trim().toLowerCase()
    list = list.filter(m =>
      m.moddedCharName?.toLowerCase().includes(q) ||
      m.modders?.some(mod => mod.toLowerCase().includes(q)) ||
      m.seriesName?.toLowerCase().includes(q)
    )
  }

  // Release state filter
  if (filterReleaseState.value != null) {
    list = list.filter(m => m.releaseState === filterReleaseState.value)
  }

  // Privacy filter
  if (filterPrivacy.value === 'public') {
    list = list.filter(m => !m.privateMoveset)
  } else if (filterPrivacy.value === 'private') {
    list = list.filter(m => m.privateMoveset)
  }

  // Vanilla character filter
  if (filterVanillaChar.value != null) {
    list = list.filter(m => m.vanillaCharName === filterVanillaChar.value)
  }

  // Article filter
  if (filterArticle.value != null) {
    list = list.filter(m => m.articleNames?.includes(filterArticle.value))
  }

  // Open source filter
  if (filterOpenSource.value === 'yes') {
    list = list.filter(m => m.hasSourceCode)
  } else if (filterOpenSource.value === 'no') {
    list = list.filter(m => !m.hasSourceCode)
  }

  // Mods wiki filter
  if (filterOnModsWiki.value === 'yes') {
    list = list.filter(m => m.hasModsWikiLink)
  } else if (filterOnModsWiki.value === 'no') {
    list = list.filter(m => !m.hasModsWikiLink)
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
  width: 1020px;
  max-width: 100%;
}

.moveset-card {
  flex: 0 1 33.33%;
  min-width: 340px;
  box-sizing: border-box;
}

@media (max-width: 768px) {
  .moveset-card {
    flex: 0 1 50%;
    min-width: unset;
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

.joke-toggle-col {
  padding-top: 0;
  padding-bottom: 0;
}

/* Vanilla char filter dropdown */
.vc-filter-option {
  display: flex;
  align-items: center;
  gap: 8px;
}

.vc-stock-icon {
  width: 30px;
  height: 30px;
  flex-shrink: 0;
  object-fit: contain;
}

/* The selection slot sits inside v-field__input alongside the native <input>.
   That input has flex-grow so it consumes left space, pushing our content right.
   Expanding the selection wrapper to fill available space corrects this. */
:deep(.v-select__selection) {
  flex: 1;
  min-width: 0;
}

.vc-remove-title :deep(.v-list-item-title:not(.vc-filter-option .v-list-item-title)) {
  display: none;
}
</style>
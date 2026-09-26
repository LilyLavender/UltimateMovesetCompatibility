<template>
  <div class="moveset-list-all no-select">
    <!-- Controls -->
    <div v-if="showControls" class="filters">
      <LabeledField label="Search" class="filters__search">
        <v-text-field
          v-model="searchQuery"
          placeholder="Name, series, creator"
          density="compact"
          hide-details
          clearable
          prepend-inner-icon="mdi-magnify"
        />
      </LabeledField>

      <LabeledField label="Sort">
        <v-select
          v-model="sortMode"
          density="compact"
          hide-details
          :items="[
            { title: 'Alphabetical', value: 'alpha' },
            { title: 'Release date', value: 'releaseDate' },
            { title: 'Most popular', value: 'popularity' },
          ]"
        />
      </LabeledField>

      <LabeledField label="Release state">
        <v-select
          v-model="filterReleaseState"
          placeholder="Any"
          clearable
          density="compact"
          hide-details
          :items="releaseStates"
          item-title="releaseStateName"
          item-value="releaseStateName"
        />
      </LabeledField>

      <LabeledField label="Privacy">
        <v-select
          v-model="filterPrivacy"
          density="compact"
          hide-details
          :items="[
            { title: 'All', value: 'all' },
            { title: 'Public', value: 'public' },
            { title: 'Private', value: 'private' },
          ]"
        />
      </LabeledField>

      <LabeledField label="Vanilla character" class="filters__wide">
        <v-autocomplete
          v-model="filterVanillaChar"
          placeholder="Any character"
          clearable
          density="compact"
          hide-details
          :items="vanillaChars"
          item-title="displayName"
          item-value="internalName"
          :custom-filter="vanillaCharFilter"
          auto-select-first
        >
          <template #item="{ props: itemProps, item }">
            <v-list-item v-bind="itemProps" class="vc-remove-title">
              <div class="vc-filter-option">
                <img
                  :src="`/UltimateMovesetCompatibility/vanilla-stock-icons/chara_2_${item.raw.internalName}.png`"
                  class="vc-stock-icon"
                  alt=""
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
                alt=""
              />
              <span>{{ item.raw.displayName }}</span>
            </div>
          </template>
        </v-autocomplete>
      </LabeledField>

      <LabeledField label="Vanilla article" class="filters__wide">
        <v-autocomplete
          v-model="filterArticle"
          placeholder="Any article"
          clearable
          density="compact"
          hide-details
          :items="articleNames"
          auto-select-first
        />
      </LabeledField>

      <LabeledField label="Source">
        <v-select
          v-model="filterOpenSource"
          density="compact"
          hide-details
          :items="[
            { title: 'All', value: 'all' },
            { title: 'Open source', value: 'yes' },
            { title: 'Closed source', value: 'no' },
          ]"
        />
      </LabeledField>

      <LabeledField label="Mods wiki">
        <v-select
          v-model="filterOnModsWiki"
          density="compact"
          hide-details
          :items="[
            { title: 'All', value: 'all' },
            { title: 'On mods wiki', value: 'yes' },
            { title: 'Not on mods wiki', value: 'no' },
          ]"
        />
      </LabeledField>

      <div class="filters__check">
        <v-checkbox
          v-model="showJokeMovesets"
          hide-details
          density="compact"
          label="Joke movesets"
        />
      </div>
    </div>

    <p v-if="showControls" class="filters__count">
      Showing {{ processedMovesets.length }} of {{ displayedMovesets.length }} movesets
    </p>

    <!-- Moveset List -->
    <div class="moveset-grid">
      <MovesetCard
        v-for="m in processedMovesets"
        :key="m.movesetId"
        :moveset="m"
        :can-view="canViewMoveset(m)"
        :blocked-series-icon-urls="blockedSeriesIconUrls"
      />
    </div>
    <EmptyState
      v-if="showControls && !processedMovesets.length"
      message="No movesets match these filters."
    />
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import MovesetCard from './MovesetCard.vue'
import LabeledField from '@/components/LabeledField.vue'
import EmptyState from '@/components/EmptyState.vue'
import api from '@/services/api'
import { UserType, ItemType, HARD_STATES } from '@/globals'
import { compareDateOnlyStrings } from '@/services/dateOnly'

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
  return (
    item.raw.displayName.toLowerCase().includes(q) ||
    item.raw.internalName.toLowerCase().includes(q)
  )
}

const displayedMovesets = computed(() => props.movesets ?? fetchedMovesets.value)

const vanillaChars = computed(() => {
  const map = new Map()
  displayedMovesets.value.forEach((m) => {
    if (m.vanillaCharName) map.set(m.vanillaCharName, m.vanillaCharDisplayName ?? m.vanillaCharName)
  })
  return [...map.entries()]
    .sort((a, b) => a[1].localeCompare(b[1]))
    .map(([internalName, displayName]) => ({ internalName, displayName }))
})

const articleNames = computed(() => {
  const set = new Set()
  displayedMovesets.value.forEach((m) => {
    m.articleNames?.forEach((a) => {
      if (a) set.add(a)
    })
  })
  return [...set].sort()
})

const canViewMoveset = (moveset) => {
  if (!moveset.privateMoveset) return true
  if (!user.value) return false

  const isAdmin = user.value.userTypeId === UserType.Admin
  const isModder = user.value.userName && moveset.modders.includes(user.value.userName) // This should absolutely not be done by username but there's security on the moveset itself so it's whatever lol

  return isAdmin || isModder
}

const processedMovesets = computed(() => {
  let list = displayedMovesets.value.filter((m) => !hardHeldMovesetIds.value.has(m.movesetId))

  // Filter joke movesets
  if (props.showControls && !showJokeMovesets.value) {
    list = list.filter((m) => !m.isJokeMoveset)
  }

  // Text search
  if (searchQuery.value?.trim()) {
    const q = searchQuery.value.trim().toLowerCase()
    list = list.filter(
      (m) =>
        m.moddedCharName?.toLowerCase().includes(q) ||
        m.modders?.some((mod) => mod.toLowerCase().includes(q)) ||
        m.seriesName?.toLowerCase().includes(q)
    )
  }

  // Release state filter
  if (filterReleaseState.value != null) {
    list = list.filter((m) => m.releaseState === filterReleaseState.value)
  }

  // Privacy filter
  if (filterPrivacy.value === 'public') {
    list = list.filter((m) => !m.privateMoveset)
  } else if (filterPrivacy.value === 'private') {
    list = list.filter((m) => m.privateMoveset)
  }

  // Vanilla character filter
  if (filterVanillaChar.value != null) {
    list = list.filter((m) => m.vanillaCharName === filterVanillaChar.value)
  }

  // Article filter
  if (filterArticle.value != null) {
    list = list.filter((m) => m.articleNames?.includes(filterArticle.value))
  }

  // Open source filter
  if (filterOpenSource.value === 'yes') {
    list = list.filter((m) => m.hasSourceCode)
  } else if (filterOpenSource.value === 'no') {
    list = list.filter((m) => !m.hasSourceCode)
  }

  // Mods wiki filter
  if (filterOnModsWiki.value === 'yes') {
    list = list.filter((m) => m.hasModsWikiLink)
  } else if (filterOnModsWiki.value === 'no') {
    list = list.filter((m) => !m.hasModsWikiLink)
  }

  // Sort
  if (sortMode.value === 'releaseDate') {
    list.sort((a, b) => {
      const aHasDate = !!a.releaseDate
      const bHasDate = !!b.releaseDate

      // Newest first
      if (aHasDate && bHasDate) {
        return compareDateOnlyStrings(b.releaseDate, a.releaseDate)
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
    list = list.filter((m) => !m.privateMoveset)
    list.sort(
      (a, b) =>
        (b.likeCount ?? 0) - (a.likeCount ?? 0) || a.moddedCharName.localeCompare(b.moddedCharName)
    )
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
  const res = await api.get('/movesets')
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

// Hardheld movesets should be dropped from the list for their owner too
const fetchBlockedIds = async () => {
  try {
    const res = await api.get('/logs/latest', {
      params: { acceptanceStates: HARD_STATES, itemTypes: [ItemType.Moveset] },
    })
    hardHeldMovesetIds.value = new Set(res.data.map((row) => row.itemId))
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
/* The cards are 340px wide, never stretch, and pack edge to edge. */
.moveset-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, 340px);
  justify-content: center;
  gap: 0;
}

.moveset-list-all {
  margin-bottom: 4em;
}

.filters {
  display: grid;
  grid-template-columns: repeat(6, minmax(0, 1fr));
  gap: 12px 16px;
  margin-bottom: 14px;
}

.filters__search,
.filters__wide {
  grid-column: span 2;
}

.filters__check {
  display: flex;
  align-items: flex-end;
  grid-column: span 2;
}

.filters__count {
  margin: 0 0 14px;
  color: var(--tx-2);
  font-size: 13px;
}

@media (max-width: 959px) {
  .filters {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (max-width: 599px) {
  .filters {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .filters__check {
    grid-column: span 1;
  }
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

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

      <div class="filters__check">
        <v-checkbox
          v-model="showJokeMovesets"
          hide-details
          density="compact"
          label="Joke movesets"
        />
      </div>

      <div class="filters__trio">
        <LabeledField label="Vanilla character">
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

        <LabeledField label="Vanilla article">
          <v-autocomplete
            v-model="filterArticle"
            placeholder="Any article"
            clearable
            density="compact"
            hide-details
            :items="articleItems"
            auto-select-first
          />
        </LabeledField>

        <LabeledField label="Hook">
          <v-autocomplete
            v-model="filterHook"
            placeholder="Any hook"
            clearable
            density="compact"
            hide-details
            :items="hookItems"
            :menu-props="{ contentClass: 'hook-filter-menu' }"
            auto-select-first
          >
            <template #item="{ props: itemProps, item }">
              <v-list-item v-bind="itemProps">
                <template v-if="item.raw.offset" #title>
                  <span class="mono">{{ formatOffset(item.raw.offset) }}</span>
                  {{ item.raw.description }}
                </template>
              </v-list-item>
            </template>
          </v-autocomplete>
        </LabeledField>
      </div>

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
    </div>

    <p v-if="showControls" class="filters__count">
      Showing {{ processedMovesets.length }} of {{ displayedMovesets.length }} movesets
    </p>

    <!-- Moveset List -->
    <template v-for="section in sections" :key="section.title">
      <SectionHeading v-if="section.title" :title="section.title" />
      <div class="moveset-grid">
        <MovesetCard
          v-for="m in section.movesets"
          :key="m.movesetId"
          :moveset="m"
          :can-view="canViewMoveset(m)"
          :blocked-series-icon-urls="blockedSeriesIconUrls"
        />
      </div>
    </template>
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
import SectionHeading from '@/components/SectionHeading.vue'
import api from '@/services/api'
import { isAdmin as isAdminUser } from '@/navigation'
import { ItemType, BLOCKED_ACCEPTANCE_STATES, ReleaseState, RELEASE_STATE_NAMES } from '@/globals'
import { releaseDateSections, compareByName } from '@/services/releaseSections'
import { formatOffset } from '@/services/offsets'

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
const filterHook = ref(null)
const hooks = ref([])
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

// The article and hook filters take "has any", "has none", or one specific item.
// A moveset whose list is hidden (null) matches none of them.
const HAS_ANY = 'has-any'
const HAS_NONE = 'has-none'

const matchesListFilter = (list, filter) => {
  if (filter == null) return true
  if (list == null) return false
  if (filter === HAS_ANY) return list.length > 0
  if (filter === HAS_NONE) return list.length === 0
  return list.includes(filter)
}

const articleItems = computed(() => {
  const set = new Set()
  displayedMovesets.value.forEach((m) => {
    m.articleNames?.forEach((a) => {
      if (a) set.add(a)
    })
  })
  return [
    { title: 'Has cloned articles', value: HAS_ANY },
    { title: 'No cloned articles', value: HAS_NONE },
    { type: 'divider' },
    ...[...set].sort().map((a) => ({ title: a, value: a })),
  ]
})

const hookItems = computed(() => {
  const used = new Set(displayedMovesets.value.flatMap((m) => m.hookIds ?? []))
  const specific = hooks.value
    .filter((h) => used.has(h.hookId))
    .sort((a, b) => parseInt(a.offset, 16) - parseInt(b.offset, 16))
    .map((h) => ({
      title: `${formatOffset(h.offset)} ${h.description}`,
      value: h.hookId,
      offset: h.offset,
      description: h.description,
    }))
  return [
    { title: 'Has hooks', value: HAS_ANY },
    { title: 'No hooks', value: HAS_NONE },
    { type: 'divider' },
    ...specific,
  ]
})

const canViewMoveset = (moveset) => {
  if (!moveset.privateMoveset) return true
  if (!user.value) return false

  const isAdmin = isAdminUser(user.value)
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
  } else if (props.showControls) {
    list = list.filter((m) => m.releaseState !== RELEASE_STATE_NAMES[ReleaseState.Deprecated])
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

  // Article and hook filters
  list = list.filter(
    (m) =>
      matchesListFilter(m.articleNames, filterArticle.value) &&
      matchesListFilter(m.hookIds, filterHook.value)
  )

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
    return list
  } else if (sortMode.value === 'popularity') {
    list = list.filter((m) => !m.privateMoveset)
    list.sort(
      (a, b) =>
        (b.likeCount ?? 0) - (a.likeCount ?? 0) || a.moddedCharName.localeCompare(b.moddedCharName)
    )
  } else if (!props.movesets) {
    list.sort(compareByName)
  }

  return list
})

// Release-date sort splits the list into Released and Unreleased. Every other sort is one group.
const sections = computed(() => {
  if (sortMode.value !== 'releaseDate') {
    return [{ title: '', movesets: processedMovesets.value }]
  }
  const { released, unreleased } = releaseDateSections(processedMovesets.value)
  return [
    { title: 'Released', movesets: released },
    { title: 'Unreleased', movesets: unreleased },
  ].filter((section) => section.movesets.length)
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

// Labels for the hook filter. List carries just hook ids.
const fetchHooks = async () => {
  try {
    const res = await api.get('/hooks')
    hooks.value = res.data
  } catch (err) {
    console.error('Failed to fetch hooks:', err)
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

// Hardheld and rejected movesets are dropped from the list for their owner too, "my content" shows them instead
const fetchBlockedIds = async () => {
  try {
    const res = await api.get('/logs/latest', {
      params: { acceptanceStates: BLOCKED_ACCEPTANCE_STATES, itemTypes: [ItemType.Moveset] },
    })
    hardHeldMovesetIds.value = new Set(res.data.map((row) => row.itemId))
  } catch {
    // fail open
  }
}

onMounted(async () => {
  if (!props.movesets) await fetchMovesets()
  await Promise.all([
    fetchUser(),
    fetchReleaseStates(),
    fetchBlockedIds(),
    props.showControls ? fetchHooks() : null,
  ])
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

.filters__search {
  grid-column: span 2;
}

/* Character, article, and hook share four columns in equal thirds */
.filters__trio {
  grid-column: span 4;
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px 16px;
}

.filters__check {
  display: flex;
  align-items: flex-end;
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

  .filters__trio {
    grid-column: 1 / -1;
  }
}

@media (max-width: 599px) {
  .filters {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .filters__trio {
    grid-template-columns: 1fr;
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

/* 
  The selection slot sits inside v-field__input alongside the native <input>.
  That input has flex-grow so it consumes left space, pushing our content right.
  Expanding the selection wrapper to fill available space corrects this.
  */
:deep(.v-select__selection) {
  flex: 1;
  min-width: 0;
}

/* 
  The hook menu stays the field's width. Long descriptions ellipsis instead of widening.
  Vuetify sets the menu's min-width to the field's width inline, so a zero width resolves to exactly that.
*/
:global(.hook-filter-menu) {
  width: 0;
}

.vc-remove-title :deep(.v-list-item-title:not(.vc-filter-option .v-list-item-title)) {
  display: none;
}
</style>

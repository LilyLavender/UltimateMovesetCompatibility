<template>
  <PageShell title="My content">
    <template #subnav>
      <SubNav section="account" label="Account" />
    </template>

    <template v-if="loading">
      <SectionHeading title="Movesets" />
      <SkeletonList :count="3" />
      <SectionHeading title="Series" />
      <SkeletonPanel :title="false" :lines="2" />
    </template>

    <template v-else>
      <!-- Movesets -->
      <SectionHeading title="Movesets" :count="movesets.length" />
      <EmptyState v-if="movesets.length === 0" message="No movesets yet." icon="mdi-view-list">
        <template #action>
          <AppButton :to="{ name: 'AddMoveset' }" size="sm" icon="mdi-plus">
            Submit a moveset
          </AppButton>
        </template>
      </EmptyState>
      <div v-else class="moveset-grid">
        <div v-for="moveset in movesets" :key="moveset.movesetId" class="moveset-wrapper">
          <MovesetCard :moveset="moveset" />
          <div v-if="movesetTags(moveset).length" class="tag-overlay">
            <StatusTag v-for="tag in movesetTags(moveset)" :key="tag.key" v-bind="tag.props">
              {{ tag.label }}
            </StatusTag>
          </div>
        </div>
      </div>

      <!-- Movesets the user edits without being credited -->
      <template v-if="editedMovesets.length > 0">
        <SectionHeading title="Movesets I can edit" :count="editedMovesets.length" />
        <div class="moveset-grid">
          <div v-for="moveset in editedMovesets" :key="moveset.movesetId" class="moveset-wrapper">
            <MovesetCard :moveset="moveset" />
            <div v-if="movesetTags(moveset).length" class="tag-overlay">
              <StatusTag v-for="tag in movesetTags(moveset)" :key="tag.key" v-bind="tag.props">
                {{ tag.label }}
              </StatusTag>
            </div>
          </div>
        </div>
      </template>

      <!-- Series -->
      <SectionHeading title="Series" :count="userSeries.length" />
      <EmptyState v-if="userSeries.length === 0" message="No series yet." icon="mdi-shape" />
      <div v-else class="series-grid">
        <SeriesCard v-for="s in userSeries" :key="s.seriesId" :series="s" :api-url="apiUrl">
          <template #subtitle>
            <span class="series-tags">
              <StatusTag v-for="stateId in seriesTagStates(s)" :key="stateId" :state="stateId" />
              <span v-if="!seriesTagStates(s).length" class="series-card__count">
                {{ s.movesetCount }} {{ s.movesetCount === 1 ? 'moveset' : 'movesets' }}
              </span>
            </span>
          </template>
        </SeriesCard>
      </div>

      <!-- Plugins -->
      <SectionHeading title="Plugins" :count="plugins.length" />
      <EmptyState v-if="plugins.length === 0" message="No plugins yet." icon="mdi-puzzle">
        <template #action>
          <AppButton :to="{ name: 'AddPlugin' }" size="sm" icon="mdi-plus">
            Submit a plugin
          </AppButton>
        </template>
      </EmptyState>
      <ul v-else class="plugin-list">
        <li v-for="plugin in plugins" :key="plugin.pluginId" class="plugin-row">
          <span class="plugin-row__name">
            {{ plugin.name }}
            <span class="faint">{{ pluginAttachmentLabel(plugin) }}</span>
          </span>
          <span class="plugin-row__versions">
            <StatusTag v-for="tag in pluginVersionTags(plugin)" :key="tag.key" v-bind="tag.props">
              {{ tag.label }}
            </StatusTag>
          </span>
        </li>
      </ul>
    </template>
  </PageShell>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'
import { ItemType, ALL_ACCEPTANCE_STATES } from '@/globals'
import PageShell from '@/components/PageShell.vue'
import SubNav from '@/components/SubNav.vue'
import SectionHeading from '@/components/SectionHeading.vue'
import SkeletonList from '@/components/SkeletonList.vue'
import SkeletonPanel from '@/components/SkeletonPanel.vue'
import EmptyState from '@/components/EmptyState.vue'
import AppButton from '@/components/AppButton.vue'
import StatusTag from '@/components/StatusTag.vue'
import MovesetCard from '@/components/MovesetCard.vue'
import SeriesCard from '@/components/SeriesCard.vue'
import { displayVersion } from '@/services/pluginVersion'
import { statusPillFor, latestStatesByItem } from '@/services/acceptanceStateDisplay'

const apiUrl = import.meta.env.VITE_API_URL

const loading = ref(true)
const movesets = ref([])
const editedMovesets = ref([])
const userSeries = ref([])
const plugins = ref([])
const movesetStates = ref({})
const seriesStates = ref({})

function pluginAttachmentLabel(plugin) {
  if (plugin.movesetId) return `Moveset: ${plugin.movesetName}`
  if (plugin.dependencyId) return `Dependency: ${plugin.dependencyName}`
  return 'Standalone'
}

// Tags drawn over a card
function movesetTags(moveset) {
  const tags = []
  const stateId = movesetStates.value[moveset.movesetId]
  if (statusPillFor(stateId)) tags.push({ key: 'state', props: { state: stateId }, label: '' })
  if (moveset.privateMoveset)
    tags.push({ key: 'private', props: { variant: 'err' }, label: 'Private' })
  return tags
}

function seriesTagStates(series) {
  const stateId = seriesStates.value[series.seriesId]
  return statusPillFor(stateId) ? [stateId] : []
}

function pluginVersionTags(plugin) {
  return (plugin.versions ?? []).map((v) => {
    const label = displayVersion(v.versionLabel)
    if (v.isCurrent) return { key: v.hash, props: { variant: 'ok' }, label: `${label} (current)` }
    const pill = statusPillFor(v.acceptanceStateId)
    return pill
      ? { key: v.hash, props: { state: v.acceptanceStateId }, label: `${label}: ${pill.label}` }
      : { key: v.hash, props: { variant: 'neutral' }, label }
  })
}

onMounted(async () => {
  try {
    const user = (await api.get('/auth/me')).data

    const [logsRes, movesetsRes, editedRes, pluginsRes] = await Promise.all([
      api.get('/logs/latest', {
        params: {
          acceptanceStates: ALL_ACCEPTANCE_STATES,
          itemTypes: [ItemType.Moveset, ItemType.Series],
        },
      }),
      user.modderId
        ? api.get('/movesets', { params: { modderId: user.modderId } })
        : Promise.resolve({ data: [] }),
      user.modderId
        ? api.get('/movesets', { params: { editorId: user.modderId } }).catch(() => ({ data: [] }))
        : Promise.resolve({ data: [] }),
      user.modderId
        ? api.get('/plugins/mine').catch(() => ({ data: [] }))
        : Promise.resolve({ data: [] }),
    ])

    movesets.value = movesetsRes.data
    editedMovesets.value = editedRes.data
    plugins.value = pluginsRes.data

    const rows = logsRes.data
    for (const [id, stateId] of latestStatesByItem(rows, ItemType.Moveset)) {
      movesetStates.value[id] = stateId
    }

    const seriesStateMap = latestStatesByItem(rows, ItemType.Series)
    const seriesIds = [...seriesStateMap.keys()]
    if (seriesIds.length > 0) {
      const results = await Promise.all(
        seriesIds.map((id) => api.get(`/series/${id}`).catch(() => null))
      )
      userSeries.value = results.filter((r) => r?.data).map((r) => r.data)
      for (const [id, stateId] of seriesStateMap) {
        seriesStates.value[id] = stateId
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
}

.series-card__count {
  color: var(--tx-3);
  font-size: 12.5px;
}

.plugin-list {
  margin: 0;
  padding: 0;
  list-style: none;
  border: 1px solid var(--line);
  background: var(--panel);
}

.plugin-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 8px 16px;
  padding: 10px 14px;
  border-bottom: 1px solid var(--line);
  font-size: 14px;
}

.plugin-row:last-child {
  border-bottom: 0;
}

.plugin-row__name {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  font-weight: 600;
}

.plugin-row__name .faint {
  font-weight: 400;
}

.plugin-row__versions {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}
</style>

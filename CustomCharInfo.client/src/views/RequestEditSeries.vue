<template>
  <PageShell
    title="Request to edit a series"
    lede="Series edits require admin approval. Pick a series, then explain what you would like to change. An admin will review the request and grant or deny edit access."
  >
    <template #subnav>
      <SubNav section="series" label="Series" />
    </template>

    <div v-if="loading" class="request-layout" aria-busy="true">
      <div class="series-grid">
        <Skeleton v-for="n in 12" :key="n" variant="line" height="110px" />
      </div>
      <SkeletonPanel :lines="3" />
    </div>

    <EmptyState
      v-else-if="!mySeries.length"
      message="You don't have any movesets assigned to a series yet."
      icon="mdi-shape-outline"
    />

    <div v-else class="request-layout reveal">
      <!-- Left: series grid -->
      <div class="series-grid">
        <button
          v-for="s in mySeries"
          :key="s.seriesId"
          type="button"
          class="series-item"
          :class="{ 'series-item--selected': selected?.seriesId === s.seriesId }"
          @click="select(s)"
        >
          <img :src="resolveIconUrl(s.seriesIconUrl)" class="series-item__icon" alt="" />
          <span class="series-item__label">{{ s.seriesName }}</span>
        </button>
      </div>

      <!-- Right: action panel -->
      <div class="panel action-panel">
        <div v-if="!selected" class="action-placeholder">
          <v-icon size="30">mdi-cursor-default-click</v-icon>
          <p>Select a series to request an edit.</p>
        </div>

        <div v-else class="action-content">
          <h3 class="action-title">{{ selected.seriesName }}</h3>

          <!-- Pending admin action -->
          <template v-if="PENDING_ADMIN_STATES.includes(currentState)">
            <StatusTag :state="currentState" />
            <p class="muted small">Your request is pending. An admin will review it soon.</p>
          </template>

          <!-- Edit access granted -->
          <template v-else-if="PENDING_USER_STATES.includes(currentState)">
            <StatusTag variant="ok" icon="mdi-check-circle">Edit access granted</StatusTag>
            <AppButton
              :to="{ name: 'EditSeries', params: { seriesId: selected.seriesId } }"
              icon="mdi-pencil"
              block
            >
              Edit {{ selected.seriesName }}
            </AppButton>
          </template>

          <!-- Request form -->
          <template v-else>
            <LabeledField
              label="Why do you want to edit this series?"
              required
              note="admins only"
              :error="notesErrors[selected.seriesId]"
            >
              <v-textarea
                v-model="notes[selected.seriesId]"
                rows="4"
                auto-grow
                :error="!!notesErrors[selected.seriesId]"
              />
            </LabeledField>
            <AppButton
              variant="primary"
              icon="mdi-send"
              block
              :busy="submitting[selected.seriesId]"
              @click="requestEdit(selected.seriesId)"
            >
              Request to edit {{ selected.seriesName }}
            </AppButton>
            <p v-if="submitted[selected.seriesId]" class="note note--ok">Request submitted.</p>
          </template>
        </div>
      </div>
    </div>
  </PageShell>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import api from '@/services/api'
import seriesIconUnknown from '@/assets/series_icon_unknown.png'
import { ItemType, AcceptanceState, PENDING_ADMIN_STATES, PENDING_USER_STATES } from '@/globals'
import { latestStatesByItem } from '@/services/acceptanceStateDisplay'
import PageShell from '@/components/PageShell.vue'
import SubNav from '@/components/SubNav.vue'
import Skeleton from '@/components/Skeleton.vue'
import SkeletonPanel from '@/components/SkeletonPanel.vue'
import EmptyState from '@/components/EmptyState.vue'
import StatusTag from '@/components/StatusTag.vue'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'

const apiUrl = import.meta.env.VITE_API_URL
const resolveIconUrl = (path) =>
  path?.startsWith('/') ? `${apiUrl}${path}` : (path ?? seriesIconUnknown)

const loading = ref(true)
const mySeries = ref([])
// Series id to its newest acceptance state id, from /logs/latest.
const latestStateBySeries = reactive({})
const selected = ref(null)
const notes = reactive({})
const notesErrors = reactive({})
const submitting = reactive({})
const submitted = reactive({})

const currentState = computed(() =>
  selected.value ? (latestStateBySeries[selected.value.seriesId] ?? null) : null
)

const select = (s) => {
  selected.value = s
  notesErrors[s.seriesId] = ''
}

onMounted(async () => {
  try {
    const [seriesRes, logsRes] = await Promise.all([
      api.get('/series'),
      api.get('/logs/latest', { params: { itemTypes: [ItemType.Series] } }),
    ])

    mySeries.value = seriesRes.data
      .filter((s) => s.isUserModder)
      .sort((a, b) => a.seriesName.localeCompare(b.seriesName))

    for (const [sid, stateId] of latestStatesByItem(logsRes.data, ItemType.Series)) {
      latestStateBySeries[sid] = stateId
    }
  } catch (err) {
    console.error('Failed to load series data:', err)
  } finally {
    loading.value = false
  }
})

const requestEdit = async (seriesId) => {
  notesErrors[seriesId] = ''
  const note = notes[seriesId]?.trim()

  if (!note) {
    notesErrors[seriesId] = 'Please explain why you want to edit this series.'
    return
  }

  submitting[seriesId] = true
  try {
    await api.post(`/series/${seriesId}/request-edit`, { notes: note })
    submitted[seriesId] = true
    latestStateBySeries[seriesId] = AcceptanceState.PendingAdminSoft
  } catch (err) {
    notesErrors[seriesId] = err.response?.data ?? 'Failed to submit request. Please try again.'
    console.error(err)
  } finally {
    submitting[seriesId] = false
  }
}
</script>

<style scoped>
.request-layout {
  display: grid;
  grid-template-columns: minmax(0, 3fr) minmax(280px, 2fr);
  gap: 28px;
  align-items: start;
}

/* Series grid (left) */
.series-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(96px, 1fr));
  gap: 8px;
}

.series-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 12px 8px;
  border: 1px solid var(--line);
  background: var(--panel);
  color: var(--tx-2);
  font: inherit;
  font-size: 13px;
  line-height: 1.25;
  text-align: center;
  cursor: pointer;
  transition:
    border-color var(--dur-fast) var(--ease),
    background-color var(--dur-fast) var(--ease),
    color var(--dur-fast) var(--ease);
}

.series-item:hover {
  background: var(--panel-2);
  color: var(--white);
}

.series-item--selected,
.series-item--selected:hover {
  border-color: var(--white);
  background: var(--panel-2);
  color: var(--white);
}

.series-item__icon {
  width: 56px;
  height: 56px;
  object-fit: contain;
  filter: brightness(4.35);
}

.series-item__label {
  word-break: break-word;
}

/* Action panel (right) */
.action-panel {
  position: sticky;
  top: 20px;
  min-height: 200px;
}

.action-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  height: 160px;
  color: var(--tx-3);
  text-align: center;
}

.action-placeholder p {
  margin: 0;
}

.action-content {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 14px;
}

.action-title {
  margin: 0;
  font-size: 20px;
  text-transform: uppercase;
  letter-spacing: 0.01em;
}

.action-content > .field {
  align-self: stretch;
}

.note {
  margin: 0;
  padding: 10px 14px;
  border: 1px solid var(--line-2);
  border-left: 4px solid var(--ok);
  background: var(--panel);
  font-size: 14px;
  align-self: stretch;
}

@media (max-width: 959px) {
  .request-layout {
    grid-template-columns: 1fr;
  }

  .action-panel {
    position: static;
  }
}
</style>

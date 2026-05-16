<template>
  <v-container max-width="1000px">
    <h1 class="mb-3 page-title">Request to Edit a Series</h1>
    <p class="mb-6 text-medium-emphasis">
      Series edits require admin approval.
      Select a series below, then submit a request explaining what you'd like to change.
      An admin will review it and grant or deny edit access.
    </p>

    <div v-if="loading" class="text-center py-10">
      <v-progress-circular indeterminate color="grey" />
    </div>

    <p v-else-if="!mySeries.length" class="text-medium-emphasis">
      You don't have any movesets assigned to a series yet.
    </p>

    <div v-else class="page-layout">
      <!-- Left: series grid -->
      <div class="series-grid">
        <div
          v-for="s in mySeries"
          :key="s.seriesId"
          class="series-item"
          :class="{ 'series-item--selected': selected?.seriesId === s.seriesId }"
          @click="select(s)"
        >
          <img
            :src="resolveIconUrl(s.seriesIconUrl)"
            class="series-icon"
            alt=""
          />
          <span class="series-label">{{ s.seriesName }}</span>
        </div>
      </div>

      <!-- Right: action panel -->
      <div class="action-panel">
        <div v-if="!selected" class="action-placeholder">
          <v-icon size="32" class="mb-2 text-medium-emphasis">mdi-cursor-default-click</v-icon>
          <p class="text-medium-emphasis">Select a series to request an edit.</p>
        </div>

        <div v-else class="action-content">
          <h2 class="selected-title mb-4">{{ selected.seriesName }}</h2>

          <!-- Awaiting admin review -->
          <template v-if="currentState === 1 || currentState === 2">
            <v-chip color="blue-lighten-3" variant="tonal" size="small" class="mb-3">
              <v-icon start size="14">mdi-clock-outline</v-icon>
              Awaiting admin review
            </v-chip>
            <p class="text-medium-emphasis text-sm">
              Your request is pending. An admin will review it soon.
            </p>
          </template>

          <!-- Edit access granted -->
          <template v-else-if="currentState === 3 || currentState === 4">
            <v-chip color="green-lighten-2" variant="tonal" size="small" class="mb-4">
              <v-icon start size="14">mdi-check-circle</v-icon>
              Edit access granted
            </v-chip>
            <div>
              <v-btn
                :to="{ name: 'EditSeries', params: { seriesId: selected.seriesId } }"
                prepend-icon="mdi-pencil"
                variant="outlined"
                class="form-btn"
              >
                Edit {{ selected.seriesName }}
              </v-btn>
            </div>
          </template>

          <!-- Request form -->
          <template v-else>
            <v-textarea
              v-model="notes[selected.seriesId]"
              variant="outlined"
              label="Why do you want to edit this series?"
              placeholder="Required. Shown to admins only."
              rows="4"
              auto-grow
              class="mb-3"
              :error="!!notesErrors[selected.seriesId]"
              :error-messages="notesErrors[selected.seriesId]"
            />
            <v-btn
              variant="outlined"
              class="form-btn"
              :loading="submitting[selected.seriesId]"
              @click="requestEdit(selected.seriesId)"
            >
              Request to edit {{ selected.seriesName }}
            </v-btn>
            <p v-if="submitted[selected.seriesId]" class="text-green text-sm mt-3">
              Request submitted!
            </p>
          </template>
        </div>
      </div>
    </div>
  </v-container>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import api from '@/services/api'
import seriesIconUnknown from '@/assets/series_icon_unknown.png'

const apiUrl = import.meta.env.VITE_API_URL
const resolveIconUrl = (path) =>
  path?.startsWith('/') ? `${apiUrl}${path}` : (path ?? seriesIconUnknown)

const loading = ref(true)
const mySeries = ref([])
const latestLogBySeries = reactive({})
const selected = ref(null)
const notes = reactive({})
const notesErrors = reactive({})
const submitting = reactive({})
const submitted = reactive({})

const currentState = computed(() =>
  selected.value
    ? (latestLogBySeries[selected.value.seriesId]?.acceptanceState?.acceptanceStateId ?? null)
    : null
)

const select = (s) => {
  selected.value = s
  notesErrors[s.seriesId] = ''
}

onMounted(async () => {
  try {
    const [seriesRes, logsRes] = await Promise.all([
      api.get('/series'),
      api.get('/logs', { params: { itemTypes: [3] } }),
    ])

    mySeries.value = seriesRes.data
      .filter(s => s.isUserModder)
      .sort((a, b) => a.seriesName.localeCompare(b.seriesName))

    const seriesLogs = logsRes.data.filter(l => l.itemType?.itemTypeId === 3)
    for (const log of seriesLogs) {
      const sid = log.item?.seriesId
      if (!sid) continue
      const existing = latestLogBySeries[sid]
      if (!existing || new Date(log.createdAt) > new Date(existing.createdAt)) {
        latestLogBySeries[sid] = log
      }
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
    latestLogBySeries[seriesId] = {
      acceptanceState: { acceptanceStateId: 1 }
    }
  } catch (err) {
    notesErrors[seriesId] = err.response?.data ?? 'Failed to submit request. Please try again.'
    console.error(err)
  } finally {
    submitting[seriesId] = false
  }
}
</script>

<style scoped>
.page-title {
  font-size: 3em;
}

.page-layout {
  display: flex;
  gap: 2rem;
  align-items: flex-start;
}

/* ── Series grid (left) ── */
.series-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(90px, 1fr));
  gap: 8px;
  flex: 3 1 0;
  min-width: 0;
}

.series-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 7px;
  padding: 12px 8px;
  border-radius: 10px;
  border: 2px solid transparent;
  cursor: pointer;
  transition: border-color 150ms ease, background-color 150ms ease;
  text-align: center;
}

.series-item:hover {
  background-color: rgba(255, 255, 255, 0.05);
}

.series-item--selected {
  border-color: rgba(255, 255, 255, 0.55);
  background-color: rgba(255, 255, 255, 0.06);
}

.series-icon {
  width: 62px;
  height: 62px;
  object-fit: contain;
}

.series-label {
  line-height: 1.25;
  color: #ccc;
  word-break: break-word;
}

/* ── Action panel (right) ── */
.action-panel {
  flex: 2 1 0;
  max-width: 380px;
  flex-shrink: 0;
  position: sticky;
  top: 80px;
  background-color: #1e1e1e;
  border-radius: 12px;
  border: 1px solid rgba(255, 255, 255, 0.10);
  padding: 1.25rem;
  min-height: 200px;
}

.action-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 160px;
  text-align: center;
}

.action-content {
  display: flex;
  flex-direction: column;
}

.selected-title {
  font-size: 1.3em;
  font-weight: 600;
  line-height: 1.3;
}

.form-btn {
  text-transform: none;
  letter-spacing: normal;
  border-color: rgba(255, 255, 255, 0.28);
  color: #e2e2e2;
  width: 100%;
}

.form-btn:hover {
  border-color: rgba(255, 255, 255, 0.55);
}

.text-sm {
  font-size: 0.875rem;
}
</style>

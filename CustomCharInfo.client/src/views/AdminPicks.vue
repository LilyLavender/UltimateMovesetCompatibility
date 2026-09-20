<template>
  <v-container max-width="1200px">
    <h1 class="mb-5 page-title no-select">Admin Picks</h1>

    <!-- Controls -->
    <v-row class="mb-5" align="center">
      <!-- Select -->
      <v-col cols="12" sm="6">
        <v-select
          v-model="selectedMovesetId"
          variant="outlined"
          :items="movesetsOptions"
          label="Select a moveset"
          item-title="moddedCharName"
          item-value="movesetId"
          hide-details
          outlined
        />
      </v-col>
      <!-- Buttons -->
      <v-col cols="12" sm="6">
        <div class="d-flex justify-space-around">
          <!-- Add -->
          <v-btn
            color="primary"
            class="btn"
            :disabled="!selectedMovesetId || adminPicksIds.has(selectedMovesetId)"
            @click="addAdminPick"
          >
            <v-icon class="mr-1">mdi-account-plus</v-icon>
            Add Admin Pick
          </v-btn>

          <!-- Remove -->
          <v-btn
            color="error"
            class="btn"
            :disabled="!selectedMovesetId || !adminPicksIds.has(selectedMovesetId)"
            @click="removeAdminPick"
          >
            <v-icon class="mr-1">mdi-account-remove</v-icon>
            Remove Admin Pick
          </v-btn>

          <!-- Save -->
          <v-btn color="success" class="btn" :loading="saving" @click="saveAdminPicks">
            <v-icon class="mr-1">mdi-content-save</v-icon>
            Save Changes
          </v-btn>
        </div>
      </v-col>
    </v-row>

    <!-- Moveset Lists -->
    <h2>Admin Picks</h2>
    <div class="moveset-grid">
      <div
        v-for="m in adminPicksList"
        :key="m.movesetId"
        class="moveset-wrapper"
        @mouseenter="hoveredId = m.movesetId"
        @mouseleave="hoveredId = null"
      >
        <MovesetCard :moveset="m" />
        <div v-if="hiddenPills(m).length" class="pill-overlay">
          <span
            v-for="pill in hiddenPills(m)"
            :key="pill.label"
            class="state-pill"
            :style="{ backgroundColor: pill.color }"
            >{{ pill.label }}</span
          >
        </div>
        <AdminNotePopover
          v-model:note="notes[m.movesetId]"
          v-model:draft="drafts[m.movesetId]"
          :moveset-id="m.movesetId"
          :visible="hoveredId === m.movesetId"
          :is-pick="adminPicksIds.has(m.movesetId)"
          @toggle-pick="togglePick(m.movesetId)"
        />
      </div>
    </div>

    <h2>Other Movesets</h2>
    <div class="moveset-grid">
      <div
        v-for="m in nonAdminPicksList"
        :key="m.movesetId"
        class="moveset-wrapper"
        @mouseenter="hoveredId = m.movesetId"
        @mouseleave="hoveredId = null"
      >
        <MovesetCard :moveset="m" />
        <div v-if="hiddenPills(m).length" class="pill-overlay">
          <span
            v-for="pill in hiddenPills(m)"
            :key="pill.label"
            class="state-pill"
            :style="{ backgroundColor: pill.color }"
            >{{ pill.label }}</span
          >
        </div>
        <AdminNotePopover
          v-model:note="notes[m.movesetId]"
          v-model:draft="drafts[m.movesetId]"
          :moveset-id="m.movesetId"
          :visible="hoveredId === m.movesetId"
          :is-pick="adminPicksIds.has(m.movesetId)"
          @toggle-pick="togglePick(m.movesetId)"
        />
      </div>
    </div>
  </v-container>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/services/api'
import MovesetCard from '@/components/MovesetCard.vue'
import AdminNotePopover from '@/components/AdminNotePopover.vue'
import { useNotify } from '@/composables/useNotify'
import { ItemType, ALL_ACCEPTANCE_STATES } from '@/globals'
import { PRIVATE_COLOR, statusPillFor, latestLogsByItem } from '@/services/acceptanceStateDisplay'

const notify = useNotify()

const movesets = ref([])
const selectedMovesetId = ref(null)
const saving = ref(false)
const movesetStates = ref({})

// Set of moveset IDs currently marked as admin picks
const adminPicksIds = ref(new Set())

// Admin notes keyed by moveset id: { note, updatedByUserName, updatedAt }
const notes = ref({})

// Which card the pointer is over; its note popover opens after a short delay.
const hoveredId = ref(null)

// Unsaved note text per moveset, kept here so it survives a card moving between the two lists.
const drafts = ref({})

onMounted(async () => {
  try {
    const [movesetsRes, notesRes, logsRes] = await Promise.all([
      api.get('/movesets', { params: { pageSize: 1000, includeHidden: true } }),
      api.get('/movesets/admin-notes').catch(() => ({ data: [] })),
      api
        .get('/logs', {
          params: {
            viewAll: true,
            acceptanceStates: ALL_ACCEPTANCE_STATES,
            itemTypes: [ItemType.Moveset],
          },
        })
        .catch(() => ({ data: [] })),
    ])
    movesets.value = movesetsRes.data
    adminPicksIds.value = new Set(
      movesetsRes.data.filter((m) => m.adminPick).map((m) => m.movesetId)
    )
    notes.value = Object.fromEntries(notesRes.data.map((n) => [n.movesetId, n]))
    drafts.value = Object.fromEntries(notesRes.data.map((n) => [n.movesetId, n.note]))
    movesetStates.value = Object.fromEntries(
      [...latestLogsByItem(logsRes.data, ItemType.Moveset, (i) => i?.movesetId)].map(
        ([id, log]) => [id, log.acceptanceState?.acceptanceStateId]
      )
    )
  } catch (err) {
    console.error('Failed to fetch movesets:', err)
  }
})

// This page loads hidden movesets too, so mark the ones the public cannot see.
const hiddenPills = (m) => {
  const pills = []
  const state = statusPillFor(movesetStates.value[m.movesetId])
  if (state) pills.push(state)
  if (m.privateMoveset) pills.push({ label: 'Private', color: PRIVATE_COLOR })
  return pills
}

// Compute lists for display
const adminPicksList = computed(() =>
  movesets.value.filter((m) => adminPicksIds.value.has(m.movesetId))
)

const nonAdminPicksList = computed(() =>
  movesets.value.filter((m) => !adminPicksIds.value.has(m.movesetId))
)

// Dropdown
const movesetsOptions = computed(() => movesets.value)

const addAdminPick = () => {
  if (selectedMovesetId.value) {
    adminPicksIds.value.add(selectedMovesetId.value)
  }
}

const removeAdminPick = () => {
  if (selectedMovesetId.value) {
    adminPicksIds.value.delete(selectedMovesetId.value)
  }
}

// From the note popover. Pending until Save Changes, like the buttons above.
const togglePick = (movesetId) => {
  if (adminPicksIds.value.has(movesetId)) adminPicksIds.value.delete(movesetId)
  else adminPicksIds.value.add(movesetId)
}

const saveAdminPicks = async () => {
  saving.value = true
  try {
    const idsToSend = Array.from(adminPicksIds.value)
    await api.post('/movesets/set-admin-picks', idsToSend)
    notify.success('Admin picks updated successfully!')
  } catch (err) {
    console.error('Failed to save admin picks:', err)
    notify.error('Failed to save admin picks. Please try again.', err)
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.page-title {
  font-size: 5em;
  margin-top: 0.5em;
}

.btn {
  text-transform: unset;
  letter-spacing: 0.009375em;
  font-size: medium;
  background-color: #2e2e2e;
  color: #e2e2e2;
}

.btn:disabled {
  background-color: grey !important;
}

.notes-hint {
  color: #888;
  font-size: 0.9em;
  margin-bottom: 1rem;
}

.moveset-grid {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 4px;
  margin-bottom: 2rem;
}

/* Hugs the 340px card so the corner icon and the popover line up with its edges */
.moveset-wrapper {
  position: relative;
  flex: 0 0 auto;
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
</style>

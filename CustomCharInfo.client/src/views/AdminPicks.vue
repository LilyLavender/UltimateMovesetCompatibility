<template>
  <PageShell
    title="Admin picks"
    :back-to="{ name: 'AdminPortal' }"
    back-label="Admin portal"
    lede="The movesets featured on the home page. Hover a card for its private admin note."
  >
    <div class="controls">
      <LabeledField label="Moveset" class="controls__pick">
        <v-select
          v-model="selectedMovesetId"
          :items="movesetsOptions"
          placeholder="Pick a moveset"
          item-title="moddedCharName"
          item-value="movesetId"
          hide-details
        />
      </LabeledField>
      <div class="controls__actions">
        <AppButton
          icon="mdi-star-plus"
          :disabled="!selectedMovesetId || adminPicksIds.has(selectedMovesetId)"
          @click="addAdminPick"
        >
          Add pick
        </AppButton>
        <AppButton
          variant="ghost"
          icon="mdi-star-minus"
          :disabled="!selectedMovesetId || !adminPicksIds.has(selectedMovesetId)"
          @click="removeAdminPick"
        >
          Remove pick
        </AppButton>
        <AppButton variant="primary" icon="mdi-content-save" :busy="saving" @click="saveAdminPicks">
          Save changes
        </AppButton>
      </div>
    </div>

    <SectionHeading title="Admin picks" :count="adminPicksList.length" />
    <SkeletonList v-if="loading" :count="6" />
    <div v-else class="moveset-grid">
      <div
        v-for="m in adminPicksList"
        :key="m.movesetId"
        class="moveset-wrapper"
        @mouseenter="hoveredId = m.movesetId"
        @mouseleave="hoveredId = null"
      >
        <MovesetCard :moveset="m" />
        <div class="tag-overlay">
          <StatusTag
            v-if="statusPillFor(movesetStates[m.movesetId])"
            :state="movesetStates[m.movesetId]"
          />
          <StatusTag v-if="m.privateMoveset" variant="err">Private</StatusTag>
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

    <SectionHeading title="Other movesets" :count="nonAdminPicksList.length" />
    <SkeletonList v-if="loading" :count="9" />
    <div v-else class="moveset-grid">
      <div
        v-for="m in nonAdminPicksList"
        :key="m.movesetId"
        class="moveset-wrapper"
        @mouseenter="hoveredId = m.movesetId"
        @mouseleave="hoveredId = null"
      >
        <MovesetCard :moveset="m" />
        <div class="tag-overlay">
          <StatusTag
            v-if="statusPillFor(movesetStates[m.movesetId])"
            :state="movesetStates[m.movesetId]"
          />
          <StatusTag v-if="m.privateMoveset" variant="err">Private</StatusTag>
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
  </PageShell>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/services/api'
import PageShell from '@/components/PageShell.vue'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'
import SectionHeading from '@/components/SectionHeading.vue'
import SkeletonList from '@/components/SkeletonList.vue'
import StatusTag from '@/components/StatusTag.vue'
import MovesetCard from '@/components/MovesetCard.vue'
import AdminNotePopover from '@/components/AdminNotePopover.vue'
import { useNotify } from '@/composables/useNotify'
import { ItemType, ALL_ACCEPTANCE_STATES } from '@/globals'
import { statusPillFor, latestLogsByItem } from '@/services/acceptanceStateDisplay'

const notify = useNotify()

const movesets = ref([])
const selectedMovesetId = ref(null)
const saving = ref(false)
const loading = ref(true)
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
  } finally {
    loading.value = false
  }
})

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
.controls {
  display: flex;
  align-items: flex-end;
  flex-wrap: wrap;
  gap: 12px 20px;
}

.controls__pick {
  flex: 1 1 280px;
  max-width: 420px;
}

.controls__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.moveset-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, 340px);
  justify-content: center;
  gap: 0;
}

/* Hugs the 340px card so the corner icon and the popover line up with its edges */
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
</style>

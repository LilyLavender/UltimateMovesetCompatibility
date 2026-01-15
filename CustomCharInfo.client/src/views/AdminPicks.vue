<template>
  <v-container max-width="1200px">
    <h1 class="mb-5 page-title no-select">Admin Picks</h1>

    <!-- Controls -->
    <v-row class="mb-5" align="center">
      <!-- Select -->
      <v-col cols="12" sm="6">
        <v-select
          variant="outlined"
          v-model="selectedMovesetId"
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
            @click="addAdminPick"
            :disabled="!selectedMovesetId || adminPicksIds.has(selectedMovesetId)"
          >
            <v-icon class="mr-1">mdi-account-plus</v-icon>
            Add Admin Pick
          </v-btn>

          <!-- Remove -->
          <v-btn
            color="error"
            class="btn"
            @click="removeAdminPick"
            :disabled="!selectedMovesetId || !adminPicksIds.has(selectedMovesetId)"
          >
            <v-icon class="mr-1">mdi-account-remove</v-icon>
            Remove Admin Pick
          </v-btn>

          <!-- Save -->
          <v-btn 
            color="success"
            class="btn"
            @click="saveAdminPicks"
            :loading="saving"
          >
            <v-icon class="mr-1">mdi-content-save</v-icon>
            Save Changes
          </v-btn>
        </div>
      </v-col>
    </v-row>

    <!-- Moveset Lists -->
    <h2>Admin Picks</h2>
    <MovesetList :movesets="adminPicksList" />

    <h2>Other Movesets</h2>
    <MovesetList :movesets="nonAdminPicksList" />
  </v-container>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/services/api'
import MovesetList from '@/components/MovesetList.vue'

const movesets = ref([])
const selectedMovesetId = ref(null)
const saving = ref(false)

// Set of moveset IDs currently marked as admin picks
const adminPicksIds = ref(new Set())

onMounted(async () => {
  try {
    const res = await api.get('/movesets', { params: { pageSize: 1000 } })
    movesets.value = res.data
    adminPicksIds.value = new Set(res.data.filter(m => m.adminPick).map(m => m.movesetId))
  } catch (err) {
    console.error('Failed to fetch movesets:', err)
  }
})

// Compute lists for display
const adminPicksList = computed(() =>
  movesets.value.filter(m => adminPicksIds.value.has(m.movesetId))
)

const nonAdminPicksList = computed(() =>
  movesets.value.filter(m => !adminPicksIds.value.has(m.movesetId))
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

const saveAdminPicks = async () => {
  saving.value = true
  try {
    const idsToSend = Array.from(adminPicksIds.value)
    await api.post('/movesets/set-admin-picks', idsToSend)
    alert('Admin picks updated successfully!')
  } catch (err) {
    console.error('Failed to save admin picks:', err)
    alert('Failed to save admin picks. Please try again.')
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
</style>

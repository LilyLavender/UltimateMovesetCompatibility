<template>
  <v-container max-width="1200px">
    <h1 class="mb-4 page-title no-select">Action Log Manager</h1>

    <v-form @submit.prevent="submitLog">
      <v-row>
        <!-- Left column -->
        <v-col cols="12" md="5">
          <!-- Row 1: Select item -->
          <v-row>
            <!-- Title -->
            <v-col cols="12" md="4">
              <h2 class="center-entire">
                Select Item
              </h2>
            </v-col>

            <!-- Item Type -->
            <v-col cols="12" md="4">
              <v-select
                variant="outlined"
                v-model="form.itemTypeId"
                :items="itemTypes"
                item-title="label"
                item-value="value"
                label="Item Type"
                required
                hide-details
                @update:modelValue="fetchItems"
              />
            </v-col>

            <!-- Item ID -->
            <v-col cols="12" md="4">
              <v-select
                variant="outlined"
                v-model="form.itemId"
                :items="items"
                item-title="name"
                item-value="id"
                label="Item"
                :disabled="!form.itemTypeId"
                required
                hide-details
              />
            </v-col>
          </v-row>

          <v-divider class="my-6" />

          <!-- Row 2: Acceptance -->
          <v-row>
            <!-- Title -->
            <v-col cols="12" md="4">
              <h2 class="center-entire">
                Create Log
              </h2>
            </v-col>

            <!-- Acceptance State -->
            <v-col cols="12" md="8">
              <v-select
                variant="outlined"
                v-model="form.acceptanceStateId"
                :items="acceptanceStates"
                item-title="name"
                item-value="id"
                label="Acceptance State"
                required
                hide-details
              />
            </v-col>
          </v-row>

          <!-- Row 3: Notes -->
          <v-row>
            <v-col cols="12">
              <v-textarea
                variant="outlined"
                v-model="form.notes"
                label="Notes"
                rows="3"
                auto-grow
              />
            </v-col>
          </v-row>

          <!-- Row 4: Submit -->
          <v-row>
            <!-- Submit -->
            <v-col cols="12" md="5">
              <v-btn type="submit" class="btn">
                <v-icon class="mr-1">mdi-file-document-plus</v-icon>
                Create Action Log
              </v-btn>
            </v-col>

            <!-- Feedback -->
            <v-col cols="12" md="7">
              <p v-if="success" class="text-green mt-3">Action Log {{ success }} submitted at {{ (new Date()).toLocaleTimeString("en-US") }}</p>
              <p v-if="error" class="text-red mt-3">{{ error }}</p>
            </v-col>
          </v-row>
        </v-col>

        <!-- Right column -->
        <v-col cols="12" md="7">
          <!-- Action log display -->
          <h2>Action Logs for Item</h2>
          <v-row v-if="itemLogs.length">
            <v-col
              v-for="log in itemLogs"
              :key="log.actionLogId"
              cols="12"
            >
              <ActionLogItem :log="log" isAdmin="false" />
            </v-col>
          </v-row>
        </v-col>
      </v-row>
    </v-form>
  </v-container>
</template>

<script setup>
import { ref, watch, onMounted } from 'vue'
import api from '@/services/api'
import ActionLogItem from '@/components/ActionLogItem.vue'

const form = ref({
  userId: null,
  itemTypeId: null,
  itemId: null,
  acceptanceStateId: null,
  notes: ''
})

// Todo get from api
const itemTypes = [
  { label: 'Moveset', value: 1 },
  { label: 'Modder', value: 2 },
  { label: 'Series', value: 3 },
]

// Todo get from api (exclude 7, admins shouldn't have access to it)
const acceptanceStates = [
  { id: 1, name: 'Pending Admin Action (Soft)' },
  { id: 2, name: 'Pending Admin Action (Hard)' },
  { id: 3, name: 'Pending User Action (Soft)' },
  { id: 4, name: 'Pending User Action (Hard)' },
  { id: 5, name: 'Accepted' },
  { id: 6, name: 'Rejected' },
]

const items = ref([])
const success = ref(null)
const error = ref(null)
const itemLogs = ref([])
const loadingLogs = ref(false)

const fetchUser = async () => {
  const res = await api.get('/auth/me')
  form.value.userId = res.data.id
}

const fetchItems = async () => {
  try {
    if (form.value.itemTypeId === 1) {
      const res = await api.get('/movesets')
      items.value = res.data.sort((a, b) => a.moddedCharName.localeCompare(b.moddedCharName))
        .map(m => ({ id: m.movesetId, name: m.moddedCharName }))
    } else if (form.value.itemTypeId === 2) {
      const res = await api.get('/modders')
      items.value = res.data.sort((a, b) => a.name.localeCompare(b.name))
        .map(m => ({ id: m.modderId, name: m.name }))
    } else if (form.value.itemTypeId === 3) {
      const res = await api.get('/series')
      items.value = res.data.sort((a, b) => a.seriesName.localeCompare(b.seriesName))
        .map(s => ({ id: s.seriesId, name: s.seriesName }))
    } else {
      items.value = []
    }
  } catch (err) {
    console.error('Failed to fetch items:', err)
  }
}

// TODO should reuse action log list for this and make action log list reusable
const fetchItemLogs = async () => {
  if (!form.value.itemTypeId || !form.value.itemId) {
    itemLogs.value = []
    return
  }

  loadingLogs.value = true

  try {
    const res = await api.get(`/logs/${form.value.itemTypeId}-${form.value.itemId}`)
    itemLogs.value = res.data
  } catch (err) {
    console.error('Failed to fetch item logs:', err)
    itemLogs.value = []
  } finally {
    loadingLogs.value = false
  }
}

const submitLog = async () => {
  try {
    const res = await api.post('/logs', form.value)
    success.value = res.data.actionLogId
    error.value = null
    fetchItemLogs()
  } catch (err) {
    success.value = false
    error.value = 'Failed to submit action log.'
    console.error(err)
  }
}

// Fetch item logs when itemId changes
watch(
  () => [form.value.itemTypeId, form.value.itemId],
  fetchItemLogs
)

onMounted(fetchUser)
</script>

<style scoped>
.page-title {
  font-size: 5em;
  margin-top: 0.5em;
}

.center-entire {
  text-align: center;
}

div:has(>.center-entire) {
  align-content: center;
}

.btn {
  text-transform: unset;
  letter-spacing: 0.009375em;
  font-size: medium;
  background-color: #2e2e2e;
  color: #e2e2e2;
}
</style>
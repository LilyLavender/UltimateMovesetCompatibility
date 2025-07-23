<template>
  <v-container max-width="1020px">
    <h1 class="mb-4 page-title no-select">Admin Accepter</h1>
    <p class="mb-4">Welcome to the super secret admin area no one should access but the admins!</p>

    <v-form @submit.prevent="submitLog">
      <!-- Top row: ItemType, Item, AcceptanceState -->
      <v-row>
        <v-col cols="12" md="4">
          <v-select
            v-model="form.itemTypeId"
            :items="itemTypes"
            item-title="label"
            item-value="value"
            label="Item Type"
            required
            @update:modelValue="fetchItems"
          />
        </v-col>

        <v-col cols="12" md="4">
          <v-select
            v-model="form.itemId"
            :items="items"
            item-title="name"
            item-value="id"
            label="Item"
            :disabled="!form.itemTypeId"
            required
          />
        </v-col>

        <v-col cols="12" md="4">
          <v-select
            v-model="form.acceptanceStateId"
            :items="acceptanceStates"
            item-title="name"
            item-value="id"
            label="Acceptance State"
            required
          />
        </v-col>
      </v-row>

      <!-- Notes row -->
      <v-row>
        <v-col cols="12">
          <v-textarea
            v-model="form.notes"
            label="Notes"
            rows="3"
            auto-grow
          />
        </v-col>
      </v-row>

      <!-- Submit -->
      <v-btn color="primary" type="submit">Submit</v-btn>
    </v-form>

    <p v-if="success" class="text-green mt-3">Action Log {{ success }} submitted at {{ (new Date()).toLocaleTimeString("en-US") }}</p>
    <p v-if="error" class="text-red mt-3">{{ error }}</p>
  </v-container>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'

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

const submitLog = async () => {
  try {
    const res = await api.post('/logs', form.value)
    success.value = res.data.actionLogId
    error.value = null
  } catch (err) {
    success.value = false
    error.value = 'Failed to submit action log.'
    console.error(err)
  }
}

onMounted(fetchUser)
</script>

<style scoped>
.page-title {
  font-size: 5em;
  margin-top: 0.5em;
}
</style>
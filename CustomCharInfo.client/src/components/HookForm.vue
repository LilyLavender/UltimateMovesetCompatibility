<template>
  <div v-if="(isEditMode && hook) || (!isEditMode && form)">
    <v-container max-width="1020px">
      <!-- Header -->
      <h1 v-if="isEditMode">Edit Hook</h1>
      <h1 v-else>Add Hook</h1>

      <!-- Basic Info -->
      <section>
        <v-row>
          <!-- Offset and the version it was read from (add mode only; edits go per version below) -->
          <template v-if="!isEditMode">
            <v-col cols="12" sm="4">
              <v-text-field
                v-model="offsetInput"
                variant="outlined"
                label="Offset (hex)"
                placeholder="123ABC"
                prefix="0x"
              />
            </v-col>

            <v-col cols="12" sm="4">
              <v-select
                v-model="form.gameVersionId"
                variant="outlined"
                :items="gameVersions"
                item-title="name"
                item-value="gameVersionId"
                label="Game Version"
              />
            </v-col>
          </template>

          <!-- Hookable Status -->
          <v-col cols="12" sm="4">
            <v-select
              v-model="form.hookableStatusId"
              variant="outlined"
              :items="hookableStatuses"
              item-title="name"
              item-value="hookableStatusId"
              label="Hookable Status"
            />
          </v-col>
        </v-row>

        <v-row>
          <!-- Description -->
          <v-col cols="12">
            <v-textarea
              v-model="form.description"
              variant="outlined"
              label="Description (what the hook normally handles)"
              auto-grow
              rows="1"
            />
          </v-col>
        </v-row>
      </section>

      <!-- Offsets by version -->
      <section v-if="isEditMode">
        <h2>Offsets by version</h2>
        <p class="help-text">
          Confirm an offset once you have checked it against that version of the game, or type a
          corrected one and save it.
        </p>

        <v-row v-for="row in versionRows" :key="row.gameVersionId" class="version-row" dense>
          <v-col cols="12" sm="2" class="version-label">{{ row.name }}</v-col>
          <v-col cols="12" sm="4">
            <v-text-field
              v-model="offsetDrafts[row.gameVersionId]"
              variant="outlined"
              density="compact"
              prefix="0x"
              :placeholder="row.entry ? '' : 'not recorded'"
              hide-details
            />
          </v-col>
          <v-col cols="12" sm="3" class="d-flex align-center">
            <span
              v-if="row.entry"
              class="offset-pill"
              :class="`offset-state-${row.entry.offsetStateId}`"
            >
              {{ OFFSET_STATE_NAMES[row.entry.offsetStateId] || row.entry.offsetState }}
            </span>
            <span v-else class="offset-pill offset-state-none">No offset</span>
          </v-col>
          <v-col cols="12" sm="3" class="d-flex align-center justify-end">
            <v-btn
              class="btn version-button"
              size="small"
              :disabled="!canSubmitRow(row) || savingVersionId === row.gameVersionId"
              :loading="savingVersionId === row.gameVersionId"
              @click="submitRow(row)"
            >
              {{ rowButtonLabel(row) }}
            </v-btn>
          </v-col>
        </v-row>
      </section>

      <!-- Notes + Submit -->
      <div class="d-flex align-start ga-3 justify-end">
        <v-textarea
          v-model="form.notes"
          variant="outlined"
          density="compact"
          :label="isEditMode ? 'Editing notes' : 'Submission notes'"
          placeholder="Optional, shown to admins only."
          rows="1"
          auto-grow
          hide-details
          class="notes-field"
        />
        <v-btn class="btn submit-button mt-1" @click="submit">
          {{ isEditMode ? 'Save' : 'Add Hook' }}
        </v-btn>
      </div>
    </v-container>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useHead } from '@unhead/vue'
import api from '@/services/api'
import { useNotify } from '@/composables/useNotify'
import { OffsetState, OFFSET_STATE_NAMES } from '@/globals'

const notify = useNotify()

const props = defineProps({
  mode: { type: String, default: 'add' },
  hookId: { type: Number, default: null },
})

const isEditMode = computed(() => props.mode === 'edit')

useHead(computed(() => (isEditMode.value ? { title: 'UMC | Editing Hook' } : {})))
const router = useRouter()

const hook = ref(null)

const form = ref({
  offset: '',
  gameVersionId: null,
  description: '',
  hookableStatusId: null,
  notes: '',
})

const offsetInput = ref('')
const hookableStatuses = ref([])
const gameVersions = ref([])

// Edit mode: one draft per game version, keyed by id, compared against the saved entry to decide the button.
const offsetDrafts = ref({})
const savingVersionId = ref(null)

onMounted(async () => {
  await Promise.all([fetchHookableStatuses(), fetchGameVersions()])

  if (isEditMode.value && props.hookId) {
    try {
      const res = await api.get(`/hooks/${props.hookId}`)
      applyHook(res.data)
    } catch (err) {
      console.error(err)
      router.replace({ name: 'ErrorPage', query: { http: 404 } })
    }
  } else {
    form.value.gameVersionId = gameVersions.value.find((v) => v.isLatest)?.gameVersionId ?? null
  }
})

const applyHook = (data) => {
  hook.value = data
  form.value.description = data.description
  form.value.hookableStatusId = data.hookableStatusId
  offsetDrafts.value = Object.fromEntries(
    (data.offsets ?? []).map((o) => [o.gameVersionId, o.offset])
  )
}

const fetchHookableStatuses = async () => {
  const res = await api.get('/hookablestatuses')
  hookableStatuses.value = res.data
}

const fetchGameVersions = async () => {
  const res = await api.get('/game-versions')
  gameVersions.value = res.data
}

watch(offsetInput, (val) => {
  form.value.offset = val.toUpperCase()
})

const normalizeDraft = (value) =>
  (value ?? '')
    .trim()
    .replace(/^0x/i, '')
    .replace(/^0+(?=.)/, '')
    .toUpperCase()

const versionRows = computed(() =>
  gameVersions.value.map((v) => ({
    gameVersionId: v.gameVersionId,
    name: v.name,
    entry: hook.value?.offsets?.find((o) => o.gameVersionId === v.gameVersionId) ?? null,
  }))
)

const rowChanged = (row) => {
  const draft = normalizeDraft(offsetDrafts.value[row.gameVersionId])
  return draft.length > 0 && draft !== (row.entry?.offset ?? '')
}

const canSubmitRow = (row) => {
  if (rowChanged(row)) return true
  return !!row.entry && row.entry.offsetStateId !== OffsetState.Confirmed
}

const rowButtonLabel = (row) => {
  if (rowChanged(row)) return 'Save'
  if (row.entry?.offsetStateId === OffsetState.Confirmed) return 'Confirmed'
  return 'Confirm'
}

const submitRow = async (row) => {
  const payload = { notes: form.value.notes }
  if (rowChanged(row)) payload.offset = normalizeDraft(offsetDrafts.value[row.gameVersionId])

  savingVersionId.value = row.gameVersionId
  try {
    const res = await api.put(`/hooks/${props.hookId}/offsets/${row.gameVersionId}`, payload)
    const others = (hook.value.offsets ?? []).filter((o) => o.gameVersionId !== row.gameVersionId)
    hook.value.offsets = [...others, res.data].sort((a, b) => b.gameVersionId - a.gameVersionId)
    offsetDrafts.value[row.gameVersionId] = res.data.offset
    notify.success(`Offset for ${row.name} ${payload.offset ? 'saved' : 'confirmed'}.`)
  } catch (err) {
    if (err.response?.status === 409 || err.response?.status === 400) {
      notify.warning(err.response.data)
      return
    }
    notify.error('Failed to save offset.', err)
  } finally {
    savingVersionId.value = null
  }
}

const submit = async () => {
  // Validation
  if (!isEditMode.value && !form.value.offset) {
    notify.warning('Offset is required')
    return
  }
  if (!form.value.description) {
    notify.warning('Description is required')
    return
  }

  const payload = isEditMode.value
    ? {
        description: form.value.description,
        hookableStatusId: form.value.hookableStatusId,
        notes: form.value.notes,
      }
    : { ...form.value }

  try {
    if (isEditMode.value && props.hookId) {
      await api.put(`/hooks/${props.hookId}`, payload)
    } else {
      await api.post('/hooks', payload)
    }
    router.push('/hooks')
  } catch (err) {
    if (err.response?.status === 409 || err.response?.status === 400) {
      notify.warning(err.response.data)
      return
    }
    console.error('Submit failed:', JSON.stringify(err.response?.data) || err.message)
    notify.error('Failed to save hook.', err)
  }
}
</script>

<style scoped>
section {
  margin-bottom: 2rem;
  background-color: #1e1e1e;
  padding: 1em;
  border-radius: 10px;
}

h1 {
  font-size: 3.25em;
}

h2 {
  font-size: 1.6em;
  margin-bottom: 0.25em;
}

.help-text {
  font-size: 0.85rem;
  opacity: 0.7;
  margin-bottom: 1em;
}

.version-row {
  align-items: center;
}

.version-label {
  font-weight: bold;
}

.version-button {
  background-color: #2e2e2e;
  color: #e2e2e2;
  box-shadow: none;
}

.submit-button {
  background-color: #1e1e1e;
  color: #e2e2e2;
}

.offset-pill {
  display: inline-block;
  padding: 1px 7px;
  border-radius: 10px;
  font-size: 0.75rem;
  font-weight: 500;
  white-space: nowrap;
}
.offset-state-1 {
  background-color: #2e7d32;
  color: white;
}
.offset-state-2 {
  background-color: #fbc02d;
  color: black;
}
.offset-state-3 {
  background-color: #ef6c00;
  color: white;
}
.offset-state-none {
  background-color: #3a3a3a;
  color: #bbbbbb;
}

.notes-field {
  max-width: 400px;
}
.notes-field :deep(.v-field__input) {
  font-size: 0.85rem;
  padding-top: 6px;
  padding-bottom: 6px;
}
.notes-field :deep(.v-label) {
  font-style: italic;
  color: #6e6e6e !important;
}
</style>

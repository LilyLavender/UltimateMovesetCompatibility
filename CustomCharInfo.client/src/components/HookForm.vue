<template>
  <PageShell
    v-if="(isEditMode && hook) || (!isEditMode && form)"
    :title="isEditMode ? 'Edit hook' : 'Submit a hook'"
    :head="false"
  >
    <FormLayout>
      <FormSection id="hook" title="Hook">
        <div class="form-grid">
          <!-- Offset and the version it was read from (add mode only; edits go per version below) -->
          <template v-if="!isEditMode">
            <LabeledField label="Offset" required hint="Hex, without leading zeros.">
              <v-text-field
                v-model="offsetInput"
                placeholder="123ABC"
                prefix="0x"
                class="mono-input"
              />
            </LabeledField>
            <LabeledField label="Game version" required>
              <v-select
                v-model="form.gameVersionId"
                :items="gameVersions"
                item-title="name"
                item-value="gameVersionId"
              />
            </LabeledField>
          </template>

          <LabeledField label="Hookable status" required>
            <v-select
              v-model="form.hookableStatusId"
              :items="hookableStatuses"
              item-title="name"
              item-value="hookableStatusId"
            />
          </LabeledField>

          <LabeledField
            label="Description"
            required
            hint="What the hook normally handles."
            class="span-3"
          >
            <v-textarea v-model="form.description" auto-grow rows="1" />
          </LabeledField>
        </div>
      </FormSection>

      <!-- Offsets by version -->
      <FormSection
        v-if="isEditMode"
        id="offsets"
        title="Offsets by version"
        description="Confirm an offset once you have checked it against that version of the game, or type a corrected one and save it."
      >
        <div class="version-rows">
          <div v-for="row in versionRows" :key="row.gameVersionId" class="version-row">
            <span class="version-row__name">{{ row.name }}</span>
            <v-text-field
              v-model="offsetDrafts[row.gameVersionId]"
              density="compact"
              prefix="0x"
              :placeholder="row.entry ? '' : 'not recorded'"
              hide-details
              class="mono-input version-row__input"
            />
            <StatusTag v-if="row.entry" :offset-state="row.entry.offsetStateId" />
            <StatusTag v-else variant="outline">No offset</StatusTag>
            <AppButton
              size="sm"
              :variant="rowChanged(row) ? 'primary' : 'default'"
              :disabled="!canSubmitRow(row)"
              :busy="savingVersionId === row.gameVersionId"
              class="version-row__button"
              @click="submitRow(row)"
            >
              {{ rowButtonLabel(row) }}
            </AppButton>
          </div>
        </div>
      </FormSection>

      <template #savebar>
        <LabeledField
          :label="isEditMode ? 'Editing notes' : 'Submission notes'"
          note="admins only"
          class="savebar-notes"
        >
          <v-textarea v-model="form.notes" density="compact" rows="1" auto-grow hide-details />
        </LabeledField>
        <span class="savebar-spacer"></span>
        <AppButton variant="primary" icon="mdi-check" :busy="isSubmitting" @click="submit">
          {{ isEditMode ? 'Save changes' : 'Submit hook' }}
        </AppButton>
      </template>
    </FormLayout>
  </PageShell>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useHead } from '@unhead/vue'
import api from '@/services/api'
import { useNotify } from '@/composables/useNotify'
import { OffsetState } from '@/globals'
import PageShell from '@/components/PageShell.vue'
import FormLayout from '@/components/FormLayout.vue'
import FormSection from '@/components/FormSection.vue'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'
import StatusTag from '@/components/StatusTag.vue'

const notify = useNotify()

const props = defineProps({
  mode: { type: String, default: 'add' },
  hookId: { type: Number, default: null },
})

const isEditMode = computed(() => props.mode === 'edit')

useHead(
  computed(() => ({ title: isEditMode.value ? 'UMC | Editing hook' : 'UMC | Submit a hook' }))
)
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
const isSubmitting = ref(false)

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

  isSubmitting.value = true
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
  } finally {
    isSubmitting.value = false
  }
}
</script>

<style scoped>
.form-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
  align-items: start;
}

.span-3 {
  grid-column: span 3;
}

.mono-input :deep(input),
.mono-input :deep(.v-text-field__prefix__text) {
  font-family: var(--font-mono);
}

.version-rows {
  display: flex;
  flex-direction: column;
}

.version-row {
  display: grid;
  grid-template-columns: 90px minmax(160px, 1fr) auto auto;
  gap: 12px;
  align-items: center;
  padding: 8px 0;
  border-bottom: 1px solid var(--line);
}

.version-row:last-child {
  border-bottom: 0;
}

.version-row__name {
  font-family: var(--font-mono);
  font-weight: 600;
}

.version-row__button {
  min-width: 110px;
}

.savebar-notes {
  flex: 1 1 320px;
  max-width: 480px;
}

.savebar-spacer {
  flex: 1;
}

@media (max-width: 959px) {
  .form-grid {
    grid-template-columns: 1fr;
  }

  .span-3 {
    grid-column: span 1;
  }

  .version-row {
    grid-template-columns: 1fr 1fr;
  }

  .version-row__input {
    grid-column: span 2;
  }
}
</style>

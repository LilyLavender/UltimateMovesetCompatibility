<template>
  <v-dialog v-bind="dialogProps" v-model="open" max-width="560px">
    <v-card>
      <v-card-title class="dialog-title">Merge plugin</v-card-title>
      <v-card-text class="dialog-body">
        <p v-if="source" class="context">
          Every version of <strong>{{ source.pluginName }}</strong> moves to the plugin you pick,
          and {{ source.pluginName }} is deleted.
        </p>
        <LabeledField label="Merge into" required>
          <v-autocomplete
            v-model="targetId"
            :items="candidates"
            :loading="loadingCandidates"
            item-title="name"
            item-value="pluginId"
            density="comfortable"
            hide-details
            autofocus
          />
        </LabeledField>
        <p v-if="!loadingCandidates && candidates.length === 0" class="faint">
          No other plugin of the same kind to merge into.
        </p>
        <p v-if="error" class="error-text">{{ error }}</p>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <AppButton variant="ghost" @click="close">Cancel</AppButton>
        <AppButton
          variant="danger"
          icon="mdi-call-merge"
          :busy="merging"
          :disabled="targetId == null"
          @click="merge"
        >
          Merge
        </AppButton>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { ref, watch } from 'vue'
import api from '@/services/api'
import { useNotify } from '@/composables/useNotify'
import { useDialogProps } from '@/composables/useDialogProps'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'

const dialogProps = useDialogProps()
const open = defineModel({ type: Boolean, default: false })
const props = defineProps({
  // { pluginId, pluginName, dependencyId } of the plugin being folded away.
  source: { type: Object, default: null },
})
const emit = defineEmits(['merged'])

const notify = useNotify()

const candidates = ref([])
const loadingCandidates = ref(false)
const targetId = ref(null)
const merging = ref(false)
const error = ref(null)

// Only plugins of the same kind can be targets
async function loadCandidates() {
  if (!props.source) return
  loadingCandidates.value = true
  try {
    const params =
      props.source.dependencyId != null
        ? { dependencyId: props.source.dependencyId }
        : { standalone: true }
    const res = await api.get('/plugins/search', { params })
    candidates.value = res.data.filter((p) => p.pluginId !== props.source.pluginId)
  } catch (err) {
    notify.error('Failed to load plugins.', err)
  } finally {
    loadingCandidates.value = false
  }
}

watch(open, (isOpen) => {
  if (!isOpen) return
  targetId.value = null
  error.value = null
  candidates.value = []
  loadCandidates()
})

function close() {
  open.value = false
}

async function merge() {
  if (!props.source || targetId.value == null) return
  merging.value = true
  error.value = null
  try {
    await api.post(`/plugins/${props.source.pluginId}/merge-into/${targetId.value}`)
    notify.success('Plugins merged.')
    emit('merged')
    open.value = false
  } catch (err) {
    error.value = typeof err.response?.data === 'string' ? err.response.data : null
    notify.error('Could not merge the plugins.', err)
  } finally {
    merging.value = false
  }
}
</script>

<style scoped>
.dialog-title {
  font-family: var(--font-condensed);
  font-weight: 700;
  text-transform: uppercase;
}

.dialog-body {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.context {
  margin: 0;
  color: var(--tx-2);
  font-size: 14px;
}

.error-text {
  margin: 0;
  color: var(--err);
  font-size: 13px;
}
</style>

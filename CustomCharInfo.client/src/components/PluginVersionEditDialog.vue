<template>
  <v-dialog v-bind="dialogProps" v-model="open" max-width="560px">
    <v-card>
      <v-card-title class="dialog-title">Edit version</v-card-title>
      <v-card-text class="dialog-body">
        <p v-if="version" class="context">
          {{ version.pluginName }}
          <span class="mono hash" :title="version.hash"
            >{{ version.hash.slice(0, 12) }}&hellip;</span
          >
        </p>
        <LabeledField label="Version" required>
          <v-text-field v-model="versionLabel" density="comfortable" hide-details autofocus />
        </LabeledField>
        <p v-if="error" class="error-text">{{ error }}</p>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <AppButton variant="ghost" @click="close">Cancel</AppButton>
        <AppButton
          variant="primary"
          icon="mdi-check"
          :busy="saving"
          :disabled="!normalizeVersionLabel(versionLabel)"
          @click="save"
        >
          Save
        </AppButton>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { ref, watch } from 'vue'
import api from '@/services/api'
import { normalizeVersionLabel } from '@/services/pluginVersion'
import { useNotify } from '@/composables/useNotify'
import { useDialogProps } from '@/composables/useDialogProps'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'

const dialogProps = useDialogProps()
const open = defineModel({ type: Boolean, default: false })
const props = defineProps({
  // { pluginId, pluginVersionId, pluginName, versionLabel, hash }
  version: { type: Object, default: null },
})
const emit = defineEmits(['saved'])

const notify = useNotify()

const versionLabel = ref('')
const saving = ref(false)
const error = ref(null)

watch(open, (isOpen) => {
  if (!isOpen || !props.version) return
  versionLabel.value = props.version.versionLabel ?? ''
  error.value = null
})

function close() {
  open.value = false
}

async function save() {
  if (!props.version) return
  saving.value = true
  error.value = null
  try {
    await api.put(`/plugins/${props.version.pluginId}/versions/${props.version.pluginVersionId}`, {
      versionLabel: normalizeVersionLabel(versionLabel.value),
    })
    notify.success('Version saved.')
    emit('saved')
    open.value = false
  } catch (err) {
    error.value = typeof err.response?.data === 'string' ? err.response.data : null
    notify.error('Could not save the version.', err)
  } finally {
    saving.value = false
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
  display: flex;
  align-items: baseline;
  gap: 10px;
  margin: 0;
  color: var(--tx-2);
  font-size: 14px;
}

.hash {
  color: var(--tx-3);
  font-size: 12px;
}

.error-text {
  margin: 0;
  color: var(--err);
  font-size: 13px;
}
</style>

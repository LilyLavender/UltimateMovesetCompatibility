<template>
  <section class="panel plugins-panel">
    <div class="plugins-panel__head">
      <h3 class="plugins-panel__title">Plugins</h3>
      <AppButton
        variant="ghost"
        size="sm"
        :icon="addPluginForm ? 'mdi-close' : 'mdi-plus'"
        @click="toggleAddForm"
      >
        {{ addPluginForm ? 'Cancel' : 'Add a plugin' }}
      </AppButton>
    </div>
    <p class="plugins-panel__hint">
      <router-link to="/plugin-lookup" target="_blank">Learn more about plugin lookup</router-link>
    </p>

    <!-- Add Plugin: upload first, the version field appears once we know it's not a duplicate. -->
    <v-expand-transition>
      <div v-if="addPluginForm" class="plugin-add-form">
        <PluginDropZone
          v-model="pluginForm.file"
          v-model:hash="pluginForm.hash"
          v-model:duplicate="pluginForm.duplicate"
          label="plugin.nro"
        />
        <div v-if="pluginForm.hash && !pluginForm.duplicate" class="plugin-add-form__row">
          <LabeledField label="Version" required class="plugin-add-form__version">
            <v-text-field
              v-model="pluginForm.versionLabel"
              placeholder="e.g. 1.0.0"
              density="compact"
              hide-details
            />
          </LabeledField>
          <AppButton variant="primary" :busy="savingPlugin" @click="submitPluginForm">
            Add plugin
          </AppButton>
          <span v-if="pluginFormError" class="plugin-add-form__error">{{ pluginFormError }}</span>
        </div>
      </div>
    </v-expand-transition>

    <!-- Plugin List: flat, one row per version -->
    <AppLoading v-if="loading" size="sm" label="Loading plugins" />
    <ul v-else-if="plugins.length" class="plugin-list">
      <li v-for="plugin in plugins" :key="plugin.pluginId" class="plugin-list__row">
        <span class="plugin-list__version">
          {{ displayVersion(plugin.versions[0].versionLabel) }}
          <StatusTag v-if="plugin.versions[0].isCurrent" variant="ok">Current</StatusTag>
        </span>
        <span class="plugin-list__hash mono">{{ plugin.versions[0].hash }}</span>
        <button
          type="button"
          class="plugin-list__delete"
          aria-label="Delete plugin version"
          @click="deletePlugin(plugin)"
        >
          <v-icon size="18">mdi-delete</v-icon>
        </button>
      </li>
    </ul>
    <p v-else class="plugins-panel__empty">No plugin registered for this moveset yet.</p>
  </section>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import api from '@/services/api'
import PluginDropZone from '@/components/PluginDropZone.vue'
import AppButton from '@/components/AppButton.vue'
import AppLoading from '@/components/AppLoading.vue'
import LabeledField from '@/components/LabeledField.vue'
import StatusTag from '@/components/StatusTag.vue'
import { normalizeVersionLabel, displayVersion } from '@/services/pluginVersion'

const props = defineProps({
  movesetId: { type: [Number, String], required: true },
  movesetName: { type: String, default: '' },
})

const plugins = ref([])
const loading = ref(false)

const addPluginForm = ref(false)
const savingPlugin = ref(false)
const pluginFormError = ref(null)

const emptyPluginForm = () => ({
  file: null,
  hash: null,
  duplicate: null,
  versionLabel: '',
})
const pluginForm = ref(emptyPluginForm())

// The user never sees or picks this - it's just what the lookup page shows for this plugin.
const generatedName = computed(() => {
  const version = normalizeVersionLabel(pluginForm.value.versionLabel)
  return `${props.movesetName} ${displayVersion(version)}`
})

async function loadPlugins() {
  loading.value = true
  try {
    const res = await api.get('/plugins', { params: { moveset: props.movesetId } })
    plugins.value = res.data
  } catch {
    plugins.value = []
  } finally {
    loading.value = false
  }
}

function toggleAddForm() {
  addPluginForm.value = !addPluginForm.value
  if (!addPluginForm.value) pluginForm.value = emptyPluginForm()
  pluginFormError.value = null
}

async function submitPluginForm() {
  pluginFormError.value = null

  if (!pluginForm.value.hash || pluginForm.value.duplicate || !pluginForm.value.versionLabel) {
    pluginFormError.value = 'A plugin.nro file and a version are required.'
    return
  }

  savingPlugin.value = true
  try {
    await api.post('/plugins', {
      name: generatedName.value,
      movesetId: Number(props.movesetId),
      versionLabel: pluginForm.value.versionLabel,
      hash: pluginForm.value.hash,
    })
    toggleAddForm()
    await loadPlugins()
  } catch (err) {
    pluginFormError.value = err.response?.data ?? 'Failed to add plugin.'
  } finally {
    savingPlugin.value = false
  }
}

async function deletePlugin(plugin) {
  if (!confirm('Delete this plugin version? This cannot be undone.')) return
  try {
    await api.delete(`/plugins/${plugin.pluginId}`)
    await loadPlugins()
  } catch {
    /**/
  }
}

watch(() => props.movesetId, loadPlugins)
onMounted(loadPlugins)
</script>

<style scoped>
.plugins-panel {
  margin-bottom: 28px;
}

.plugins-panel__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.plugins-panel__title {
  margin: 0;
  font-size: 20px;
  text-transform: uppercase;
  letter-spacing: 0.01em;
}

.plugins-panel__hint {
  margin: 4px 0 14px;
  font-size: 12px;
}

.plugins-panel__hint a {
  color: var(--tx-2);
}

.plugin-add-form {
  display: flex;
  flex-direction: column;
  gap: 14px;
  margin-bottom: 18px;
}

.plugin-add-form__row {
  display: flex;
  align-items: flex-end;
  flex-wrap: wrap;
  gap: 12px;
}

.plugin-add-form__version {
  flex: 1 1 200px;
  max-width: 280px;
}

.plugin-add-form__error {
  color: var(--err);
  font-size: 13px;
  flex-basis: 100%;
}

.plugin-list {
  margin: 0;
  padding: 0;
  list-style: none;
}

.plugin-list__row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 0;
  border-bottom: 1px solid var(--line);
  font-size: 14px;
}

.plugin-list__row:last-child {
  border-bottom: 0;
}

.plugin-list__version {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  white-space: nowrap;
}

.plugin-list__hash {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  color: var(--tx-3);
  font-size: 12px;
}

.plugin-list__delete {
  display: flex;
  padding: 4px;
  border: 0;
  background: none;
  color: var(--tx-2);
  cursor: pointer;
}

.plugin-list__delete:hover {
  color: var(--err);
}

.plugins-panel__empty {
  margin: 0;
  color: var(--tx-3);
  font-size: 13px;
}
</style>

<template>
  <section>
    <!-- Header -->
    <h2>
      Plugins
      <v-btn
        variant="text"
        density="compact"
        icon="mdi-plus"
        class="rotate-toggle"
        :class="{ rotated: addPluginForm }"
        @click="toggleAddForm"
      />
    </h2>
    <!-- learn more -->
    <p class="subheader">
      <router-link to="/plugin-lookup" class="unvisitable" target="_blank">
        Learn more about plugin lookup
      </router-link>
    </p>

    <!-- Add Plugin: upload first, the version field appears once we know it's not a duplicate. -->
    <v-expand-transition>
      <div v-if="addPluginForm" class="plugin-add-form">
        <v-row>
          <v-col cols="12">
            <PluginDropZone
              v-model="pluginForm.file"
              v-model:hash="pluginForm.hash"
              v-model:duplicate="pluginForm.duplicate"
              label="plugin.nro"
            />
          </v-col>
          <template v-if="pluginForm.hash && !pluginForm.duplicate">
            <v-col cols="12" sm="6">
              <v-text-field variant="outlined" v-model="pluginForm.versionLabel" label="Version" placeholder="e.g. 1.0.0" />
            </v-col>
            <v-col cols="12" sm="2" class="justify-content-center">
              <v-btn @click="submitPluginForm" class="btn add-button" :loading="savingPlugin">Add Plugin</v-btn>
            </v-col>
            <v-col v-if="pluginFormError" cols="12" sm="10" class="d-flex align-center">
              <span class="text-red">{{ pluginFormError }}</span>
            </v-col>
          </template>
        </v-row>
      </div>
    </v-expand-transition>

    <!-- Plugin List: flat, one row per version -->
    <div v-if="loading" class="subheader">Loading plugins…</div>
    <v-list v-else>
      <v-list-item v-for="plugin in plugins" :key="plugin.pluginId">
        <v-list-item-title>
          {{ displayVersion(plugin.versions[0].versionLabel) }}{{ plugin.versions[0].isCurrent ? ' (current)' : '' }}
          <span class="hook-usage-dim">{{ plugin.versions[0].hash }}</span>
        </v-list-item-title>
        <template #append>
          <v-icon @click="deletePlugin(plugin)" class="delete-icon">mdi-delete</v-icon>
        </template>
      </v-list-item>
    </v-list>
  </section>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import api from '@/services/api'
import PluginDropZone from '@/components/PluginDropZone.vue'
import { normalizeVersionLabel, displayVersion } from '@/services/pluginVersion'

const props = defineProps({
  movesetId: { type: [Number, String], required: true },
  movesetName: { type: String, default: '' }
})

const plugins = ref([])
const loading = ref(false)

const addPluginForm = ref(false)
const savingPlugin = ref(false)
const pluginFormError = ref(null)

const emptyPluginForm = () => ({
  file: null, hash: null, duplicate: null, versionLabel: ''
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
  } catch { /**/ }
}

watch(() => props.movesetId, loadPlugins)
onMounted(loadPlugins)
</script>

<style scoped>
/* Shared with the Hooks/Articles sections in MovesetForm.vue - duplicated here because Vue's
   scoped styles don't cross component boundaries. This component renders the section as its
   own root element (not wrapped in another <section> by the parent), so it needs the card/
   heading rules too, not just the icon/button ones. */
section {
  margin-bottom: 2rem;
  background-color: #1e1e1e;
  padding: 1em;
  border-radius: 10px;
}
h2 {
  font-size: 2.25em;
  margin-bottom: 10px;
}
.add-button {
  background-color: #2e2e2e;
  color: #e2e2e2;
  margin-top: 10px;
  margin-left: 10px;
  box-shadow: none;
}
.btn {
  text-transform: unset;
  letter-spacing: 0.009375em;
  font-size: medium;
}
.edit-icon,
.delete-icon {
  background: none;
  font-size: 20px;
  margin-left: 8px;
  color: #aaaaaa;
  transition: color 150ms ease-in-out;
}
.edit-icon:hover,
.delete-icon:hover {
  color: #dddddd;
}
.edit-icon::before,
.delete-icon::before {
  margin-top: -4px;
}
.hook-usage-dim {
  opacity: 0.6;
}
.subheader {
  margin-top: -1.5em;
  margin-bottom: 0.5em;
  font-size: 12px;
}
:deep(.rotate-toggle > span > i::before) {
  transition: transform 250ms ease-in-out;
}
:deep(.rotate-toggle.rotated span > i::before) {
  transform: rotate(-45deg);
}
.plugin-add-form {
  margin-bottom: 1rem;
}
</style>

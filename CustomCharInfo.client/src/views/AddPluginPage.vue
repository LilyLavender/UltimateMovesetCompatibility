<template>
  <div class="add-plugin-page">
    <h1 class="page-title no-select">Submit a Plugin</h1>
    <p class="subtitle">
      For a plugin belonging to a shared dependency or unrelated to any moveset.
      To attach a plugin to a moveset, check out the moveset's edit page instead.
    </p>
    <p class="subtitle subtitle--note">
      Submissions here go through admin review before they're searchable on the
      <router-link to="/plugin-lookup" class="unvisitable">Plugin Lookup</router-link> page.
    </p>

    <v-row>
      <!-- Upload first. Everything else appears once we know it's not a duplicate -->
      <v-col cols="12" sm="6">
        <PluginDropZone
          :model-value="form.file"
          @update:model-value="onFilePicked"
          v-model:hash="form.hash"
          v-model:duplicate="form.duplicate"
          label="plugin.nro"
          large
        />
      </v-col>

      <template v-if="form.hash && !form.duplicate">
        <v-col cols="12">
          <v-radio-group v-model="attachment" inline hide-details>
            <v-radio label="Belongs to an existing dependency" value="dependency" />
            <v-radio label="Unrelated / other" value="other" />
          </v-radio-group>
        </v-col>

        <!-- Other -->
        <template v-if="attachment === 'other'">
          <v-col cols="12" sm="6">
            <v-combobox
              variant="outlined"
              v-model="pluginSelection"
              :items="otherPlugins"
              :custom-filter="filterByName"
              item-title="name"
              item-value="pluginId"
              return-object
              label="Plugin Name"
              hint="Pick an existing plugin to add a new version to it, or type a new name"
              persistent-hint
            />
          </v-col>
          <v-col cols="12" sm="4">
            <v-text-field variant="outlined" v-model="form.versionLabel" label="Version" placeholder="e.g. 1.0.0" hide-details />
          </v-col>
        </template>

        <!-- Dependency -->
        <template v-else>
          <v-col cols="12" sm="6">
            <v-autocomplete
              variant="outlined"
              v-model="form.dependencyId"
              :items="dependencies"
              item-title="name"
              item-value="dependencyId"
              label="Dependency"
              hide-details
            />
          </v-col>
          <v-col cols="12" sm="4">
            <v-text-field variant="outlined" v-model="form.versionLabel" label="Version" placeholder="e.g. 1.0.0" hide-details />
          </v-col>
        </template>

        <!-- Adding a version to an existing plugin -->
        <v-col v-if="isExistingSelected" cols="12">
          <div class="existing-preview">
            <p v-if="matchedPlugin.description" class="existing-preview-desc">{{ matchedPlugin.description }}</p>
            <a
              v-if="matchedPlugin.defaultLearnMoreUrl"
              :href="matchedPlugin.defaultLearnMoreUrl"
              target="_blank"
              rel="noopener"
              class="unvisitable"
            >
              Mod Link <v-icon size="small">mdi-open-in-new</v-icon>
            </a>
            <p v-else class="hook-usage-dim">No mod link set for this plugin.</p>
          </div>
        </v-col>
        <template v-else>
          <v-col v-if="attachment === 'other'" cols="12">
            <v-textarea variant="outlined" v-model="form.description" label="Description (optional)" rows="2" hide-details />
          </v-col>
          <v-col cols="12" sm="6">
            <v-text-field variant="outlined" v-model="form.defaultLearnMoreUrl" label="Mod Link (optional)" hide-details />
          </v-col>
        </template>

        <v-col cols="12">
          <div class="d-flex align-start ga-3 justify-end">
            <v-textarea
              variant="outlined"
              density="compact"
              v-model="form.notes"
              label="Notes for admins"
              placeholder="Optional, shown to admins only."
              rows="1"
              auto-grow
              hide-details
              class="notes-field"
            />
            <v-btn
              @click="submit"
              class="btn submit-button mt-1"
              :loading="submitting"
              :disabled="submitting"
            >
              Submit for review
            </v-btn>
          </div>
          <div class="submit-feedback">
            <span v-if="error" class="error-text">{{ error }}</span>
            <span v-if="submitted" class="success-text">Submitted!</span>
          </div>
        </v-col>
      </template>
    </v-row>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import api from '@/services/api'
import PluginDropZone from '@/components/PluginDropZone.vue'

const attachment = ref('other')
const dependencies = ref([])
const otherPlugins = ref([])
// Either a string (a brand-new name, "other" only),
// an existing-plugin object ({ pluginId, name, description, defaultLearnMoreUrl }),
// or null.
const pluginSelection = ref(null)
// The dependency plugin that already exists for the currently-selected dependency, if any.
const existingDependencyPlugin = ref(null)
const submitting = ref(false)
const submitted = ref(false)
const error = ref(null)

const isExistingSelected = computed(() => {
  if (attachment.value === 'dependency') return existingDependencyPlugin.value != null
  return pluginSelection.value != null && typeof pluginSelection.value === 'object'
})
const matchedPlugin = computed(() => attachment.value === 'dependency' ? existingDependencyPlugin.value : pluginSelection.value)

const emptyForm = () => ({
  description: '', defaultLearnMoreUrl: '',
  dependencyId: null,
  file: null, hash: null, duplicate: null, versionLabel: '', notes: ''
})
const form = ref(emptyForm())

function onFilePicked(file) {
  form.value.file = file
  submitted.value = false
  error.value = null
}

function filterByName(itemTitle, queryText, item) {
  return (item?.raw?.name ?? itemTitle ?? '').toLowerCase().includes(queryText.toLowerCase())
}

// A dependency plugin's identity is the dependency itself, not any one version of it.
// The version being submitted is shown separately, so the name doesn't bake in "v4.0.9"
// and then go stale the moment a different version (e.g. 4.0.8) gets added to the same plugin.
const selectedDependency = computed(() =>
  dependencies.value.find(d => d.dependencyId === form.value.dependencyId)
)
const generatedName = computed(() => selectedDependency.value?.name ?? '')

const resolvedName = computed(() => {
  if (isExistingSelected.value) return pluginSelection.value.name
  if (attachment.value === 'dependency') return generatedName.value
  return typeof pluginSelection.value === 'string' ? pluginSelection.value : ''
})

async function loadOtherPlugins() {
  try {
    const res = await api.get('/plugins/search', { params: { standalone: true } })
    otherPlugins.value = res.data
  } catch { otherPlugins.value = [] }
}

watch(attachment, () => { pluginSelection.value = null; existingDependencyPlugin.value = null })

// A dependency's own download link is the natural "mod link" for a plugin registered under it.
// A dependency can only ever have one plugin identity, so if it already has one,
// new versions attach to it automatically instead of registering a duplicate.
watch(() => form.value.dependencyId, async (id) => {
  const dep = dependencies.value.find(d => d.dependencyId === id)
  form.value.defaultLearnMoreUrl = dep?.downloadLink || ''
  existingDependencyPlugin.value = null
  if (id == null) return
  try {
    const res = await api.get('/plugins/search', { params: { dependencyId: id } })
    existingDependencyPlugin.value = res.data[0] ?? null
  } catch { existingDependencyPlugin.value = null }
})

async function submit() {
  error.value = null
  submitted.value = false

  if (!form.value.hash || form.value.duplicate || !form.value.versionLabel) {
    error.value = 'A plugin.nro file and a version are required.'
    return
  }

  submitting.value = true
  try {
    if (isExistingSelected.value) {
      await api.post(`/plugins/${matchedPlugin.value.pluginId}/versions`, {
        versionLabel: form.value.versionLabel,
        hash: form.value.hash,
        notes: form.value.notes || null,
      })
    } else {
      if (!resolvedName.value) {
        error.value = 'Name, a plugin.nro file, and a version are required.'
        return
      }
      if (attachment.value === 'dependency' && !form.value.dependencyId) {
        error.value = 'Pick which dependency this plugin belongs to.'
        return
      }

      await api.post('/plugins', {
        name: resolvedName.value,
        description: form.value.description || null,
        defaultLearnMoreUrl: form.value.defaultLearnMoreUrl || null,
        dependencyId: attachment.value === 'dependency' ? form.value.dependencyId : null,
        versionLabel: form.value.versionLabel,
        hash: form.value.hash,
        notes: form.value.notes || null,
      })
    }
    submitted.value = true
    form.value = emptyForm()
    pluginSelection.value = null
    existingDependencyPlugin.value = null
    if (attachment.value === 'other') await loadOtherPlugins()
  } catch (err) {
    error.value = err.response?.data ?? 'Failed to submit plugin.'
  } finally {
    submitting.value = false
  }
}

onMounted(async () => {
  try {
    const res = await api.get('/dependencies')
    dependencies.value = res.data
  } catch { dependencies.value = [] }
  loadOtherPlugins()
})
</script>

<style scoped>
.add-plugin-page {
  max-width: 900px;
  margin: 0 auto;
  padding: 2rem 1.5rem 4rem;
}
.page-title { font-size: 3em; margin-bottom: 0.15em; }
.subtitle { color: #aaa; margin-bottom: 1rem; line-height: 1.5; }
.subtitle--note { font-size: 0.9em; }
.error-text { color: #ef9a9a; font-size: 0.9em; margin-left: 0.75rem; }
.success-text { color: #81c784; font-size: 0.9em; margin-left: 0.75rem; }
.submit-feedback { text-align: right; }
.disabled :deep(input) {
  color: #484848;
}
.existing-preview {
  background-color: #1a1a1a;
  border-radius: 6px;
  padding: 0.75rem 1rem;
}
.existing-preview-desc { margin: 0 0 0.4rem; font-size: 0.92em; }
.hook-usage-dim { opacity: 0.6; }

/* Shared with MovesetForm.vue's "Notes + Submit" row - duplicated because Vue's scoped
   styles don't cross component boundaries. */
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
.submit-button {
  background-color: #2e2e2e;
  color: #e2e2e2;
}
.btn {
  text-transform: unset;
  letter-spacing: 0.009375em;
  font-size: medium;
}
</style>

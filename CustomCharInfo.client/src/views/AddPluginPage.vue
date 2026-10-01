<template>
  <PageShell title="Submit a plugin">
    <p class="intro">
      For a plugin belonging to a shared dependency or unrelated to any moveset. To attach a plugin
      to a moveset, use the moveset's edit page instead. Submissions go through admin review before
      they show up on the
      <router-link to="/plugin-lookup">plugin lookup</router-link> page.
    </p>

    <FormLayout>
      <FormSection
        id="file"
        title="File"
        description="Upload first. The rest appears once the file is not already known."
      >
        <PluginDropZone
          v-model:hash="form.hash"
          v-model:duplicate="form.duplicate"
          :model-value="form.file"
          label="plugin.nro"
          large
          @update:model-value="onFilePicked"
        />
      </FormSection>

      <FormSection v-if="form.hash && !form.duplicate" id="plugin" title="Plugin">
        <v-radio-group v-model="attachment" inline hide-details class="attachment-choice">
          <v-radio label="Belongs to an existing dependency" value="dependency" />
          <v-radio label="Unrelated or other" value="other" />
        </v-radio-group>

        <div class="form-grid">
          <LabeledField
            v-if="attachment === 'other'"
            label="Plugin name"
            required
            hint="Pick an existing plugin to add a new version to it, or type a new name."
            class="span-2"
          >
            <v-combobox
              v-model="pluginSelection"
              :items="otherPlugins"
              :custom-filter="filterByName"
              item-title="name"
              item-value="pluginId"
              return-object
            />
          </LabeledField>
          <LabeledField v-else label="Dependency" required class="span-2">
            <v-autocomplete
              v-model="form.dependencyId"
              :items="dependencies"
              item-title="name"
              item-value="dependencyId"
            />
          </LabeledField>

          <LabeledField label="Version" required>
            <v-text-field v-model="form.versionLabel" placeholder="e.g. 1.0.0" />
          </LabeledField>

          <!-- Adding a version to an existing plugin -->
          <div v-if="isExistingSelected" class="existing-preview span-3">
            <p v-if="matchedPlugin.description" class="existing-preview__desc">
              {{ matchedPlugin.description }}
            </p>
            <a
              v-if="matchedPlugin.defaultLearnMoreUrl"
              :href="matchedPlugin.defaultLearnMoreUrl"
              target="_blank"
              rel="noopener"
            >
              Mod link <v-icon size="small">mdi-open-in-new</v-icon>
            </a>
            <p v-else class="faint">No mod link set for this plugin.</p>
          </div>
          <template v-else>
            <LabeledField
              v-if="attachment === 'other'"
              label="Description"
              note="optional"
              class="span-3"
            >
              <v-textarea v-model="form.description" rows="2" />
            </LabeledField>
            <LabeledField label="Mod link" note="optional" class="span-2">
              <v-text-field v-model="form.defaultLearnMoreUrl" />
            </LabeledField>
          </template>
        </div>

        <p v-if="error" class="note note--err">{{ error }}</p>
        <p v-if="submitted" class="note note--ok">Submitted for review.</p>
      </FormSection>

      <template v-if="form.hash && !form.duplicate" #savebar>
        <LabeledField label="Notes for admins" note="optional" class="savebar-notes">
          <v-textarea v-model="form.notes" density="compact" rows="1" auto-grow hide-details />
        </LabeledField>
        <span class="savebar-spacer"></span>
        <AppButton variant="primary" icon="mdi-check" :busy="submitting" @click="submit">
          Submit for review
        </AppButton>
      </template>
    </FormLayout>
  </PageShell>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import api from '@/services/api'
import PageShell from '@/components/PageShell.vue'
import FormLayout from '@/components/FormLayout.vue'
import FormSection from '@/components/FormSection.vue'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'
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
const matchedPlugin = computed(() =>
  attachment.value === 'dependency' ? existingDependencyPlugin.value : pluginSelection.value
)

const emptyForm = () => ({
  description: '',
  defaultLearnMoreUrl: '',
  dependencyId: null,
  file: null,
  hash: null,
  duplicate: null,
  versionLabel: '',
  notes: '',
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
  dependencies.value.find((d) => d.dependencyId === form.value.dependencyId)
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
  } catch {
    otherPlugins.value = []
  }
}

watch(attachment, () => {
  pluginSelection.value = null
  existingDependencyPlugin.value = null
})

// A dependency's own download link is the natural "mod link" for a plugin registered under it.
// A dependency can only ever have one plugin identity, so if it already has one,
// new versions attach to it automatically instead of registering a duplicate.
watch(
  () => form.value.dependencyId,
  async (id) => {
    const dep = dependencies.value.find((d) => d.dependencyId === id)
    form.value.defaultLearnMoreUrl = dep?.downloadLink || ''
    existingDependencyPlugin.value = null
    if (id == null) return
    try {
      const res = await api.get('/plugins/search', { params: { dependencyId: id } })
      existingDependencyPlugin.value = res.data[0] ?? null
    } catch {
      existingDependencyPlugin.value = null
    }
  }
)

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
  } catch {
    dependencies.value = []
  }
  loadOtherPlugins()
})
</script>

<style scoped>
.intro {
  max-width: 720px;
  margin: 0 0 20px;
  color: var(--tx-2);
  line-height: 1.55;
}

.intro a,
.existing-preview a {
  color: var(--white);
  text-decoration: underline;
}

.attachment-choice {
  margin-bottom: 4px;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
  align-items: start;
}

.span-2 {
  grid-column: span 2;
}

.span-3 {
  grid-column: span 3;
}

.existing-preview {
  padding: 12px 14px;
  border: 1px solid var(--line);
  background: var(--panel-2);
  font-size: 14px;
}

.existing-preview__desc {
  margin: 0 0 6px;
}

.note {
  margin: 0;
  padding: 10px 14px;
  border: 1px solid var(--line-2);
  border-left: 4px solid var(--tx-3);
  background: var(--panel);
  color: var(--tx);
  font-size: 14px;
}

.note--err {
  border-left-color: var(--err);
}

.note--ok {
  border-left-color: var(--ok);
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

  .span-2,
  .span-3 {
    grid-column: span 1;
  }
}
</style>

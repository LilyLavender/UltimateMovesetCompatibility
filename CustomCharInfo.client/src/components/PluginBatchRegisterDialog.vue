<template>
  <v-dialog v-model="open" max-width="1100px" scrollable>
    <v-card color="#2e2e2e">
      <v-card-title class="d-flex align-center">
        <span>Register {{ selectedCount }} of {{ lines.length }} releases</span>
        <span v-if="repo" class="repo-label ml-2">{{ repo.owner }}/{{ repo.repo }}</span>
        <v-spacer />
        <v-btn icon="mdi-close" variant="text" @click="close" />
      </v-card-title>

      <v-card-text>
        <p class="helper-text mb-3">
          Every line below is added as a version of one plugin. Pick the plugin first, then adjust
          any line before submitting.
        </p>

        <v-radio-group v-model="attachment" inline hide-details class="mb-2">
          <v-radio label="Belongs to an existing dependency" value="dependency" />
          <v-radio label="Unrelated / other" value="other" />
        </v-radio-group>

        <v-row dense>
          <template v-if="attachment === 'other'">
            <v-col cols="12" sm="6">
              <v-combobox
                v-model="pluginSelection"
                variant="outlined"
                density="comfortable"
                :items="otherPlugins"
                :custom-filter="filterByName"
                item-title="name"
                item-value="pluginId"
                return-object
                label="Plugin Name"
                hint="Pick an existing plugin to add versions to it, or type a new name"
                persistent-hint
              />
            </v-col>
          </template>
          <template v-else>
            <v-col cols="12" sm="6">
              <v-autocomplete
                v-model="dependencyId"
                variant="outlined"
                density="comfortable"
                :items="dependencies"
                item-title="name"
                item-value="dependencyId"
                label="Dependency"
                hide-details
              />
            </v-col>
          </template>

          <v-col v-if="isExistingSelected" cols="12" sm="6">
            <div class="existing-preview">
              Adding versions to <strong>{{ matchedPlugin.name }}</strong>
              <span v-if="matchedPlugin.description" class="existing-desc">
                {{ matchedPlugin.description }}
              </span>
            </div>
          </v-col>
          <template v-else>
            <v-col cols="12" sm="6">
              <v-text-field
                v-model="defaultLearnMoreUrl"
                variant="outlined"
                density="comfortable"
                label="Mod Link (optional)"
                hide-details
              />
            </v-col>
            <v-col v-if="attachment === 'other'" cols="12">
              <v-textarea
                v-model="description"
                variant="outlined"
                density="comfortable"
                label="Description (optional)"
                rows="2"
                hide-details
              />
            </v-col>
          </template>
        </v-row>

        <v-table class="lines-table mt-4" density="compact">
          <thead>
            <tr>
              <th class="include-col">
                <v-checkbox-btn
                  :model-value="allSelected"
                  :indeterminate="someSelected && !allSelected"
                  @update:model-value="toggleAll"
                />
              </th>
              <th>Release</th>
              <th>Asset</th>
              <th>Version</th>
              <th>Mod Link</th>
              <th>Hash</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="line in lines" :key="line.key" :class="{ 'line--skipped': !line.include }">
              <td class="include-col">
                <v-checkbox-btn v-model="line.include" />
              </td>
              <td class="release-col">
                <span class="release-name">{{ line.name }}</span>
                <span class="release-tag">{{ line.tag }}</span>
              </td>
              <td class="asset-col">{{ line.asset }}</td>
              <td class="version-col">
                <v-text-field
                  v-model="line.versionLabel"
                  variant="outlined"
                  density="compact"
                  hide-details
                  :disabled="!line.include"
                />
              </td>
              <td class="link-col">
                <v-text-field
                  v-model="line.learnMoreUrl"
                  variant="outlined"
                  density="compact"
                  hide-details
                  :disabled="!line.include"
                />
              </td>
              <td class="hash-col" :title="line.hash">{{ line.hash.slice(0, 12) }}…</td>
            </tr>
          </tbody>
        </v-table>

        <v-textarea
          v-model="notes"
          variant="outlined"
          density="compact"
          label="Notes for the action log"
          placeholder="Optional, stored with every version's log entry."
          rows="1"
          auto-grow
          hide-details
          class="mt-4 notes-field"
        />
      </v-card-text>

      <v-card-actions>
        <span v-if="error" class="error-text">{{ error }}</span>
        <v-spacer />
        <v-btn variant="text" @click="close">Cancel</v-btn>
        <v-btn
          class="btn submit-button"
          :loading="submitting"
          :disabled="submitting || selectedCount === 0"
          @click="submit"
        >
          Register {{ selectedCount }} version{{ selectedCount === 1 ? '' : 's' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import api from '@/services/api'
import { normalizeVersionLabel } from '@/services/pluginVersion'
import { repoUrl } from '@/services/githubReleases'
import { useNotify } from '@/composables/useNotify'

const open = defineModel({ type: Boolean, default: false })
const props = defineProps({
  // Rows from githubReleases.applyMatches that are registrable.
  rows: { type: Array, default: () => [] },
  // { owner, repo } the rows came from.
  repo: { type: Object, default: null },
})
const emit = defineEmits(['registered'])

const notify = useNotify()

const attachment = ref('other')
const dependencies = ref([])
const otherPlugins = ref([])
// A string (new name), an existing plugin object, or null. Same shape as AddPluginPage.
const pluginSelection = ref(null)
const dependencyId = ref(null)
const existingDependencyPlugin = ref(null)
const description = ref('')
const defaultLearnMoreUrl = ref('')
const notes = ref('')
const lines = ref([])
const submitting = ref(false)
const error = ref(null)

const isExistingSelected = computed(() => {
  if (attachment.value === 'dependency') return existingDependencyPlugin.value != null
  return pluginSelection.value != null && typeof pluginSelection.value === 'object'
})
const matchedPlugin = computed(() =>
  attachment.value === 'dependency' ? existingDependencyPlugin.value : pluginSelection.value
)
const selectedDependency = computed(() =>
  dependencies.value.find((d) => d.dependencyId === dependencyId.value)
)
const resolvedName = computed(() => {
  if (isExistingSelected.value) return matchedPlugin.value.name
  if (attachment.value === 'dependency') return selectedDependency.value?.name ?? ''
  return typeof pluginSelection.value === 'string' ? pluginSelection.value.trim() : ''
})

const selectedCount = computed(() => lines.value.filter((l) => l.include).length)
const allSelected = computed(
  () => lines.value.length > 0 && selectedCount.value === lines.value.length
)
const someSelected = computed(() => selectedCount.value > 0)

function toggleAll(value) {
  for (const line of lines.value) line.include = value
}

function filterByName(itemTitle, queryText, item) {
  return (item?.raw?.name ?? itemTitle ?? '').toLowerCase().includes(queryText.toLowerCase())
}

function buildLines() {
  return props.rows.map((row) => ({
    key: row.key,
    include: true,
    name: row.name,
    tag: row.tag,
    asset: row.asset,
    hash: row.hash,
    versionLabel: normalizeVersionLabel(row.tag),
    learnMoreUrl: row.releaseUrl ?? '',
  }))
}

function resetForm() {
  attachment.value = 'other'
  pluginSelection.value = props.repo?.repo ?? null
  dependencyId.value = null
  existingDependencyPlugin.value = null
  description.value = ''
  defaultLearnMoreUrl.value = props.repo ? repoUrl(props.repo) : ''
  notes.value = props.repo ? `Registered from ${repoUrl(props.repo)}` : ''
  lines.value = buildLines()
  error.value = null
}

async function loadLookups() {
  try {
    const [depsRes, pluginsRes] = await Promise.all([
      api.get('/dependencies'),
      api.get('/plugins/search', { params: { standalone: true } }),
    ])
    dependencies.value = depsRes.data
    otherPlugins.value = pluginsRes.data
  } catch (err) {
    notify.error('Failed to load plugins and dependencies.', err)
  }
}

watch(open, (isOpen) => {
  if (!isOpen) return
  resetForm()
  loadLookups()
})

watch(attachment, () => {
  pluginSelection.value = attachment.value === 'other' ? (props.repo?.repo ?? null) : null
  existingDependencyPlugin.value = null
})

watch(dependencyId, async (id) => {
  const dep = dependencies.value.find((d) => d.dependencyId === id)
  defaultLearnMoreUrl.value = dep?.downloadLink || (props.repo ? repoUrl(props.repo) : '')
  existingDependencyPlugin.value = null
  if (id == null) return
  try {
    const res = await api.get('/plugins/search', { params: { dependencyId: id } })
    existingDependencyPlugin.value = res.data[0] ?? null
  } catch {
    existingDependencyPlugin.value = null
  }
})

function close() {
  open.value = false
}

function validate() {
  const selected = lines.value.filter((l) => l.include)
  if (selected.length === 0) return 'Select at least one release.'
  if (selected.some((l) => !normalizeVersionLabel(l.versionLabel)))
    return 'Every selected line needs a version.'
  if (!isExistingSelected.value) {
    if (!resolvedName.value) return 'A plugin name is required.'
    if (attachment.value === 'dependency' && !dependencyId.value)
      return 'Pick which dependency this plugin belongs to.'
  }
  return null
}

async function submit() {
  error.value = validate()
  if (error.value) return

  const versions = lines.value
    .filter((l) => l.include)
    .map((l) => ({
      hash: l.hash,
      versionLabel: l.versionLabel,
      learnMoreUrl: l.learnMoreUrl || null,
    }))
  const body = { versions, notes: notes.value || null }
  if (isExistingSelected.value) {
    body.pluginId = matchedPlugin.value.pluginId
  } else {
    body.newPlugin = {
      name: resolvedName.value,
      description: attachment.value === 'other' ? description.value || null : null,
      defaultLearnMoreUrl: defaultLearnMoreUrl.value || null,
      dependencyId: attachment.value === 'dependency' ? dependencyId.value : null,
    }
  }

  submitting.value = true
  try {
    const res = await api.post('/plugins/batch', body)
    notify.success(`Registered ${versions.length} version${versions.length === 1 ? '' : 's'}.`)
    emit('registered', res.data)
    open.value = false
  } catch (err) {
    error.value = typeof err.response?.data === 'string' ? err.response.data : null
    notify.error('Failed to register versions.', err)
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.repo-label {
  font-size: 0.8em;
  color: #9e9e9e;
  font-weight: normal;
}
.helper-text {
  color: #b0b0b0;
}
.existing-preview {
  background-color: #1a1a1a;
  border-radius: 6px;
  padding: 0.75rem 1rem;
  height: 100%;
}
.existing-desc {
  display: block;
  font-size: 0.9em;
  color: #b0b0b0;
  margin-top: 0.25rem;
}
/* The dialog is teleported outside App.vue, so its dark-table rules never reach this table. */
.lines-table,
.lines-table :deep(.v-table__wrapper),
.lines-table :deep(table),
.lines-table :deep(thead),
.lines-table :deep(tbody),
.lines-table :deep(tr) {
  background-color: #1e1e1e;
  color: #ccc;
}
.lines-table :deep(th) {
  background-color: #1e1e1e;
  color: #ddd;
}
.lines-table :deep(td),
.lines-table :deep(th) {
  vertical-align: middle;
  border-bottom-color: #333;
}
.include-col {
  width: 48px;
}
.release-col {
  min-width: 160px;
}
.release-name {
  display: block;
}
.release-tag {
  display: block;
  font-size: 0.8em;
  color: #9e9e9e;
}
.asset-col {
  min-width: 140px;
  font-family: monospace;
  font-size: 0.85em;
}
.version-col {
  width: 150px;
}
.link-col {
  min-width: 240px;
}
.hash-col {
  font-family: monospace;
  font-size: 0.85em;
  color: #9e9e9e;
  white-space: nowrap;
}
.line--skipped {
  opacity: 0.5;
}
.notes-field {
  max-width: 500px;
}
.notes-field :deep(.v-label) {
  font-style: italic;
  color: #6e6e6e !important;
}
.error-text {
  color: #ef9a9a;
  font-size: 0.9em;
  margin-left: 0.75rem;
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

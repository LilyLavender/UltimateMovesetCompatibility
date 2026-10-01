<template>
  <v-dialog v-bind="dialogProps" v-model="open" max-width="1100px" scrollable>
    <v-card>
      <div class="dialog-head">
        <div>
          <h3 class="dialog-title">Register {{ selectedCount }} of {{ lines.length }} releases</h3>
          <span v-if="repo" class="repo-label mono">{{ repo.owner }}/{{ repo.repo }}</span>
        </div>
        <button type="button" class="dialog-close" aria-label="Close" @click="close">
          <v-icon>mdi-close</v-icon>
        </button>
      </div>

      <v-card-text class="dialog-body">
        <p class="helper-text">
          Every line below is added as a version of one plugin. Pick the plugin first, then adjust
          any line before submitting.
        </p>

        <v-radio-group v-model="attachment" inline hide-details class="attachment-choice">
          <v-radio label="Belongs to an existing dependency" value="dependency" />
          <v-radio label="Unrelated or other" value="other" />
        </v-radio-group>

        <div class="fields">
          <LabeledField
            v-if="attachment === 'other'"
            label="Plugin name"
            hint="Pick an existing plugin to add versions to it, or type a new name."
          >
            <v-combobox
              v-model="pluginSelection"
              density="comfortable"
              :items="otherPlugins"
              :custom-filter="filterByName"
              item-title="name"
              item-value="pluginId"
              return-object
              hide-details
            />
          </LabeledField>
          <LabeledField v-else label="Dependency" required>
            <v-autocomplete
              v-model="dependencyId"
              density="comfortable"
              :items="dependencies"
              item-title="name"
              item-value="dependencyId"
              hide-details
            />
          </LabeledField>

          <div v-if="isExistingSelected" class="existing-preview">
            Adding versions to <strong>{{ matchedPlugin.name }}</strong>
            <span v-if="matchedPlugin.description" class="existing-desc">
              {{ matchedPlugin.description }}
            </span>
          </div>
          <template v-else>
            <LabeledField label="Mod link" note="optional">
              <v-text-field v-model="defaultLearnMoreUrl" density="comfortable" hide-details />
            </LabeledField>
            <LabeledField
              v-if="attachment === 'other'"
              label="Description"
              note="optional"
              class="fields__wide"
            >
              <v-textarea v-model="description" density="comfortable" rows="2" hide-details />
            </LabeledField>
          </template>
        </div>

        <TableScroll min-width="820px">
          <v-table class="lines-table" density="compact">
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
                <th>Mod link</th>
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
                  <span class="release-tag mono">{{ line.tag }}</span>
                </td>
                <td class="asset-col mono">{{ line.asset }}</td>
                <td class="version-col">
                  <v-text-field
                    v-model="line.versionLabel"
                    density="compact"
                    hide-details
                    :disabled="!line.include"
                  />
                </td>
                <td class="link-col">
                  <v-text-field
                    v-model="line.learnMoreUrl"
                    density="compact"
                    hide-details
                    :disabled="!line.include"
                  />
                </td>
                <td class="hash-col mono" :title="line.hash">
                  {{ line.hash.slice(0, 12) }}&hellip;
                </td>
              </tr>
            </tbody>
          </v-table>
        </TableScroll>

        <LabeledField label="Notes for the action log" note="optional" class="notes-field">
          <v-textarea
            v-model="notes"
            placeholder="Stored with every version's log entry."
            density="compact"
            rows="1"
            auto-grow
            hide-details
          />
        </LabeledField>
      </v-card-text>

      <div class="dialog-actions">
        <span v-if="error" class="error-text">{{ error }}</span>
        <span class="dialog-actions__spacer"></span>
        <AppButton variant="ghost" @click="close">Cancel</AppButton>
        <AppButton
          variant="primary"
          icon="mdi-check"
          :busy="submitting"
          :disabled="selectedCount === 0"
          @click="submit"
        >
          Register {{ selectedCount }} version{{ selectedCount === 1 ? '' : 's' }}
        </AppButton>
      </div>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import api from '@/services/api'
import { normalizeVersionLabel } from '@/services/pluginVersion'
import { repoUrl } from '@/services/githubReleases'
import { useNotify } from '@/composables/useNotify'
import { useDialogProps } from '@/composables/useDialogProps'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'
import TableScroll from '@/components/TableScroll.vue'
const dialogProps = useDialogProps()

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
.dialog-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding: 18px 20px 12px;
  border-bottom: 1px solid var(--line);
}

.dialog-title {
  margin: 0;
  font-size: 20px;
  text-transform: uppercase;
  letter-spacing: 0.01em;
}

.repo-label {
  font-size: 12px;
  color: var(--tx-3);
}

.dialog-close {
  display: flex;
  padding: 4px;
  border: 0;
  background: none;
  color: var(--tx-2);
  cursor: pointer;
}

.dialog-close:hover {
  color: var(--white);
}

.dialog-body {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 18px 20px !important;
}

.helper-text {
  margin: 0;
  color: var(--tx-2);
  font-size: 14px;
}

.attachment-choice {
  margin: -4px 0;
}

.fields {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px 16px;
  align-items: start;
}

.fields__wide {
  grid-column: span 2;
}

.existing-preview {
  align-self: stretch;
  padding: 12px 14px;
  border: 1px solid var(--line);
  background: var(--panel-2);
  font-size: 14px;
}

.existing-desc {
  display: block;
  margin-top: 4px;
  color: var(--tx-2);
  font-size: 13px;
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
  font-size: 12px;
  color: var(--tx-3);
}

.asset-col {
  min-width: 140px;
  font-size: 12px;
}

.version-col {
  width: 150px;
}

.link-col {
  min-width: 240px;
}

.hash-col {
  font-size: 12px;
  color: var(--tx-3);
  white-space: nowrap;
}

.line--skipped {
  opacity: 0.5;
}

.notes-field {
  max-width: 520px;
}

.dialog-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 20px 18px;
  border-top: 1px solid var(--line);
}

.dialog-actions__spacer {
  flex: 1;
}

.error-text {
  color: var(--err);
  font-size: 13px;
}

@media (max-width: 599px) {
  .fields {
    grid-template-columns: 1fr;
  }

  .fields__wide {
    grid-column: span 1;
  }
}
</style>

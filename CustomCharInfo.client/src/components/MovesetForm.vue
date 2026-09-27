<template>
  <PageShell
    v-if="(isEditMode && moveset) || (!isEditMode && form)"
    :title="isEditMode ? `Edit ${moveset.moddedCharName}` : 'Submit a moveset'"
    :head="false"
  >
    <p v-if="!isEditMode" class="form-intro">
      Not sure whether it's time?
      <router-link to="/moveset-submission-guide" target="_blank">
        Read when a moveset should be submitted.
      </router-link>
    </p>

    <FormLayout :sections="sections">
      <!-- Basic info -->
      <FormSection id="basic" title="Basic info">
        <div class="form-grid">
          <LabeledField label="Modded character name" required>
            <v-text-field v-model="form.moddedCharName" />
          </LabeledField>

          <LabeledField label="Modders" required>
            <v-select
              v-model="form.modderIds"
              :items="modders"
              item-title="name"
              item-value="modderId"
              multiple
              chips
              clearable
              :disabled="!canManageMembers"
            />
            <router-link to="/modder-credit-guide" target="_blank" class="field-link">
              Who should I include?
            </router-link>
          </LabeledField>

          <LabeledField
            label="Editors"
            :hint="
              canManageMembers
                ? 'Modders who can edit this moveset but are not credited on it.'
                : 'Only credited modders and full-access editors can change who is on this moveset.'
            "
          >
            <v-select
              v-model="form.editorIds"
              :items="editorCandidates"
              item-title="name"
              item-value="modderId"
              multiple
              chips
              clearable
              :disabled="!canManageMembers"
            />
            <div v-if="form.editorIds.length" class="editor-access-list">
              <div v-for="id in form.editorIds" :key="id" class="editor-access-row">
                <span class="editor-access-name">{{ modderName(id) }}</span>
                <v-switch
                  v-model="editorFullAccess[id]"
                  density="compact"
                  hide-details
                  label="Full access"
                  :disabled="!canManageMembers"
                />
              </div>
              <p class="editor-access-hint">
                Full access can also change the Modders and Editors lists. Partial access can edit
                everything else.
              </p>
            </div>
          </LabeledField>

          <LabeledField label="Series" required>
            <v-select
              v-model="form.seriesId"
              :items="seriesList"
              item-title="seriesName"
              item-value="seriesId"
            >
              <template #item="{ props: itemProps, item }">
                <v-list-item v-bind="itemProps" class="remove-bound-props">
                  <div class="filter-option">
                    <img
                      v-if="item.raw.seriesIconUrl"
                      :src="getFullImageUrl(item.raw.seriesIconUrl)"
                      class="series-icon series-icon-small"
                      alt=""
                    />
                    <v-list-item-title>{{ item.raw.seriesName }}</v-list-item-title>
                  </div>
                </v-list-item>
              </template>
              <template #selection="{ item }">
                <div class="filter-option">
                  <img
                    v-if="item.raw.seriesIconUrl"
                    :src="getFullImageUrl(item.raw.seriesIconUrl)"
                    class="series-icon series-icon-small"
                    alt=""
                  />
                  <span>{{ item.raw.seriesName }}</span>
                </div>
              </template>
            </v-select>
            <router-link to="/series/add" target="_blank" class="field-link">
              Don't see your series?
            </router-link>
          </LabeledField>

          <MovesetIdentityFields
            v-model:slotted-id="form.slottedId"
            v-model:replacement-id="form.replacementId"
            v-model:vanilla-char-internal-name="form.vanillaCharInternalName"
            v-model:slots-start="form.slotsStart"
            v-model:slots-end="form.slotsEnd"
            :vanilla-chars="vanillaChars"
          />

          <LabeledField label="Release date">
            <v-menu :close-on-content-click="false" transition="scale-transition">
              <template #activator="{ props: activatorProps }">
                <v-text-field
                  v-model="formattedReleaseDate"
                  readonly
                  clearable
                  placeholder="Pick a date"
                  v-bind="activatorProps"
                />
              </template>
              <v-date-picker
                v-model="releaseDatePickerValue"
                title="Release date"
                header="Select date"
              />
            </v-menu>
          </LabeledField>

          <LabeledField label="Availability" required>
            <v-select
              v-model="form.releaseStateId"
              :items="releaseStates"
              item-title="releaseStateName"
              item-value="releaseStateId"
            />
          </LabeledField>

          <LabeledField
            label="Modpack"
            hint="Leave blank unless the moveset is exclusive to a modpack."
          >
            <v-text-field v-model="form.modpackName" />
          </LabeledField>

          <LabeledField label="Dependencies" class="span-3">
            <v-select
              v-model="form.dependencyIds"
              :items="dependencies"
              item-title="name"
              item-value="dependencyId"
              multiple
              chips
              clearable
            />
          </LabeledField>
        </div>
      </FormSection>

      <!-- Display -->
      <FormSection id="display" title="Display">
        <p class="section-note">
          For information on image hosting in UMC, see
          <router-link to="/image-hosting" target="_blank">image hosting</router-link>.
        </p>
        <div class="form-grid form-grid--2">
          <LabeledField label="Thumbnail" note="340 by 82" hint="Shown in every moveset list.">
            <ImageUploadField
              v-model="form.thumbhImageUrl"
              :required-width="IMAGE_UPLOAD_SPECS.thumb_h.width"
              :required-height="IMAGE_UPLOAD_SPECS.thumb_h.height"
            />
            <a :href="thumbhUnknown" download class="field-link">Download placeholder image</a>
          </LabeledField>
          <LabeledField
            label="Render"
            note="1200 by 1200"
            hint="The character render, cropped to fit."
          >
            <ImageUploadField
              v-model="form.movesetHeroImageUrl"
              :required-width="IMAGE_UPLOAD_SPECS.moveset_hero.width"
              :required-height="IMAGE_UPLOAD_SPECS.moveset_hero.height"
            />
            <a :href="movesetHeroUnknown" download class="field-link">Download placeholder image</a>
          </LabeledField>
        </div>
        <div class="form-grid">
          <LabeledField label="Background color" note="hex">
            <v-text-field v-model="form.backgroundColor" maxlength="6" prefix="#">
              <template #append-inner>
                <span
                  class="color-swatch"
                  :style="{ backgroundColor: '#' + form.backgroundColor }"
                ></span>
              </template>
            </v-text-field>
          </LabeledField>
          <div class="check-item">
            <v-checkbox v-model="form.privateMoveset" label="Private" hide-details />
            <p class="check-hint">
              Hides the name, series, and images, and disables the detail page. Does not hide
              modders.
            </p>
          </div>
          <div class="check-item">
            <v-checkbox v-model="form.privateModder" label="Hide modder info" hide-details />
            <p class="check-hint">
              Hides the modder name from submissions. Only applies while the moveset is private.
            </p>
          </div>
        </div>
      </FormSection>

      <!-- Links -->
      <FormSection id="links" title="Links">
        <MovesetLinksSection
          v-model:mod-page-url="form.modPageUrl"
          v-model:gamebanana-wip-id="form.gamebananaWipId"
          v-model:mods-wiki-link="form.modsWikiLink"
          v-model:source-code="form.sourceCode"
        />
      </FormSection>

      <!-- Function usage -->
      <FormSection id="functions" title="Function usage">
        <div class="form-grid">
          <div v-for="fn in functionFlags" :key="fn.key" class="check-item check-item--fn">
            <v-checkbox
              v-model="form[fn.key]"
              :label="fn.label"
              true-icon="mdi-check-bold"
              false-icon="mdi-close-thick"
              hide-details
            />
            <p class="check-hint">{{ fn.hint }}</p>
          </div>
        </div>
      </FormSection>

      <FormSection id="articles" title="Cloned articles">
        <MovesetArticlesEditor v-model="form.articles" :article-options="articles" />
      </FormSection>

      <FormSection id="hooks" title="Hooks">
        <MovesetHooksEditor
          v-model="form.hooks"
          :hook-options="hooks"
          :character-name="form.moddedCharName"
        />
      </FormSection>

      <!-- Plugins (only available once the moveset exists) -->
      <MovesetPluginsPanel
        v-if="isEditMode && props.movesetId"
        id="plugins"
        :moveset-id="props.movesetId"
        :moveset-name="form.moddedCharName"
      />
      <FormSection v-else id="plugins" title="Plugins">
        <p class="section-note">
          Save this moveset first, then attach its plugin from the edit page.
        </p>
      </FormSection>

      <!-- Advanced -->
      <FormSection id="advanced" v-model:open="showAdvanced" title="Advanced" collapsible>
        <div class="form-grid">
          <div class="check-item">
            <v-checkbox
              v-model="form.isJokeMoveset"
              true-icon="mdi-egg-easter"
              false-icon="mdi-egg-outline"
              label="Joke moveset"
              hide-details
            />
            <p class="check-hint">
              Marks this as a joke moveset. Required for April Fools movesets.
            </p>
          </div>
          <LabeledField
            label="Subtitle"
            hint="Shown in parentheses after the name, for disambiguation only."
            class="span-2"
          >
            <v-text-field v-model="form.subtitle" placeholder="e.g. V2, Ult-S, Standalone" />
          </LabeledField>
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
          {{ uploadStatus || (isEditMode ? 'Save changes' : 'Submit moveset') }}
        </AppButton>
      </template>
    </FormLayout>
  </PageShell>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useHead } from '@unhead/vue'
import api from '@/services/api'
import PageShell from '@/components/PageShell.vue'
import FormLayout from '@/components/FormLayout.vue'
import FormSection from '@/components/FormSection.vue'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'
import ImageUploadField from '@/components/ImageUploadField.vue'
import MovesetPluginsPanel from '@/components/MovesetPluginsPanel.vue'
import MovesetIdentityFields from '@/components/MovesetIdentityFields.vue'
import MovesetLinksSection from '@/components/MovesetLinksSection.vue'
import MovesetArticlesEditor from '@/components/MovesetArticlesEditor.vue'
import MovesetHooksEditor from '@/components/MovesetHooksEditor.vue'
import { useImageUpload, isStagedFile } from '@/composables/useImageUpload'
import { useUnsavedChanges } from '@/composables/useUnsavedChanges'
import thumbhUnknown from '@/assets/thumb_h_unknown.png'
import movesetHeroUnknown from '@/assets/moveset_hero_unknown.png'
import { IMAGE_UPLOAD_SPECS } from '@/globals'
import { dateOnlyStringToLocalDate, localDateToDateOnlyString } from '@/services/dateOnly'
import { useNotify } from '@/composables/useNotify'

const notify = useNotify()

const props = defineProps({
  mode: { type: String, default: 'add' },
  movesetId: { type: Number, default: null },
})

const isEditMode = computed(() => props.mode === 'edit')
const router = useRouter()

const sections = [
  { id: 'basic', label: 'Basic info' },
  { id: 'display', label: 'Display' },
  { id: 'links', label: 'Links' },
  { id: 'functions', label: 'Function usage' },
  { id: 'articles', label: 'Articles' },
  { id: 'hooks', label: 'Hooks' },
  { id: 'plugins', label: 'Plugins' },
  { id: 'advanced', label: 'Advanced' },
]

const showAdvanced = ref(false)
const isSubmitting = ref(false)
const uploadStatus = ref('')

const apiUrl = import.meta.env.VITE_API_URL

const getFullImageUrl = (path) => {
  if (!path) return null
  return path.startsWith('/') ? `${apiUrl}${path}` : path
}

const getVanillaCharDisplayName = (internalName) => {
  const match = vanillaChars.value.find((v) => v.vanillaCharInternalName === internalName)
  return match ? match.displayName : internalName
}

const moveset = ref(null)

useHead(
  computed(() => ({
    title: moveset.value
      ? `UMC | Editing ${moveset.value.moddedCharName}`
      : 'UMC | Submit a moveset',
  }))
)

const form = ref({
  // Basic Info
  moddedCharName: '',
  modderIds: [],
  editorIds: [],
  seriesId: null,
  slottedId: null,
  replacementId: null,
  vanillaCharInternalName: '',
  slotsStart: null,
  slotsEnd: null,
  releaseDate: null,
  releaseStateId: null,
  modpackName: '',
  dependencyIds: [],
  // Display
  thumbhImageUrl: '',
  movesetHeroImageUrl: '',
  backgroundColor: '',
  privateMoveset: false,
  privateModder: false,
  // Links
  modPageUrl: '',
  gamebananaWipId: null,
  modsWikiLink: '',
  sourceCode: '',
  // Function Usage
  hasGlobalOpff: false,
  hasCharacterOpff: false,
  hasAgentInit: false,
  hasGlobalOnLinePre: false,
  hasGlobalOnLineEnd: false,
  // Articles & Hooks
  articles: [],
  hooks: [],
  // Advanced
  isJokeMoveset: false,
  subtitle: '',
  // Admin notes
  notes: '',
})

const functionFlags = computed(() => [
  { key: 'hasGlobalOpff', label: 'Global OPFF', hint: 'Runs once every frame for all characters.' },
  {
    key: 'hasCharacterOpff',
    label: 'Character OPFF',
    hint: `Runs once every frame for ${getVanillaCharDisplayName(form.value.vanillaCharInternalName) || 'the character'}.`,
  },
  { key: 'hasAgentInit', label: 'Agent init', hint: 'Runs once when a fighter is spawned in.' },
  {
    key: 'hasGlobalOnLinePre',
    label: 'Global on_line pre',
    hint: 'Runs once every time a pre status script runs.',
  },
  {
    key: 'hasGlobalOnLineEnd',
    label: 'Global on_line end',
    hint: 'Runs once every time an end status script runs.',
  },
])

const { uploadIfNeeded } = useImageUpload()
const { takeSnapshot, markSaved } = useUnsavedChanges(form)

const vanillaChars = ref([])
const seriesList = ref([])
const releaseStates = ref([])
const modders = ref([])

// Editors: which modders may edit without credit, and whether each may also manage the member lists.
// Only credited modders and full-access editors may change either list; partial editors see them disabled.
const editorFullAccess = ref({})
const canManageMembers = ref(true)

const editorCandidates = computed(() =>
  modders.value.filter((m) => !form.value.modderIds.includes(m.modderId))
)

const modderName = (id) => modders.value.find((m) => m.modderId === id)?.name ?? `#${id}`

// Someone credited as a modder cannot also be an editor.
watch(
  () => form.value.modderIds,
  (ids) => {
    form.value.editorIds = form.value.editorIds.filter((id) => !ids.includes(id))
  }
)

const editorsPayload = () =>
  form.value.editorIds.map((id) => ({ modderId: id, fullAccess: !!editorFullAccess.value[id] }))
const dependencies = ref([])
const articles = ref([])
const hooks = ref([])

onMounted(async () => {
  const [
    vanillaCharsRes,
    seriesRes,
    releaseStatesRes,
    moddersRes,
    dependenciesRes,
    articlesRes,
    hooksRes,
  ] = await Promise.all([
    api.get('/vanillachars'),
    api.get('/series'),
    api.get('/releasestates'),
    api.get('/modders'),
    api.get('/dependencies'),
    api.get('/articles'),
    api.get('/hooks'),
  ])

  vanillaChars.value = vanillaCharsRes.data.sort((a, b) =>
    a.displayName.localeCompare(b.displayName)
  )
  seriesList.value = seriesRes.data.sort((a, b) => a.seriesName.localeCompare(b.seriesName))
  releaseStates.value = releaseStatesRes.data
  modders.value = moddersRes.data.sort((a, b) => a.name.localeCompare(b.name))
  dependencies.value = dependenciesRes.data
  articles.value = articlesRes.data
  hooks.value = hooksRes.data

  // Only load moveset if editing
  if (props.mode === 'edit' && props.movesetId) {
    try {
      const res = await api.get(`/movesets/${props.movesetId}`)
      moveset.value = res.data
      Object.assign(form.value, res.data)
      if (res.data.isJokeMoveset || res.data.subtitle) {
        showAdvanced.value = true
      }
      form.value.modderIds = res.data.movesetModders?.map((m) => m.modder.modderId) || []
      form.value.editorIds = res.data.movesetEditors?.map((e) => e.modder.modderId) || []
      editorFullAccess.value = Object.fromEntries(
        (res.data.movesetEditors ?? []).map((e) => [e.modder.modderId, !!e.fullAccess])
      )
      canManageMembers.value = res.data.canManageMembers !== false
      form.value.dependencyIds =
        res.data.movesetDependencies?.map((d) => d.dependency.dependencyId) || []
      form.value.articles =
        res.data.movesetArticles?.map((a) => ({
          articleId: a.article.articleId,
          moddedName: a.moddedName,
          description: a.description,
        })) || []
      form.value.hooks =
        res.data.movesetHooks?.map((h) => ({
          hookId: h.hook.hookId,
          offset: h.hook.offset,
          hookDescription: h.hook.description,
          description: h.description,
        })) || []
    } catch (err) {
      console.error(err)
      router.replace({ name: 'ErrorPage', query: { http: 404, reason: 'Moveset not found' } })
    }
  }

  takeSnapshot()
})

// Vuetify's date picker always emits a raw JS Date on selection regardless of what type it's bound to,
// so this exists purely to translate between that Date and the "yyyy-MM-dd" string that actually gets submitted.
const releaseDatePickerValue = computed({
  get() {
    return dateOnlyStringToLocalDate(form.value.releaseDate)
  },
  set(newVal) {
    form.value.releaseDate = localDateToDateOnlyString(newVal)
  },
})

const formattedReleaseDate = computed({
  get() {
    const date = dateOnlyStringToLocalDate(form.value.releaseDate)
    return date ? date.toLocaleDateString() : ''
  },
  set(newVal) {
    if (!newVal) {
      form.value.releaseDate = null
    }
  },
})

const submit = async () => {
  const slotsStart = parseInt(form.value.slotsStart)
  const slotsEnd = parseInt(form.value.slotsEnd)

  // Validate slots
  if (isNaN(slotsStart) || isNaN(slotsEnd) || slotsStart < 8 || slotsEnd > 255) {
    notify.warning('Please ensure Start Slot and End Slot are between 8 and 255.')
    return
  }
  if (slotsStart > slotsEnd) {
    notify.warning('Please ensure End Slot is greater than Start Slot.')
    return
  }

  // Validate modderId
  const user = await api.get('/auth/me')
  const stillOnMoveset =
    form.value.modderIds.includes(user.data.modderId) ||
    form.value.editorIds.includes(user.data.modderId)
  if (!stillOnMoveset) {
    if (!isEditMode.value) {
      notify.warning('You cannot save a moveset you do not own.')
      return
    }
    const confirmed = window.confirm(
      'You are removing yourself from this moveset. You will lose access to edit it. Continue?'
    )
    if (!confirmed) return
  }

  // Validate other fields
  const requiredFields = [
    'moddedCharName',
    'seriesId',
    'slottedId',
    'vanillaCharInternalName',
    'releaseStateId',
  ]

  for (const field of requiredFields) {
    if (!form.value[field] && form.value[field] !== 0) {
      notify.warning(`Field "${field}" is required.`)
      return
    }
  }

  if (/\d/.test(form.value.slottedId)) {
    notify.warning('Slotted ID cannot contain digits.')
    return
  }

  // Ensure slottedId/replacementId fallback
  if (form.value.slottedId && !form.value.replacementId) {
    form.value.replacementId = form.value.slottedId
  } else if (!form.value.slottedId && form.value.replacementId) {
    form.value.slottedId = form.value.replacementId
  }

  isSubmitting.value = true

  if (props.mode === 'edit' && props.movesetId) {
    // Edit mode: the moveset already exists, so upload first (as before) and save in one request.
    uploadStatus.value = 'Uploading images'
    try {
      form.value.thumbhImageUrl = await uploadIfNeeded(form.value.thumbhImageUrl, {
        type: 'thumb_h',
        itemName: form.value.moddedCharName,
      })
      form.value.movesetHeroImageUrl = await uploadIfNeeded(form.value.movesetHeroImageUrl, {
        type: 'moveset_hero',
        itemName: form.value.moddedCharName,
      })
    } catch (err) {
      console.error('Image upload failed:', JSON.stringify(err.response?.data) || err.message)
      notify.error('Failed to upload image(s). Please check the file and try again.', err)
      isSubmitting.value = false
      uploadStatus.value = ''
      return
    }

    uploadStatus.value = 'Saving moveset'
    try {
      await api.put(`/movesets/${props.movesetId}`, { ...form.value, editors: editorsPayload() })
      markSaved()
      router.push(`/moveset/${props.movesetId}`)
    } catch (err) {
      console.error('Submit failed:', JSON.stringify(err.response?.data) || err.message)
      notify.error('Failed to save moveset. Please check the form and try again.', err)
    } finally {
      isSubmitting.value = false
      uploadStatus.value = ''
    }
    return
  }

  // Create mode: no moveset exists yet to attach an image to, so an upload failure or a save
  // failure after upload would otherwise orphan the image in R2 with nothing referencing it.
  // Create the moveset first (without staged files), then upload and attach images after -
  // that way a failure at any step never leaves an upload with no surviving record.
  const stagedThumb = isStagedFile(form.value.thumbhImageUrl) ? form.value.thumbhImageUrl : null
  const stagedHero = isStagedFile(form.value.movesetHeroImageUrl)
    ? form.value.movesetHeroImageUrl
    : null

  const payload = { ...form.value, editors: editorsPayload() }
  if (stagedThumb) payload.thumbhImageUrl = null
  if (stagedHero) payload.movesetHeroImageUrl = null

  uploadStatus.value = 'Saving moveset'

  let newId
  try {
    const res = await api.post('/movesets', payload)
    newId = res.data.movesetId
  } catch (err) {
    console.error('Submit failed:', JSON.stringify(err.response?.data) || err.message)
    notify.error('Failed to save moveset. Please check the form and try again.', err)
    isSubmitting.value = false
    uploadStatus.value = ''
    return
  }

  if (stagedThumb || stagedHero) {
    uploadStatus.value = 'Uploading images'
    try {
      const images = {}
      if (stagedThumb)
        images.thumbhImageUrl = await uploadIfNeeded(stagedThumb, {
          type: 'thumb_h',
          itemName: form.value.moddedCharName,
        })
      if (stagedHero)
        images.movesetHeroImageUrl = await uploadIfNeeded(stagedHero, {
          type: 'moveset_hero',
          itemName: form.value.moddedCharName,
        })
      await api.patch(`/movesets/${newId}/images`, images)
    } catch (err) {
      console.error(
        'Image upload failed after moveset creation:',
        JSON.stringify(err.response?.data) || err.message
      )
      notify.error(
        'Moveset created, but the image(s) failed to upload. You can add them later by editing the moveset.',
        err
      )
    }
  }

  markSaved()
  isSubmitting.value = false
  uploadStatus.value = ''
  router.push(`/moveset/${newId}`)
}
</script>

<style scoped>
.form-intro {
  margin: 0 0 20px;
  color: var(--tx-2);
  font-size: 14px;
}

.form-intro a,
.section-note a,
.field-link {
  color: var(--white);
  text-decoration: underline;
}

.section-note {
  margin: 0 0 4px;
  color: var(--tx-2);
  font-size: 14px;
}

.field-link {
  align-self: flex-start;
  margin-top: 4px;
  font-size: 12px;
  color: var(--tx-2);
}

/* Three columns of fields on desktop, one on mobile */
.form-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
  align-items: start;
}

.form-grid--2 {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.form-grid + .form-grid {
  margin-top: 16px;
}

.span-2 {
  grid-column: span 2;
}

.span-3 {
  grid-column: span 3;
}

.check-item {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.check-hint {
  margin: 0;
  padding-left: 40px;
  color: var(--tx-3);
  font-size: 12px;
}

/* Function usage: a red X when off, a green check when on */
.check-item--fn :deep(.v-selection-control__input .v-icon) {
  color: var(--err);
}

.check-item--fn :deep(.v-selection-control--dirty .v-selection-control__input .v-icon) {
  color: var(--ok);
}

.color-swatch {
  display: inline-block;
  width: 22px;
  height: 22px;
  border: 1px solid var(--line-2);
}

/* Per-editor access switches under the Editors picker */
.editor-access-list {
  margin-top: 8px;
  padding: 8px 10px;
  border: 1px solid var(--line);
  background: var(--panel-2);
}

.editor-access-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.editor-access-name {
  font-size: 14px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.editor-access-hint {
  margin: 4px 0 0;
  font-size: 12px;
  color: var(--tx-3);
}

/* Series picker with icons. Icon turns black when its item is hovered or selected */
.series-icon {
  filter: brightness(4.35);
}

.v-list-item:hover .series-icon,
.v-list-item--active .series-icon {
  filter: brightness(0);
}

.series-icon-small {
  width: 28px;
  height: 28px;
  object-fit: contain;
}

.filter-option {
  display: flex;
  align-items: center;
  gap: 8px;
}

.remove-bound-props :deep(.v-list-item-title:not(.filter-option .v-list-item-title)) {
  display: none;
}

/* Save bar */
.savebar-notes {
  flex: 1 1 320px;
  max-width: 480px;
}

.savebar-spacer {
  flex: 1;
}

@media (max-width: 959px) {
  .form-grid,
  .form-grid--2 {
    grid-template-columns: 1fr;
  }

  .span-2,
  .span-3 {
    grid-column: span 1;
  }
}
</style>

<template>
  <div v-if="(isEditMode && moveset) || (!isEditMode && form)">
    <v-container max-width="1020px">
      <!-- Header -->
      <h1 v-if="isEditMode">Edit {{ moveset.moddedCharName }}</h1>
      <h1 v-else>Submit Moveset</h1>
      <div v-if="!isEditMode" class="submission-guide-hint">
        <router-link to="/moveset-submission-guide" class="unvisitable" target="_blank">
          <i class="mdi mdi-arrow-right-bottom"></i>
          When should a moveset be submitted?
        </router-link>
      </div>

      <!-- Basic Info -->
      <section>
        <h2>Basic Info</h2>
        <v-row>
          <!-- Moveset Name -->
          <v-col cols="12" sm="4">
            <v-text-field
              v-model="form.moddedCharName"
              variant="outlined"
              label="Modded Character Name"
            >
              <template #label
                >Modded Character Name <span class="required-asterisk">*</span></template
              >
            </v-text-field>
          </v-col>
          <!-- Modders -->
          <v-col cols="12" sm="4">
            <v-select
              v-model="form.modderIds"
              variant="outlined"
              :items="modders"
              item-title="name"
              item-value="modderId"
              label="Modders"
              multiple
              chips
              clearable
            >
              <template #label>Modders <span class="required-asterisk">*</span></template>
              <!-- Hint -->
              <template #details>
                <router-link
                  to="/modder-credit-guide"
                  class="offsite unvisitable text-decoration-none"
                  target="_blank"
                  >Who should I include?</router-link
                >
              </template>
            </v-select>
          </v-col>
          <!-- Series -->
          <v-col cols="12" sm="4">
            <v-select
              v-model="form.seriesId"
              variant="outlined"
              :items="seriesList"
              item-title="seriesName"
              item-value="seriesId"
              label="Series"
            >
              <template #label>Series <span class="required-asterisk">*</span></template>
              <!-- List item -->
              <template #item="{ props, item }">
                <v-list-item v-bind="props" class="remove-bound-props">
                  <div class="filter-option">
                    <div v-if="item.raw.seriesIconUrl">
                      <v-img
                        :src="getFullImageUrl(item.raw.seriesIconUrl)"
                        class="series-icon series-icon-small"
                      />
                    </div>
                    <v-list-item-title>{{ item.raw.seriesName }}</v-list-item-title>
                  </div>
                </v-list-item>
              </template>

              <!-- Selected item -->
              <template #selection="{ item }">
                <div class="filter-option d-flex align-center">
                  <v-avatar class="me-1" size="26">
                    <v-img :src="getFullImageUrl(item.raw.seriesIconUrl)" class="series-icon" />
                  </v-avatar>
                  <span>{{ item.raw.seriesName }}</span>
                </div>
              </template>

              <!-- Hint -->
              <template #details>
                <router-link
                  to="/series/add"
                  class="offsite unvisitable text-decoration-none"
                  target="_blank"
                  >Don't see your series?</router-link
                >
              </template>
            </v-select>
          </v-col>
          <MovesetIdentityFields
            v-model:slotted-id="form.slottedId"
            v-model:replacement-id="form.replacementId"
            v-model:vanilla-char-internal-name="form.vanillaCharInternalName"
            v-model:slots-start="form.slotsStart"
            v-model:slots-end="form.slotsEnd"
            :vanilla-chars="vanillaChars"
          />
          <!-- Release Date -->
          <v-col cols="12" sm="4">
            <v-menu :close-on-content-click="false" transition="scale-transition">
              <template #activator="{ props }">
                <v-text-field
                  v-model="formattedReleaseDate"
                  variant="outlined"
                  label="Release Date"
                  readonly
                  clearable
                  v-bind="props"
                />
              </template>
              <!-- ??? can't get any props to work -->
              <v-date-picker
                v-model="releaseDatePickerValue"
                title="Release Date"
                header="Select date"
              />
            </v-menu>
          </v-col>
          <!-- Release State -->
          <v-col cols="12" sm="4">
            <v-select
              v-model="form.releaseStateId"
              variant="outlined"
              :items="releaseStates"
              item-title="releaseStateName"
              item-value="releaseStateId"
              label="Availability"
            >
              <template #label>Availability <span class="required-asterisk">*</span></template>
            </v-select>
          </v-col>
          <!-- Modpack -->
          <v-col cols="12" sm="4">
            <v-text-field
              v-model="form.modpackName"
              variant="outlined"
              label="Modpack"
              placeholder="(leave blank if not exclusive)"
            />
          </v-col>
          <!-- Dependencies -->
          <v-col>
            <v-select
              v-model="form.dependencyIds"
              variant="outlined"
              :items="dependencies"
              item-title="name"
              item-value="dependencyId"
              label="Dependencies"
              multiple
              chips
              clearable
            />
          </v-col>
        </v-row>
      </section>

      <!-- Display -->
      <section>
        <h2>Display</h2>
        <p class="mb-3">
          For information on image hosting in UMC, see
          <router-link to="/image-hosting" class="unvisitable" target="_blank">here</router-link>.
        </p>
        <v-row>
          <!-- ThumbH URL -->
          <v-col cols="12" sm="6">
            <p class="field-label">Thumbnail (340x82)</p>
            <ImageUploadField
              v-model="form.thumbhImageUrl"
              :required-width="IMAGE_UPLOAD_SPECS.thumb_h.width"
              :required-height="IMAGE_UPLOAD_SPECS.thumb_h.height"
              hint="A thumbnail image displayed in moveset lists."
            />
            <!-- Download -->
            <a :href="thumbhUnknown" download class="unvisitable text-caption">
              Download placeholder image
            </a>
          </v-col>
          <!-- Hero URL -->
          <v-col cols="12" sm="6">
            <p class="field-label">Render (1200x1200)</p>
            <ImageUploadField
              v-model="form.movesetHeroImageUrl"
              :required-width="IMAGE_UPLOAD_SPECS.moveset_hero.width"
              :required-height="IMAGE_UPLOAD_SPECS.moveset_hero.height"
              hint="The render of the character cropped to fit."
            />
            <!-- Download -->
            <a :href="movesetHeroUnknown" download class="unvisitable text-caption">
              Download placeholder image
            </a>
          </v-col>
          <!-- Background Color -->
          <v-col cols="12" sm="4">
            <v-text-field
              v-model="form.backgroundColor"
              variant="outlined"
              label="Background Color (Hex)"
              maxlength="6"
              prefix="#"
            >
              <template #append-inner>
                <div
                  :style="{
                    backgroundColor: '#' + form.backgroundColor,
                    width: '32px',
                    height: '32px',
                    borderRadius: '8px',
                  }"
                ></div>
              </template>
            </v-text-field>
          </v-col>
          <!-- Private Moveset -->
          <v-col cols="12" sm="4">
            <v-checkbox
              v-model="form.privateMoveset"
              label="Private"
              messages="Hides moveset name, series, and images, and disables the detail page. Does not hide modders."
            />
          </v-col>
          <!-- Private Modder -->
          <v-col cols="12" sm="4">
            <v-checkbox
              v-model="form.privateModder"
              label="Hide Modder Info"
              messages="Hides modder name from submissions. Only goes into affect if moveset is private."
            />
          </v-col>
        </v-row>
      </section>

      <!-- Links + Function Usage -->
      <div class="links-functions-row">
        <MovesetLinksSection
          v-model:mod-page-url="form.modPageUrl"
          v-model:gamebanana-wip-id="form.gamebananaWipId"
          v-model:mods-wiki-link="form.modsWikiLink"
          v-model:source-code="form.sourceCode"
        />

        <!-- Function Usage -->
        <section class="functions-section">
          <h2>Function Usage</h2>
          <v-row class="functions">
            <v-col cols="12">
              <v-checkbox
                v-model="form.hasGlobalOpff"
                label="Global OPFF"
                messages="Runs once every frame for all characters"
                true-icon="mdi-check-bold"
                false-icon="mdi-close-thick"
              />
            </v-col>
            <v-col cols="12">
              <v-checkbox
                v-model="form.hasCharacterOpff"
                label="Character OPFF"
                :messages="`Runs once every frame for ${getVanillaCharDisplayName(form.vanillaCharInternalName)}`"
                true-icon="mdi-check-bold"
                false-icon="mdi-close-thick"
              />
            </v-col>
            <v-col cols="12">
              <v-checkbox
                v-model="form.hasAgentInit"
                label="Agent init"
                messages="Runs once when a fighter is spawned in"
                true-icon="mdi-check-bold"
                false-icon="mdi-close-thick"
              />
            </v-col>
            <v-col cols="12">
              <v-checkbox
                v-model="form.hasGlobalOnLinePre"
                label="Global on_line pre"
                messages="Runs once every time a pre status script runs"
                true-icon="mdi-check-bold"
                false-icon="mdi-close-thick"
              />
            </v-col>
            <v-col cols="12">
              <v-checkbox
                v-model="form.hasGlobalOnLineEnd"
                label="Global on_line end"
                messages="Runs once every time an end status script runs"
                true-icon="mdi-check-bold"
                false-icon="mdi-close-thick"
              />
            </v-col>
          </v-row>
        </section>
      </div>
      <!-- end links-functions-row -->

      <MovesetArticlesEditor v-model="form.articles" :article-options="articles" />

      <MovesetHooksEditor
        v-model="form.hooks"
        :hook-options="hooks"
        :character-name="form.moddedCharName"
      />

      <!-- Plugins (only available once the moveset exists) -->
      <MovesetPluginsPanel
        v-if="isEditMode && props.movesetId"
        :moveset-id="props.movesetId"
        :moveset-name="form.moddedCharName"
      />
      <section v-else>
        <h2>Plugins</h2>
        <p class="subheader">
          Save this moveset first, then attach its plugin(s) from the edit page.
        </p>
      </section>

      <!-- Advanced Settings -->
      <section class="advanced-section">
        <h3 class="advanced-toggle" @click="showAdvanced = !showAdvanced">
          Advanced Settings
          <v-icon class="advanced-chevron" :class="{ rotated: showAdvanced }"
            >mdi-chevron-down</v-icon
          >
        </h3>
        <v-expand-transition>
          <div v-if="showAdvanced">
            <v-row>
              <!-- Joke Moveset -->
              <v-col cols="12" sm="4">
                <v-checkbox
                  v-model="form.isJokeMoveset"
                  true-icon="mdi-egg-easter"
                  false-icon="mdi-egg-outline"
                  label="Joke Moveset"
                  messages="Marks this as a joke moveset. Required for April Fool's movesets."
                  class="joke-checkbox"
                />
              </v-col>

              <!-- Subtitle -->
              <v-col cols="12" sm="4">
                <v-text-field
                  v-model="form.subtitle"
                  variant="outlined"
                  label="Subtitle"
                  placeholder="e.g. V2, Ult-S, Standalone"
                  messages="Shown in parentheses next to this moveset's name. For disambiguation purposes only."
                />
              </v-col>
            </v-row>
          </div>
        </v-expand-transition>
      </section>

      <!-- Notes + Submit -->
      <div class="d-flex align-start ga-3 justify-end">
        <v-textarea
          v-model="form.notes"
          variant="outlined"
          density="compact"
          :label="isEditMode ? 'Editing notes' : 'Submission notes'"
          placeholder="Optional, shown to admins only."
          rows="1"
          auto-grow
          hide-details
          class="notes-field"
        />
        <v-btn
          class="btn submit-button mt-1"
          :loading="isSubmitting"
          :disabled="isSubmitting"
          @click="submit"
        >
          {{ uploadStatus || (isEditMode ? 'Save' : 'Submit Moveset') }}
        </v-btn>
      </div>
    </v-container>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '@/services/api'
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

const props = defineProps({
  mode: { type: String },
  movesetId: { type: Number },
})

const emit = defineEmits(['submitted'])

const isEditMode = computed(() => props.mode === 'edit')
const route = useRoute()
const router = useRouter()

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
const form = ref({
  // Basic Info
  moddedCharName: '',
  modderIds: [],
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

const { uploadIfNeeded } = useImageUpload()
const { takeSnapshot, markSaved } = useUnsavedChanges(form)

const vanillaChars = ref([])
const seriesList = ref([])
const releaseStates = ref([])
const modders = ref([])
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
      document.title = `UMC | Editing ${moveset.value?.moddedCharName}` // sets page title

      if (res.data.isJokeMoveset || res.data.subtitle) {
        showAdvanced.value = true
      }
      form.value.modderIds = res.data.movesetModders?.map((m) => m.modder.modderId) || []
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
    alert('Please ensure Start Slot and End Slot are between 8 and 255.')
    return
  }
  if (slotsStart > slotsEnd) {
    alert('Please ensure End Slot is greater than Start Slot.')
    return
  }

  // Validate modderId
  const user = await api.get('/auth/me')
  if (!form.value.modderIds.includes(user.data.modderId)) {
    if (!isEditMode.value) {
      alert('You cannot save a moveset you do not own.')
      return
    }
    const confirmed = window.confirm(
      'You are removing yourself as a modder on this moveset. You will lose access to edit it. Continue?'
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
      alert(`Field "${field}" is required.`)
      return
    }
  }

  if (/\d/.test(form.value.slottedId)) {
    alert('Slotted ID cannot contain digits.')
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
    uploadStatus.value = 'Uploading images...'
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
      alert(
        'Failed to upload image(s). Please check the file and try again.\n\n' +
          (JSON.stringify(err.response?.data) || err.message)
      )
      isSubmitting.value = false
      uploadStatus.value = ''
      return
    }

    uploadStatus.value = 'Saving moveset...'
    try {
      await api.put(`/movesets/${props.movesetId}`, { ...form.value })
      markSaved()
      router.push(`/moveset/${props.movesetId}`)
    } catch (err) {
      console.error('Submit failed:', JSON.stringify(err.response?.data) || err.message)
      alert(
        'Failed to save moveset. Please check the form and try again.\n\n' +
          (JSON.stringify(err.response?.data) || err.message)
      )
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

  const payload = { ...form.value }
  if (stagedThumb) payload.thumbhImageUrl = null
  if (stagedHero) payload.movesetHeroImageUrl = null

  uploadStatus.value = 'Saving moveset...'

  let newId
  try {
    const res = await api.post('/movesets', payload)
    newId = res.data.movesetId
  } catch (err) {
    console.error('Submit failed:', JSON.stringify(err.response?.data) || err.message)
    alert(
      'Failed to save moveset. Please check the form and try again.\n\n' +
        (JSON.stringify(err.response?.data) || err.message)
    )
    isSubmitting.value = false
    uploadStatus.value = ''
    return
  }

  if (stagedThumb || stagedHero) {
    uploadStatus.value = 'Uploading images...'
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
      alert(
        'Moveset created, but the image(s) failed to upload. You can add them later by editing the moveset.\n\n' +
          (JSON.stringify(err.response?.data) || err.message)
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
/* General display of form */
section {
  margin-bottom: 2rem;
  background-color: #1e1e1e;
  padding: 1em;
  border-radius: 10px;
}
/* .form-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 1em;
} */
h1 {
  font-size: 3.25em;
}
section h2 {
  font-size: 2.25em;
  margin-bottom: 10px;
}
:deep(.v-text-field__prefix__text) {
  color: #e4e4e4;
}
.preview-image {
  border: 1px solid #686868;
  border-radius: 3px;
}
.field-label {
  font-size: 0.85rem;
  color: #b0b0b0;
  margin-bottom: 4px;
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
/* Dropdown display */
.series-icon {
  filter: brightness(4.35);
}
.series-icon-small {
  width: 36px;
  height: 36px;
  margin-top: -4px;
}
.filter-option {
  display: flex;
}
:deep(.filter-option div) {
  margin-right: 4px;
}
.remove-bound-props :deep(.v-list-item-title:not(.filter-option .v-list-item-title)) {
  display: none;
}
.v-avatar {
  background: transparent;
}

/* subheader helper class */
.subheader {
  margin-top: -1.5em;
  margin-bottom: 0.5em;
  font-size: 12px;
}

/* Img download caption link helper */
.text-caption {
  font-family: unset;
  margin-top: 0;
  display: block;
  width: fit-content;
  color: #939393 !important;
  transition: color 200ms ease-in-out;
}
.text-caption:hover {
  color: #c8c8c8 !important;
}

/* Links + Function Usage side by side */
.links-functions-row {
  display: flex;
  gap: 1rem;
  margin-bottom: 2rem;
  align-items: flex-start;
}
.links-functions-row > section {
  margin-bottom: 0;
}
.links-section {
  flex: 7;
}
.functions-section {
  flex: 5;
  min-width: 0;
  padding-left: 1.75em;
}

/* Function usage section */
.functions {
  margin-bottom: 0.75em;
}
.functions > div {
  padding: 0;
}

/* Checkbox hints sit directly below the checkbox */
:deep(.functions-section .v-checkbox .v-input__details) {
  padding-inline-start: 0;
  min-height: unset;
  overflow: revert;
}
:deep(.functions-section .v-checkbox .v-messages) {
  padding-left: 0;
  left: 48.5px;
  top: -16px;
  font-size: 1.1em;
}

/* Checkbox: red bg + X when unchecked, green bg + check when checked */
:deep(.functions-section .v-selection-control__input) {
  background-color: rgb(180, 40, 40);
  border-radius: 5px;
  color: white;
  width: 30px;
  height: 30px;
  top: 4px;
  left: -1px;
}
:deep(.functions-section .v-selection-control--dirty .v-selection-control__input) {
  background-color: rgb(40, 160, 60);
}
:deep(.functions-section .v-selection-control__input > i) {
  opacity: 1 !important;
}
:deep(.functions-section .v-label--clickable) {
  font-size: 1.1em;
}
:deep(.functions-section .v-input__control) {
  margin-left: 0.5em;
}

.required-asterisk {
  color: #cf6679;
}
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

.submission-guide-hint {
  text-align: left;
  margin-top: -1em;
  margin-bottom: 1em;
  opacity: 0.5;
  margin-left: 1em;
}

.advanced-toggle {
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 0.3em;
  user-select: none;
}
.advanced-toggle:hover {
  opacity: 0.8;
}
.advanced-chevron {
  font-size: 1.2em;
  transition: transform 250ms ease-in-out;
}
.advanced-chevron.rotated {
  transform: rotate(180deg);
}
:deep(.joke-checkbox .v-selection-control__input > i) {
  font-size: 1.4em;
  color: #888;
  transition: color 150ms ease-in-out;
}
:deep(.joke-checkbox .v-selection-control--dirty .v-selection-control__input > i) {
  color: #ff733c;
}
</style>

<!-- Extra styling that cannot be scoped because content is added to the DOM dynamically -->
<style>
/* Dropdowns */
.v-list {
  background-color: #2e2e2e !important;
  padding-top: 0 !important;
  padding-bottom: 0 !important;
}
.v-list-item {
  background-color: #2e2e2e !important;
  color: white !important;
}
.v-list-item:hover {
  background-color: #3e3e3e /* !not so important */;
}

/* Date select */
.v-date-picker {
  background-color: #2e2e2e !important;
  color: white !important;
}

/* Autocomplete highlight */
.v-autocomplete__mask {
  background-color: black !important;
}
</style>

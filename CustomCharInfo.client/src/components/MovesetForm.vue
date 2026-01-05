<template>
  <div v-if="(isEditMode && moveset) || (!isEditMode && form)">
    <v-container max-width="1020px">
      <!-- Header -->
      <h1 v-if="isEditMode">Edit {{ moveset.moddedCharName }}</h1>
      <h1 v-else>Submit Moveset</h1>

      <!-- Basic Info -->
      <section>
        <h2>Basic Info</h2>
        <v-row>
          <!-- Moveset Name -->
          <v-col cols="12" sm="4">
            <v-text-field v-model="form.moddedCharName" label="Modded Character Name" />
          </v-col>
          <!-- Modders -->
          <v-col cols="12" sm="4">
            <v-select
              v-model="form.modderIds"
              :items="modders"
              item-title="name"
              item-value="modderId"
              label="Modders"
              multiple
              chips
              clearable
            />
          </v-col>
          <!-- Series -->
          <v-col cols="12" sm="4">
            <v-select
              v-model="form.seriesId"
              :items="seriesList"
              item-title="seriesName"
              item-value="seriesId"
              label="Series"
            >
              <!-- List item -->
              <template #item="{ props, item }">
                <v-list-item v-bind="props" class="remove-bound-props">
                  <div class="filter-option">
                    <div v-if="item.raw.seriesIconUrl">
                      <v-img :src="getFullImageUrl(item.raw.seriesIconUrl)" class="series-icon series-icon-small" />
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
                >Don't see your series?</router-link>
              </template>
            </v-select>
          </v-col>
          <!-- Internal ID -->
          <v-col cols="12" sm="4">
            <v-checkbox
              v-model="showSeparateIds"
              label="Slotted ID and Replacement ID are different"
            />
          </v-col>
          <v-col cols="12" sm="4" v-if="!showSeparateIds">
            <v-text-field
              v-model="form.slottedId"
              label="Internal ID"
            />
          </v-col>
          <v-col cols="12" sm="8" v-else class="two-of-them">
            <v-col cols="6">
              <v-text-field
                v-model="form.slottedId"
                label="Slotted ID"
              />
            </v-col>
            <v-col cols="6">
              <v-text-field
                v-model="form.replacementId"
                label="Replacement ID"
              />
            </v-col>
          </v-col>
          <!-- Vanilla Character -->
          <v-col cols="12" sm="4">
            <v-select
              v-model="form.vanillaCharInternalName"
              :items="vanillaChars"
              item-title="displayName"
              item-value="vanillaCharInternalName"
              label="Vanilla Character"
            >
              <!-- List item -->
              <template #item="{ props, item }">
                <v-list-item v-bind="props" class="remove-bound-props">
                  <div class="filter-option">
                    <div>
                      <v-img :src="`/UltimateMovesetCompatibility/vanilla-stock-icons/chara_2_${item.raw.vanillaCharInternalName}.png`" class="stock-icon-small" />
                    </div>
                    <v-list-item-title>{{ item.raw.displayName }}</v-list-item-title>
                  </div>
                </v-list-item>
              </template>

              <!-- Selected item -->
              <template #selection="{ item }">
                <div class="filter-option d-flex align-center">
                  <v-avatar class="me-2" size="26">
                    <v-img :src="`/UltimateMovesetCompatibility/vanilla-stock-icons/chara_2_${item.raw.vanillaCharInternalName}.png`" />
                  </v-avatar>
                  <span>{{ item.raw.displayName }}</span>
                </div>
              </template>
            </v-select>
          </v-col>
          <!-- Slots -->
          <v-col cols="12" sm="2" class="left-merged-input-container">
            <v-text-field
              v-model.number="form.slotsStart"
              label="Start Slot"
              @input="digitsOnly('slotsStart')"
              :min="8"
              :max="255"
              prefix="c"
              class="left-merged-input"
            />
          </v-col>
          <v-col cols="12" sm="2" class="right-merged-input-container">
            <v-text-field
              v-model.number="form.slotsEnd"
              label="End Slot"
              @input="digitsOnly('slotsEnd')"
              :min="8"
              :max="255"
              prefix="c"
              class="right-merged-input"
            />
          </v-col>
          <!-- Release Date -->
          <v-col cols="12" sm="4">
            <v-menu 
              :close-on-content-click="false" 
              transition="scale-transition"
              activator="parent"
              open-on-focus
            >
              <template v-slot:activator="{ props }">
                <v-text-field
                  v-model="formattedReleaseDate"
                  label="Release Date"
                  readonly
                  clearable
                  v-bind="props"
                />
              </template>
              <v-date-picker
                v-model="form.releaseDate"
                title="Release Date"
                header="Select date"
              />
            </v-menu>
          </v-col>
          <!-- Release State -->
          <v-col cols="12" sm="4">
            <v-select
              v-model="form.releaseStateId"
              :items="releaseStates"
              item-title="releaseStateName"
              item-value="releaseStateId"
              label="Availability"
            />
          </v-col>
          <!-- Modpack -->
          <v-col cols="12" sm="4">
            <v-text-field
              v-model="form.modpackName"
              label="Modpack"
              placeholder="(leave blank if not exclusive)"
            />
          </v-col>
          <!-- Dependencies -->
          <v-col>
            <v-select
              v-model="form.dependencyIds"
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
        <v-row>
          <!-- ThumbH Upload -->
          <v-col cols="12" sm="6">
            <v-file-input
              label="Thumbnail (340x82)"
              accept="image/*"
              @change="event => uploadImage(event, 'thumb_h')"
              messages="WARNING: Images update automatically, unlike all other data."
            />
            <v-img
              :src="getFullImageUrl(form.thumbhImageUrl) || thumbhUnknown"
              class="mt-2 preview-image"
            />
            <!-- Download -->
            <a
              :href="thumbhUnknown"
              download
              class="unvisitable text-caption"
            >
              Download placeholder image
            </a>
          </v-col>
          <!-- Hero Upload -->
          <v-col cols="12" sm="6">
            <v-file-input
              label="Render (1200x1200)"
              accept="image/*"
              @change="file => uploadImage(file, 'moveset_hero')"
            />
            <v-img
              :src="getFullImageUrl(form.movesetHeroImageUrl) || movesetHeroUnknown"
              height="150"
              width="150"
              class="mt-2 preview-image"
              cover
            />
            <!-- Download -->
            <a
              :href="movesetHeroUnknown"
              download
              class="unvisitable text-caption"
            >
              Download placeholder image
            </a>
          </v-col>
          <!-- Background Color -->
          <v-col cols="12" sm="4">
            <v-text-field
              v-model="form.backgroundColor"
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
      
      <!-- Links -->
      <section class="darker-preview-text">
        <h2>Links</h2>
        <v-row>
          <!-- GameBanana Page -->
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="form.gamebananaPageId"
              label="GameBanana Page"
              :prefix="GB_PAGE_URL"
              @input="digitsOnly('gamebananaPageId')"
            />
          </v-col>
          <!-- GameBanana WIP -->
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="form.gamebananaWipId"
              label="GameBanana WIP"
              :prefix="GB_WIP_URL"
              @input="digitsOnly('gamebananaWipId')"
            />
          </v-col>
          <!-- Mods Wiki -->
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="form.modsWikiLink"
              label="SSBU Mods Wiki"
              :prefix="MODS_WIKI_URL"
            />
          </v-col>
          <!-- Source Code -->
          <v-col cols="12" sm="6">
            <v-text-field
              v-model="form.sourceCode"
              label="Source Code URL"
              type="url"
            />
          </v-col>
        </v-row>
      </section>

      <!-- Function Usage -->
      <section class="flex-1">
        <h2>Function Usage</h2>
        <v-row>
          <v-col cols="12" sm="12">
            <v-checkbox 
              v-model="form.hasGlobalOpff"
              label="Global OPFF"
              messages="Runs once every frame for all characters"
            />
            <v-checkbox 
              v-model="form.hasCharacterOpff"
              label="Character OPFF"
              :messages="`Runs once every frame for ${getVanillaCharDisplayName(form.vanillaCharInternalName)}`"
            />
            <v-checkbox 
              v-model="form.hasAgentInit"
              label="agent_init"
              messages="Runs once when a fighter is spawned in"
            />
            <v-checkbox 
              v-model="form.hasGlobalOnLinePre"
              label="Global on_line pre"
              messages="Runs once every time a pre status script runs"
            />
            <v-checkbox 
              v-model="form.hasGlobalOnLineEnd"
              label="Global on_line end"
              messages="Runs once every time an end status script runs"
            />
          </v-col>
        </v-row>
      </section>

      <!-- Articles -->
      <section>
        <!-- Header -->
        <h2>
          Cloned Articles
          <v-btn
            variant="text"
            density="compact"
            icon="mdi-plus"
            class="rotate-toggle"
            :class="{ rotated: addArticleForm }"
            @click="addArticleForm = !addArticleForm"
          />
        </h2>
        <!-- learn more -->
        <p class="subheader"><a
            href="https://docs.google.com/spreadsheets/d/16SEU3MibrzTJHTjxJb7c5e7JzGgrfWY_c_hqNJtGvNY/"
            target="_blank"
            class="offsite unvisitable"
          >
            Learn more about articles
        </a></p>
        <!-- Add Article -->
        <v-expand-transition>
          <div v-if="addArticleForm">
            <v-row>
              <v-col cols="12" sm="4">
                <v-autocomplete
                  v-model="newArticle.articleId"
                  :items="articles"
                  :item-title="item => `${item.vanillaCharInternalName}_${item.articleName}`"
                  item-value="articleId"
                  label="Article"
                />
              </v-col>
              <v-col cols="12" sm="3">
                <v-text-field
                  v-model="newArticle.moddedName"
                  label="Modded Internal Name"
                  placeholder="eg. shortaxe"
                />
              </v-col>
              <v-col cols="12" sm="3">
                <v-text-field
                  v-model="newArticle.description"
                  label="Display Name"
                  placeholder="eg. Short Axe"
                />
              </v-col>
              <v-col cols="12" sm="2">
                <v-btn @click="addArticle" class="btn add-button">Add Article</v-btn>
              </v-col>
            </v-row>
          </div>
        </v-expand-transition>

        <!-- Article List -->
        <v-list>
          <v-list-item
            v-for="(entry, i) in form.articles"
            :key="i"
          >
            <v-list-item-title class="d-inline">
              <strong>{{ entry.description }}</strong>: {{ getArticleName(entry.articleId) }} ({{ entry.moddedName }})
            </v-list-item-title>
            <v-icon @click="form.articles.splice(i, 1)" class="d-inline delete-icon">
              mdi-delete
            </v-icon>
          </v-list-item>
        </v-list>
      </section>

      <!-- Hooks -->
      <section>
        <h2>
          Hooks
          <v-btn
            variant="text"
            density="compact"
            icon="mdi-plus"
            class="rotate-toggle"
            :class="{ rotated: addHookForm }"
            @click="addHookForm = !addHookForm"
          />
        </h2>
        <!-- Hint -->
        <p class="subheader"><router-link
            to="/hooks/add"
            class="unvisitable"
            target="_blank"
          >
            Don't see your hook?
        </router-link></p>
        <!-- Add Hook -->
        <v-expand-transition>
          <div v-if="addHookForm">
            <v-row>
              <v-col cols="12">
                <v-autocomplete
                  v-model="newHook.hookId"
                  :items="hooks"
                  :item-title="item => `0x${item.offset} (${item.description})`"
                  item-value="hookId"
                  label="Add Hook"
                />
                <v-row>
                  <v-col cols="12" sm="10">
                    <v-text-field
                      v-model="newHook.description"
                      label="Hook Usage"
                      :placeholder="`What does ${form.moddedCharName || 'the character'} use this for?`"
                    />
                  </v-col>
                  <v-col cols="12" sm="2">
                    <v-btn @click="addHook" class="btn add-button">Add Hook</v-btn>
                  </v-col>
                </v-row>
              </v-col>
            </v-row>
          </div>
        </v-expand-transition>

        <!-- Hook List -->
        <v-list>
          <v-list-item
            v-for="(entry, i) in form.hooks"
            :key="i"
          >
            <v-list-item-title class="d-inline">
              0x{{ entry.offset }} ({{ entry.hookDescription }}) – {{ entry.description }}
            </v-list-item-title>
            <v-icon @click="form.hooks.splice(i, 1)" class="d-inline delete-icon">
              mdi-delete
            </v-icon>
          </v-list-item>
        </v-list>
      </section>

      <!-- Submit -->
      <div class="d-flex justify-end">
        <v-btn @click="submit" class="btn submit-button">
          {{ isEditMode ? 'Save' : 'Submit Moveset' }}
        </v-btn>
      </div>
    </v-container>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '@/services/api'
import thumbhUnknown from "@/assets/thumb_h_unknown.png"
import movesetHeroUnknown from "@/assets/moveset_hero_unknown.png"
import { GB_PAGE_URL, GB_WIP_URL, MODS_WIKI_URL } from '@/globals'

const props = defineProps({
  mode: { type: String },
  movesetId: { type: Number }
})

const emit = defineEmits(['submitted'])

const isEditMode = computed(() => props.mode === 'edit')
const route = useRoute()
const router = useRouter()

const addArticleForm = ref(false)
const addHookForm = ref(false)

const apiUrl = import.meta.env.VITE_API_URL

const getFullImageUrl = (path) => {
  if (!path) return null
  return path.startsWith('/') ? `${apiUrl}${path}` : path
}

const getVanillaCharDisplayName = (internalName) => {
  const match = vanillaChars.value.find(v => v.vanillaCharInternalName === internalName)
  return match ? match.displayName : internalName
}

const getArticleName = (id) => {
  const article = articles.value.find(a => a.articleId === id);
  return article ? `${article.vanillaCharInternalName}_${article.articleName}` : 'Unknown';
}

const addArticle = () => {
  if (!newArticle.value.articleId) return;
  form.value.articles.push({ ...newArticle.value });
  newArticle.value = { articleId: null, moddedName: "", description: "" };
}

const addHook = () => {
  if (!newHook.value.hookId) return

  const hook = hooks.value.find(h => h.hookId === newHook.value.hookId)
  if (!hook) return

  form.value.hooks.push({
    hookId: hook.hookId,
    offset: hook.offset,
    hookDescription: hook.description,
    description: newHook.value.description
  })

  newHook.value = { hookId: null, description: '' }
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
  gamebananaPageId: null,
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
})

const newArticle = ref({ articleId: null, moddedName: "", description: "" });
const newHook = ref({ hookId: null, description: "" });

const vanillaChars = ref([])
const seriesList = ref([])
const releaseStates = ref([])
const modders = ref([])
const dependencies = ref([])
const articles = ref([]);
const hooks = ref([]);

const showSeparateIds = ref(false)

const digitsOnly = (field) => {
  if (form.value[field] == null) return
  form.value[field] = String(form.value[field]).replace(/\D+/g, '')
}

onMounted(async () => {
  const [vanillaCharsRes, seriesRes, releaseStatesRes, moddersRes, dependenciesRes, articlesRes, hooksRes] = await Promise.all([
    api.get('/vanillachars'),
    api.get('/series'),
    api.get('/releasestates'),
    api.get('/modders'),
    api.get('/dependencies'),
    api.get('/articles'),
    api.get('/hooks'),
  ])

  vanillaChars.value = vanillaCharsRes.data.sort((a, b) => a.displayName.localeCompare(b.displayName))
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
      document.title = `UMC | Editing ${moveset.value?.moddedCharName}`; // sets page title

      showSeparateIds.value = form.value.slottedId !== form.value.replacementId
      slotRange.value = [form.value.slotsStart, form.value.slotsEnd]
      form.value.modderIds = res.data.movesetModders?.map(m => m.modder.modderId) || []
      form.value.dependencyIds = res.data.movesetDependencies?.map(d => d.dependency.dependencyId) || []
      form.value.articles = res.data.movesetArticles?.map(a => ({
        articleId: a.article.articleId,
        moddedName: a.moddedName,
        description: a.description
      })) || []
      form.value.hooks = res.data.movesetHooks?.map(h => ({
        hookId: h.hook.hookId,
        offset: h.hook.offset,
        hookDescription: h.hook.description,
        description: h.description
      })) || []
    } catch (err) {
      console.error(err)
      router.replace({ name: 'ErrorPage', query: { http: 404, reason: 'Moveset not found' } })
    }
  }
})

const slotRange = ref([form.value.slotsStart || 8, form.value.slotsEnd || 8])

watch(slotRange, ([start, end]) => {
  form.value.slotsStart = start
  form.value.slotsEnd = end
})

const formattedReleaseDate = computed({
  get() {
    if (!form.value.releaseDate) return ''
    const date = new Date(form.value.releaseDate)
    return date.toLocaleDateString()
  },
  set(newVal) {
    if (!newVal) {
      form.value.releaseDate = null
      return
    }

    const parsed = new Date(newVal)
    if (!isNaN(parsed)) {
      form.value.releaseDate = parsed.toISOString().split('T')[0]
    }
  }
})

const uploadImage = async (event, type) => {
  const file = event?.target?.files?.[0];
  const slottedId = form.value.slottedId;

  if (!file || !slottedId) {
    alert("Please select a valid image and ensure slottedId is filled.");
    return;
  }

  try {
    // Check if a moveset with this slottedId already exists
    const existingRes = await api.get(`/movesets/${slottedId}`);
    const existingMoveset = existingRes.data;

    if (existingMoveset) {
      const user = await api.get('/auth/me');
      const modderIds = existingMoveset.movesetModders?.map(m => m.modder.modderId) || [];

      if (!modderIds.includes(user.data.modderId)) {
        alert(`You do not own the moveset using ID '${slottedId}', so you cannot upload an image for it.`);
        return;
      }
    }
  } catch (err) {
    if (err.response?.status !== 404) {
      console.error("Validation failed:", err.response?.data || err.message);
      alert("Failed to validate slottedId.");
      return;
    }
  }

  // Proceed with image upload
  const formData = new FormData();
  formData.append("file", file);
  formData.append("type", type);
  formData.append("itemName", slottedId);

  try {
    const res = await api.post("/upload/moveset-image", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });

    const timestamp = new Date().getTime();
    const imageUrlWithCacheBust = res.data.url + `?t=${timestamp}`;

    if (type === "thumb_h") {
      form.value.thumbhImageUrl = imageUrlWithCacheBust;
    } else if (type === "moveset_hero") {
      form.value.movesetHeroImageUrl = imageUrlWithCacheBust;
    }
  } catch (err) {
    console.error("Image upload failed:", err.response?.data || err.message);
    alert("Failed to upload image. Please ensure it meets size requirements.");
  }
};

const submit = async () => {
  // Validate slots
  if (parseInt(form.value.slotsStart) < 8 || parseInt(form.value.slotsEnd > 255)) {
    alert('Please ensure Start Slot and End Slot are between 8 and 255.')
    return
  }
  if (parseInt(form.value.slotsStart) > parseInt(form.value.slotsEnd)) {
    alert('Please ensure End Slot is greater than Start Slot.')
    return
  }

  // Validate slottedId and replacementId
  if (form.value.slottedId && !form.value.replacementId) {
    form.value.replacementId = form.value.slottedId
  } else if (!form.value.slottedId && form.value.replacementId) {
    form.value.slottedId = form.value.replacementId
  }

  const payload = { ...form.value }

  try {
    if (props.mode === 'edit' && props.movesetId) {
      await api.put(`/movesets/${props.movesetId}`, payload)
      router.push(`/moveset/${props.movesetId}`)
    } else {
      const res = await api.post('/movesets', payload)
      const newId = res.data.movesetId
      router.push(`/moveset/${newId}`)
    }
  } catch (err) {
    console.error("Submit failed:", err.response?.data || err.message)
    alert("Failed to save moveset. Please check the form and try again.\n\n" + err.response?.data || err.message)
    // TODO permanent solution
  }
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
/* Todo grid form??? */
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
:deep(.darker-preview-text .v-text-field__prefix__text) {
  color: #9b9b9b;
}
.preview-image {
  border: 2px solid white;
  border-radius: 5px;
}
.submit-button {
  background-color: #1e1e1e;
  color: #e2e2e2;
}
.add-button {
  background-color: #2e2e2e;
  color: #e2e2e2;
  height: 74%;
  width: 100%;
}
.btn {
  text-transform: unset;
  letter-spacing: 0.009375em;
  font-size: medium;
}
.delete-icon {
  background: none;
  font-size: 20px;
  margin-left: 8px;
}
.delete-icon::before {
  margin-top: -4px;
}

/* Fix for showing/hiding extra ID input */
.two-of-them {
  display: flex;
}
.two-of-them > .v-col {
  padding-top: 0;
  padding-bottom: 0;
}
.two-of-them > .v-col:first-of-type {
  padding-left: 0;
}
.two-of-them > .v-col:last-of-type {
  padding-right: 0;
}

/* Dropdown display */
.stock-icon-small {
  width: 20px;
}
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

/* Merge two inputs */
.left-merged-input-container {
  padding-right: 0;
}
.right-merged-input-container {
  padding-left: 0;
}
.right-merged-input :deep(.v-field) {
  border-top-left-radius: 0;
}
.left-merged-input :deep(.v-field) {
  border-top-right-radius: 0;
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
}

/* Toggle form button */
:deep(.rotate-toggle > span > i::before) {
  transition: transform 250ms ease-in-out;
}
:deep(.rotate-toggle.rotated span > i::before) {
  transform: rotate(-45deg);
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
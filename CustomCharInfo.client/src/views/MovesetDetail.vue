<template>
  <div v-if="moveset" class="moveset-detail">
    <!-- Background -->
    <div
      class="background-column center-column gradient-column"
      :style="{
        backgroundImage: `linear-gradient(to bottom, ${backgroundColor} 0%, white 80%)`
      }"
    ></div>
    <div
      class="background-column center-column icon-column"
      :style="{
        backgroundImage: `url('${getFullImageUrl(moveset.series.seriesIconUrl, '')}')`
      }"
    ></div>
    <div class="background-column side-column left-black"></div>
    <div class="background-column side-column right-black"></div>

    <!-- Main content -->
    <div class="moveset-columns">
      <!-- Column 1 (left) -->
      <div class="column-left">
        <div class="title-container">
          <h1 class="title-font page-title no-select">{{ moveset.moddedCharName }}</h1>
        </div>
        <img
          :src="getFullImageUrl(moveset.movesetHeroImageUrl, movesetHeroUnknown)"
          alt="Character UI"
          class="character-image"
          :class="{ 'slide-in': imageLoaded }"
          @load="imageLoaded = true"
        />
      </div>

      <!-- Column 2 (right) -->
      <div class="column-right">
        <!-- Row 1 -->
        <v-row dense class="row-1">
          <!-- Basic Info -->
          <v-col cols="12" md="5">
            <div class="info-card">
              <!-- Header -->
              <h1>
                Basic Info 
                <router-link
                  :to="{ name: 'EditMoveset', params: { movesetId: route.params.movesetId } }"
                  class="edit-link unvisitable"
                >
                  <v-icon v-if="userIsModder">mdi-pencil</v-icon>
                </router-link>
              </h1>

              <!-- Creator(s) -->
              <div class="align-center" v-if="moveset.movesetModders?.length">
                <p class="d-inline mr-2">Creator<span v-if="moveset.movesetModders.length > 1">s</span>:</p>
                <strong>
                  <template v-for="(mm, index) in moveset.movesetModders" :key="mm.modder.modderId">
                    <router-link
                      :to="{ name: 'ModderDetail', params: { id: mm.modder.modderId } }"
                      class="unvisitable"
                    >
                      {{ mm.modder.name }}
                    </router-link><span v-if="index < moveset.movesetModders.length - 1">, </span>
                  </template>
                </strong>
              </div>

              <!-- Series -->
              <div>
                <p>Series: <strong>{{ moveset.series?.seriesName }} <img :src="getFullImageUrl(moveset.series.seriesIconUrl, seriesIconUnknown)" alt="series icon" class="inline-series" /></strong></p>
              </div>

              <!-- IDs -->
              <div>
                <p v-if="moveset.slottedId === moveset.replacementId">
                  Internal ID: <strong>{{ moveset.slottedId }}</strong>
                </p>
                <template v-else>
                  <p>Slotted ID: <strong>{{ moveset.slottedId }}</strong></p>
                  <p>Replacement ID: <strong>{{ moveset.replacementId }}</strong></p>
                </template>
              </div>

              <!-- Slots -->
              <div>
                <p v-if="moveset.slotsStart && moveset.slotsEnd">
                  {{ new Date().getMonth() == 3 && new Date().getDate() == 1 ? 'Schmeebulates:' : 'Slots:' }}
                  <strong>{{ moveset.vanillaChar?.displayName }} c{{ String(moveset.slotsStart).padStart(2, '0') }}-c{{ String(moveset.slotsEnd).padStart(2, '0') }}</strong>
                </p>
                <p v-else>
                  {{ new Date().getMonth() == 3 && new Date().getDate() == 1 ? 'Schmeebulates:' : 'Slots:' }}
                  <strong>{{ moveset.vanillaChar?.displayName }} c???</strong>
                </p>
              </div>

              <!-- Availability -->
              <p>
                Availability:
                <strong>
                  <span v-if="releaseDisplay">
                    <template v-if="releaseDisplay.url">
                      <a
                        :href="releaseDisplay.url"
                        target="_blank"
                        class="offsite unvisitable"
                      >
                        {{ releaseDisplay.text }}
                      </a>
                    </template>
                    <template v-else>
                      {{ releaseDisplay.text }}
                    </template>
                  </span>
                  <span v-else>
                    {{ moveset.releaseState?.releaseStateName }}
                  </span>
                </strong>
              </p>

              <!-- External Links -->
              <p v-if="moveset.modsWikiLink">
                <a :href="`${MODS_WIKI_URL}${moveset.modsWikiLink}`" target="_blank" class="offsite unvisitable">
                  View {{ moveset.moddedCharName }} on SSBU Mods Wiki
                </a>
              </p>
              <p v-if="moveset.sourceCode && !moveset.modpackName">
                <a :href="moveset.sourceCode" target="_blank" class="offsite unvisitable">Source Code</a>
              </p>
            </div>
          </v-col>

          <!-- Function usage -->
          <v-col cols="12" md="3">
            <div class="mb-4 info-card">
              <h1>Functions</h1>
              <ul class="functions-list">
                <li>Global OPFF: <StatusIcon :value="moveset.hasGlobalOpff" /></li>
                <li>Character OPFF: <StatusIcon :value="moveset.hasCharacterOpff" /></li>
                <li>agent_init: <StatusIcon :value="moveset.hasAgentInit" /></li>
                <li>on_line pre: <StatusIcon :value="moveset.hasGlobalOnLinePre" /></li>
                <li>on_line end: <StatusIcon :value="moveset.hasGlobalOnLineEnd" /></li>
              </ul>
            </div>
          </v-col>

          <!-- Dependencies -->
          <v-col cols="12" md="4">
            <div class="mb-4 info-card">
              <h1>Dependencies</h1>
              <ul v-if="moveset.movesetDependencies.length > 0">
                <li v-for="md in moveset.movesetDependencies" :key="md.dependencyId">
                  • <a :href="md.dependency.downloadLink" target="_blank" class="offsite unvisitable dependency-link">
                    {{ md.dependency.name }}
                  </a>
                </li>
              </ul>
              <p v-else>No dependencies set!</p>
            </div>
          </v-col>
        </v-row>

        <!-- Row 2 -->
        <v-row dense class="row-2">
          <!-- Articles -->
          <v-col cols="12" md="6">
            <div class="mb-4 info-card">
              <h1>Articles</h1>
              <ul v-if="moveset.movesetArticles.length > 0">
                <li v-for="ma in moveset.movesetArticles" :key="ma.articleId" class="d-flex justify-space-between">
                  <div>
                    <strong>{{ ma.description }}</strong>&nbsp;
                    <span>({{ ma.moddedName }})</span>
                  </div>
                  <div>
                    <span>{{ ma.article.vanillaCharInternalName }}_{{ ma.article.articleName }}</span>
                  </div>
                </li>
              </ul>
              <p v-else>This moveset does not clone any articles!</p>
            </div>
          </v-col>

          <!-- Hooks -->
          <v-col cols="12" md="6">
            <div class="info-card">
              <h1>Hooks</h1>
              <ul v-if="moveset.movesetHooks.length > 0">
                <li v-for="mh in moveset.movesetHooks" :key="mh.hookId" class="d-flex justify-space-between">
                  <div>
                    <strong :title="mh.hook.description" class="hastooltip">
                      0x{{ mh.hook.offset }}
                    </strong>
                  </div>
                  <div class="hook-usage">
                    <em v-if="mh.description">{{ mh.description }}</em>
                    <em v-else>(unknown usage)</em>
                  </div>
                </li>
              </ul>
              <p v-else>This moveset does not use any hooks!</p>
            </div>
          </v-col>
        </v-row>
      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, watch, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '@/services/api'
import movesetHeroUnknown from "@/assets/moveset_hero_unknown.png"
import seriesIconUnknown from "@/assets/series_icon_unknown.png"
import { GB_PAGE_URL, GB_WIP_URL, MODS_WIKI_URL } from '@/globals'

const route = useRoute()
const router = useRouter()
const moveset = ref(null)
const user = ref(null)

const apiUrl = import.meta.env.VITE_API_URL

const imageLoaded = ref(false)

function getFullImageUrl (path, elsepath) {
  if (!path) return elsepath
  return path.startsWith('/') ? `${apiUrl}${path}` : path
}

const backgroundColor = computed(() => {
  const color = moveset.value?.backgroundColor || '000000'
  return `#${color}`
})

const userIsModder = computed(() => {
  if (!user.value || !moveset.value?.movesetModders) return false
  return moveset.value.movesetModders.some(mm => mm.modder.modderId === user.value.modderId)
})

// Calculate the releaseState to display
const releaseDisplay = computed(() => {
  if (!moveset.value) return null

  const {
    modpackName,
    sourceCode,
    releaseState,
    gamebananaPageId,
    gamebananaWipId,
    releaseDate
  } = moveset.value

  const state = releaseState?.releaseStateName
  const pageUrl = gamebananaPageId ? `${GB_PAGE_URL}${gamebananaPageId}` : null
  const wipUrl = gamebananaWipId ? `${GB_WIP_URL}${gamebananaWipId}` : null
  const url = pageUrl || wipUrl

  const hasDate = !!releaseDate
  const date = hasDate ? new Date(releaseDate) : null
  const today = new Date()
  const isPast = date && date <= today

  const formatDate = (d) => d.toLocaleDateString()

  // Modpack
  if (modpackName) {
    return {
      text: `Exclusive to ${modpackName}`,
      url: sourceCode || null
    }
  }

  // Depreciated
  if (state === 'Depreciated') {
    return { text: 'Depreciated', url }
  }

  // Beta testing
  if (state === 'Open for Beta Testing') {
    let text = 'Open for Beta Testing'
    if (hasDate) {
      text += isPast
        ? ` (Released ${formatDate(date)})`
        : ` (Releases ${formatDate(date)})`
    }

    return { text, url }
  }

  // Pending Update
  if (state === 'Pending Update') {
    if (!hasDate) {
      return { text: 'Pending Update', url }
    }

    return {
      text: isPast
        ? `Originally Released ${formatDate(date)}`
        : `Update Releases ${formatDate(date)}`,
      url
    }
  }

  // Released
  if (state === 'Released') {
    let text = 'Released'
    if (hasDate) {
      text += ` ${formatDate(date)}`
    }

    return { text, url }
  }

  // Unreleased
  if (state === 'Unreleased') {
    let text = 'Upcoming'
    if (hasDate) {
      text += ` ${formatDate(date)}`
    }

    return { text, url }
  }

  return null
})

// Mounted
onMounted(async () => {
  try {
    const movesetRes = await api.get(`/movesets/${route.params.movesetId}`)
    moveset.value = movesetRes.data
    document.title = `UMC | ${moveset.value?.moddedCharName} Moveset`; // sets page title
  } catch (err) {
    router.replace({ name: 'ErrorPage', query: { http: 404, reason: 'Moveset not found' } })
  }
  try {
    const userRes = await api.get('/auth/me')
    user.value = userRes.data
  } catch (err) {
    // 
  }
})
</script>

<!-- Checkmark/x component -->
<script>
import { defineComponent, h } from 'vue'

const StatusIcon = defineComponent({
  props: {
    value: Boolean,
  },
  setup(props) {
    return () =>
      h(
        'i',
        {
          class: [
            'mdi',
            props.value ? 'mdi-check-bold' : 'mdi-close-thick'
          ]
        }
      )
  }
})
</script>

<style scoped>
.moveset-detail {
  position: relative;
  padding: 2rem;
  overflow: hidden;
  background-color: black;
  perspective: 1000px;
}

.background-column {
  position: absolute;
  top: 0;
  min-height: 100vh;
  height: 100%;
  width: 100%;
  pointer-events: none;
}

.center-column {
  z-index: 0;
}

.gradient-column {
  background-repeat: no-repeat;
  background-size: cover;
}

.icon-column {
  background-repeat: repeat;
  background-size: 100px auto;
  opacity: 0.06;
  filter: brightness(10);
  mix-blend-mode: plus-lighter;
  z-index: 1;
  transform: skewX(-9deg) rotate3d(1, 0, 0, 30deg) translate(3%, -4%);
  transform-origin: top center;
}

.side-column {
  transform: skewX(-12deg);
  background-color: black;
  z-index: 2;
}

.left-black {
  left: -11%;
  width: 20%;
}

.right-black {
  right: -8%;
  width: 70%;
}

.moveset-columns {
  display: flex;
  position: relative;
  z-index: 10;
  min-height: 100vh;
}

.column-left {
  flex: 7;
}

.column-right {
  flex: 9;
  margin-top: 5em;
}

.title-container {
  position: absolute;
  width: fit-content;
  padding-right: 1em;
}

.title-container::after {
  content: '';
  position: absolute;
  top: 0;
  left: -50%;
  right: 0;
  bottom: 0;
  background-color: black;
  background-image: url(/src/assets/ptn_diagonal_12.png);
  background-repeat: repeat;
  display: block;
  -webkit-transform: skewX(-29deg);
  transform: skewX(-29deg);
}

.title-container::before {
  content: '';
  position: absolute;
  top: 0;
  left: 99%;
  right: -20px;
  bottom: 0;
  background-color: #dedede;
  display: block;
  -webkit-transform: skewX(-29deg);
  transform: skewX(-29deg);
}

.page-title {
  font-size: 5em;
  position: relative;
  z-index: 10;
  margin: -20px 0.25em -16px 1.5em;
  filter: drop-shadow(5px 4px 3px #000000c0)
}

.character-image {
  width: 70vw;
  position: fixed;
  left: 0;
  top: 0;
  z-index: 0;
  pointer-events: none;
  opacity: 0;
  transform: translateX(-300px);
}

.character-image.slide-in {
  animation: slideInLeft 0.6s ease-out forwards;
}

@keyframes slideInLeft {
  0% {
    opacity: 0;
  }

  80% {
    opacity: 1;
  }

  to {
    transform: translateX(0);
    opacity: 1;
  }
}

.info-card {
  background-color: #12121280;
  padding: 0.5em 1em 0.5em 1em;
  margin: 1em 0.5em;
  border-radius: 3px;
  position: relative;
  z-index: 20;
  backdrop-filter: blur(2px) 
                   saturate(0.8) 
                   brightness(0.9);
}

.inline-series {
  width: 30px;
  height: 30px;
  filter: brightness(4.35);
  margin-bottom: -9px;
  margin-left: -4px;
}

strong {
  font-size: larger;
}

.dependency-link {
  margin-left: 5px;
}

.hastooltip {
  border-bottom: 1px dotted #dedede;
  cursor: help;
  margin-right: 5px;
}

ul.functions-list > li {
  margin-bottom: -12px;
}

.row-1 > *:nth-child(2) li,
.row-1 > *:nth-child(3) li {
  font-size: 15px;
}

.row-2 li {
  font-size: 14px;
}

.hook-usage {
  text-align: end;
  width: 100%;
}

.hook-usage > em {
  width: 90%;
  display: inline-block;
}

.edit-link {
  display: inline-block;
  font-size: 16px;
  transition: filter 0.2s ease-in-out;
}

.edit-link:hover {
  filter: brightness(0.8);
}

.mdi-pencil {
  margin-top: -12px;
}

/* Display of checkmark/x */
li {
  display: flex;
  align-items: center;
}

.mdi-check-bold, .mdi-close-thick {
  font-size: 1.6em;
  margin-left: 2px;
}

.mdi-check-bold {
  color: lime;
}

.mdi-close-thick {
  color: red;
}
</style>
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
        backgroundImage: `url('${getFullImageUrl(moveset.series.seriesIconUrl)}')`
      }"
    ></div>
    <div class="background-column side-column left-black"></div>
    <div class="background-column side-column right-black"></div>

    <!-- Main content -->
    <div class="moveset-columns">
      <!-- Column 1 (left) -->
      <div class="column-1">
        <div class="title-container">
          <h1 class="title-font page-title no-select">{{ moveset.moddedCharName }}</h1>
        </div>
        <img
          :src="getFullImageUrl(moveset.movesetHeroImageUrl)"
          alt="Character UI"
          class="character-image"
        />
      </div>

      <!-- Column 2 (middle) -->
      <div class="column-2">
        <!-- Basic Info -->
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
              <template v-for="(mm, index) in moveset.movesetModders" :key="mm.modderId">
                <router-link
                  :to="{ name: 'ModderDetail', params: { id: mm.modderId } }"
                  class="unvisitable"
                >
                  {{ mm.modder.name }}
                </router-link><span v-if="index < moveset.movesetModders.length - 1">, </span>
              </template>
            </strong>
          </div>
          <!-- Series & IDs -->
          <div class="mb-2">
            <p>Series: <strong>{{ moveset.series?.seriesName }} <img :src="getFullImageUrl(moveset.series.seriesIconUrl)" alt="series icon" class="inline-series" /></strong></p>
            <p v-if="moveset.slottedId === moveset.replacementId">
              Internal ID: <strong>{{ moveset.slottedId }}</strong>
            </p>
            <template v-else>
              <p>Slotted ID: <strong>{{ moveset.slottedId }}</strong></p>
              <p>Replacement ID: <strong>{{ moveset.replacementId }}</strong></p>
            </template>
            <p v-if="moveset.slotsStart && moveset.slotsEnd">Slots: <strong>{{ moveset.vanillaChar?.displayName }} c{{ moveset.slotsStart }}-c{{ moveset.slotsEnd }}</strong></p>
            <p v-else>Slots: <strong>{{ moveset.vanillaChar?.displayName }} c???</strong></p>
            <!-- Availability -->
            <p>
              Availability:
              <strong>
                <template v-if="moveset.releaseState?.releaseStateName === 'Released' && !moveset.gamebananaPageId && !moveset.releaseDate">
                  Released
                </template>
                <template v-else-if="moveset.releaseState?.releaseStateName === 'Released' && moveset.gamebananaPageId && !moveset.releaseDate">
                  <a
                    :href="`${GB_PAGE_URL}${moveset.gamebananaPageId}`"
                    target="_blank"
                    class="offsite unvisitable"
                  >
                    Released
                  </a>
                </template>
                <template v-else-if="moveset.modpackName">
                  This moveset is exclusive to {{ moveset.modpackName }}
                </template>
                <template v-else-if="moveset.releaseState?.releaseStateName === 'Released'">
                  <a
                    :href="`${GB_PAGE_URL}${moveset.gamebananaPageId}`"
                    target="_blank"
                    class="offsite unvisitable"
                  >
                    Released {{ new Date(moveset.releaseDate).toLocaleDateString() }}
                  </a>
                </template>
                <template v-else-if="moveset.releaseState?.releaseStateName === 'Unreleased' && moveset.gamebananaWipId">
                  <a
                    :href="`${GB_PAGE_URL}${moveset.gamebananaPageId}`"
                    target="_blank"
                    class="offsite unvisitable"
                  >
                    Released {{ new Date(moveset.releaseDate).toLocaleDateString() }}
                  </a>
                </template>
                <template v-else-if="moveset.releaseState?.releaseStateName === 'Pending Update'">
                  <a
                    :href="`${GB_PAGE_URL}${moveset.gamebananaPageId}`"
                    target="_blank"
                    class="offsite unvisitable"
                  >
                    Originally Released {{ new Date(moveset.releaseDate).toLocaleDateString() }}
                  </a>, {{ moveset.releaseState?.releaseStateName }}
                </template>
                <template v-else>
                  {{ moveset.releaseState?.releaseStateName }}
                </template>
              </strong>
            </p>
            
            <!-- External Links -->
            <p v-if="moveset.modsWikiLink">
              <a
                :href="`${MODS_WIKI_URL}${moveset.modsWikiLink}`"
                target="_blank"
                class="offsite unvisitable"
              >
                View {{ moveset.moddedCharName }} on SSBU Mods Wiki
              </a>
            </p>
            <p v-if="moveset.sourceCodeUrl">
              <a :href="moveset.sourceCodeUrl" target="_blank" class="offsite unvisitable">Source Code</a>
            </p>
          </div>
        </div>

        <!-- Articles -->
        <div class="mb-4 info-card">
          <h1>Articles</h1>
          <ul v-if="moveset.movesetArticles.length > 0">
            <li v-for="ma in moveset.movesetArticles" :key="ma.articleId">
              <strong>{{ ma.description }}</strong>: {{ ma.article.vanillaCharInternalName }}_{{ ma.article.articleName }} ({{ ma.moddedName }})
            </li>
          </ul>
          <p v-else>This moveset does not clone any articles!</p>
        </div>

        <!-- Hooks -->
        <div class="info-card">
          <h1>Hooks</h1>
          <ul v-if="moveset.movesetHooks.length > 0">
            <li v-for="mh in moveset.movesetHooks" :key="mh.hookId">
              <strong :title="mh.hook.description" class="hastooltip">0x{{ mh.hook.offset }}</strong>
              <em v-if="mh.description">{{ mh.description }}</em><em v-else>(unknown usage)</em>
            </li>
          </ul>
          <p v-else>This moveset does not use any hooks!</p>
        </div>
      </div>

      <!-- Column 3 (right) -->
      <div class="column-3">
        <!-- Dependencies -->
        <div class="mb-4 info-card">
          <h1>Dependencies</h1>
          <ul v-if="moveset.movesetDependencies.length > 0">
            <li v-for="md in moveset.movesetDependencies" :key="md.dependencyId">
              • <a :href="md.dependency.downloadLink" target="_blank" class="offsite unvisitable dependency-link">{{ md.dependency.name }}</a>
            </li>
          </ul>
          <p v-else>No dependencies set!</p>
        </div>

        <!-- Function usage -->
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
      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '@/services/api'
import movesetHeroUnknown from "@/assets/moveset_hero_unknown.png"
import { GB_PAGE_URL, GB_WIP_URL, MODS_WIKI_URL } from '@/globals'

const route = useRoute()
const router = useRouter()
const moveset = ref(null)
const user = ref(null)

const apiUrl = import.meta.env.VITE_API_URL

const getFullImageUrl = (path) => {
  if (!path) return movesetHeroUnknown
  return path.startsWith('/') ? `${apiUrl}${path}` : path
}

const backgroundColor = computed(() => {
  const color = moveset.value?.backgroundColor || '000000'
  return `#${color}`
})

const userIsModder = computed(() => {
  if (!user.value || !moveset.value?.movesetModders) return false
  return moveset.value.movesetModders.some(mm => mm.modderId === user.value.modderId)
})

onMounted(async () => {
  try {
    const [movesetRes, userRes] = await Promise.all([
      api.get(`/movesets/${route.params.movesetId}`),
    ])
    moveset.value = movesetRes.data
  } catch (err) {
    router.replace({ name: 'ErrorPage', query: { http: 404, reason: 'Moveset not found' } })
  }
  try {
    api.get('/auth/me')
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
  left: -8%;
  width: 20%;
}

.right-black {
  right: -16%;
  width: 63%;
}

.moveset-columns {
  display: flex;
  position: relative;
  z-index: 10;
  min-height: 100vh;
}

.column-1 {
  flex: 7;
}

.column-2, .column-3 {
  margin-top: 40px;
}

.column-2 {
  flex: 3;
}

.column-3 {
  flex: 2;
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
  max-width: 100%;
  position: fixed;
  left: 0;
  top: 0;
  z-index: 0;
  pointer-events: none;
}

.info-card {
  background-color: #00000080;
  padding: 0.5em 1em 0.5em 1em;
  margin: 1em 0.5em;
  border-radius: 10px;
  position: relative;
  z-index: 20;
  filter: drop-shadow(0px 0px 5px #000000c0);
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
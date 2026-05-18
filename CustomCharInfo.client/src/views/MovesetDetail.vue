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
          <h1 class="title-font page-title no-select">
            {{ moveset.moddedCharName }}<span v-if="moveset.subtitle" class="page-title-subtitle"> ({{ moveset.subtitle }})</span>
          </h1>
        </div>
        <div class="left-overlay">
          <div v-if="moveset.isJokeMoveset" class="moveset-warning joke-warning">
            <span class="pill pill--joke"><v-icon size="14">mdi-egg-easter</v-icon> Joke Moveset</span>
          </div>
          <div v-if="warningInfo" class="moveset-warning">This moveset is <span v-if="warningInfo.isPrivate" class="pill pill--private">Private</span><span v-if="warningInfo.isPrivate && warningInfo.pendingType"> and </span><span v-if="warningInfo.pendingType" :class="['pill', warningInfo.pendingType === 'Admin' ? 'pill--admin' : 'pill--user']">Pending {{ warningInfo.pendingType }} Action</span>. It can only be seen by {{ singleModder ? 'you' : 'its creators' }} and site admins.</div>
          <div class="like-row">
            <button
              class="like-btn"
              :class="{ 'like-btn--liked': userLiked }"
              @click="toggleLike"
              :title="user ? (userLiked ? 'Unlike' : 'Like') : 'Sign in to like'"
            >
              <v-icon>{{ userLiked ? 'mdi-heart' : 'mdi-heart-outline' }}</v-icon>
            </button>
            <span class="like-count">{{ likeCount }}</span>
            <router-link
              v-if="userIsModder"
              :to="{ name: 'EditMoveset', params: { movesetId: route.params.movesetId } }"
              class="edit-link unvisitable"
              title="Edit moveset"
            >
              <v-icon>mdi-pencil</v-icon>
            </router-link>
          </div>
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
            <div class="info-card basic-info-card">
              <!-- Header -->
              <h1>Basic Info</h1>

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
              <div class="functions-list">
                <v-tooltip text="Runs once every frame for all characters" location="left" open-delay="500">
                  <template #activator="{ props }">
                    <div class="function-row" v-bind="props">
                      <span>Global OPFF</span><StatusIcon :value="moveset.hasGlobalOpff" />
                    </div>
                  </template>
                </v-tooltip>
                <v-tooltip :text="`Runs once every frame for ${moveset.vanillaChar?.displayName ?? 'the character'}`" location="left" open-delay="500">
                  <template #activator="{ props }">
                    <div class="function-row" v-bind="props">
                      <span>Character OPFF</span><StatusIcon :value="moveset.hasCharacterOpff" />
                    </div>
                  </template>
                </v-tooltip>
                <v-tooltip text="Runs once when a fighter is spawned in" location="left" open-delay="500">
                  <template #activator="{ props }">
                    <div class="function-row" v-bind="props">
                      <span>agent_init</span><StatusIcon :value="moveset.hasAgentInit" />
                    </div>
                  </template>
                </v-tooltip>
                <v-tooltip text="Runs once every time a pre status script runs" location="left" open-delay="500">
                  <template #activator="{ props }">
                    <div class="function-row" v-bind="props">
                      <span>on_line pre</span><StatusIcon :value="moveset.hasGlobalOnLinePre" />
                    </div>
                  </template>
                </v-tooltip>
                <v-tooltip text="Runs once every time an end status script runs" location="left" open-delay="500">
                  <template #activator="{ props }">
                    <div class="function-row" v-bind="props">
                      <span>on_line end</span><StatusIcon :value="moveset.hasGlobalOnLineEnd" />
                    </div>
                  </template>
                </v-tooltip>
              </div>
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
import { useHead } from '@unhead/vue'
import api from '@/services/api'
import movesetHeroUnknown from "@/assets/moveset_hero_unknown.png"
import seriesIconUnknown from "@/assets/series_icon_unknown.png"
import { GB_WIP_URL, MODS_WIKI_URL } from '@/globals'

const route = useRoute()
const router = useRouter()
const moveset = ref(null)
const user = ref(null)
const likeCount = ref(0)
const userLiked = ref(false)
const latestLog = ref(null)

useHead(computed(() => {
  const name = moveset.value?.moddedCharName
  const description = name
    ? `View ${name} moveset info on Ultimate Moveset Compatibility.`
    : 'View information on Super Smash Bros. Ultimate custom movesets.'
  const image = moveset.value?.movesetHeroImageUrl
    ? getFullImageUrl(moveset.value.movesetHeroImageUrl, '')
    : null
  return {
    title: name ? `UMC | ${name} Moveset` : 'UMC',
    meta: [
      { name: 'description', content: description },
      { property: 'og:title', content: name ? `${name} Moveset` : 'Ultimate Moveset Compatibility' },
      { property: 'og:description', content: description },
      ...(image ? [
        { property: 'og:image', content: image },
        { name: 'twitter:card', content: 'summary_large_image' },
        { name: 'twitter:image', content: image },
      ] : []),
      { name: 'twitter:title', content: name ? `${name} Moveset` : 'Ultimate Moveset Compatibility' },
      { name: 'twitter:description', content: description },
    ],
  }
}))

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

const singleModder = computed(() => moveset.value?.movesetModders?.length === 1)

const warningInfo = computed(() => {
  if (!moveset.value) return null
  const isPrivate = !!moveset.value.privateMoveset
  const stateId = latestLog.value?.acceptanceState?.acceptanceStateId
  const pendingType = stateId === 2 ? 'Admin' : stateId === 4 ? 'User' : null
  if (!isPrivate && !pendingType) return null
  return { isPrivate, pendingType }
})

// Calculate the releaseState to display
const releaseDisplay = computed(() => {
  if (!moveset.value) return null

  const {
    modpackName,
    sourceCode,
    releaseState,
    modPageUrl,
    gamebananaWipId,
    releaseDate
  } = moveset.value

  const state = releaseState?.releaseStateName
  const pageUrl = modPageUrl || null
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

  // Upcoming
  if (state === 'Upcoming') {
    let text = 'Upcoming'
    if (hasDate) {
      text += ` ${formatDate(date)}`
    }

    return { text, url }
  }

  return null
})

const toggleLike = async () => {
  if (!user.value) return
  try {
    const res = await api.post(`/movesets/${route.params.movesetId}/like`)
    likeCount.value = res.data.likeCount
    userLiked.value = res.data.userLiked
  } catch {
    //
  }
}

// Mounted
onMounted(async () => {
  try {
    const movesetRes = await api.get(`/movesets/${route.params.movesetId}`)
    moveset.value = movesetRes.data
    likeCount.value = movesetRes.data.likeCount ?? 0
    userLiked.value = movesetRes.data.userLiked ?? false
  } catch (err) {
    router.replace({ name: 'ErrorPage', query: { http: 404, reason: 'Moveset not found' } })
  }
  try {
    const userRes = await api.get('/auth/me')
    user.value = userRes.data
  } catch {
    //
  }
  try {
    const isAdmin = user.value?.userTypeId === 3
    const logsRes = await api.get(isAdmin ? '/logs?viewAll=true' : '/logs')
    const movesetId = parseInt(route.params.movesetId)
    latestLog.value = logsRes.data
      .filter(log => log.itemType?.itemTypeId === 1 && log.item?.movesetId === movesetId)
      .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))[0] ?? null
  } catch {
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
  position: relative;
}

.column-right {
  flex: 9;
  margin-top: 5em;
}

.title-container {
  position: absolute;
  width: max-content;
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

.basic-info-card p {
  color: #999;
}
.basic-info-card p strong,
.basic-info-card p a {
  color: #dedede;
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

:deep(.v-overlay__content) {
  background-color: #000000e0 !important;
  border: 1px solid #666 !important;
}

.hastooltip {
  border-bottom: 1px dotted #dedede;
  cursor: help;
  margin-right: 5px;
}

.functions-list {
  display: flex;
  flex-direction: column;
  gap: 0;
}
.function-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  cursor: default;
}
.function-row > i {
  line-height: 24px;
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
  transition: color 0.2s ease-in-out;
}

.edit-link:hover {
  color: #fff;
}

.left-overlay {
  position: absolute;
  top: 5.5em;
  left: 1.1em;
  z-index: 10;
  display: flex;
  flex-direction: column;
  gap: 0.5em;
}

.moveset-warning {
  font-size: 0.75em;
  color: #ccc;
  max-width: 22.5em;
  background-color: #12121299;
  padding: 0.35em 0.6em;
  border-radius: 4px;
  backdrop-filter: blur(3px);
  line-height: 1.5;
}

.pill {
  display: inline-block;
  font-size: 0.9em;
  padding: 0.05em 0.45em;
  border-radius: 999px;
  font-weight: bold;
  color: #111;
  vertical-align: baseline;
}

.pill--private {
  background-color: rgb(220, 50, 50);
  color: #fff;
}

.pill--admin {
  background-color: rgb(52, 194, 241);
}

.pill--user {
  background-color: rgb(241, 241, 52);
}

.pill--joke {
  background-color: #ff733c;
  color: #111;
  display: inline-flex;
  align-items: center;
  font-size: 12px;
  gap: 4px;
}

.joke-warning {
  background: none;
  padding: 0;
  backdrop-filter: none;
}

.page-title-subtitle {
  font-size: 0.45em;
  opacity: 0.55;
  font-weight: normal;
  letter-spacing: 0;
  vertical-align: middle;
}

.like-row {
  display: flex;
  align-items: center;
  gap: 0.4em;
  color: #ccc;
}

.like-btn {
  background: none;
  border: none;
  cursor: pointer;
  padding: 0;
  line-height: 1;
  transition: color 0.15s;
}

.like-btn:hover {
  color: #fff;
}

.like-count {
  font-size: 0.9em;
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
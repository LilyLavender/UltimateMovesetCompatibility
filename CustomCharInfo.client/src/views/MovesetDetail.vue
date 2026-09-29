<template>
  <div v-if="moveset" class="moveset-detail" :style="{ '--accent': backgroundColor }">
    <!-- Background: color column fading to black, grid, and series icon pattern -->
    <div class="color-column">
      <div class="color-column__grid"></div>
      <div
        class="color-column__icons"
        :style="{
          backgroundImage: `url('${getFullImageUrl(moveset.series.seriesIconUrl, '')}')`,
        }"
      ></div>
    </div>

    <!-- Site grid is disabled on this page since it creates its own: it runs down only the right edge of the content -->
    <div class="edge-grid"></div>

    <div class="detail-grid">
      <!-- Left: the name over the render -->
      <div class="column-left">
        <div class="title-container">
          <h1 class="detail-title no-select">
            {{ moveset.moddedCharName
            }}<span v-if="moveset.subtitle" class="detail-title__subtitle">
              ({{ moveset.subtitle }})</span
            >
          </h1>
        </div>
        <img
          :src="getFullImageUrl(moveset.movesetHeroImageUrl, movesetHeroUnknown)"
          alt="Character render"
          class="character-image"
          :class="{ 'slide-in': imageLoaded }"
          @load="imageLoaded = true"
        />
        <div class="left-overlay">
          <div v-if="warningInfo" class="moveset-warning">
            This moveset is
            <StatusTag v-if="warningInfo.isPrivate" variant="err">Private</StatusTag>
            <span v-if="warningInfo.isPrivate && warningInfo.pendingType"> and </span>
            <StatusTag v-if="warningInfo.pendingType" :state="warningInfo.stateId" />. It can only
            be seen by {{ singleModder ? 'you' : 'its creators' }} and site admins.
          </div>
        </div>
      </div>

      <!-- Right: two independent stacks: Basic info and Dependencies, then Creators and Functions -->
      <div class="column-right">
        <div class="stack">
          <section class="panel panel--basic">
            <div class="panel__head">
              <h3 class="panel__title">Basic info</h3>
              <StatusTag v-if="moveset.isJokeMoveset" variant="warn" icon="mdi-egg-easter">
                Joke moveset
              </StatusTag>
              <AppButton
                v-if="canEdit"
                :to="{ name: 'EditMoveset', params: { movesetId: route.params.movesetId } }"
                variant="ghost"
                size="sm"
                icon="mdi-pencil"
                aria-label="Edit moveset"
              />
              <div class="like-group">
                <button
                  type="button"
                  class="like-btn"
                  :class="{ 'like-btn--liked': userLiked }"
                  :title="user ? (userLiked ? 'Unlike' : 'Like') : 'Sign in to like'"
                  :aria-label="userLiked ? 'Unlike' : 'Like'"
                  @click="toggleLike"
                >
                  <v-icon size="18">{{ userLiked ? 'mdi-heart' : 'mdi-heart-outline' }}</v-icon>
                </button>
                <span class="like-count" :title="`${likeCount} likes`">{{ likeCount }}</span>
              </div>
            </div>
            <dl class="kv">
              <dt>Series</dt>
              <dd class="kv__series">
                <img
                  :src="getFullImageUrl(moveset.series.seriesIconUrl, seriesIconUnknown)"
                  alt=""
                  class="inline-series"
                />
                {{ moveset.series?.seriesName }}
              </dd>

              <template v-if="moveset.slottedId === moveset.replacementId">
                <dt>Internal id</dt>
                <dd class="mono">{{ moveset.slottedId }}</dd>
              </template>
              <template v-else>
                <dt>Slotted id</dt>
                <dd class="mono">{{ moveset.slottedId }}</dd>
                <dt>Replacement id</dt>
                <dd class="mono">{{ moveset.replacementId }}</dd>
              </template>

              <dt>{{ slotsLabel }}</dt>
              <dd>
                {{ moveset.vanillaChar?.displayName }}
                <span class="mono">{{
                  moveset.slotsStart && moveset.slotsEnd
                    ? formatSlotRange(moveset.slotsStart, moveset.slotsEnd)
                    : 'c???'
                }}</span>
              </dd>

              <dt>Availability</dt>
              <dd>
                <template v-if="releaseDisplay">
                  <a
                    v-if="releaseDisplay.url"
                    :href="releaseDisplay.url"
                    target="_blank"
                    class="offsite unvisitable"
                  >
                    {{ releaseDisplay.text }}
                  </a>
                  <template v-else>{{ releaseDisplay.text }}</template>
                </template>
                <template v-else>{{ moveset.releaseState?.releaseStateName }}</template>
              </dd>

              <template v-if="moveset.modsWikiLink">
                <dt>Wiki</dt>
                <dd>
                  <a
                    :href="`${MODS_WIKI_URL}${moveset.modsWikiLink}`"
                    target="_blank"
                    class="offsite unvisitable"
                  >
                    {{ moveset.moddedCharName }} on SSBU Mods Wiki
                  </a>
                </dd>
              </template>

              <template v-if="moveset.sourceCode && !moveset.modpackName">
                <dt>Source</dt>
                <dd>
                  <a :href="moveset.sourceCode" target="_blank" class="offsite unvisitable">
                    Source code
                  </a>
                </dd>
              </template>
            </dl>
            <AppButton
              :to="{ name: 'CompatibilityCheck', query: { moveset: moveset.movesetId } }"
              variant="ghost"
              size="sm"
              icon="mdi-swap-horizontal"
              class="panel__corner"
            >
              Compatibility check
            </AppButton>
          </section>

          <section class="panel panel--dependencies">
            <h3 class="panel__title">Dependencies</h3>
            <div v-if="moveset.movesetDependencies.length > 0" class="dependencies">
              <a
                v-for="md in moveset.movesetDependencies"
                :key="md.dependencyId"
                :href="md.dependency.downloadLink"
                target="_blank"
                rel="noopener"
                class="dependency"
              >
                {{ md.dependency.name }}
                <v-icon size="13">mdi-open-in-new</v-icon>
              </a>
            </div>
            <p v-else class="panel__empty">No dependencies set!</p>
            <AppButton
              :to="{ name: 'PluginLookup' }"
              variant="ghost"
              size="sm"
              icon="mdi-file-search"
              class="panel__corner"
            >
              Plugin lookup
            </AppButton>
          </section>
        </div>

        <div class="stack">
          <section v-if="moveset.movesetModders?.length" class="panel panel--creators">
            <h3 class="panel__title">
              Creator<span v-if="moveset.movesetModders.length > 1">s</span>
            </h3>
            <ul class="creators">
              <li v-for="mm in moveset.movesetModders" :key="mm.modder.modderId">
                <router-link
                  :to="{ name: 'ModderDetail', params: { id: mm.modder.modderId } }"
                  class="creator"
                >
                  <span class="creator__pfp">
                    <img
                      v-if="creatorPicture(mm.modder)"
                      :src="creatorPicture(mm.modder)"
                      alt=""
                      loading="lazy"
                    />
                    <v-icon v-else size="20">mdi-account</v-icon>
                  </span>
                  <span class="creator__name">{{ mm.modder.name }}</span>
                </router-link>
              </li>
            </ul>
          </section>

          <section class="panel panel--functions">
            <h3 class="panel__title">Functions</h3>
            <div class="functions">
              <v-tooltip
                v-for="fn in functionRows"
                :key="fn.key"
                :text="fn.tip"
                location="top"
                open-delay="500"
              >
                <template #activator="{ props }">
                  <span v-bind="props">
                    <StatusTag
                      :variant="moveset[fn.key] ? 'ok' : 'neutral'"
                      :icon="moveset[fn.key] ? 'mdi-check-bold' : 'mdi-close-thick'"
                    >
                      {{ fn.label }}
                    </StatusTag>
                  </span>
                </template>
              </v-tooltip>
            </div>
          </section>
        </div>
      </div>

      <!-- Tables full width under both columns -->
      <div
        v-if="moveset.movesetArticles.length || moveset.movesetHooks.length"
        class="tables"
        :class="{ 'tables--stacked': stackTables }"
      >
        <section
          v-if="moveset.movesetArticles.length"
          class="panel panel--table"
          :class="{ 'panel--table-right': stackTables || !moveset.movesetHooks.length }"
        >
          <h3 class="panel__title panel__title--inset">
            Articles
            <span v-if="moveset.movesetArticles.length" class="panel__count">{{
              moveset.movesetArticles.length
            }}</span>
          </h3>
          <v-table density="compact">
            <thead>
              <tr>
                <th>Article</th>
                <th>Cloned from</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="ma in moveset.movesetArticles" :key="ma.articleId ?? ma.moddedName">
                <td>
                  <span class="cell-strong">{{ ma.description }}</span>
                  <span class="cell-sub mono">{{ ma.moddedName }}</span>
                </td>
                <td class="mono">
                  {{ ma.article.vanillaCharInternalName }}_{{ ma.article.articleName }}
                </td>
              </tr>
            </tbody>
          </v-table>
        </section>

        <section v-if="moveset.movesetHooks.length" class="panel panel--table panel--table-right">
          <h3 class="panel__title panel__title--inset">
            Hooks
            <span v-if="moveset.movesetHooks.length" class="panel__count">{{
              moveset.movesetHooks.length
            }}</span>
          </h3>
          <v-table density="compact">
            <thead>
              <tr>
                <th>Offset</th>
                <th>Hook</th>
                <th>Why it is used</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="mh in moveset.movesetHooks" :key="mh.hookId ?? mh.hook.offset">
                <td class="mono cell-nowrap">{{ formatOffset(mh.hook.offset) }}</td>
                <td>{{ mh.hook.description }}</td>
                <td class="muted">
                  <template v-if="mh.description">{{ mh.description }}</template>
                  <em v-else>(unknown usage)</em>
                </td>
              </tr>
            </tbody>
          </v-table>
        </section>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useHead } from '@unhead/vue'
import axios from 'axios'
import api from '@/services/api'
import movesetHeroUnknown from '@/assets/moveset_hero_unknown.png'
import seriesIconUnknown from '@/assets/series_icon_unknown.png'
import { GB_WIP_URL, MODS_WIKI_URL, ItemType, AcceptanceState, ReleaseState } from '@/globals'
import { dateOnlyStringToLocalDate } from '@/services/dateOnly'
import { formatOffset } from '@/services/offsets'
import { formatSlotRange } from '@/services/slots'
import StatusTag from '@/components/StatusTag.vue'
import AppButton from '@/components/AppButton.vue'

const route = useRoute()
const router = useRouter()
const moveset = ref(null)
const user = ref(null)
const likeCount = ref(0)
const userLiked = ref(false)
const latestLog = ref(null)

useHead(
  computed(() => {
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
        {
          property: 'og:title',
          content: name ? `${name} Moveset` : 'Ultimate Moveset Compatibility',
        },
        { property: 'og:description', content: description },
        ...(image
          ? [
              { property: 'og:image', content: image },
              { name: 'twitter:card', content: 'summary_large_image' },
              { name: 'twitter:image', content: image },
            ]
          : []),
        {
          name: 'twitter:title',
          content: name ? `${name} Moveset` : 'Ultimate Moveset Compatibility',
        },
        { name: 'twitter:description', content: description },
      ],
    }
  })
)

const apiUrl = import.meta.env.VITE_API_URL

const imageLoaded = ref(false)

function getFullImageUrl(path, elsepath) {
  if (!path) return elsepath
  return path.startsWith('/') ? `${apiUrl}${path}` : path
}

const backgroundColor = computed(() => {
  const color = moveset.value?.backgroundColor || '000000'
  return `#${color}`
})

// The API decides: credited modders and editors may edit, admins only when they are one of those.
const canEdit = computed(() => !!moveset.value?.canEdit)

const singleModder = computed(() => moveset.value?.movesetModders?.length === 1)

// Short article and hook lists stack in the right column, long ones get a column each
const stackTables = computed(() => {
  const m = moveset.value
  if (!m) return false
  return m.movesetArticles.length + m.movesetHooks.length < 10
})

const slotsLabel = computed(() => {
  const now = new Date()
  return now.getMonth() === 3 && now.getDate() === 1 ? 'Schmeebulates' : 'Slots'
})

const functionRows = computed(() => [
  { key: 'hasGlobalOpff', label: 'Global OPFF', tip: 'Runs once every frame for all characters' },
  {
    key: 'hasCharacterOpff',
    label: 'Character OPFF',
    tip: `Runs once every frame for ${moveset.value?.vanillaChar?.displayName ?? 'the character'}`,
  },
  { key: 'hasAgentInit', label: 'agent_init', tip: 'Runs once when a fighter is spawned in' },
  {
    key: 'hasGlobalOnLinePre',
    label: 'on_line pre',
    tip: 'Runs once every time a pre status script runs',
  },
  {
    key: 'hasGlobalOnLineEnd',
    label: 'on_line end',
    tip: 'Runs once every time an end status script runs',
  },
])

// Creator pictures: uploaded one, else the GameBanana avatar
const avatars = ref({})
const creatorPicture = (modder) => modder.pfpUrl || avatars.value[modder.modderId] || null

const fetchAvatars = async () => {
  const pending = (moveset.value?.movesetModders ?? [])
    .map((mm) => mm.modder)
    .filter((m) => !m.pfpUrl && m.gamebananaId)
  await Promise.all(
    pending.map(async (m) => {
      try {
        const r = await axios.get(
          `https://api.gamebanana.com/Core/Item/Data?itemtype=Member&itemid=${m.gamebananaId}&fields=Url().sHdAvatarUrl(),Url().sAvatarUrl()`
        )
        const url = r.data[0] || r.data[1] || null
        if (url) avatars.value[m.modderId] = url
      } catch {
        // A missing avatar falls back to the account icon
      }
    })
  )
}

watch(moveset, (m) => {
  if (m) fetchAvatars()
})

const warningInfo = computed(() => {
  if (!moveset.value) return null
  const isPrivate = !!moveset.value.privateMoveset
  const stateId = latestLog.value?.acceptanceState?.acceptanceStateId
  const pendingType =
    stateId === AcceptanceState.PendingAdminHard
      ? 'Admin'
      : stateId === AcceptanceState.PendingUserHard
        ? 'User'
        : null
  if (!isPrivate && !pendingType) return null
  return { isPrivate, pendingType, stateId }
})

// Calculate the releaseState to display
const releaseDisplay = computed(() => {
  if (!moveset.value) return null

  const { modpackName, sourceCode, releaseState, modPageUrl, gamebananaWipId, releaseDate } =
    moveset.value

  const stateId = releaseState?.releaseStateId
  const pageUrl = modPageUrl || null
  const wipUrl = gamebananaWipId ? `${GB_WIP_URL}${gamebananaWipId}` : null
  const url = pageUrl || wipUrl

  const hasDate = !!releaseDate
  const date = hasDate ? dateOnlyStringToLocalDate(releaseDate) : null
  const today = new Date()
  const isPast = date && date <= today

  const formatDate = (d) => d.toLocaleDateString()

  // Modpack
  if (modpackName) {
    return {
      text: `Exclusive to ${modpackName}`,
      url: sourceCode || null,
    }
  }

  if (stateId === ReleaseState.Deprecated) {
    return { text: 'Deprecated', url }
  }

  if (stateId === ReleaseState.OpenBeta) {
    let text = 'Open Beta'
    if (hasDate) {
      text += isPast ? ` (Released ${formatDate(date)})` : ` (Releases ${formatDate(date)})`
    }

    return { text, url }
  }

  if (stateId === ReleaseState.PendingUpdate) {
    if (!hasDate) {
      return { text: 'Pending Update', url }
    }

    return {
      text: isPast
        ? `Originally Released ${formatDate(date)}`
        : `Update Releases ${formatDate(date)}`,
      url,
    }
  }

  if (stateId === ReleaseState.Released) {
    let text = 'Released'
    if (hasDate) {
      text += ` ${formatDate(date)}`
    }

    return { text, url }
  }

  if (stateId === ReleaseState.Upcoming) {
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
  } catch {
    router.replace({ name: 'ErrorPage', query: { http: 404, reason: 'Moveset not found' } })
  }
  try {
    const userRes = await api.get('/auth/me')
    user.value = userRes.data
  } catch {
    //
  }
  try {
    // Newest first. Only owners/editors and admins read it, anyone else lands in the catch.
    const logsRes = await api.get(`/logs/${ItemType.Moveset}-${moveset.value.movesetId}`)
    latestLog.value = logsRes.data[0] ?? null
  } catch {
    //
  }
})
</script>

<style scoped>
/*
  The page: a skewed column in the moveset's color fading to black, the render fixed in front of it,
  the name on the striped slab, the panels on the right, and the tables across the bottom.
*/
/* At least as tall as the window and as the 70vw render, so the footer stays below both */
.moveset-detail {
  position: relative;
  min-height: max(100vh, 70vw);
  padding: 2rem;
  background-color: var(--bg);
}

/* Color column */
/* Flush with the top of the window; the header floats over it */
.color-column {
  position: absolute;
  top: 0;
  left: 8%;
  width: 40%;
  height: 100%;
  min-height: 100vh;
  overflow: hidden;
  pointer-events: none;
  transform: skewX(-12deg);
  transform-origin: top left;
  background: linear-gradient(
    180deg,
    var(--accent) 0,
    color-mix(in srgb, var(--accent) 55%, #000) 700px,
    #000 1150px
  );
  z-index: 0;
}

/* A white grid inside the color only, gone before the color is */
.color-column__grid {
  position: absolute;
  inset: 0;
  background-image:
    linear-gradient(rgba(255, 255, 255, 0.14) 1px, transparent 1px),
    linear-gradient(90deg, rgba(255, 255, 255, 0.14) 1px, transparent 1px);
  background-size: 28px 28px;
  background-repeat: repeat;
  -webkit-mask-image: linear-gradient(180deg, #000 0, transparent 900px);
  mask-image: linear-gradient(180deg, #000 0, transparent 900px);
}

.color-column__icons {
  position: absolute;
  inset: -10% 0 0;
  -webkit-mask-image: linear-gradient(180deg, #000 0, transparent 1250px);
  mask-image: linear-gradient(180deg, #000 0, transparent 1250px);
  background-repeat: repeat;
  background-size: 100px auto;
  opacity: 0.06;
  filter: brightness(10);
  mix-blend-mode: plus-lighter;
  transform: skewX(3deg) rotate3d(1, 0, 0, 30deg) translate(3%, -4%);
  transform-origin: top center;
}

/* The right-edge grid, this page's only one outside the color */
.edge-grid {
  position: absolute;
  inset: 0 0 -160px;
  z-index: 0;
  pointer-events: none;
  background-image:
    linear-gradient(rgba(255, 255, 255, 0.1) 1px, transparent 1px),
    linear-gradient(90deg, rgba(255, 255, 255, 0.1) 1px, transparent 1px);
  background-size: 28px 28px;
  background-repeat: repeat;
  -webkit-mask-image: linear-gradient(
    90deg,
    transparent 48%,
    rgba(0, 0, 0, 0.4) 74%,
    #000 92%,
    #000 100%
  );
  mask-image: linear-gradient(90deg, transparent 48%, rgba(0, 0, 0, 0.4) 74%, #000 92%, #000 100%);
}

/* The render: behind the title text (z 10), in front of the banner (z auto), scrolling with the page */
.character-image {
  width: 70vw;
  position: absolute;
  left: -2rem;
  top: -2rem;
  z-index: 1;
  pointer-events: none;
  opacity: 0;
  transform: translateX(-300px);
  -webkit-mask-image: linear-gradient(180deg, #000 85%, transparent 100%);
  mask-image: linear-gradient(180deg, #000 85%, transparent 100%);
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

/* Layout */
.detail-grid {
  position: relative;
  z-index: 2;
  display: grid;
  grid-template-columns: 7fr 9fr;
  gap: 0 24px;
  align-items: start;
}

/* The left column is only as tall as the title; the tables follow the panels, and the render runs on behind them */
.column-left {
  position: relative;
}

/* Two independent stacks of panels, so nothing waits for its neighbor's height. Panels sit above the render */
.column-right {
  position: relative;
  z-index: 3;
  display: grid;
  grid-template-columns: 3fr 2fr;
  gap: 16px;
  align-items: start;
  margin-top: 8em;
}

.stack {
  display: flex;
  flex-direction: column;
  gap: 16px;
  min-width: 0;
}

/* The same 7:9 split as the columns above, so the right table lines up with the panels */
.tables {
  grid-column: 1 / -1;
  position: relative;
  z-index: 3;
  display: grid;
  grid-template-columns: 7fr 9fr;
  gap: 0 24px;
  align-items: start;
  margin-top: 16px;
}

.panel--table-right {
  grid-column: 2;
}

.tables--stacked {
  row-gap: 16px;
}

/* Name slab */
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
  background-color: #000;
  background-image: url(/src/assets/ptn_diagonal_12.png);
  background-repeat: repeat;
  display: block;
  transform: skewX(-29deg);
}

.title-container::before {
  content: '';
  position: absolute;
  top: 0;
  left: 99%;
  right: -20px;
  bottom: 0;
  background-color: var(--white);
  display: block;
  transform: skewX(-29deg);
}

.detail-title {
  font-family: var(--font-display);
  font-weight: 400;
  font-size: 5em;
  line-height: normal;
  text-transform: uppercase;
  position: relative;
  z-index: 10;
  margin: -2px 0.25em 2px 1.5em;
  filter: drop-shadow(5px 4px 3px color-mix(in srgb, var(--bg) 75%, transparent));
}

.detail-title__subtitle {
  font-size: 0.45em;
  opacity: 0.55;
  font-weight: normal;
  letter-spacing: 0;
  vertical-align: middle;
}

/* Far left: joke tag and visibility warning */
.left-overlay {
  position: absolute;
  top: 7em;
  left: 0;
  z-index: 10;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 8px;
}

.moveset-warning {
  font-size: 0.8em;
  color: var(--tx-2);
  max-width: 24em;
  background-color: color-mix(in srgb, var(--panel-2) 60%, transparent);
  border: 1px solid var(--line);
  padding: 0.4em 0.7em;
  backdrop-filter: blur(3px);
  line-height: 1.9;
}

/* Panel header row: title, edit, and the like group on the right */
.panel__head {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 12px;
}

.panel__head .panel__title {
  margin: 0;
}

.like-group {
  margin-left: auto;
  display: inline-flex;
  align-items: center;
}

.like-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  height: 30px;
  padding: 0 12px;
  background: #000;
  border: 1px solid var(--white);
  color: var(--white);
  font: inherit;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  line-height: 1;
  transition:
    background-color var(--dur-fast) var(--ease),
    color var(--dur-fast) var(--ease);
}

.like-btn--liked {
  background: var(--white);
  color: #000;
}

.like-count {
  display: inline-flex;
  align-items: center;
  height: 30px;
  padding: 0 10px;
  border: 1px solid var(--white);
  border-left: 0;
  font-family: var(--font-mono);
  font-size: 13px;
  font-weight: 600;
}

/* Panels */
/* Translucent, without a backdrop filter: Chromium hides the masked render behind a backdrop-filtered sibling stacked above it */
.panel {
  background-color: color-mix(in srgb, var(--panel) 85%, transparent);
}

.panel--table {
  padding: 0;
}

.panel__title {
  margin: 0 0 12px;
  font-size: 1.25rem;
  text-transform: uppercase;
  letter-spacing: 0.01em;
}

.panel__title--inset {
  padding: 16px 20px 0;
}

.panel__count {
  margin-left: 6px;
  font-family: var(--font-body);
  font-size: 13px;
  font-weight: 400;
  text-transform: none;
  color: var(--tx-3);
}

/* A button pinned to the panel's bottom right corner. Content keeps clear of it on the right */
.panel--basic,
.panel--dependencies {
  position: relative;
}

.panel__corner {
  position: absolute;
  right: 16px;
  bottom: 16px;
}

/* Only the rows level with the button keep clear of it */
.panel--basic .kv > dd:nth-last-child(-n + 3) {
  padding-right: 180px;
}

.panel--dependencies .dependencies,
.panel--dependencies .panel__empty {
  padding-right: 150px;
}

.panel__empty {
  margin: 0;
  color: var(--tx-2);
}

.panel__empty--inset {
  padding: 0 20px 18px;
}

.panel-pair {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

.kv {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: 6px 16px;
  margin: 0;
  font-size: 14px;
}

.kv dt {
  color: var(--tx-3);
}

.kv dd {
  margin: 0;
  color: var(--tx);
}

.kv__series {
  display: flex;
  align-items: center;
  gap: 6px;
}

.inline-series {
  width: 22px;
  height: 22px;
  filter: brightness(4.35);
}

.creators {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.creator {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  color: var(--tx);
  text-decoration: none;
  font-weight: 600;
}

.creator:hover .creator__name {
  text-decoration: underline;
}

.creator__pfp {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  flex: none;
  overflow: hidden;
  background: var(--panel-2);
  color: var(--tx-3);
}

.creator__pfp img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.functions {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.functions .status-tag {
  cursor: default;
}

.dependencies {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.dependency {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  height: 22px;
  padding: 0 8px;
  border: 1px solid var(--line-2);
  color: var(--tx-2);
  font-size: 12px;
  font-weight: 600;
  text-decoration: none;
  transition:
    color var(--dur-fast) var(--ease),
    border-color var(--dur-fast) var(--ease);
}

.dependency:hover {
  color: var(--white);
  border-color: var(--white);
}

/* Tables */
.cell-strong {
  display: block;
  font-weight: 600;
}

.cell-sub {
  display: block;
  color: var(--tx-3);
  font-size: 12px;
}

.cell-nowrap {
  white-space: nowrap;
}

:deep(.v-table) {
  background: transparent;
  border: 0;
}

:deep(.v-table th) {
  background: transparent;
  border-bottom: 1px solid var(--line) !important;
}

:deep(.v-table tbody tr:hover > td) {
  background: color-mix(in srgb, var(--panel-2) 60%, transparent);
}

:deep(.v-overlay__content) {
  background-color: color-mix(in srgb, var(--bg) 88%, transparent) !important;
  border: 1px solid var(--tx-3) !important;
}

/* Tablet and phone: the render becomes a normal block above everything, the columns stack. */
@media (max-width: 959px) {
  .moveset-detail {
    padding: 1rem;
  }

  .color-column {
    left: -10%;
    width: 80%;
    height: 520px;
    min-height: 0;
  }

  .character-image {
    position: relative;
    left: 0;
    top: 0;
    width: 100%;
    margin-top: 1em;
    -webkit-mask-image: linear-gradient(180deg, #000 88%, transparent 100%);
    mask-image: linear-gradient(180deg, #000 88%, transparent 100%);
  }

  .detail-grid {
    grid-template-columns: 1fr;
  }

  .title-container {
    position: relative;
    width: auto;
    max-width: 100%;
  }

  .detail-title {
    font-size: 2.8em;
    margin: 0 0.25em 0 0.6em;
    white-space: normal;
  }

  .left-overlay {
    position: static;
    margin: 1em 0 0;
  }

  .moveset-warning {
    max-width: none;
  }

  .column-right {
    margin-top: 1.5em;
  }

  .column-right {
    grid-template-columns: 1fr;
  }

  .panel__corner {
    position: static;
    margin-top: 12px;
  }

  .panel--basic .kv > dd:nth-last-child(-n + 3),
  .panel--dependencies .dependencies,
  .panel--dependencies .panel__empty {
    padding-right: 0;
  }

  .tables {
    grid-template-columns: 1fr;
    gap: 16px;
  }

  .panel--table-right {
    grid-column: 1;
  }
}

@media (prefers-reduced-motion: reduce) {
  .character-image {
    opacity: 1;
    transform: none;
    animation: none;
  }
}
</style>

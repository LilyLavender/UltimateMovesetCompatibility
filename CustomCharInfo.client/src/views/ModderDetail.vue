<template>
  <v-container>
    <v-row class="modder-section-1">
      <v-col cols="2" class="text-center">
        <!-- Pfp -->
        <img v-if="modderPfpUrl" :src="modderPfpUrl" class="modder-pfp" alt="Profile picture" />
        <div v-else class="modder-pfp-null"><v-icon size="128">mdi-account</v-icon></div>

        <!-- Social Links -->
        <div class="social-links">
          <v-tooltip v-if="modder?.gamebananaId" location="bottom">
            <template #activator="{ props: tip }">
              <a v-bind="tip" :href="`${GB_MEMBER_URL}${modder.gamebananaId}`" class="social-link" target="_blank" rel="noopener">
                <img src="https://images.gamebanana.com/img/ico/games/banana.gif" class="social-icon-img" alt="GameBanana" />
              </a>
            </template>
            <span class="tooltip-label">GameBanana <v-icon size="x-small">mdi-open-in-new</v-icon></span>
          </v-tooltip>

          <v-tooltip v-if="modder?.discordUsername && !modder?.problematic" location="bottom">
            <template #activator="{ props: tip }">
              <span v-bind="tip" class="social-link" @click="copyDiscord" role="button">
                <img src="https://cdn.simpleicons.org/discord/5865F2" class="social-icon-img" alt="Discord" />
              </span>
            </template>
            <span class="tooltip-label">
              {{ discordCopied ? 'Copied!' : `@${modder.discordUsername}` }}
              <v-icon size="x-small">mdi-content-copy</v-icon>
            </span>
          </v-tooltip>

          <v-tooltip v-if="modder?.twitterUsername" location="bottom">
            <template #activator="{ props: tip }">
              <a v-bind="tip" :href="`https://x.com/${modder.twitterUsername}`" class="social-link" target="_blank" rel="noopener">
                <img src="https://cdn.simpleicons.org/x/ffffff" class="social-icon-img" alt="Twitter" />
              </a>
            </template>
            <span class="tooltip-label">@{{ modder.twitterUsername }} <v-icon size="x-small">mdi-open-in-new</v-icon></span>
          </v-tooltip>

          <v-tooltip v-if="modder?.blueskyHandle" location="bottom">
            <template #activator="{ props: tip }">
              <a v-bind="tip" :href="`https://bsky.app/profile/${modder.blueskyHandle}`" class="social-link" target="_blank" rel="noopener">
                <img src="https://cdn.simpleicons.org/bluesky/0085FF" class="social-icon-img" alt="Bluesky" />
              </a>
            </template>
            <span class="tooltip-label">{{ modder.blueskyHandle }} <v-icon size="x-small">mdi-open-in-new</v-icon></span>
          </v-tooltip>

          <v-tooltip v-if="modder?.githubUsername" location="bottom">
            <template #activator="{ props: tip }">
              <a v-bind="tip" :href="`https://github.com/${modder.githubUsername}`" class="social-link" target="_blank" rel="noopener">
                <img src="https://cdn.simpleicons.org/github/ffffff" class="social-icon-img" alt="GitHub" />
              </a>
            </template>
            <span class="tooltip-label">@{{ modder.githubUsername }} <v-icon size="x-small">mdi-open-in-new</v-icon></span>
          </v-tooltip>
        </div>
      </v-col>
      <v-col cols="10">
        <div class="title-container">
          <h1 class="page-title">{{ modder?.name }}</h1>
          <v-icon 
            v-if="modderIsAdmin"
            class="admin-display"
          >
            mdi-shield-account
          </v-icon>
        </div>
        <p v-if="modder?.problematic" class="problematic-warning">
          <v-icon>mdi-alert</v-icon>
          This user has been deemed problematic by the community. Please be careful when interacting with them and do your own research on their actions.
        </p>
        <p v-else class="bio">{{ modder?.bio }}</p>
      </v-col>
    </v-row>

    <v-row>
      <v-col cols="12">
        <h2 class="movesets-title">Movesets</h2>
        <MovesetList :movesets="movesets" />
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { onMounted, ref, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useHead } from '@unhead/vue'
import axios from 'axios'
import api from '@/services/api'
import MovesetList from '@/components/MovesetList.vue'
import { GB_MEMBER_URL } from '@/globals'

const apiUrl = import.meta.env.VITE_API_URL

const route = useRoute()
const modderId = route.params.id

const modder = ref(null)

useHead(computed(() => {
  const name = modder.value?.name
  const description = name
    ? `View ${name}'s movesets on Ultimate Moveset Compatibility.`
    : 'View information on Super Smash Bros. Ultimate custom movesets.'
  return {
    title: name ? `UMC | ${name}` : 'UMC',
    meta: [
      { name: 'description', content: description },
      { property: 'og:title', content: name ?? 'Ultimate Moveset Compatibility' },
      { property: 'og:description', content: description },
      { name: 'twitter:title', content: name ?? 'Ultimate Moveset Compatibility' },
      { name: 'twitter:description', content: description },
    ],
  }
}))
const movesets = ref([])
const modderPfpUrl = ref(null)
const modderIsAdmin = ref(false)
const discordCopied = ref(false)

const copyDiscord = async () => {
  try {
    await navigator.clipboard.writeText(modder.value.discordUsername)
  } catch {}
  discordCopied.value = true
  setTimeout(() => { discordCopied.value = false }, 2000)
}

onMounted(async () => {
  // Fetch modder info
  try {
    const { data } = await api.get(`/modders/${modderId}`)
    modder.value = data

    // Check if modder is admin
    const adminRes = await api.get(`/modders/is-admin?modderId=${modderId}`)
    modderIsAdmin.value = adminRes.data.isAdmin

    // Fetch movesets via the general endpoint (which already excludes hardheld movesets)
    // and filter client-side by modder name.
    const movesetRes = await api.get('movesets')
    movesets.value = movesetRes.data
      .filter(m => m.modders.includes(modder.value.name))
      .sort((a, b) => new Date(a.releaseDate) - new Date(b.releaseDate))

    if (modder.value.pfpUrl) {
      modderPfpUrl.value = modder.value.pfpUrl
    } else if (modder.value.gamebananaId) {
      try {
        const res = await axios.get(
          `https://api.gamebanana.com/Core/Item/Data?itemtype=Member&itemid=${modder.value.gamebananaId}&fields=Url().sHdAvatarUrl(),Url().sAvatarUrl()`
        )
        modderPfpUrl.value = res.data[0] || res.data[1] || null
      } catch (err) {
        console.warn('Could not fetch GameBanana avatar:', err)
        modderPfpUrl.value = null
      }
    }
  } catch (err) {
    if (err.response?.status === 404) {
      router.replace({
        name: 'ErrorPage',
        query: {
          httpCode: '404 Not Found',
          reason: 'This modder page is not available.',
        }
      })
    }
  }
})
</script>

<style scoped>
.title-container {
  position: absolute;
  width: fit-content;
  padding-right: 1em;
  z-index: 0;
}

.title-container::after {
  content: '';
  position: absolute;
  top: 0;
  left: -100px;
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
  display: inline-block;
  position: relative;
  z-index: 10;
  font-size: 4.5em;
  margin: -16px 0.25em -16px -0.25em;
  filter: drop-shadow(5px 4px 3px #000000c0)
}

i.admin-display {
  z-index: 10;
  font-size: 32px;
  margin-left: -0.5em;
  margin-top: -0.5em;
}

.social-links {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 2px;
  margin-top: 8px;
}

.social-link {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  opacity: 0.8;
  transition: opacity 0.15s;
  text-decoration: none;
  padding: 2px;
}

.social-link:hover {
  opacity: 1;
}

.social-icon-img {
  width: 22px;
  height: 22px;
}

.modder-section-1 {
  margin-top: 1.5em;
  padding-top: 0.5em;
}

.modder-pfp, .modder-pfp-null {
  z-index: 20;
  position: relative;
  width: 128px;
  height: 128px;
  background-color: black;
}

.modder-pfp-null {
  margin-left: 23px;
  margin-bottom: 6px;
  color: #333333;
}

.bio {
  margin-top: 4em;
  margin-left: -2em;
  font-size: larger;
}

.problematic-warning {
  margin-top: 4em;
  margin-left: -2em;
  font-size: larger;
  color: #b00020;
}

.movesets-title {
  font-size: 2.5em;
}

.tooltip-label {
  display: flex;
  align-items: center;
  gap: 4px;
}
</style>
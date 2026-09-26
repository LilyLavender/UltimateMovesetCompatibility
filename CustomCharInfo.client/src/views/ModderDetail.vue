<template>
  <PageShell :title="modder?.name ?? 'Modder'" :head="false">
    <div v-if="modder" class="modder">
      <div class="modder__side">
        <img v-if="modderPfpUrl" :src="modderPfpUrl" class="modder__pfp" alt="Profile picture" />
        <div v-else class="modder__pfp modder__pfp--empty">
          <v-icon size="72">mdi-account</v-icon>
        </div>

        <div class="modder__social">
          <v-tooltip v-if="modder.gamebananaId" location="bottom">
            <template #activator="{ props: tip }">
              <a
                v-bind="tip"
                :href="`${GB_MEMBER_URL}${modder.gamebananaId}`"
                class="social-link"
                target="_blank"
                rel="noopener"
              >
                <img
                  src="https://images.gamebanana.com/img/ico/games/banana.gif"
                  class="social-icon-img"
                  alt="GameBanana"
                />
              </a>
            </template>
            <span class="tooltip-label"
              >GameBanana <v-icon size="x-small">mdi-open-in-new</v-icon></span
            >
          </v-tooltip>

          <v-tooltip v-if="modder.discordUsername && !modder.problematic" location="bottom">
            <template #activator="{ props: tip }">
              <button v-bind="tip" type="button" class="social-link" @click="copyDiscord">
                <img
                  src="https://cdn.simpleicons.org/discord/5865F2"
                  class="social-icon-img"
                  alt="Discord"
                />
              </button>
            </template>
            <span class="tooltip-label">
              {{ discordCopied ? 'Copied!' : `@${modder.discordUsername}` }}
              <v-icon size="x-small">mdi-content-copy</v-icon>
            </span>
          </v-tooltip>

          <v-tooltip v-if="modder.twitterUsername" location="bottom">
            <template #activator="{ props: tip }">
              <a
                v-bind="tip"
                :href="`https://x.com/${modder.twitterUsername}`"
                class="social-link"
                target="_blank"
                rel="noopener"
              >
                <img
                  src="https://cdn.simpleicons.org/x/ffffff"
                  class="social-icon-img"
                  alt="Twitter"
                />
              </a>
            </template>
            <span class="tooltip-label"
              >@{{ modder.twitterUsername }} <v-icon size="x-small">mdi-open-in-new</v-icon></span
            >
          </v-tooltip>

          <v-tooltip v-if="modder.blueskyHandle" location="bottom">
            <template #activator="{ props: tip }">
              <a
                v-bind="tip"
                :href="`https://bsky.app/profile/${modder.blueskyHandle}`"
                class="social-link"
                target="_blank"
                rel="noopener"
              >
                <img
                  src="https://cdn.simpleicons.org/bluesky/0085FF"
                  class="social-icon-img"
                  alt="Bluesky"
                />
              </a>
            </template>
            <span class="tooltip-label"
              >{{ modder.blueskyHandle }} <v-icon size="x-small">mdi-open-in-new</v-icon></span
            >
          </v-tooltip>

          <v-tooltip v-if="modder.githubUsername" location="bottom">
            <template #activator="{ props: tip }">
              <a
                v-bind="tip"
                :href="`https://github.com/${modder.githubUsername}`"
                class="social-link"
                target="_blank"
                rel="noopener"
              >
                <img
                  src="https://cdn.simpleicons.org/github/ffffff"
                  class="social-icon-img"
                  alt="GitHub"
                />
              </a>
            </template>
            <span class="tooltip-label"
              >@{{ modder.githubUsername }} <v-icon size="x-small">mdi-open-in-new</v-icon></span
            >
          </v-tooltip>
        </div>
      </div>

      <div class="modder__body">
        <div class="modder__tags">
          <StatusTag v-if="modderIsAdmin" variant="neutral" icon="mdi-shield-account"
            >Admin</StatusTag
          >
          <HudReadout label="Movesets" :value="movesets.length" tone="info" />
        </div>
        <p v-if="modder.problematic" class="note note--err">
          <v-icon size="18">mdi-alert</v-icon>
          This user has been deemed problematic by the community. Please be careful when interacting
          with them and do your own research on their actions.
        </p>
        <p v-else-if="modder.bio" class="modder__bio">{{ modder.bio }}</p>
      </div>
    </div>

    <SectionHeading title="Movesets" :count="movesets.length" />
    <SkeletonList v-if="loading" :count="3" />
    <template v-else>
      <MovesetList v-if="movesets.length" :movesets="movesets" />
      <EmptyState v-else message="This modder doesn't have any movesets yet..." />
    </template>
  </PageShell>
</template>

<script setup>
import { onMounted, ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useHead } from '@unhead/vue'
import axios from 'axios'
import api from '@/services/api'
import PageShell from '@/components/PageShell.vue'
import SectionHeading from '@/components/SectionHeading.vue'
import StatusTag from '@/components/StatusTag.vue'
import HudReadout from '@/components/HudReadout.vue'
import SkeletonList from '@/components/SkeletonList.vue'
import EmptyState from '@/components/EmptyState.vue'
import MovesetList from '@/components/MovesetList.vue'
import { GB_MEMBER_URL } from '@/globals'
import { compareDateOnlyStrings } from '@/services/dateOnly'

const route = useRoute()
const router = useRouter()
const modderId = route.params.id

const modder = ref(null)

useHead(
  computed(() => {
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
  })
)
const movesets = ref([])
const modderPfpUrl = ref(null)
const modderIsAdmin = ref(false)
const discordCopied = ref(false)
const loading = ref(true)

const copyDiscord = async () => {
  try {
    await navigator.clipboard.writeText(modder.value.discordUsername)
  } catch {
    // Clipboard access can be denied; the copied indicator still shows so the user can copy by hand.
  }
  discordCopied.value = true
  setTimeout(() => {
    discordCopied.value = false
  }, 2000)
}

onMounted(async () => {
  // Fetch modder info
  try {
    const { data } = await api.get(`/modders/${modderId}`)
    modder.value = data

    // Check if modder is admin
    const adminRes = await api.get(`/modders/is-admin?modderId=${modderId}`)
    modderIsAdmin.value = adminRes.data.isAdmin

    // Ask for this modder's movesets only (the endpoint already excludes hardheld ones),
    // and filter so a modder hidden from the credits list stays hidden here.
    const movesetRes = await api.get('movesets', { params: { modderId } })
    movesets.value = movesetRes.data
      .filter((m) => m.modders.includes(modder.value.name))
      .sort((a, b) => compareDateOnlyStrings(a.releaseDate, b.releaseDate))
    loading.value = false

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
        },
      })
    }
  }
})
</script>

<style scoped>
.modder {
  display: grid;
  grid-template-columns: 128px 1fr;
  gap: 28px;
  align-items: start;
  margin-bottom: 8px;
}

.modder__side {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
}

.modder__pfp {
  width: 128px;
  height: 128px;
  object-fit: cover;
  border: 1px solid var(--line);
  background: var(--panel);
}

.modder__pfp--empty {
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--tx-3);
}

.modder__social {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 2px;
}

.social-link {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 4px;
  border: 0;
  background: none;
  cursor: pointer;
  opacity: 0.8;
  transition: opacity var(--dur-fast) var(--ease);
  text-decoration: none;
}

.social-link:hover {
  opacity: 1;
}

.social-icon-img {
  width: 22px;
  height: 22px;
}

.modder__body {
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-width: 0;
}

.modder__tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.modder__bio {
  margin: 0;
  font-size: 16px;
  white-space: pre-line;
}

.note {
  display: flex;
  gap: 8px;
  margin: 0;
  padding: 10px 14px;
  border: 1px solid var(--line-2);
  border-left: 4px solid var(--err);
  background: var(--panel);
  color: var(--tx-2);
  font-size: 14px;
}

.tooltip-label {
  display: flex;
  align-items: center;
  gap: 4px;
}

@media (max-width: 599px) {
  .modder {
    grid-template-columns: 1fr;
    justify-items: center;
    text-align: center;
  }

  .modder__tags {
    justify-content: center;
  }
}
</style>

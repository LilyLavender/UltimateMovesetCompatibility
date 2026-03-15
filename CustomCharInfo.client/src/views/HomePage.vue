<template>
  <ScrollingHero />

  <!-- Disabled message -->
  <div v-if="siteDisabled" class="text-center mb-5">
    <blockquote class="twitter-tweet" data-theme="dark" data-dnt="true" align="center"><p lang="en" dir="ltr">UMC is currently down due to high site traffic. Please be patient as I come up with a solution.<br><br>This will most likely involve upgrading the service plan UMC is being hosted with.<br><br>Any donation to my Ko-Fi would be appreciated to help cover server costs &lt;3</p>&mdash; Lily Lambda (@LilyLambda) <a href="https://twitter.com/LilyLambda/status/2033029378116841947?ref_src=twsrc%5Etfw">March 15, 2026</a></blockquote>
  </div>

  <!-- Main site -->
  <template v-else>
  <v-container max-width="1080px" class="p-6 mx-auto display-above-hero">

    <!-- Header -->
    <h1 class="mb-4 title-font page-title no-select text-center">Custom Movesets</h1>

    <!-- Sections -->
    <div>
      <h1>Recent Releases</h1>
      <MovesetList :movesets="recentReleases" />
    </div>

    <div>
      <h1>Upcoming Releases</h1>
      <MovesetList :movesets="upcomingReleases" />
    </div>

    <div v-if="latestBlogPost" class="mb-10">
      <h1>Latest from the Blog</h1>
      <v-container>
        <BlogPost :post="latestBlogPost" />
        <router-link
          to="/blog"
          class="unvisitable text-decoration-none router-link mini"
        >
          <i class="mdi mdi-arrow-right-bottom"></i>
          View Blog
        </router-link>
      </v-container>
    </div>

    <div v-if="showBetaSection">
      <h1>Currently in Beta</h1>
      <MovesetList :movesets="betaMovesets" />
    </div>

    <div>
      <h1>Featured</h1>
      <router-link
        to="/movesets"
        class="unvisitable text-decoration-none router-link"
      >
        <i class="mdi mdi-arrow-right-bottom"></i>
        View All Movesets
      </router-link>
      <MovesetList :movesets="adminPicks" />
    </div>

  </v-container>
  </template>
</template>

<script setup>
import { ref, onMounted, computed, nextTick } from 'vue'
import { getMovesets } from '@/services/movesetService'
import api from '@/services/api'

import ScrollingHero from '@/components/ScrollingHero.vue'
import MovesetList from '@/components/MovesetList.vue'
import BlogPost from '@/components/BlogPost.vue'

const allMovesets = ref([])
const latestBlogPost = ref(null)
const siteDisabled = ref(false)

const adminPicks = computed(() =>
  allMovesets.value.filter(m => m.adminPick)
)

const recentReleases = computed(() => {
  const now = Date.now()

  return adminPicks.value
    .filter(m => {
      if (!m.releaseDate) return false
      const t = Date.parse(m.releaseDate)
      return !Number.isNaN(t) && t <= now
    })
    .sort((a, b) => Date.parse(b.releaseDate) - Date.parse(a.releaseDate))
    .slice(0, 6)
})

const upcomingReleases = computed(() => {
  const withDate = adminPicks.value
    .filter(m => m.releaseDate && new Date(m.releaseDate) > new Date())
    .sort((a, b) => new Date(a.releaseDate) - new Date(b.releaseDate))
  
  const noDate = adminPicks.value
    .filter(m => !m.releaseDate && !m.privateMoveset)

  return [...withDate, ...noDate].slice(0, 6)
})

const betaMovesets = computed(() =>
  adminPicks.value.filter(m => m.releaseStateId === 4)
)

const showBetaSection = computed(() =>
  betaMovesets.value.length >= 3
)

const fetchLatestBlogPost = async () => {
  try {
    const res = await api.get('/blog')
    latestBlogPost.value = res.data
      .filter(p => p.postedDate)
      .sort((a, b) => Date.parse(b.postedDate) - Date.parse(a.postedDate))[0]
  } catch {
    latestBlogPost.value = null
  }
}

function loadTwitterScript() {
  if (!document.getElementById("twitter-wjs")) {
    const script = document.createElement("script")
    script.id = "twitter-wjs"
    script.src = "https://platform.twitter.com/widgets.js"
    script.async = true
    document.body.appendChild(script)
  } else if (window.twttr) {
    window.twttr.widgets.load()
  }
}

onMounted(async () => {
  try {
    const res = await getMovesets()
    allMovesets.value = res.data
    await fetchLatestBlogPost()
  } catch (err) {
    siteDisabled.value = true
    await nextTick()
    loadTwitterScript()
  }
})
</script>

<style scoped>

</style>
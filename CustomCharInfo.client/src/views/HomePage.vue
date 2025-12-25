<template>
  <ScrollingHero />
  <div class="p-6 mx-auto display-above-hero">

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
        class="unvisitable text-decoration-none router-link"
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
      <MovesetList :movesets="adminPicks" />
    </div>

  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { getMovesets } from '@/services/movesetService'
import api from '@/services/api'

import ScrollingHero from '@/components/ScrollingHero.vue'
import MovesetList from '@/components/MovesetList.vue'
import BlogPost from '@/components/BlogPost.vue'

const allMovesets = ref([])
const latestBlogPost = ref(null)

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

onMounted(async () => {
  const res = await getMovesets()
  allMovesets.value = res.data

  await fetchLatestBlogPost()
})
</script>

<style scoped>
.router-link {
  margin-top: -1em;
  margin-left: 1em;
  display: block;
}
</style>
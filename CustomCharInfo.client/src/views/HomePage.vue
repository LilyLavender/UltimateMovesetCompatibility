<template>
  <ScrollingHero />

  <!-- Disabled message -->
  <div v-if="siteDisabled" class="tier tier-standard text-center mb-5">
    <blockquote class="twitter-tweet" data-theme="dark" data-dnt="true" align="center">
      <p lang="en" dir="ltr">
        UMC is currently down due to high site traffic. Please be patient as I come up with a
        solution.<br /><br />This will most likely involve upgrading the service plan UMC is being
        hosted with.<br /><br />Any donation to my Ko-Fi would be appreciated to help cover server
        costs &lt;3
      </p>
      &mdash; Lily Lambda (@LilyLambda)
      <a href="https://twitter.com/LilyLambda/status/2033029378116841947?ref_src=twsrc%5Etfw"
        >March 15, 2026</a
      >
    </blockquote>
  </div>

  <!-- Main site -->
  <PageShell v-else title="Custom Movesets" size="lg" over-hero :head="false">
    <SectionHeading title="Recent releases" :to="{ name: 'Movesets' }" link-label="All movesets" />
    <SkeletonList v-if="loading" />
    <MovesetList v-else :movesets="recentReleases" />

    <SectionHeading title="Upcoming releases" />
    <SkeletonList v-if="loading" />
    <MovesetList v-else :movesets="upcomingReleases" />

    <template v-if="latestBlogPost">
      <SectionHeading title="Latest from the blog" :to="{ name: 'Blog' }" link-label="View blog" />
      <div class="panel">
        <BlogPost :post="latestBlogPost" />
      </div>
    </template>

    <template v-if="showBetaSection">
      <SectionHeading title="Currently in beta" />
      <MovesetList :movesets="betaMovesets" />
    </template>

    <SectionHeading title="Featured" :to="{ name: 'Movesets' }" link-label="All movesets" />
    <SkeletonList v-if="loading" />
    <MovesetList v-else :movesets="adminPicks" />
  </PageShell>
</template>

<script setup>
import { ref, onMounted, computed, nextTick } from 'vue'
import { useHead } from '@unhead/vue'
import api from '@/services/api'
import { ReleaseState } from '@/globals'
import { localDateToDateOnlyString, compareDateOnlyStrings } from '@/services/dateOnly'

import ScrollingHero from '@/components/ScrollingHero.vue'
import PageShell from '@/components/PageShell.vue'
import SectionHeading from '@/components/SectionHeading.vue'
import SkeletonList from '@/components/SkeletonList.vue'
import MovesetList from '@/components/MovesetList.vue'
import BlogPost from '@/components/BlogPost.vue'

useHead({
  title: 'Ultimate Moveset Compatibility',
  meta: [
    {
      name: 'description',
      content: 'View information on Super Smash Bros. Ultimate custom movesets.',
    },
    { property: 'og:title', content: 'Ultimate Moveset Compatibility' },
    {
      property: 'og:description',
      content: 'View information on Super Smash Bros. Ultimate custom movesets.',
    },
    { name: 'twitter:title', content: 'Ultimate Moveset Compatibility' },
    {
      name: 'twitter:description',
      content: 'View information on Super Smash Bros. Ultimate custom movesets.',
    },
  ],
})

// Every home section is drawn from the admin picks, so only those are requested.
const adminPicks = ref([])
const latestBlogPost = ref(null)
const siteDisabled = ref(false)
const loading = ref(true)

const recentReleases = computed(() => {
  const todayStr = localDateToDateOnlyString(new Date())

  return adminPicks.value
    .filter((m) => m.releaseDate && compareDateOnlyStrings(m.releaseDate, todayStr) <= 0)
    .sort((a, b) => compareDateOnlyStrings(b.releaseDate, a.releaseDate))
    .slice(0, 6)
})

const upcomingReleases = computed(() => {
  const todayStr = localDateToDateOnlyString(new Date())

  const withDate = adminPicks.value
    .filter((m) => m.releaseDate && compareDateOnlyStrings(m.releaseDate, todayStr) > 0)
    .sort(
      (a, b) =>
        compareDateOnlyStrings(a.releaseDate, b.releaseDate) ||
        (b.likeCount ?? 0) - (a.likeCount ?? 0) ||
        a.moddedCharName.localeCompare(b.moddedCharName)
    )

  const noDate = adminPicks.value
    .filter((m) => !m.releaseDate && !m.privateMoveset)
    .sort(
      (a, b) =>
        (b.likeCount ?? 0) - (a.likeCount ?? 0) || a.moddedCharName.localeCompare(b.moddedCharName)
    )

  return [...withDate, ...noDate].slice(0, 6)
})

const betaMovesets = computed(() =>
  adminPicks.value.filter((m) => m.releaseStateId === ReleaseState.OpenBeta)
)

const showBetaSection = computed(() => betaMovesets.value.length >= 3)

const fetchLatestBlogPost = async () => {
  try {
    const res = await api.get('/blog')
    latestBlogPost.value = res.data
      .filter((p) => p.postedDate)
      .sort((a, b) => Date.parse(b.postedDate) - Date.parse(a.postedDate))[0]
  } catch {
    latestBlogPost.value = null
  }
}

function loadTwitterScript() {
  if (!document.getElementById('twitter-wjs')) {
    const script = document.createElement('script')
    script.id = 'twitter-wjs'
    script.src = 'https://platform.twitter.com/widgets.js'
    script.async = true
    document.body.appendChild(script)
  } else if (window.twttr) {
    window.twttr.widgets.load()
  }
}

onMounted(async () => {
  try {
    const res = await api.get('/movesets', { params: { adminPickOnly: true } })
    adminPicks.value = res.data
    loading.value = false
    await fetchLatestBlogPost()
  } catch {
    siteDisabled.value = true
    await nextTick()
    loadTwitterScript()
  }
})
</script>

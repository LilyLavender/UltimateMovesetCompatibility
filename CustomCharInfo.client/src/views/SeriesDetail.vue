<template>
  <PageShell :title="series?.seriesName ?? 'Series'" :head="false" keep-case>
    <div v-if="series" class="series-meta">
      <img
        v-if="series.seriesIconUrl"
        :src="getFullImageUrl(series.seriesIconUrl)"
        :alt="`${series.seriesName} icon`"
        class="series-meta__icon"
      />
      <span class="series-meta__count">
        {{ series.movesetCount }} {{ series.movesetCount === 1 ? 'moveset' : 'movesets' }}
      </span>
      <AppButton
        v-if="canEdit"
        :to="{ name: 'EditSeries', params: { seriesId: series.seriesId } }"
        variant="ghost"
        size="sm"
        icon="mdi-pencil"
      >
        Edit series
      </AppButton>
    </div>

    <SkeletonList v-if="loading" />
    <template v-else-if="series">
      <MovesetList v-if="movesets.length" class="reveal" :movesets="movesets" />
      <EmptyState v-else message="No movesets in this series yet." />
    </template>
  </PageShell>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useHead } from '@unhead/vue'
import api from '@/services/api'
import PageShell from '@/components/PageShell.vue'
import AppButton from '@/components/AppButton.vue'
import SkeletonList from '@/components/SkeletonList.vue'
import EmptyState from '@/components/EmptyState.vue'
import MovesetList from '@/components/MovesetList.vue'
import { isAdmin } from '@/navigation'

const route = useRoute()
const router = useRouter()
const seriesId = parseInt(route.params.seriesId)

const series = ref(null)
const movesets = ref([])
const canEdit = ref(false)
const loading = ref(true)
const apiUrl = import.meta.env.VITE_API_URL

const getFullImageUrl = (path) => (path?.startsWith('/') ? `${apiUrl}${path}` : path)

useHead(
  computed(() => {
    const name = series.value?.seriesName
    const description = name
      ? `Browse ${name} movesets on Ultimate Moveset Compatibility.`
      : 'View information on Super Smash Bros. Ultimate custom movesets.'
    const image = series.value?.seriesIconUrl ? getFullImageUrl(series.value.seriesIconUrl) : null
    return {
      title: name ? `UMC | ${name}` : 'UMC',
      meta: [
        { name: 'description', content: description },
        { property: 'og:title', content: name ?? 'Ultimate Moveset Compatibility' },
        { property: 'og:description', content: description },
        ...(image
          ? [
              { property: 'og:image', content: image },
              { name: 'twitter:card', content: 'summary' },
              { name: 'twitter:image', content: image },
            ]
          : []),
        { name: 'twitter:title', content: name ?? 'Ultimate Moveset Compatibility' },
        { name: 'twitter:description', content: description },
      ],
    }
  })
)

onMounted(async () => {
  try {
    const [seriesRes, movesetsRes] = await Promise.all([
      api.get(`/series/${seriesId}`),
      api.get('/movesets', { params: { seriesId, sort: 'alpha' } }),
    ])

    series.value = seriesRes.data
    movesets.value = movesetsRes.data

    // Check if current user can edit this series
    try {
      const userRes = await api.get('/auth/me')
      const user = userRes.data
      canEdit.value =
        seriesRes.data.canEdit ||
        isAdmin(user) ||
        movesetsRes.data.some((m) => m.modders.includes(user.userName))
    } catch {
      canEdit.value = false
    }
  } catch (err) {
    if (err.response?.status === 404 || err.response?.status === 403) {
      router.replace({
        name: 'ErrorPage',
        query: { httpCode: '404 Not Found', reason: 'This series is not available.' },
      })
    }
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.series-meta {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 24px;
}

.series-meta__icon {
  width: 56px;
  height: 56px;
  object-fit: contain;
  filter: brightness(4.35);
}

.series-meta__count {
  color: var(--tx-2);
}
</style>

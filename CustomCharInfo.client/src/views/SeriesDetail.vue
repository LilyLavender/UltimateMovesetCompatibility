<template>
  <v-container v-if="series">
    <!-- Header -->
    <v-row class="series-header">
      <v-col cols="12" class="d-flex align-center gap">
        <v-img
          v-if="series.seriesIconUrl"
          :src="getFullImageUrl(series.seriesIconUrl)"
          :alt="`${series.seriesName} icon`"
          width="64"
          max-width="64"
          height="64"
          max-height="64"
          class="series-icon"
        />
        <div>
          <h1 class="series-name">{{ series.seriesName }}</h1>
          <p class="series-count">
            {{ series.movesetCount }} {{ series.movesetCount === 1 ? 'moveset' : 'movesets' }}
          </p>
        </div>
        <router-link
          v-if="canEdit"
          :to="{ name: 'EditSeries', params: { seriesId: series.seriesId } }"
          class="unvisitable ml-4"
        >
          <v-icon>mdi-pencil</v-icon>
        </router-link>
      </v-col>
    </v-row>

    <!-- Movesets -->
    <v-row>
      <v-col cols="12">
        <MovesetList :movesets="movesets" />
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '@/services/api'
import MovesetList from '@/components/MovesetList.vue'

const route = useRoute()
const router = useRouter()
const seriesId = parseInt(route.params.seriesId)

const series = ref(null)
const movesets = ref([])
const canEdit = ref(false)
const apiUrl = import.meta.env.VITE_API_URL

const getFullImageUrl = (path) =>
  path?.startsWith('/') ? `${apiUrl}${path}` : path

onMounted(async () => {
  try {
    const [seriesRes, movesetsRes] = await Promise.all([
      api.get(`/series/${seriesId}`),
      api.get('/movesets', { params: { seriesId, sort: 'alpha' } }),
    ])

    series.value = seriesRes.data
    movesets.value = movesetsRes.data
    document.title = `UMC | ${series.value.seriesName}`

    // Check if current user can edit this series
    try {
      const userRes = await api.get('/auth/me')
      const user = userRes.data
      canEdit.value = seriesRes.data.canEdit ||
        user.userTypeId === 3 ||
        movesetsRes.data.some(m => m.modders.includes(user.userName))
    } catch {
      canEdit.value = false
    }
  } catch (err) {
    if (err.response?.status === 404 || err.response?.status === 403) {
      router.replace({
        name: 'ErrorPage',
        query: { httpCode: '404 Not Found', reason: 'This series is not available.' }
      })
    }
  }
})
</script>

<style scoped>
.series-header {
  margin-top: 1.5em;
  margin-bottom: 1em;
}
.gap {
  gap: 1.25rem;
}
.series-icon {
  filter: brightness(4.35);
  flex-shrink: 0;
}
.series-name {
  font-size: 3em;
  line-height: 1.1;
}
.series-count {
  color: #888;
  margin-top: 0.1em;
}
</style>

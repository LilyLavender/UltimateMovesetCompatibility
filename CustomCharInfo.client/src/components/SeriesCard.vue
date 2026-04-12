<template>
  <v-card class="series-card d-flex align-center justify-space-between">
    <router-link
      :to="{ name: 'SeriesDetail', params: { seriesId: series.seriesId } }"
      class="series-link d-flex align-center flex-grow-1 unvisitable text-decoration-none"
    >
      <v-img
        :src="getFullImageUrl(series.seriesIconUrl)"
        :alt="`${series.seriesName} Series Icon`"
        height="64" max-height="64"
        width="64" max-width="64"
      />
      <div>
        <v-card-title class="p-0">{{ series.seriesName }}</v-card-title>
        <v-card-subtitle class="p-0 mt--1">
          {{ series.movesetCount }} {{ series.movesetCount === 1 ? 'moveset' : 'movesets' }}
        </v-card-subtitle>
      </div>
    </router-link>

    <router-link
      v-if="series.canEdit"
      :to="{ name: 'EditSeries', params: { seriesId: series.seriesId } }"
      class="unvisitable ml-2"
    >
      <v-icon>mdi-pencil</v-icon>
    </router-link>
  </v-card>
</template>

<script setup>
const props = defineProps({
  series: {
    type: Object,
    required: true,
  },
  apiUrl: {
    type: String,
    required: true,
  },
});

const getFullImageUrl = (path) =>
  path?.startsWith("/") ? `${props.apiUrl}${path}` : path;
</script>

<style scoped>
.series-card {
  background-color: transparent;
  color: #dedede;
}
.series-link {
  color: inherit;
  min-width: 0;
}

.p-0 {
  padding: 0;
}

.mt--1 {
  margin-top: -0.5em;
}
</style>

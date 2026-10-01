<template>
  <div class="series-card">
    <router-link
      :to="{ name: 'SeriesDetail', params: { seriesId: series.seriesId } }"
      class="series-card__link"
    >
      <img
        :src="getFullImageUrl(series.seriesIconUrl)"
        :alt="`${series.seriesName} series icon`"
        class="series-card__icon"
        :class="{ 'series-card__icon--raw': rawIcon }"
      />
      <span class="series-card__text">
        <span class="series-card__name">{{ series.seriesName }}</span>
        <slot name="subtitle">
          <span class="series-card__count">
            {{ series.movesetCount }} {{ series.movesetCount === 1 ? 'moveset' : 'movesets' }}
          </span>
        </slot>
      </span>
    </router-link>

    <router-link
      v-if="series.canEdit"
      :to="{ name: 'EditSeries', params: { seriesId: series.seriesId } }"
      class="series-card__edit"
      aria-label="Edit series"
    >
      <v-icon size="18">mdi-pencil</v-icon>
    </router-link>
  </div>
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
  // Show the uploaded file as is, for checking a submission's color
  rawIcon: {
    type: Boolean,
    default: false,
  },
})

const getFullImageUrl = (path) => (path?.startsWith('/') ? `${props.apiUrl}${path}` : path)
</script>

<style scoped>
.series-card {
  display: flex;
  align-items: center;
  border: 1px solid var(--line);
  background: var(--panel);
  transition:
    border-color var(--dur-fast) var(--ease),
    background-color var(--dur-fast) var(--ease);
}

.series-card:hover {
  border-color: var(--white);
  background: var(--panel-2);
}

.series-card__link {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 1;
  min-width: 0;
  padding: 8px 10px;
  color: var(--tx);
  text-decoration: none;
}

/* Series icons are drawn dark grey on transparent; the filter lifts them to white. */
.series-card__icon {
  width: 56px;
  height: 56px;
  flex: none;
  object-fit: contain;
  filter: brightness(4.35);
}

.series-card__icon--raw {
  filter: none;
}

.series-card__text {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.series-card__name {
  font-weight: 600;
  font-size: 15px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.series-card__count {
  color: var(--tx-3);
  font-size: 12.5px;
}

.series-card__edit {
  display: flex;
  align-items: center;
  align-self: stretch;
  padding: 0 10px;
  border-left: 1px solid var(--line);
  color: var(--tx-2);
}

.series-card__edit:hover {
  background: var(--white);
  color: #000;
}
</style>

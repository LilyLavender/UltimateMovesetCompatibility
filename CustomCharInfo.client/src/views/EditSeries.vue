<template>
  <SeriesForm mode="edit" :seriesId="parseInt(route.params.seriesId)" />
</template>

<script setup>
import SeriesForm from '@/components/SeriesForm.vue'
import { useRoute } from 'vue-router'

const route = useRoute()
</script>

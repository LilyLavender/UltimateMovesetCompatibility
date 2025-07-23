<template>
  <SeriesForm mode="edit" :movesetId="parseInt(route.params.movesetId)" />
</template>

<script setup>
import SeriesForm from '@/components/SeriesForm.vue'
import { useRoute } from 'vue-router'

const route = useRoute()
</script>

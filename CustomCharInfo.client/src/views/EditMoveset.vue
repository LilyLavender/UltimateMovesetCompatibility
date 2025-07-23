<template>
  <MovesetForm mode="edit" :movesetId="parseInt(route.params.movesetId)" />
</template>

<script setup>
import MovesetForm from '@/components/MovesetForm.vue'
import { useRoute } from 'vue-router'

const route = useRoute()
</script>

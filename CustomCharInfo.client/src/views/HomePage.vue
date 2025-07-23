<template>
  <ScrollingHero />
  <div class="p-6 mx-auto display-above-hero">
    <h1 class="mb-4 title-font page-title no-select">Modded Movesets</h1>
    <MovesetList />
  </div>
</template>

<script setup>
import ScrollingHero from '@/components/ScrollingHero.vue'
import MovesetList from '@/components/MovesetList.vue'
import ActionLogList from '@/components/ActionLogList.vue'
</script>

<style scoped>

</style>
<template>
  <PageShell title="Series">
    <template #subnav>
      <SubNav section="series" label="Series" />
    </template>
    <SeriesList />
  </PageShell>
</template>

<script setup>
import PageShell from '@/components/PageShell.vue'
import SubNav from '@/components/SubNav.vue'
import SeriesList from '@/components/SeriesList.vue'
</script>

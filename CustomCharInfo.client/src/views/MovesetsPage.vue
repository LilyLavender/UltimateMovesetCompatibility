<template>
  <PageShell title="All movesets">
    <template #subnav>
      <SubNav section="movesets" label="Movesets" />
    </template>
    <MovesetList show-controls />
  </PageShell>
</template>

<script setup>
import PageShell from '@/components/PageShell.vue'
import SubNav from '@/components/SubNav.vue'
import MovesetList from '@/components/MovesetList.vue'
</script>

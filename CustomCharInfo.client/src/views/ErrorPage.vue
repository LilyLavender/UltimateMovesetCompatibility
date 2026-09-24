<template>
  <PageShell :title="httpCode" tier="narrow" :lede="reason" :head="false">
    <p v-if="extra" class="error-extra">{{ extra }}</p>
    <div class="error-actions">
      <AppButton :to="{ name: 'Home' }" icon="mdi-home">Back to home</AppButton>
      <AppButton :to="{ name: 'Movesets' }" variant="ghost" icon="mdi-view-list">
        All movesets
      </AppButton>
    </div>
  </PageShell>
</template>

<script setup>
import { useHead } from '@unhead/vue'
import PageShell from '@/components/PageShell.vue'
import AppButton from '@/components/AppButton.vue'

const props = defineProps({
  httpCode: {
    type: String,
    default: '404 Error',
  },
  reason: {
    type: String,
    default: 'Page not found!',
  },
  extra: {
    type: String,
    default: '',
  },
})

useHead({ title: `UMC | ${props.httpCode}` })
</script>

<style scoped>
.error-extra {
  margin: 0 0 20px;
  color: var(--tx-2);
}

.error-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}
</style>

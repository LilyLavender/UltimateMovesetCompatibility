<template>
  <PageShell title="My likes">
    <template #subnav>
      <SubNav section="account" label="Account" />
    </template>

    <SkeletonList v-if="loading" />
    <template v-else>
      <EmptyState
        v-if="movesets.length === 0"
        message="You haven't liked any movesets yet."
        icon="mdi-heart-outline"
      >
        <template #action>
          <AppButton :to="{ name: 'Movesets' }" size="sm" icon="mdi-view-list">
            Browse movesets
          </AppButton>
        </template>
      </EmptyState>
      <MovesetList v-else class="reveal" :movesets="movesets" />
    </template>
  </PageShell>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'
import PageShell from '@/components/PageShell.vue'
import SubNav from '@/components/SubNav.vue'
import SkeletonList from '@/components/SkeletonList.vue'
import EmptyState from '@/components/EmptyState.vue'
import AppButton from '@/components/AppButton.vue'
import MovesetList from '@/components/MovesetList.vue'

const loading = ref(true)
const movesets = ref([])

onMounted(async () => {
  try {
    const user = (await api.get('/auth/me')).data
    const movesetsRes = await api.get('/movesets', {
      params: { likedByUserId: user.id, sort: 'alpha' },
    })
    movesets.value = movesetsRes.data
  } catch (err) {
    console.error('Failed to load liked movesets:', err)
  } finally {
    loading.value = false
  }
})
</script>

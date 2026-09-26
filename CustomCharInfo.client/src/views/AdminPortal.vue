<template>
  <PageShell title="Admin portal">
    <template v-for="group in adminTiles" :key="group.group">
      <SectionHeading :title="group.group" />
      <div class="tiles">
        <ToolTile
          v-for="tile in group.items"
          :key="tile.label"
          :to="tile.to"
          :icon="tile.icon"
          :label="tile.label"
          :description="tile.description"
          :badge="tile.badge === 'pendingAdmin' && pendingAdminCount ? pendingAdminCount : ''"
        />
      </div>
    </template>

    <SectionHeading
      title="Notifications"
      :to="{ name: 'AdminAccepter' }"
      link-label="Action log manager"
    />
    <section class="panel">
      <ActionLogList view-all />
    </section>
  </PageShell>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '@/services/api'
import { ItemType, PENDING_ADMIN_STATES } from '@/globals'
import { adminTiles } from '@/navigation'
import PageShell from '@/components/PageShell.vue'
import SectionHeading from '@/components/SectionHeading.vue'
import ToolTile from '@/components/ToolTile.vue'
import ActionLogList from '@/components/ActionLogList.vue'

const pendingAdminCount = ref(0)

// The badge on the action log manager tile: items whose newest log is waiting on an admin.
onMounted(async () => {
  try {
    const res = await api.get('/logs/latest', {
      params: {
        acceptanceStates: PENDING_ADMIN_STATES,
        itemTypes: Object.values(ItemType),
        viewAll: true,
      },
    })
    pendingAdminCount.value = res.data.length
  } catch {
    pendingAdminCount.value = 0
  }
})
</script>

<style scoped>
.tiles {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
}

@media (max-width: 959px) {
  .tiles {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 599px) {
  .tiles {
    grid-template-columns: 1fr;
  }
}
</style>

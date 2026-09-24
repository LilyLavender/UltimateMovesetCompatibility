<template>
  <PageShell
    title="Delete movesets"
    :back-to="{ name: 'AdminPortal' }"
    back-label="Admin portal"
    lede="Permanently deletes a moveset and everything tied to it. Action log history is kept for audit purposes."
  >
    <div class="toolbar">
      <LabeledField label="Search" class="toolbar__search">
        <v-text-field
          v-model="search"
          placeholder="Character name"
          density="compact"
          hide-details
          clearable
          prepend-inner-icon="mdi-magnify"
        />
      </LabeledField>
      <p v-if="loaded" class="toolbar__count">
        {{ filtered.length }} {{ pluralize(filtered.length, 'moveset') }}
      </p>
    </div>

    <SkeletonLog v-if="!loaded" :rows="6" />
    <ul v-else-if="filtered.length" class="rows">
      <li v-for="item in filtered" :key="item.movesetId" class="row">
        <div class="row__info">
          <div class="row__name">{{ item.moddedCharName }}</div>
          <div class="row__meta">
            {{ item.modders.join(', ') || 'No modders listed' }}
            <StatusTag variant="outline">{{ item.releaseState }}</StatusTag>
          </div>
        </div>
        <AppButton
          variant="danger"
          size="sm"
          icon="mdi-delete"
          :busy="deletingId === item.movesetId"
          @click="deleteMoveset(item)"
        >
          Delete
        </AppButton>
      </li>
    </ul>
    <EmptyState v-else message="No movesets match." icon="mdi-magnify" />
  </PageShell>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/services/api'
import { useNotify } from '@/composables/useNotify'
import PageShell from '@/components/PageShell.vue'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'
import StatusTag from '@/components/StatusTag.vue'
import SkeletonLog from '@/components/SkeletonLog.vue'
import EmptyState from '@/components/EmptyState.vue'

const notify = useNotify()

const movesets = ref([])
const loaded = ref(false)
const search = ref('')
const deletingId = ref(null)

const pluralize = (count, singular, plural = `${singular}s`) => (count === 1 ? singular : plural)

const filtered = computed(() => {
  const term = search.value?.trim().toLowerCase()
  if (!term) return movesets.value
  return movesets.value.filter((m) => m.moddedCharName.toLowerCase().includes(term))
})

const loadMovesets = async () => {
  try {
    const res = await api.get('/movesets', { params: { includeHidden: true } })
    movesets.value = res.data
    loaded.value = true
  } catch (err) {
    console.error('Failed to load movesets:', err)
    notify.error('Failed to load movesets.')
  }
}

const deleteMoveset = async (item) => {
  if (
    !confirm(
      `Permanently delete "${item.moddedCharName}"? This will also delete its likes, compatibility reports, and modder/hook/article/dependency listings. This cannot be undone.`
    )
  )
    return

  deletingId.value = item.movesetId
  try {
    await api.delete(`/movesets/${item.movesetId}`)
    movesets.value = movesets.value.filter((m) => m.movesetId !== item.movesetId)
  } catch (err) {
    console.error('Failed to delete moveset:', err)
    notify.error('Failed to delete moveset.')
  } finally {
    deletingId.value = null
  }
}

onMounted(loadMovesets)
</script>

<style scoped>
.toolbar {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.toolbar__search {
  flex: 1;
  max-width: 420px;
}

.toolbar__count {
  margin: 0 0 10px;
  color: var(--tx-2);
  font-size: 13px;
}

.rows {
  margin: 0;
  padding: 0;
  list-style: none;
  border: 1px solid var(--line);
  background: var(--panel);
}

.row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding: 10px 14px;
  border-bottom: 1px solid var(--line);
}

.row:last-child {
  border-bottom: 0;
}

.row__name {
  font-weight: 600;
}

.row__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  margin-top: 3px;
  font-size: 13px;
  color: var(--tx-3);
}
</style>

<template>
  <v-container max-width="1020px">
    <h1 class="mb-5 page-title no-select">Moveset Delete Manager</h1>

    <p class="mb-4 helper-text">
      Permanently deletes a moveset and everything tied to it. Action log history is kept for audit purposes.
    </p>

    <v-text-field
      v-model="search"
      label="Search by character name"
      density="comfortable"
      class="mb-4"
      clearable
    />

    <p v-if="loaded" class="mb-4 summary-text">
      {{ filtered.length }} {{ pluralize(filtered.length, 'moveset') }}.
    </p>

    <div class="rows">
      <div v-for="item in filtered" :key="item.movesetId" class="row">
        <div class="row-info">
          <div class="row-name">{{ item.moddedCharName }}</div>
          <div class="row-meta">
            {{ item.modders.join(', ') || 'No modders listed' }} · {{ item.releaseState }}
          </div>
        </div>
        <v-btn
          class="delete-btn"
          @click="deleteMoveset(item)"
          :loading="deletingId === item.movesetId"
        >
          <v-icon class="mr-1">mdi-delete</v-icon>
          Delete
        </v-btn>
      </div>
      <p v-if="loaded && filtered.length === 0" class="helper-text">No movesets match.</p>
    </div>
  </v-container>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/services/api'

const movesets = ref([])
const loaded = ref(false)
const search = ref('')
const deletingId = ref(null)

const pluralize = (count, singular, plural = `${singular}s`) => (count === 1 ? singular : plural)

const filtered = computed(() => {
  const term = search.value?.trim().toLowerCase()
  if (!term) return movesets.value
  return movesets.value.filter(m => m.moddedCharName.toLowerCase().includes(term))
})

const loadMovesets = async () => {
  try {
    const res = await api.get('/movesets')
    movesets.value = res.data
    loaded.value = true
  } catch (err) {
    console.error('Failed to load movesets:', err)
    alert('Failed to load movesets.')
  }
}

const deleteMoveset = async (item) => {
  if (!confirm(`Permanently delete "${item.moddedCharName}"? This will also delete its likes, compatibility reports, and modder/hook/article/dependency listings. This cannot be undone.`)) return

  deletingId.value = item.movesetId
  try {
    await api.delete(`/movesets/${item.movesetId}`)
    movesets.value = movesets.value.filter(m => m.movesetId !== item.movesetId)
  } catch (err) {
    console.error('Failed to delete moveset:', err)
    alert('Failed to delete moveset.')
  } finally {
    deletingId.value = null
  }
}

onMounted(loadMovesets)
</script>

<style scoped>
.page-title {
  font-size: 2.5rem;
}
.helper-text {
  color: #b0b0b0;
}
.summary-text {
  color: #d0d0d0;
  font-size: 1.05em;
}
.delete-btn {
  text-transform: unset;
  background-color: #7a1f1f !important;
  color: #ffffff !important;
}

.rows {
  display: flex;
  flex-direction: column;
  gap: 0.5em;
}

.row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1em;
  background-color: #1e1e1e;
  border-radius: 8px;
  padding: 0.75em 1em;
}

.row-name {
  font-weight: 600;
  color: #e2e2e2;
}
.row-meta {
  font-size: 0.85em;
  color: #8a8a8a;
  margin-top: 0.15em;
}
</style>

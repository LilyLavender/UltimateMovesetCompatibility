<template>
  <v-container>
    <h1 class="mb-4 page-title">Modders</h1>

    <div class="modders-grid">
      <router-link
        v-for="modder in modders"
        :key="modder.modderId"
        :to="{ name: 'ModderDetail', params: { id: modder.modderId } }"
        class="modder-card unvisitable text-decoration-none"
      >
        <div class="modder-pfp-wrap">
          <img
            v-if="avatars[modder.modderId]"
            :src="avatars[modder.modderId]"
            class="modder-pfp"
            alt=""
          />
          <v-icon v-else size="36" class="modder-pfp-placeholder">mdi-account</v-icon>
        </div>
        <div class="modder-info">
          <span class="modder-name">{{ modder.name }}</span>
          <span v-if="modder.bio" class="modder-bio">{{ modder.bio }}</span>
          <!-- <span class="modder-count">
            {{ modder.movesetCount }} {{ modder.movesetCount === 1 ? 'moveset' : 'movesets' }}
          </span> -->
        </div>
      </router-link>
    </div>
  </v-container>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import axios from 'axios'
import api from '@/services/api'

const modders = ref([])
const avatars = ref({})

onMounted(async () => {
  const res = await api.get('/modders/public')
  modders.value = res.data
  document.title = 'UMC | Modders'

  const fetches = modders.value
    .filter(m => m.gamebananaId)
    .map(async (m) => {
      try {
        const r = await axios.get(
          `https://api.gamebanana.com/Core/Item/Data?itemtype=Member&itemid=${m.gamebananaId}&fields=Url().sHdAvatarUrl(),Url().sAvatarUrl()`
        )
        const url = r.data[0] || r.data[1] || null
        if (url) avatars.value[m.modderId] = url
      } catch {
        // silently ignore failed avatar fetches
      }
    })
  await Promise.all(fetches)
})
</script>

<style scoped>
.page-title {
  font-size: 4em;
}
.modders-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 6px;
}
.modder-card {
  display: flex;
  flex-direction: row;
  align-items: center;
  gap: 0.6em;
  background-color: #1e1e1e;
  border-radius: 8px;
  padding: 0.5em 0.75em;
  color: #dedede;
  transition: background-color 150ms ease-in-out;
  min-width: 0;
}
.modder-card:hover {
  background-color: #2a2a2a;
}
.modder-pfp-wrap {
  flex-shrink: 0;
  width: 40px;
  height: 40px;
  border-radius: 8px;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: #333;
}
.modder-pfp {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.modder-pfp-placeholder {
  color: #888;
}
.modder-info {
  display: flex;
  flex-direction: column;
  min-width: 0;
}
.modder-name {
  font-size: 1.05em;
  font-weight: 500;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.modder-count {
  font-size: 0.8em;
  color: #888;
  margin-top: 1px;
}
.modder-bio {
  font-size: 0.78em;
  color: #aaa;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  margin-top: 1px;
}
</style>

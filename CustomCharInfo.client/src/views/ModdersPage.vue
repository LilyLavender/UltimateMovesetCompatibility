<template>
  <PageShell title="Modders" :head="false">
    <template #subnav>
      <SubNav section="modders" label="Modders" />
    </template>

    <div v-if="loading" class="modders-grid" aria-busy="true">
      <Skeleton v-for="n in 10" :key="n" variant="line" height="58px" />
    </div>
    <div v-else class="modders-grid reveal">
      <router-link
        v-for="modder in modders"
        :key="modder.modderId"
        :to="{ name: 'ModderDetail', params: { id: modder.modderId } }"
        class="modder-card"
      >
        <span class="modder-card__pfp">
          <img
            v-if="modder.pfpUrl || avatars[modder.modderId]"
            :src="modder.pfpUrl || avatars[modder.modderId]"
            alt=""
          />
          <v-icon v-else size="28">mdi-account</v-icon>
        </span>
        <span class="modder-card__text">
          <span class="modder-card__name">
            {{ modder.name }}
            <v-icon v-if="modder.isAdmin" size="15" title="Admin">mdi-shield-account</v-icon>
          </span>
          <span v-if="modder.bio" class="modder-card__bio">{{ modder.bio }}</span>
        </span>
      </router-link>
    </div>
  </PageShell>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useHead } from '@unhead/vue'
import axios from 'axios'
import api from '@/services/api'
import PageShell from '@/components/PageShell.vue'
import SubNav from '@/components/SubNav.vue'
import Skeleton from '@/components/Skeleton.vue'

useHead({
  title: 'UMC | Modders',
  meta: [
    { name: 'description', content: 'Browse modders on Ultimate Moveset Compatibility.' },
    { property: 'og:title', content: 'Modders | Ultimate Moveset Compatibility' },
    { property: 'og:description', content: 'Browse modders on Ultimate Moveset Compatibility.' },
    { name: 'twitter:title', content: 'Modders | Ultimate Moveset Compatibility' },
    { name: 'twitter:description', content: 'Browse modders on Ultimate Moveset Compatibility.' },
  ],
})

const modders = ref([])
const avatars = ref({})
const loading = ref(true)

onMounted(async () => {
  const res = await api.get('/modders/public')
  modders.value = res.data
  loading.value = false

  const fetches = modders.value
    .filter((m) => !m.pfpUrl && m.gamebananaId)
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
.modders-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 6px;
}

.modder-card {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
  padding: 8px 10px;
  border: 1px solid var(--line);
  background: var(--panel);
  color: var(--tx);
  text-decoration: none;
  transition:
    background-color var(--dur-fast) var(--ease),
    border-color var(--dur-fast) var(--ease);
}

.modder-card:hover {
  background: var(--panel-2);
  border-color: var(--white);
}

.modder-card__pfp {
  flex: none;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  overflow: hidden;
  background: var(--panel-2);
  color: var(--tx-3);
}

.modder-card__pfp img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.modder-card__text {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.modder-card__name {
  display: flex;
  align-items: center;
  gap: 4px;
  font-weight: 600;
  font-size: 15px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.modder-card__bio {
  font-size: 12.5px;
  color: var(--tx-3);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
</style>

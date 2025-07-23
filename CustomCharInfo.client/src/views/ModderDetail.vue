<!-- ModderPage.vue -->
<template>
  <v-container>
    <v-row class="modder-section-1">
      <v-col cols="2" class="text-center">
        <img v-if="modderPfpUrl" :src="modderPfpUrl" class="modder-pfp" alt="GameBanana PFP" />
        <div v-else class="modder-pfp-null"><v-icon size="128">mdi-account</v-icon></div>
        <a
          :href="`${GB_MEMBER_URL}${modder?.gamebananaId}`"
          class="offsite unvisitable"
          target="_blank" rel="noopener"
        >GameBanana</a>
      </v-col>
      <v-col cols="10">
        <!-- <h1 class="page-title">{{ modder?.name }}</h1> -->
        <div class="title-container">
          <h1 class="page-title">{{ modder?.name }}</h1>
        </div>
        <p class="bio">{{ modder?.bio }}</p>
      </v-col>
    </v-row>

    <v-row>
      <v-col cols="12">
        <h2 class="movesets-title">Movesets</h2>
        <MovesetList :movesets="movesets" />
      </v-col>
    </v-row>
  </v-container>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import axios from 'axios'
import api from '@/services/api'
import MovesetList from '@/components/MovesetList.vue'
import { GB_MEMBER_URL } from '@/globals'

const apiUrl = import.meta.env.VITE_API_URL

const route = useRoute()
const modderId = route.params.id

const modder = ref(null)
const movesets = ref([])

const modderPfpUrl = ref(null)

onMounted(async () => {
  // Fetch modder info
  const { data } = await api.get(`/modders/${modderId}`)
  modder.value = data

  // Fetch movesets
  const movesetRes = await api.get(`movesets?modderId=${modderId}&page=1&pageSize=10`)
  movesets.value = movesetRes.data.sort((a, b) => new Date(a.releaseDate) - new Date(b.releaseDate))

  // Fetch GameBanana pfp
  if (modder.value.gamebananaId) {
    try {
      const res = await axios.get(
        `https://api.gamebanana.com/Core/Item/Data?itemtype=Member&itemid=${modder.value.gamebananaId}&fields=Url().sHdAvatarUrl(),Url().sAvatarUrl()`
      )
      modderPfpUrl.value = res.data[0] ? res.data[0] : res.data[1];
    } catch (err) {
      console.warn('Could not fetch GameBanana avatar:', err)
    }
  }
})
</script>

<style scoped>
.title-container {
  position: absolute;
  width: fit-content;
  padding-right: 1em;
  z-index: 0;
}

.title-container::after {
  content: '';
  position: absolute;
  top: 0;
  left: -100px;
  right: 0;
  bottom: 0;
  background-color: black;
  background-image: url(/src/assets/ptn_diagonal_12.png);
  background-repeat: repeat;
  display: block;
  -webkit-transform: skewX(-29deg);
  transform: skewX(-29deg);
}

.title-container::before {
  content: '';
  position: absolute;
  top: 0;
  left: 99%;
  right: -20px;
  bottom: 0;
  background-color: #dedede;
  display: block;
  -webkit-transform: skewX(-29deg);
  transform: skewX(-29deg);
}

.page-title {
  position: relative;
  z-index: 10;
  font-size: 4.5em;
  margin: -16px 0.25em -16px -0.25em;
  filter: drop-shadow(5px 4px 3px #000000c0)
}

.modder-section-1 {
  margin-top: 1.5em;
  padding-top: 0.5em;
}

.modder-pfp, .modder-pfp-null {
  z-index: 20;
  position: relative;
  width: 128px;
  height: 128px;
  background-color: black;
}

.modder-pfp-null {
  margin-left: 23px;
  margin-bottom: 6px;
  color: #333333;
}

.bio {
  margin-top: 4em;
  margin-left: -2em;
  font-size: larger;
}

.movesets-title {
  font-size: 2.5em;
}
</style>
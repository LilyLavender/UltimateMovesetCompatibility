<template>
  <div class="moveset-card no-select">
    <component
      :is="canView ? 'router-link' : 'div'"
      :to="canView ? { name: 'MovesetDetail', params: { movesetId: moveset.movesetId } } : null"
      class="moveset-card__link"
    >
      <!-- Background gradient -->
      <div
        class="moveset-card__background"
        :style="{ background: backgroundGradient }"
      >
      </div>
      <!-- Character image -->
      <div
        class="moveset-card__img"
        :style="{ backgroundImage: `url(${getFullImageUrl(moveset.thumbhImageUrl)})` }"
      ></div>

      <!-- Series icon -->
      <div v-if="moveset.seriesIconUrl" class="moveset-card__series">
        <img
          :src="getFullImageUrl(moveset.seriesIconUrl)"
          alt="Series Icon"
        />
      </div>

      <!-- Text -->
      <p class="moveset-card__charname">{{ moveset.moddedCharName }}</p>
      <p class="moveset-card__creator">{{ moveset.modders.join(', ') }}</p>
    </component>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import thumbhUnknown from "@/assets/thumb_h_unknown.png"
import api from '@/services/api'

const apiUrl = import.meta.env.VITE_API_URL

const props = defineProps({
  moveset: Object
})

// TODO this should be done in the list, NOT the card
const user = ref(null)
const canView = computed(() => {
  if (!props.moveset.privateMoveset) return true
  if (!user.value) return false
  const isAdmin = user.value.userTypeId === 3
  const isModder = user.value.modderId != null &&
    props.moveset.modders.some(m => m === user.value.userName) // This should absolutely not be done by username but there's security on the moveset itself so it's whatever lol
  return isAdmin || isModder
})

const getFullImageUrl = (path) => {
  if (!path) return thumbhUnknown
  return path.startsWith('/') ? `${apiUrl}${path}` : path
}

const backgroundGradient = computed(() => {
  const color = props.moveset.backgroundColor || '000000'
  return `linear-gradient(55deg, #${color}00 40%, #${color}33 50%, #${color}ff 100%)`
})

// Fetch current user if moveset private
onMounted(async () => {
  if (props.moveset.privateMoveset) {
    try {
      const res = await api.get('/auth/me')
      user.value = res.data
    } catch (err) {
      user.value = null
    }
  }
})
</script>

<style scoped>
.moveset-card {
  display: flex;
  width: 340px;
  height: 82px;
  background-color: black;
  flex: 0 1 33.333%;
}

.moveset-card__link {
  width: 100%;
  height: 100%;
  background-color: black;
  position: relative;
  z-index: 0;
  color: white;
  text-decoration: none;
  display: block;
  overflow: hidden;
}

.moveset-card__link:visited {
  color: white;
}

.moveset-card__link::after {
  content: "";
  display: block;
  width: 0;
  height: 82px;
  background: repeat url(/src/assets/ptn_diagonal_12.png) 0 0;
  background-size: 6px;
  transition: 0.25s cubic-bezier(0.165, 0.84, 0.44, 1);
  position: absolute;
  top: 0;
  left: 0;
  z-index: 20;
}

.moveset-card__link:hover::after {
  width: 340px;
}

.moveset-card__background {
  width: 98%;
  height: 93%;
  position: absolute;
  z-index: 10;
  margin: 3px;
}

.moveset-card__img {
  position: absolute;
  height: 82px;
  width: 340px;
  z-index: 30;
  filter: drop-shadow(5px 5px 8px #00000080);
}

.moveset-card__series {
  width: 80px;
  height: 80px;
  position: absolute;
  z-index: 40;
}

.moveset-card__series > img {
  height: 120%;
  width: 120%;
  margin: -9px;
  margin-left: 0.6em;
}

.moveset-card__charname {
  position: absolute;
  z-index: 50;
  margin-top: 7.5%;
  margin-left: 1.4em;
  font-size: larger;
  font-family: "Roboto Condensed";
  text-transform: uppercase;
}

.moveset-card__creator {
  position: absolute;
  z-index: 50;
  left: 2px;
  bottom: -2px;
  font-size: x-small;
}
</style>
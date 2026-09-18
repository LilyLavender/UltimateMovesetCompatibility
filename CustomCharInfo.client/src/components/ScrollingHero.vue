<template>
  <div class="hero-scroller no-select">
    <div class="scroller-track" :style="{ '--scroll-end': `-${scrollPercent}%` }">
      <div v-for="(column, index) in finalColumns" :key="index" class="image-column">
        <img
          v-for="(img, i) in column"
          :key="i"
          :src="img"
          class="scroller-img"
          :alt="'Scrolling image ' + i"
          draggable="false"
        />
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/services/api'

// Bundled fallback images, used if the banner-images API has nothing (or fails) so the hero never renders empty.
const fallbackModules = import.meta.glob(
  '@/assets/scrolling-banner-images/*.{jpg,jpeg,png,JPG,JPEG,PNG}',
  { eager: true }
)
const fallbackPaths = Object.values(fallbackModules).map((m) => m.default)

const columns = ref([])
const finalColNum = 5

// Helper function to shuffle images
function shuffle(array) {
  const result = [...array]
  for (let i = result.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1))
    ;[result[i], result[j]] = [result[j], result[i]]
  }
  return result
}

function buildColumns(imagePaths) {
  const shuffled = shuffle(imagePaths)
  const workingPaths = [...shuffled, ...shuffled, ...shuffled]

  const built = []
  for (let i = 0; i < workingPaths.length; i += 3) {
    built.push([workingPaths[i], workingPaths[i + 1], workingPaths[i + 2]])
  }
  return { columns: built, workingPaths }
}

let workingPaths = []

// Append final extra columns
const finalColumns = computed(() => {
  const firstFew = workingPaths.slice(0, finalColNum * 3)

  const extraColumns = []
  for (let i = 0; i < firstFew.length; i += 3) {
    extraColumns.push([firstFew[i], firstFew[i + 1], firstFew[i + 2]])
  }

  return [...columns.value, ...extraColumns]
})

// Calculate percentage to scroll
const scrollPercent = computed(() => {
  const base = columns.value.length
  const total = base + finalColNum
  return total === 0 ? 0 : ((base / total) * 100).toFixed(2)
})

onMounted(async () => {
  let imagePaths = fallbackPaths
  try {
    const res = await api.get('/banner-images')
    if (res.data.length > 0) {
      imagePaths = res.data.map((i) => i.imageUrl)
    }
  } catch (err) {
    console.error('Failed to load banner images, using bundled fallback:', err)
  }

  const built = buildColumns(imagePaths)
  columns.value = built.columns
  workingPaths = built.workingPaths
})
</script>

<style scoped>
.hero-scroller {
  width: 100%;
  overflow: hidden;
  position: relative;
  height: 75vh;
  background: black;
  z-index: 1;
}

.hero-scroller::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 0;
  width: 100%;
  height: 36vh;
  background-image: linear-gradient(
    0deg,
    rgba(0, 0, 0, 1) 6%,
    rgba(0, 0, 0, 0.96) 29%,
    rgba(0, 0, 0, 0.92) 38%,
    rgba(0, 0, 0, 0.87) 44%,
    rgba(0, 0, 0, 0.83) 49%,
    rgba(0, 0, 0, 0.79) 53%,
    rgba(0, 0, 0, 0.75) 56%,
    rgba(0, 0, 0, 0.71) 59%,
    rgba(0, 0, 0, 0.67) 62%,
    rgba(0, 0, 0, 0.63) 64%,
    rgba(0, 0, 0, 0.58) 67%,
    rgba(0, 0, 0, 0.54) 68%,
    rgba(0, 0, 0, 0.5) 70%,
    rgba(0, 0, 0, 0.46) 72%,
    rgba(0, 0, 0, 0.42) 74%,
    rgba(0, 0, 0, 0.38) 75%,
    rgba(0, 0, 0, 0.33) 77%,
    rgba(0, 0, 0, 0.29) 78%,
    rgba(0, 0, 0, 0.25) 80%,
    rgba(0, 0, 0, 0.21) 81%,
    rgba(0, 0, 0, 0.17) 83%,
    rgba(0, 0, 0, 0.13) 85%,
    rgba(0, 0, 0, 0.08) 87%,
    rgba(0, 0, 0, 0.04) 91%,
    rgba(0, 0, 0, 0) 100%
  );
  pointer-events: none;
  z-index: 2;
}

.scroller-track {
  display: flex;
  width: max-content;
  animation: scroll-left linear infinite 180s;
}

.image-column {
  display: flex;
  flex-direction: column;
}

.scroller-img {
  width: auto;
  height: 25vh;
  object-fit: cover;
  flex-shrink: 0;
}

@keyframes scroll-left {
  0% {
    transform: translateX(0%);
  }
  100% {
    transform: translateX(var(--scroll-end));
  }
}
</style>

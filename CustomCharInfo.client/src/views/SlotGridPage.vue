<template>
  <div class="slot-grid-page">
    <h1 class="page-title no-select mb-4">Moveset Slots</h1>

    <div v-if="loading" class="text-center py-10">Loading...</div>

    <template v-else>
      <!-- Legend + sort controls -->
      <div class="controls-row mb-3">
        <div class="legend">
          <span class="swatch swatch-normal" />
          <span class="legend-text">No overlap</span>
          <span class="swatch swatch-overlap ml-4" />
          <span class="legend-text">Slot overlap</span>
          <p class="slot-note ml-5">Most movesets allow their slots to be changed, so two movesets sharing a slot range isn't necessarily a dealbreaker.</p>
        </div>
        <div class="sort-btns">
          <span class="sort-label">Sort:</span>
          <button :class="['sort-btn', sortOrder === 'alpha' ? 'sort-btn--active' : '']" @click="sortOrder = 'alpha'">A–Z</button>
          <button :class="['sort-btn', sortOrder === 'count' ? 'sort-btn--active' : '']" @click="sortOrder = 'count'">Moveset Count</button>
        </div>
      </div>


      <!-- Table -->
      <div class="scroll-container">
        <div class="grid-table">

          <!-- Header row -->
          <div class="g-row header-row">
            <div class="char-col">Character</div>
            <div class="slots-col" :style="{ height: HEADER_H + 'px' }">
              <template v-for="(t, idx) in headerTicks" :key="t">
                <div class="tick-line" :style="{ left: slotXPct(t) }" />
                <div v-if="idx > 0" class="tick-label" :style="{ left: slotXPct(t) }">c{{ t }}</div>
              </template>
            </div>
          </div>

          <!-- Character rows -->
          <div
            v-for="row in processedGrid"
            :key="row.vanillaChar"
            class="g-row"
          >
            <div class="char-col" :style="{ height: rowHeight(row) + 'px' }">
              <img
                :src="iconUrl(row.vanillaChar)"
                class="char-icon"
                :alt="row.displayName"
                loading="lazy"
              />
              <span class="char-name">{{ row.displayName }} <span class="char-count">({{ row.movesetCount }})</span></span>
            </div>
            <div class="slots-col" :style="{ height: rowHeight(row) + 'px' }">
              <div
                v-for="t in headerTicks"
                :key="'tl' + t"
                class="tick-line tick-line-row"
                :style="{ left: slotXPct(t) }"
              />
              <template v-for="m in row.movesets" :key="m.movesetId">
                <!-- Private: non-clickable -->
                <div
                  v-if="m.isPrivate"
                  class="moveset-bar bar-private"
                  :class="{ 'bar-overlap': m.hasOverlap }"
                  :style="{
                    left: slotXPct(m.slotsStart),
                    width: slotWPct(m.slotsStart, m.slotsEnd),
                    top: m.lane * LANE_H + 2 + 'px',
                    height: LANE_H - 4 + 'px',
                  }"
                >
                  <v-tooltip activator="parent" location="top" :text="slotRangeText(m)" />
                  <span class="bar-label">???</span>
                </div>
                <!-- Public: clickable -->
                <router-link
                  v-else
                  :to="{ name: 'MovesetDetail', params: { movesetId: m.movesetId } }"
                  class="moveset-bar"
                  :class="{ 'bar-overlap': m.hasOverlap }"
                  :style="{
                    left: slotXPct(m.slotsStart),
                    width: slotWPct(m.slotsStart, m.slotsEnd),
                    top: m.lane * LANE_H + 2 + 'px',
                    height: LANE_H - 4 + 'px',
                  }"
                >
                  <v-tooltip activator="parent" location="top" :text="tooltipText(m)" />
                  <span class="bar-label">{{ displayName(m) }}</span>
                </router-link>
              </template>
            </div>
          </div>

        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/services/api'

const CHAR_W = 168
const HEADER_H = 36
const LANE_H = 26

const loading = ref(true)
const allChars = ref([])
const gridData = ref([])
const sortOrder = ref('alpha')

const movesetsByChar = computed(() => {
  const map = {}
  gridData.value.forEach(row => { map[row.vanillaChar] = row.movesets })
  return map
})

const minSlot = computed(() => {
  let min = Infinity
  gridData.value.forEach(row => row.movesets.forEach(m => { if (m.slotsStart < min) min = m.slotsStart }))
  return min === Infinity ? 0 : Math.floor(min / 8) * 8
})

const maxSlot = computed(() => {
  let max = -Infinity
  gridData.value.forEach(row => row.movesets.forEach(m => { if (m.slotsEnd > max) max = m.slotsEnd }))
  return max === -Infinity ? 7 : Math.ceil((max + 1) / 8) * 8 - 1
})

const numSlots = computed(() => maxSlot.value - minSlot.value + 1)

const headerTicks = computed(() => {
  const ticks = []
  for (let i = minSlot.value; i <= maxSlot.value; i += 8) ticks.push(i)
  return ticks
})

function slotXPct(slot) {
  return ((slot - minSlot.value) / numSlots.value * 100) + '%'
}

function slotWPct(start, end) {
  return ((end - start + 1) / numSlots.value * 100) + '%'
}

function pad(n) {
  return String(n).padStart(3, '0')
}

function iconUrl(internalName) {
  return `${import.meta.env.BASE_URL}vanilla-stock-icons/chara_2_${internalName}.png`
}

function rowHeight(row) {
  return Math.max(row.laneCount, 1) * LANE_H
}

function slotRangeText(m) {
  return `c${pad(m.slotsStart)}–c${pad(m.slotsEnd)}`
}

function displayName(m) {
  return m.subtitle ? `${m.name} (${m.subtitle})` : m.name
}

function tooltipText(m) {
  return `${displayName(m)} (${slotRangeText(m)})`
}

function assignLanesAndOverlaps(movesets) {
  if (!movesets.length) return { movesets: [], laneCount: 0 }

  const sorted = [...movesets].sort((a, b) => a.slotsStart - b.slotsStart)

  // Which movesets overlap which
  const overlapsWith = sorted.map(() => [])
  for (let i = 0; i < sorted.length; i++) {
    for (let j = i + 1; j < sorted.length; j++) {
      if (sorted[i].slotsStart <= sorted[j].slotsEnd && sorted[j].slotsStart <= sorted[i].slotsEnd) {
        overlapsWith[i].push(j)
        overlapsWith[j].push(i)
      }
    }
  }

  // Greedy lane assignment: fit each moveset into the first lane it doesn't conflict with
  const laneEnds = []
  const result = sorted.map((m, i) => {
    let lane = laneEnds.findIndex(end => end < m.slotsStart)
    if (lane === -1) { lane = laneEnds.length; laneEnds.push(0) }
    laneEnds[lane] = m.slotsEnd
    return {
      ...m,
      lane,
      hasOverlap: overlapsWith[i].length > 0,
      overlapNames: overlapsWith[i].map(j => sorted[j].name)
    }
  })

  return { movesets: result, laneCount: laneEnds.length }
}

const processedGrid = computed(() => {
  const rows = allChars.value
    .filter(c => c.vanillaCharInternalName !== 'kirby')
    .map(c => {
      const raw = movesetsByChar.value[c.vanillaCharInternalName] ?? []
      const { movesets, laneCount } = assignLanesAndOverlaps(raw)
      return {
        vanillaChar: c.vanillaCharInternalName,
        displayName: c.displayName,
        movesets,
        laneCount,
        movesetCount: raw.length
      }
    })

  if (sortOrder.value === 'count') {
    rows.sort((a, b) => b.movesetCount - a.movesetCount || a.displayName.localeCompare(b.displayName))
  } else {
    rows.sort((a, b) => a.displayName.localeCompare(b.displayName))
  }

  return rows
})

onMounted(async () => {
  const [charsRes, gridRes] = await Promise.all([
    api.get('/vanillachars'),
    api.get('/movesets/slot-grid')
  ])
  allChars.value = charsRes.data
  gridData.value = gridRes.data
  loading.value = false
})
</script>

<style scoped>
.page-title {
  text-align: center;
}

.slot-grid-page {
  padding: 1.5rem;
}

.scroll-container {
  border: 1px solid #333;
  border-radius: 6px;
}

.grid-table {
  display: block;
  width: 100%;
  font-size: 12px;
}

/* Row */
.g-row {
  display: flex;
  align-items: stretch;
  border-bottom: 1px solid #252525;
}

.g-row:not(.header-row):hover {
  background: #181818;
}

/* Sticky character column */
.char-col {
  flex: 0 0 168px;
  width: 168px;
  position: sticky;
  left: 0;
  z-index: 1;
  background: #121212;
  padding: 0 8px;
  display: flex;
  align-items: center;
  gap: 6px;
  border-right: 2px solid #333;
  font-size: 15px;
  white-space: nowrap;
  overflow: hidden;
}

.header-row {
  position: sticky;
  top: 0;
  z-index: 3;
  background: #121212;
}

.header-row .char-col {
  z-index: 4;
  font-size: 16px;
  height: 36px;
}

.char-icon {
  width: 20px;
  height: 20px;
  object-fit: contain;
  flex-shrink: 0;
}

.char-name {
  overflow: hidden;
  text-overflow: ellipsis;
}

/* Slots area */
.slots-col {
  position: relative;
  flex: 1;
  min-width: 0;
}

/* Tick lines */
.tick-line {
  position: absolute;
  top: 0;
  bottom: 0;
  width: 1px;
  background: #2e2e2e;
  pointer-events: none;
}

.tick-line-row {
  background: #1e1e1e;
}

/* Tick labels in header */
.tick-label {
  position: absolute;
  bottom: 4px;
  font-size: 16px;
  color: #888;
  transform: translateX(-50%);
  white-space: nowrap;
  pointer-events: none;
  user-select: none;
}

/* Moveset bars */
.moveset-bar {
  position: absolute;
  border-radius: 3px;
  background: #1565c0cc;
  border: 1px solid #1976d2;
  cursor: pointer;
  text-decoration: none;
  overflow: hidden;
  display: flex;
  align-items: center;
  min-width: 2px;
  transition: filter 0.1s;
}

.moveset-bar:hover {
  filter: brightness(1.4);
  z-index: 2;
}

.moveset-bar.bar-overlap {
  background: #4fc3f7cc;
  border-color: #81d4fa;
}

.moveset-bar.bar-private {
  cursor: default;
}

.moveset-bar.bar-private:hover {
  filter: none;
}

.bar-label {
  font-size: 10px;
  color: #fff;
  white-space: nowrap;
  padding: 0 3px;
  overflow: hidden;
  text-overflow: ellipsis;
  pointer-events: none;
  user-select: none;
  flex-shrink: 1;
  min-width: 0;
}

/* Controls row */
.controls-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 8px;
}

/* Legend */
.legend {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
}

/* Sort buttons */
.sort-btns {
  display: flex;
  align-items: center;
  gap: 4px;
}

.sort-label {
  font-size: 12px;
  color: #888;
  margin-right: 2px;
}

.sort-btn {
  font-size: 12px;
  padding: 2px 10px;
  border-radius: 4px;
  border: 1px solid #444;
  background: #1e1e1e;
  color: #ccc;
  cursor: pointer;
  transition: background 0.1s, color 0.1s;
}

.sort-btn:hover { background: #2a2a2a; }

.sort-btn--active {
  background: #1565c0;
  border-color: #1976d2;
  color: #fff;
}

/* Slot note */
.slot-note {
  font-size: 12px;
  color: #888;
  margin: 0;
}

/* Char count badge */
.char-count {
  color: #666;
  font-size: 0.85em;
}

.swatch {
  display: inline-block;
  width: 20px;
  height: 14px;
  border-radius: 3px;
}

.swatch-normal {
  background: #1565c0cc;
  border: 1px solid #1976d2;
}

.swatch-overlap {
  background: #4fc3f7cc;
  border: 1px solid #81d4fa;
}

.legend-text {
  color: #ccc;
}

.ml-4 {
  margin-left: 1rem;
}
</style>

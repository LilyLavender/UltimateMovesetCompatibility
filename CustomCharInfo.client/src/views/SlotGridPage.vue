<template>
  <PageShell title="Slot grid" tier="wide">
    <template #subnav>
      <SubNav section="movesets" label="Movesets" />
    </template>

    <!-- Legend + sort controls -->
    <div class="controls-row">
      <div class="legend">
        <span class="swatch swatch-normal" />
        <span class="legend-text">No overlap</span>
        <span class="swatch swatch-overlap" />
        <span class="legend-text">Slot overlap</span>
        <p class="slot-note">
          Most movesets allow their slots to be changed, so two movesets sharing a slot range isn't
          necessarily a dealbreaker.
        </p>
      </div>
      <div class="sort-btns">
        <span class="sort-label">Sort</span>
        <AppButton
          size="sm"
          :variant="sortOrder === 'alpha' ? 'primary' : 'ghost'"
          @click="sortOrder = 'alpha'"
        >
          A to Z
        </AppButton>
        <AppButton
          size="sm"
          :variant="sortOrder === 'count' ? 'primary' : 'ghost'"
          @click="sortOrder = 'count'"
        >
          Moveset count
        </AppButton>
      </div>
    </div>

    <SkeletonTable v-if="loading" :headers="['Character', 'Slots']" :columns="[1, 6]" :rows="12" />

    <TableScroll v-else min-width="720px" class="reveal">
      <div class="grid-table">
        <!-- Header row -->
        <div class="g-row header-row">
          <div class="char-col">Character</div>
          <div class="slots-col" :style="{ height: HEADER_H + 'px' }">
            <template v-for="(t, idx) in headerTicks" :key="t">
              <div class="tick-line" :style="{ left: slotXPct(t) }" />
              <div v-if="idx > 0" class="tick-label" :style="{ left: slotXPct(t) }">
                {{ formatSlot(t) }}
              </div>
            </template>
          </div>
        </div>

        <!-- Character rows -->
        <div v-for="row in processedGrid" :key="row.vanillaChar" class="g-row">
          <div class="char-col" :style="{ height: rowHeight(row) + 'px' }">
            <img
              :src="iconUrl(row.vanillaChar)"
              class="char-icon"
              :alt="row.displayName"
              loading="lazy"
            />
            <span class="char-name"
              >{{ row.displayName }} <span class="char-count">({{ row.movesetCount }})</span></span
            >
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
    </TableScroll>
  </PageShell>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '@/services/api'
import { formatSlot, formatSlotRange } from '@/services/slots'
import PageShell from '@/components/PageShell.vue'
import SubNav from '@/components/SubNav.vue'
import AppButton from '@/components/AppButton.vue'
import TableScroll from '@/components/TableScroll.vue'
import SkeletonTable from '@/components/SkeletonTable.vue'

const HEADER_H = 36
const LANE_H = 26

const loading = ref(true)
const allChars = ref([])
const gridData = ref([])
const sortOrder = ref('alpha')

const movesetsByChar = computed(() => {
  const map = {}
  gridData.value.forEach((row) => {
    map[row.vanillaChar] = row.movesets
  })
  return map
})

const minSlot = computed(() => {
  let min = Infinity
  gridData.value.forEach((row) =>
    row.movesets.forEach((m) => {
      if (m.slotsStart < min) min = m.slotsStart
    })
  )
  return min === Infinity ? 0 : Math.floor(min / 8) * 8
})

const maxSlot = computed(() => {
  let max = -Infinity
  gridData.value.forEach((row) =>
    row.movesets.forEach((m) => {
      if (m.slotsEnd > max) max = m.slotsEnd
    })
  )
  return max === -Infinity ? 7 : Math.ceil((max + 1) / 8) * 8 - 1
})

const numSlots = computed(() => maxSlot.value - minSlot.value + 1)

const headerTicks = computed(() => {
  const ticks = []
  for (let i = minSlot.value; i <= maxSlot.value; i += 8) ticks.push(i)
  return ticks
})

function slotXPct(slot) {
  return ((slot - minSlot.value) / numSlots.value) * 100 + '%'
}

function slotWPct(start, end) {
  return ((end - start + 1) / numSlots.value) * 100 + '%'
}

function iconUrl(internalName) {
  return `${import.meta.env.BASE_URL}vanilla-stock-icons/chara_2_${internalName}.png`
}

function rowHeight(row) {
  return Math.max(row.laneCount, 1) * LANE_H
}

function slotRangeText(m) {
  return formatSlotRange(m.slotsStart, m.slotsEnd)
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
      if (
        sorted[i].slotsStart <= sorted[j].slotsEnd &&
        sorted[j].slotsStart <= sorted[i].slotsEnd
      ) {
        overlapsWith[i].push(j)
        overlapsWith[j].push(i)
      }
    }
  }

  // Greedy lane assignment: fit each moveset into the first lane it doesn't conflict with
  const laneEnds = []
  const result = sorted.map((m, i) => {
    let lane = laneEnds.findIndex((end) => end < m.slotsStart)
    if (lane === -1) {
      lane = laneEnds.length
      laneEnds.push(0)
    }
    laneEnds[lane] = m.slotsEnd
    return {
      ...m,
      lane,
      hasOverlap: overlapsWith[i].length > 0,
      overlapNames: overlapsWith[i].map((j) => sorted[j].name),
    }
  })

  return { movesets: result, laneCount: laneEnds.length }
}

const processedGrid = computed(() => {
  const rows = allChars.value
    .filter((c) => c.vanillaCharInternalName !== 'kirby')
    .map((c) => {
      const raw = movesetsByChar.value[c.vanillaCharInternalName] ?? []
      const { movesets, laneCount } = assignLanesAndOverlaps(raw)
      return {
        vanillaChar: c.vanillaCharInternalName,
        displayName: c.displayName,
        movesets,
        laneCount,
        movesetCount: raw.length,
      }
    })

  if (sortOrder.value === 'count') {
    rows.sort(
      (a, b) => b.movesetCount - a.movesetCount || a.displayName.localeCompare(b.displayName)
    )
  } else {
    rows.sort((a, b) => a.displayName.localeCompare(b.displayName))
  }

  return rows
})

onMounted(async () => {
  const [charsRes, gridRes] = await Promise.all([
    api.get('/vanillachars'),
    api.get('/movesets/slot-grid'),
  ])
  allChars.value = charsRes.data
  gridData.value = gridRes.data
  loading.value = false
})
</script>

<style scoped>
/*
  One grid for every row, so the character column sizes to the longest name and never clamps.
  Rows are display: contents; their two cells are the grid items.
*/
.grid-table {
  display: grid;
  grid-template-columns: max-content minmax(0, 1fr);
  width: 100%;
  border: 1px solid var(--line);
  background: var(--panel);
  font-size: 12px;
}

.g-row {
  display: contents;
}

.g-row > * {
  border-bottom: 1px solid var(--line);
}

.g-row:last-child > * {
  border-bottom: 0;
}

.g-row:not(.header-row):hover > * {
  background: var(--panel-2);
}

/* Sticky character column */
.char-col {
  position: sticky;
  left: 0;
  z-index: 1;
  background: var(--panel);
  padding: 0 12px 0 8px;
  display: flex;
  align-items: center;
  gap: 6px;
  border-right: 2px solid var(--line-2);
  font-size: 14px;
  white-space: nowrap;
}

.header-row > * {
  position: sticky;
  top: 0;
  z-index: 3;
  background: var(--panel);
  border-bottom: 1px solid var(--white);
}

.header-row .char-col {
  z-index: 4;
  height: 36px;
  color: var(--tx-2);
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.03em;
}

.char-icon {
  width: 20px;
  height: 20px;
  object-fit: contain;
  flex-shrink: 0;
}

/* Slots area */
.slots-col {
  position: relative;
  min-width: 0;
}

/* Tick lines */
.tick-line {
  position: absolute;
  top: 0;
  bottom: 0;
  width: 1px;
  background: var(--line-2);
  pointer-events: none;
}

.tick-line-row {
  background: var(--line);
}

/* Tick labels in header */
.tick-label {
  position: absolute;
  bottom: 6px;
  font-family: var(--font-mono);
  font-size: 13px;
  color: var(--tx-2);
  transform: translateX(-50%);
  white-space: nowrap;
  pointer-events: none;
  user-select: none;
}

/* Moveset bars */
.moveset-bar {
  position: absolute;
  background: color-mix(in srgb, var(--info) 55%, #000);
  border: 1px solid var(--info);
  cursor: pointer;
  text-decoration: none;
  overflow: hidden;
  display: flex;
  align-items: center;
  min-width: 2px;
  transition: filter var(--dur-fast) var(--ease);
}

.moveset-bar:hover {
  filter: brightness(1.4);
  z-index: 2;
}

.moveset-bar.bar-overlap {
  background: color-mix(in srgb, var(--warn) 55%, #000);
  border-color: var(--warn);
}

.moveset-bar.bar-private {
  cursor: default;
}

.moveset-bar.bar-private:hover {
  filter: none;
}

.bar-label {
  font-size: 10px;
  color: var(--white);
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
  gap: 8px 16px;
  margin-bottom: 14px;
}

/* Legend */
.legend {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 6px 8px;
  font-size: 12px;
}

.swatch {
  display: inline-block;
  width: 20px;
  height: 14px;
}

.swatch-normal {
  background: color-mix(in srgb, var(--info) 55%, #000);
  border: 1px solid var(--info);
}

.swatch-overlap {
  background: color-mix(in srgb, var(--warn) 55%, #000);
  border: 1px solid var(--warn);
  margin-left: 8px;
}

.legend-text {
  color: var(--tx-2);
}

.slot-note {
  font-size: 12px;
  color: var(--tx-3);
  margin: 0 0 0 12px;
}

/* Sort buttons */
.sort-btns {
  display: flex;
  align-items: center;
  gap: 6px;
}

.sort-label {
  font-size: 12px;
  color: var(--tx-3);
  margin-right: 2px;
}

.char-count {
  color: var(--tx-3);
  font-size: 0.85em;
}

@media (max-width: 599px) {
  .slot-note {
    flex-basis: 100%;
    margin-left: 0;
  }
}
</style>

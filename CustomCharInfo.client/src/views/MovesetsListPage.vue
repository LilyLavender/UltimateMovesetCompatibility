<template>
  <PageShell title="Moveset table" tier="wide">
    <template #subnav>
      <SubNav section="movesets" label="Movesets">
        <template #actions>
          <AppButton
            variant="ghost"
            size="sm"
            icon="mdi-file-download"
            :disabled="!normalizedMovesets.length"
            @click="downloadCSV"
          >
            Download CSV
          </AppButton>
        </template>
      </SubNav>
    </template>

    <p class="table-hint">
      Every moveset with its slots, release state, functions, articles, and hooks. Scroll sideways
      for the full width; the character column stays put.
    </p>

    <SkeletonTable
      v-if="loading"
      :headers="['Creators', 'Modded char', 'Vanilla char', 'Slotted', 'Slots', 'Release']"
      :columns="[1.5, 1.5, 1.2, 1.2, 1, 1]"
      :rows="10"
    />

    <TableScroll v-else class="reveal">
      <v-data-table
        :headers="headers"
        :items="normalizedMovesets"
        item-key="moddedCharName"
        class="umc-table"
        density="compact"
        :items-per-page="-1"
        hide-default-footer
      >
        <!-- Modded char name with subtitle -->
        <template #item.moddedCharName="{ item }">
          {{ item.moddedCharName
          }}<span v-if="item.subtitle" class="table-subtitle"> ({{ item.subtitle }})</span>
        </template>

        <!-- Slotted/Replacement ID -->
        <template #item.slotReplacementId="{ item }">
          <span v-if="item.slottedId === item.replacementId" class="mono">
            {{ item.slottedId }}
          </span>
          <span v-else class="mono"> {{ item.slottedId }} / {{ item.replacementId }} </span>
        </template>

        <template #item.slotsRange="{ value }">
          <span class="mono">{{ value }}</span>
        </template>

        <!-- Release state -->
        <template #item.releaseState="{ item }">
          <StatusTag :variant="releaseTone(item.releaseState)">{{ item.releaseState }}</StatusTag>
        </template>

        <!-- Bools -->
        <template v-for="key in boolKeys" :key="`bool-${key}`" #[`item.${key}`]="{ value }">
          <StatusTag :variant="value ? 'ok' : 'err'">{{ value ? 'Yes' : 'No' }}</StatusTag>
        </template>

        <!-- Articles -->
        <template
          v-for="key in articleKeys"
          :key="`article-${key}`"
          #[`item.article:${key}`]="{ value }"
        >
          <StatusTag v-if="value" variant="neutral">{{ value }}</StatusTag>
        </template>

        <!-- Hooks -->
        <template v-for="key in hookKeys" :key="`hook-${key}`" #[`item.hook:${key}`]="{ value }">
          <StatusTag v-if="value" variant="neutral">{{ value }}</StatusTag>
        </template>
      </v-data-table>
    </TableScroll>
  </PageShell>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import api from '@/services/api'
import { ReleaseState, RELEASE_STATE_NAMES } from '@/globals'
import { formatOffset } from '@/services/offsets'
import PageShell from '@/components/PageShell.vue'
import SubNav from '@/components/SubNav.vue'
import AppButton from '@/components/AppButton.vue'
import StatusTag from '@/components/StatusTag.vue'
import TableScroll from '@/components/TableScroll.vue'
import SkeletonTable from '@/components/SkeletonTable.vue'

const movesets = ref([])
const loading = ref(true)

const boolKeys = [
  'hasGlobalOpff',
  'hasCharacterOpff',
  'hasAgentInit',
  'hasGlobalOnLinePre',
  'hasGlobalOnLineEnd',
]

const RELEASE_TONES = {
  [RELEASE_STATE_NAMES[ReleaseState.Released]]: 'ok',
  [RELEASE_STATE_NAMES[ReleaseState.Upcoming]]: 'warn',
  [RELEASE_STATE_NAMES[ReleaseState.PendingUpdate]]: 'warn',
  [RELEASE_STATE_NAMES[ReleaseState.OpenBeta]]: 'info',
  [RELEASE_STATE_NAMES[ReleaseState.Deprecated]]: 'err',
}
const releaseTone = (name) => RELEASE_TONES[name] ?? 'neutral'

// Headers before processing
const baseHeaders = [
  {
    title: 'Creators',
    key: 'modders',
    headerProps: { class: 'core-col' },
    cellProps: { class: 'core-col' },
  },
  {
    title: 'Modded char',
    key: 'moddedCharName',
    headerProps: { class: 'sticky core-col' },
    cellProps: { class: 'sticky core-col' },
  },
  {
    title: 'Vanilla char',
    key: 'vanillaCharName',
    headerProps: { class: 'core-col' },
    cellProps: { class: 'core-col' },
  },
  {
    title: 'Slotted / replacement',
    key: 'slotReplacementId',
    headerProps: { class: 'core-col' },
    cellProps: { class: 'core-col' },
  },
  {
    title: 'Slots',
    key: 'slotsRange',
    headerProps: { class: 'core-col' },
    cellProps: { class: 'core-col' },
  },
  { title: 'Release', key: 'releaseState' },
  { title: 'Global OPFF', key: 'hasGlobalOpff' },
  { title: 'Char OPFF', key: 'hasCharacterOpff' },
  { title: 'Agent init', key: 'hasAgentInit' },
  { title: 'on_line pre', key: 'hasGlobalOnLinePre' },
  { title: 'on_line end', key: 'hasGlobalOnLineEnd' },
]

// Gather unique articles and hooks
const articleKeys = computed(() => {
  const set = new Set()
  movesets.value.forEach((m) => m.articles.forEach((a) => set.add(a.original)))
  return [...set].sort()
})

const hookKeys = computed(() => {
  const set = new Set()
  movesets.value.forEach((m) => m.hooks.forEach((h) => set.add(h.offset)))
  return [...set].sort()
})

// Final headers
// section-divider is a calculated class to set sections in the table
const headers = computed(() => [
  ...baseHeaders.map((h) => {
    if (h.key === 'releaseState') {
      return {
        ...h,
        headerProps: { class: 'section-divider core-col' },
        cellProps: { class: 'section-divider core-col' },
      }
    }

    if (h.key === 'hasGlobalOnLineEnd') {
      return {
        ...h,
        headerProps: { class: 'section-divider' },
        cellProps: { class: 'section-divider' },
      }
    }

    return h
  }),

  ...articleKeys.value.map((a, i, arr) => ({
    title: a,
    key: `article:${a}`,
    headerProps: {
      class: i === arr.length - 1 ? 'section-divider' : '',
    },
    cellProps: {
      class: i === arr.length - 1 ? 'section-divider' : '',
    },
  })),

  ...hookKeys.value.map((h) => ({
    title: formatOffset(h),
    key: `hook:${h}`,
  })),
])

// Normalize rows
const normalizedMovesets = computed(() =>
  movesets.value.map((m) => {
    const row = { ...m }

    row.slotReplacementId =
      m.slottedId === m.replacementId ? String(m.slottedId) : `${m.slottedId}-${m.replacementId}`

    m.articles.forEach((a) => {
      row[`article:${a.original}`] = a.cloned
    })

    m.hooks.forEach((h) => {
      row[`hook:${h.offset}`] = h.usage ?? 'Yes'
    })

    return row
  })
)

onMounted(async () => {
  const res = await api.get('/movesets/report')
  movesets.value = res.data
  loading.value = false
})

// Download table as csv
function downloadCSV() {
  if (!normalizedMovesets.value.length) return

  // Headers
  const headersCsv = headers.value
    .map((h) => {
      if (h.key === 'slotReplacementId') return ['Slotted ID', 'Replacement ID']
      return [h.title]
    })
    .flat()

  // Rows
  const rowsCsv = normalizedMovesets.value.map((row) =>
    headers.value
      .map((h) => {
        let val
        if (h.key === 'slotReplacementId') {
          val = [row.slottedId, row.replacementId]
        } else {
          val = row[h.key] ?? ''
          if (boolKeys.includes(h.key)) {
            val = val ? 'Yes' : 'No'
          }
        }
        return (Array.isArray(val) ? val : [val])
          .map((v) => `"${String(v).replace(/"/g, '""')}"`)
          .join(',')
      })
      .join(',')
  )

  // Download
  const csvContent = [headersCsv.join(','), ...rowsCsv].join('\n')
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)

  const link = document.createElement('a')
  link.setAttribute('href', url)

  const now = new Date()
  const pad = (n) => String(n).padStart(2, '0')
  const timestamp = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}-${pad(now.getHours())}-${pad(now.getMinutes())}-${pad(now.getSeconds())}`
  link.setAttribute('download', `umc-movesets-${timestamp}.csv`)

  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}
</script>

<style scoped>
.table-hint {
  margin: 0 0 14px;
  color: var(--tx-3);
  font-size: 13px;
}

:deep(.umc-table .v-data-table__th) {
  padding: 4px 10px !important;
  white-space: nowrap;
}

:deep(.umc-table .v-data-table__td) {
  max-width: none !important;
  padding: 4px 10px !important;
  font-size: 13px !important;
  white-space: nowrap;
}

/* Section dividers */
:deep(.section-divider) {
  border-right: 2px solid var(--line-2) !important;
}

/* Core columns sit on the panel color so they read as one block */
:deep(.umc-table .v-data-table__th.core-col),
:deep(.umc-table .v-data-table__td.core-col) {
  background: var(--panel) !important;
}

:deep(.umc-table .v-data-table__tr:hover .v-data-table__td.core-col) {
  background: var(--panel-2) !important;
}

/* Sticky char name column */
:deep(.v-data-table__th.sticky),
:deep(.v-data-table__td.sticky) {
  position: sticky;
  left: 0;
  z-index: 1;
}

/* Subtitle inside cell */
.table-subtitle {
  font-size: 0.85em;
  opacity: 0.5;
  font-weight: normal;
}
</style>

<template>
  <div class="moveset-table-page">
    <h1 class="page-title no-select mb-3">Moveset Table</h1>

    <!-- Controls row -->
    <div class="controls-row mb-3">
      <button class="dl-btn" @click="downloadCSV">
        <span class="mdi mdi-file-download" />
        Download CSV
      </button>
    </div>

    <!-- Table -->
    <div class="scroll-container">
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
        {{ item.moddedCharName }}<span v-if="item.subtitle" class="table-subtitle"> ({{ item.subtitle }})</span>
      </template>

      <!-- Slotted/Replacement ID -->
      <template #item.slotReplacementId="{ item }">
        <span v-if="item.slottedId === item.replacementId">
          {{ item.slottedId }}
        </span>
        <span v-else>
          {{ item.slottedId }} / {{ item.replacementId }}
        </span>
      </template>

      <!-- Release state -->
      <template #item.releaseState="{ item }">
        <span
          class="release-pill"
          :class="{
            released: item.releaseState === 'Released',
            upcoming: item.releaseState === 'Upcoming',
            pending: item.releaseState === 'Pending Update',
            beta: item.releaseState === 'Open Beta',
            deprecated: item.releaseState === 'Deprecated'
          }"
        >
          {{ item.releaseState }}
        </span>
      </template>

      <!-- Bools -->
      <template #item.hasGlobalOpff="{ value }">
        <span :class="['bool-pill', value ? 'yes' : 'no']">
          {{ value ? 'Yes' : 'No' }}
        </span>
      </template>

      <template #item.hasCharacterOpff="{ value }">
        <span :class="['bool-pill', value ? 'yes' : 'no']">
          {{ value ? 'Yes' : 'No' }}
        </span>
      </template>

      <template #item.hasAgentInit="{ value }">
        <span :class="['bool-pill', value ? 'yes' : 'no']">
          {{ value ? 'Yes' : 'No' }}
        </span>
      </template>

      <template #item.hasGlobalOnLinePre="{ value }">
        <span :class="['bool-pill', value ? 'yes' : 'no']">
          {{ value ? 'Yes' : 'No' }}
        </span>
      </template>

      <template #item.hasGlobalOnLineEnd="{ value }">
        <span :class="['bool-pill', value ? 'yes' : 'no']">
          {{ value ? 'Yes' : 'No' }}
        </span>
      </template>

      <!-- Articles -->
      <template
        v-for="key in [...articleKeys, ...hookKeys]"
        #[`item.article:${key}`]="{ value }"
      >
        <div v-if="value" class="usage-pill">
          {{ value }}
        </div>
      </template>

      <!-- Hooks -->
      <template
        v-for="key in hookKeys"
        #[`item.hook:${key}`]="{ value }"
      >
        <div v-if="value" class="usage-pill">
          {{ value }}
        </div>
      </template>


    </v-data-table>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from "vue";
import api from "@/services/api";

const movesets = ref([]);

// Headers before processing
const baseHeaders = [
  { title: "Creators",              key: "modders",           headerProps: { class: "core-col" },         cellProps: { class: "core-col" } },
  { title: "Modded Char",           key: "moddedCharName",    headerProps: { class: "sticky core-col" },  cellProps: { class: "sticky core-col" } },
  { title: "Vanilla Char",          key: "vanillaCharName",   headerProps: { class: "core-col" },         cellProps: { class: "core-col" } },
  { title: "Slotted/Replacement",   key: "slotReplacementId", headerProps: { class: "core-col" },         cellProps: { class: "core-col" } },
  { title: "Slots",                 key: "slotsRange",        headerProps: { class: "core-col" },         cellProps: { class: "core-col" } },
  { title: "Release",               key: "releaseState" },
  { title: "Global OPFF",           key: "hasGlobalOpff" },
  { title: "Char OPFF",             key: "hasCharacterOpff" },
  { title: "Agent init",            key: "hasAgentInit" },
  { title: "on_line pre",           key: "hasGlobalOnLinePre" },
  { title: "on_line end",           key: "hasGlobalOnLineEnd" },
];

// Gather unique articles and hooks
const articleKeys = computed(() => {
  const set = new Set();
  movesets.value.forEach(m =>
    m.articles.forEach(a => set.add(a.original))
  );
  return [...set].sort();
});

const hookKeys = computed(() => {
  const set = new Set();
  movesets.value.forEach(m =>
    m.hooks.forEach(h => set.add(h.offset))
  );
  return [...set].sort();
});

// Final headers
// section-divider is a calculated class to set sections in the table
const headers = computed(() => [
  ...baseHeaders.map(h => {
    if (h.key === "releaseState") {
      return {
        ...h,
        headerProps: { class: "section-divider core-col" },
        cellProps: { class: "section-divider core-col" },
      };
    }

    if (h.key === "hasGlobalOnLineEnd") {
      return {
        ...h,
        headerProps: { class: "section-divider" },
        cellProps: { class: "section-divider" },
      };
    }

    return h;
  }),

  ...articleKeys.value.map((a, i, arr) => ({
    title: a,
    key: `article:${a}`,
    headerProps: {
      class: i === arr.length - 1 ? "section-divider" : ""
    },
    cellProps: {
      class: i === arr.length - 1 ? "section-divider" : ""
    },
  })),

  ...hookKeys.value.map(h => ({
    title: `0x${h}`,
    key: `hook:${h}`,
  })),
]);

// Normalize rows
const normalizedMovesets = computed(() =>
  movesets.value.map(m => {
    const row = { ...m };

    row.slotReplacementId =
      m.slottedId === m.replacementId
        ? String(m.slottedId)
        : `${m.slottedId}-${m.replacementId}`;

    m.articles.forEach(a => {
      row[`article:${a.original}`] = a.cloned;
    });

    m.hooks.forEach(h => {
      row[`hook:${h.offset}`] = h.usage ?? "Yes";
    });

    return row;
  })
);

// Store column indexes
const coreEndIndex = computed(() =>
  headers.value.findIndex(h => h.key === "releaseState") + 1
);

const boolEndIndex = computed(() =>
  headers.value.findIndex(h => h.key === "hasGlobalOnLineEnd") + 1
);

const articleEndIndex = computed(() =>
  headers.value.findLastIndex(h => h.key.startsWith("article:")) + 1
);

onMounted(async () => {
  const res = await api.get("/movesets/report");
  movesets.value = res.data;
});

// Download table as csv
function downloadCSV() {
  if (!normalizedMovesets.value.length) return;

  // Headers
  const headersCsv = headers.value
    .map(h => {
      if (h.key === "slotReplacementId") return ["Slotted ID", "Replacement ID"];
      return [h.title];
    })
    .flat();

  // Rows
  const rowsCsv = normalizedMovesets.value.map(row =>
    headers.value
      .map(h => {
        let val;
        if (h.key === "slotReplacementId") {
          val = [row.slottedId, row.replacementId];
        } else {
          val = row[h.key] ?? "";
          if (
            ["hasGlobalOpff","hasCharacterOpff","hasAgentInit","hasGlobalOnLinePre","hasGlobalOnLineEnd"].includes(h.key)
          ) {
            val = val ? "Yes" : "No";
          }
        }
        return (Array.isArray(val) ? val : [val])
          .map(v => `"${String(v).replace(/"/g, '""')}"`)
          .join(",");
      })
      .join(",")
  );

  // Download
  const csvContent = [headersCsv.join(","), ...rowsCsv].join("\n");
  const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
  const url = URL.createObjectURL(blob);

  const link = document.createElement("a");
  link.setAttribute("href", url);

  const now = new Date();
  const pad = n => String(n).padStart(2, "0");
  const timestamp = `${now.getFullYear()}-${pad(now.getMonth()+1)}-${pad(now.getDate())}-${pad(now.getHours())}-${pad(now.getMinutes())}-${pad(now.getSeconds())}`;
  link.setAttribute("download", `umc-movesets-${timestamp}.csv`);

  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}
</script>

<style scoped>
/* Page */
.moveset-table-page {
  padding: 1.5rem;
}

.page-title {
  text-align: center;
}

/* Controls row */
.controls-row {
  display: flex;
  align-items: center;
  justify-content: flex-end;
}

/* Download button — styled like slot-grid sort-btn, but larger */
.dl-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  padding: 5px 16px;
  border-radius: 4px;
  border: 1px solid #444;
  background: #1e1e1e;
  color: #ccc;
  cursor: pointer;
  transition: background 0.1s, color 0.1s;
}

.dl-btn:hover {
  background: #2a2a2a;
}

/* Table container */
.scroll-container {
  border: 1px solid #333;
  border-radius: 6px;
  overflow: hidden;
}

/* Vuetify table overrides */
:deep(.umc-table) {
  background: transparent !important;
}

:deep(.umc-table .v-data-table__th) {
  padding: 3px 10px !important;
  font-size: 13px !important;
  white-space: nowrap;
  border-bottom: 1px solid #333 !important;
  color: #fff !important;
}

:deep(.umc-table .v-data-table__td) {
  max-width: none !important;
  padding: 3px 10px !important;
  font-size: 13px !important;
  white-space: nowrap;
  border-bottom: 1px solid #252525 !important;
  color: #fff !important;
}

:deep(.umc-table .v-data-table__tr:hover td) {
  background: #181818 !important;
}

/* Section dividers */
:deep(.section-divider) {
  border-right: 2px solid #1a1a1a !important;
}

/* Core columns (#121212 background) */
:deep(.umc-table .v-data-table__th.core-col) {
  background: #121212 !important;
}

:deep(.umc-table .v-data-table__td.core-col) {
  background: #121212 !important;
}

/* Sticky char name column */
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

/* Pills — flat corners to match slot-grid */
.bool-pill {
  display: inline-block;
  text-align: center;
  padding: 1px 8px;
  border-radius: 3px;
  font-size: 12px;
  color: #fff;
}

.bool-pill.yes {
  background-color: #2e7d32;
}

.bool-pill.no {
  background-color: #c62828;
}

.usage-pill {
  display: inline-block;
  background-color: #1565c0cc;
  border: 1px solid #1976d2;
  color: #fff;
  padding: 1px 6px;
  border-radius: 3px;
  font-size: 12px;
  white-space: nowrap;
}

.release-pill {
  display: inline-block;
  padding: 1px 8px;
  border-radius: 3px;
  font-size: 12px;
  font-weight: 500;
  color: #fff;
  white-space: nowrap;
}

.release-pill.released  { background-color: #2e7d32; }
.release-pill.upcoming,
.release-pill.pending   { background-color: #fbc02d; color: #000; }
.release-pill.beta      { background-color: #acba22; color: #000; }
.release-pill.deprecated { background-color: #c62828; }
</style>

<template>
  <v-container max-width="1200px">
    <div class="d-flex align-baseline justify-space-between">
      <!-- Header -->
      <h1 class="mb-4 page-title no-select">Moveset Table</h1>

      <!-- Download -->
      <div class="d-flex justify-end mb-2">
        <v-btn @click="downloadCSV" class="btn">
          <v-icon>mdi-file-download</v-icon>
          Download CSV
        </v-btn>
      </div>
    </div>

    <!-- Table -->
    <v-data-table
      :headers="headers"
      :items="normalizedMovesets"
      item-key="moddedCharName"
      class="dark-table sectioned-table"
      density="comfortable"
      :items-per-page="-1"
      hide-default-footer
      :style="{
        '--core-end': coreEndIndex,
        '--bool-end': boolEndIndex,
        '--article-end': articleEndIndex
      }"
    >
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
            unreleased: item.releaseState === 'Unreleased',
            pending: item.releaseState === 'Pending Update',
            beta: item.releaseState === 'Open for Beta Testing',
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
  </v-container>
</template>

<script setup>
import { ref, onMounted, computed } from "vue";
import api from "@/services/api";

const movesets = ref([]);

// Headers before processing
const baseHeaders = [
  { title: "Creators", key: "modders" },
  { title: "Modded Char", key: "moddedCharName", headerProps: { class: "sticky" }, cellProps: { class: "sticky" },},
  { title: "Vanilla Char", key: "vanillaCharName" },
  { title: "Slotted/Replacement ID", key: "slotReplacementId" },
  { title: "Slots", key: "slotsRange" },
  { title: "Release", key: "releaseState" },
  { title: "Global OPFF", key: "hasGlobalOpff" },
  { title: "Char OPFF", key: "hasCharacterOpff" },
  { title: "agent_init", key: "hasAgentInit" },
  { title: "on_line pre", key: "hasGlobalOnLinePre" },
  { title: "on_line end", key: "hasGlobalOnLineEnd" },
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
        headerProps: { class: "section-divider" },
        cellProps: { class: "section-divider" },
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
  const res = await api.get("/moveset-report");
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
.page-title {
  width: fit-content; 
}

/* Expanded section */
.expanded-section {
  background: #00000080;
  padding: 1rem;
  border-radius: 8px;
}

.expanded-section h3 {
  margin-bottom: 0.25rem;
}

/* Pills */
/* Todo base pill class */
.bool-pill {
  display: inline-block;
  width: 80%;
  text-align: center;
  padding: 0.25em 1em;
  border-radius: 999px;
  font-size: 0.8rem;
  color: #fff;
}

.bool-pill.yes {
  background-color: #2e7d32;
}

.bool-pill.no {
  background-color: #c62828;
}

.usage-pill {
  background-color: #1565c0;
  color: #fff;
  padding: 0.25em 0.5em;
  border-radius: 6px;
  font-size: 0.75rem;
  white-space: nowrap;
  min-width: 80%;
  width: fit-content;
}

.release-pill {
  display: inline-block;
  padding: 0.25em 0.75em;
  border-radius: 999px;
  font-size: 0.8rem;
  font-weight: 500;
  color: #fff;
  text-align: center;
  min-width: 80%;
}

.release-pill.released {
  background-color: #2e7d32;
}

.release-pill.unreleased,
.release-pill.pending {
  background-color: #fbc02d;
  color: #000;
}

.release-pill.beta {
  background-color: #acba22;
  color: #000;
}

.release-pill.deprecated {
  background-color: #c62828;
}

/* Table sections */
:deep(.section-divider) {
  border-right: 3px solid #1a1a1a;
}

/* Sticky column */
:deep(.v-data-table__td.sticky) {
  position: sticky;
  left: 0;
  z-index: 1;
}

/* Remove max-width on cells */
:deep(.v-data-table__td) {
  max-width: none !important;
}

/* Todo make btn class common */
.btn {
  text-transform: unset;
  letter-spacing: 0.009375em;
  font-size: medium;
  background-color: #2e2e2e;
  color: #e2e2e2;
}
</style>

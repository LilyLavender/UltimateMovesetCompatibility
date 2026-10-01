<template>
  <div class="skeleton-table" aria-busy="true">
    <div v-if="headers.length" class="skeleton-table__head">
      <span v-for="(h, i) in headers" :key="i" :style="{ flex: widths[i] }">{{ h }}</span>
    </div>
    <div v-for="r in rows" :key="r" class="skeleton-table__row">
      <span v-for="(w, i) in widths" :key="i" :style="{ flex: w }">
        <Skeleton variant="line" :width="cellWidth(i)" />
      </span>
    </div>
  </div>
</template>

<script setup>
import Skeleton from '@/components/Skeleton.vue'

/*
  A table while its rows load. The header row keeps its labels so the columns do not jump.
  `columns` is the relative width of each column; `headers` the labels, in the same order.
*/
const props = defineProps({
  headers: { type: Array, default: () => [] },
  columns: { type: Array, default: () => [1, 2, 3, 1] },
  rows: { type: Number, default: 5 },
})

const widths = props.columns

const cellWidth = (i) => (i === widths.length - 1 ? '40%' : `${55 + ((i * 17) % 35)}%`)
</script>

<style scoped>
.skeleton-table {
  border: 1px solid var(--line);
  background: var(--panel);
}

.skeleton-table__head,
.skeleton-table__row {
  display: flex;
  gap: 12px;
  padding: 10px 12px;
}

.skeleton-table__head {
  border-bottom: 1px solid var(--white);
  color: var(--tx-2);
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.03em;
}

.skeleton-table__row {
  border-bottom: 1px solid var(--line);
  align-items: center;
  min-height: 42px;
}

.skeleton-table__row:last-child {
  border-bottom: 0;
}
</style>

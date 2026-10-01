<template>
  <div class="table-scroll-wrap">
    <p v-if="overflows && mobile" class="table-scroll__hint">Swipe sideways to see every column.</p>
    <div ref="scroller" class="table-scroll">
      <div :style="{ minWidth }">
        <slot />
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useDisplay } from 'vuetify'

/* Wraps a wide table so it scrolls sideways instead of breaking the page, with a hint on phones when it overflows. */
defineProps({
  minWidth: { type: String, default: '' },
})

const { smAndDown: mobile } = useDisplay()
const scroller = ref(null)
const overflows = ref(false)
let observer = null

const measure = () => {
  const el = scroller.value
  overflows.value = !!el && el.scrollWidth > el.clientWidth + 1
}

onMounted(() => {
  measure()
  if (typeof ResizeObserver !== 'undefined' && scroller.value) {
    observer = new ResizeObserver(measure)
    observer.observe(scroller.value)
  }
})

onBeforeUnmount(() => {
  observer?.disconnect()
})
</script>

<style scoped>
.table-scroll {
  overflow-x: auto;
  -webkit-overflow-scrolling: touch;
}

.table-scroll__hint {
  margin: 0 0 6px;
  font-size: 12px;
  color: var(--tx-3);
}
</style>

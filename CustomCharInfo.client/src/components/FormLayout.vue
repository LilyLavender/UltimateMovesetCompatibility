<template>
  <div ref="root" class="form-layout" :class="{ 'form-layout--has-nav': sections.length }">
    <nav v-if="sections.length" class="form-layout__nav" aria-label="Form sections">
      <a
        v-for="s in sections"
        :key="s.id"
        :href="`#${s.id}`"
        class="form-layout__link"
        :class="{ 'form-layout__link--on': active === s.id }"
        @click.prevent="jump(s.id)"
      >
        {{ s.label }}
      </a>
    </nav>
    <div class="form-layout__body">
      <slot />
    </div>
    <div v-if="$slots.savebar" class="form-layout__savebar savebar">
      <slot name="savebar" />
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount, watch } from 'vue'

/*
  The frame for a long form: a section list that follows the reader on the left (a strip on mobile),
  the FormSections in the default slot, and a sticky save bar at the bottom.
  `sections` is [{ id, label }] and each id must match a FormSection's id.
*/
const props = defineProps({
  sections: { type: Array, default: () => [] },
})

const root = ref(null)
const active = ref(props.sections[0]?.id ?? '')
let observer = null

const jump = (id) => {
  active.value = id
  document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

const observe = () => {
  observer?.disconnect()
  if (typeof IntersectionObserver === 'undefined' || !props.sections.length) return
  observer = new IntersectionObserver(
    (entries) => {
      const hit = entries.filter((e) => e.isIntersecting).sort((a, b) => a.time - b.time)[0]
      if (hit) active.value = hit.target.id
    },
    { rootMargin: '-15% 0px -70% 0px' }
  )
  for (const s of props.sections) {
    const el = document.getElementById(s.id)
    if (el) observer.observe(el)
  }
}

onMounted(observe)
watch(() => props.sections, observe, { deep: true })
onBeforeUnmount(() => observer?.disconnect())
</script>

<style scoped>
.form-layout {
  position: relative;
  display: grid;
  grid-template-columns: 1fr;
  gap: 24px;
}

.form-layout--has-nav {
  grid-template-columns: 200px 1fr;
  column-gap: 40px;
}

.form-layout__nav {
  position: sticky;
  top: 20px;
  align-self: start;
  display: flex;
  flex-direction: column;
}

.form-layout__link {
  padding: 8px 12px;
  border-left: 3px solid var(--line);
  color: var(--tx-2);
  font-size: 14px;
  font-weight: 500;
  text-decoration: none;
  transition:
    color var(--dur-fast) var(--ease),
    border-color var(--dur-fast) var(--ease);
}

.form-layout__link:hover {
  color: var(--white);
}

.form-layout__link--on {
  border-left-color: var(--white);
  color: var(--white);
  font-weight: 600;
}

.form-layout__body {
  display: flex;
  flex-direction: column;
  gap: 28px;
  min-width: 0;
}

.form-layout__savebar {
  grid-column: 1 / -1;
}

@media (max-width: 959px) {
  .form-layout--has-nav {
    grid-template-columns: 1fr;
  }

  .form-layout__nav {
    position: static;
    flex-direction: row;
    overflow-x: auto;
    border-bottom: 1px solid var(--line-2);
    scrollbar-width: none;
  }

  .form-layout__nav::-webkit-scrollbar {
    display: none;
  }

  .form-layout__link {
    padding: 8px 12px;
    border-left: 0;
    border-bottom: 3px solid transparent;
    margin-bottom: -1px;
    white-space: nowrap;
  }

  .form-layout__link--on {
    border-bottom-color: var(--white);
  }
}
</style>

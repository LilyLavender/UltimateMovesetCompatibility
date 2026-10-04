<template>
  <span class="log-state-pill">
    <StatusTag :state="state" />
    <v-tooltip v-if="log" activator="parent" location="top" content-class="log-state-pill__tip">
      <!-- The width cap sits on an inner block, because Vuetify sets its own inline max-width on the tooltip itself. -->
      <div class="log-state-pill__body">
        <p class="log-state-pill__who">
          <strong>{{ log.userName ?? 'Unknown' }}</strong>
          <span class="log-state-pill__when">{{ when }} UTC</span>
        </p>
        <p class="log-state-pill__notes">{{ notes }}</p>
      </div>
    </v-tooltip>
  </span>
</template>

<script setup>
import { computed } from 'vue'
import { format } from 'date-fns'
import StatusTag from '@/components/StatusTag.vue'

/*
  An acceptance-state pill that explains itself on hover: the newest log's author and date on one line,
  its notes on the next, laid out like a row of the action log list. Takes a /logs/latest row.
  Sits inside the card tag overlays, which let clicks through, so it takes the pointer back itself.
*/
const props = defineProps({
  state: { type: Number, required: true },
  log: { type: Object, default: null },
})

const when = computed(() => format(new Date(props.log.createdAt), 'PPpp'))

const notes = computed(() => {
  const text = props.log.notes?.trim()
  return text ? `"${text}"` : 'No notes'
})
</script>

<style scoped>
.log-state-pill {
  display: inline-flex;
  pointer-events: auto;
}
</style>

<style>
/* The tooltip body is teleported out of this component, so its styles are global. */
.v-tooltip.v-overlay > .v-overlay__content.log-state-pill__tip {
  padding: 8px 10px;
}

.log-state-pill__body {
  max-width: 320px;
}

.log-state-pill__who {
  display: flex;
  align-items: baseline;
  gap: 8px;
  margin: 0;
  font-size: 12px;
}

.log-state-pill__when {
  color: var(--tx-2);
}

.log-state-pill__notes {
  margin: 4px 0 0;
  font-style: italic;
  font-size: 12px;
  color: var(--tx-2);
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}
</style>

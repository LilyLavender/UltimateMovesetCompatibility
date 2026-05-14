<template>
  <v-card class="pa-3" color="#2e2e2e">
    <v-row>
      <v-col cols="12" sm="6">
        <!-- User & Date -->
        <strong>{{ log.user.userName }}</strong> | {{ formatDate(log.createdAt) }} UTC
        <!-- Item (if moveset) -->
        <div v-if="log.itemType.itemTypeId === 1">
          <h3>
            Moveset:
            <router-link
              v-if="log.item?.movesetId"
              :to="{ name: 'MovesetDetail', params: { movesetId: log.item.movesetId } }"
              class="unvisitable"
            >
              {{ log.item.moddedCharName }}
            </router-link>
            <span v-else>{{ log.item?.moddedCharName ?? '(deleted)' }}</span>
          </h3>
        </div>

        <!-- Item (if user) -->
        <div v-else-if="log.itemType.itemTypeId === 2">
          <h3>
            User:
            <router-link
              v-if="log.item?.modderId"
              :to="{ name: 'ModderDetail', params: { id: log.item.modderId } }"
              class="unvisitable"
            >
              {{ log.item.name }}
            </router-link>
            <span v-else>{{ log.item?.name ?? '(deleted)' }}</span>

            <router-link
              v-if="pendingUser && log.item?.modderId"
              :to="{ name: 'EditModder', params: { id: log.item.modderId } }"
              class="unvisitable ml-1 small-link"
            >
              <v-icon>mdi-pencil</v-icon>
              Edit
            </router-link>
          </h3>
        </div>

        <!-- Item (if series) -->
        <div v-if="log.itemType.itemTypeId === 3">
          <h3>
            Series:
            {{ log.item?.seriesName ?? '(deleted)' }}
            <router-link
              v-if="log.item?.seriesId"
              :to="{ name: 'EditSeries', params: { seriesId: log.item.seriesId } }"
              class="unvisitable ml-1 small-link"
            >
              <v-icon>mdi-pencil</v-icon>
              Edit
            </router-link>
          </h3>
        </div>
      </v-col>

      <v-col cols="12" sm="6">
        <!-- Acceptance -->
        <span class="pa-1 px-2 rounded" :style="acceptanceStyle">
          {{ log.acceptanceState.acceptanceStateName }}
        </span>

        <!-- Notes -->
        <p v-if="log.notes" class="mt-1 fst-italic">"{{ log.notes }}"</p>

        <!-- Diff -->
        <div v-if="parsedDiff.length" class="mt-2 diff-list">
          <div v-for="change in parsedDiff" :key="change.field" class="diff-row">
            <span class="diff-field">{{ change.field }}</span>
            <template v-if="change.old && change.new">
              <span class="diff-old">{{ change.old }}</span>
              <span class="diff-arrow">→</span>
              <span class="diff-new">{{ change.new }}</span>
            </template>
            <template v-else-if="change.old">
              <span class="diff-old">{{ change.old }}</span>
            </template>
            <template v-else>
              <span class="diff-new">{{ change.new }}</span>
            </template>
          </div>
        </div>
      </v-col>
    </v-row>
  </v-card>
</template>

<script setup>
import { computed } from 'vue'
import { format } from 'date-fns'

const props = defineProps({
  log: Object,
  isAdmin: Boolean
})

const formatDate = (date) => {
  return format(new Date(date), 'PPpp')
}

const pendingUser = computed(() => [3, 4].includes(props.log.acceptanceState.acceptanceStateId))

const parsedDiff = computed(() => {
  if (!props.log.diff) return []
  try { return JSON.parse(props.log.diff) } catch { return [] }
})

const acceptanceStyle = computed(() => {
  const id = props.log.acceptanceState.acceptanceStateId
  const bgColors = {
    1: 'rgb(187, 224, 236)', // Pending Admin (Soft)
    2: 'rgb(52, 194, 241)', // Pending Admin (Hard)
    3: 'rgb(241, 241, 142)', // Pending User (Soft)
    4: 'rgb(241, 241, 52)', // Pending User (Hard)
    5: 'rgb(52, 241, 52)', // Accepted
    6: 'rgb(241, 52, 52)',  // Rejected
    7: 'rgb(52, 241, 52)', // Auto-Accepted
  }

  return {
    backgroundColor: bgColors[id],
    color: 'rgb(20, 20, 20)',
    fontWeight: 'bold'
  }
})
</script>

<style scoped>
.small-link {
  font-size: 1rem;
}
.diff-list {
  font-size: 0.8rem;
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.diff-row {
  display: flex;
  gap: 6px;
  align-items: baseline;
  flex-wrap: wrap;
}
.diff-field {
  font-weight: bold;
  min-width: 90px;
  color: #ccc;
}
.diff-old {
  color: #f08080;
  text-decoration: line-through;
  word-break: break-all;
}
.diff-arrow {
  color: #888;
}
.diff-new {
  color: #90ee90;
  word-break: break-all;
}
</style>
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
              :to="{ name: 'MovesetDetail', params: { movesetId: log.item?.movesetId } }"
              class="unvisitable"
            >
              {{ log.item?.moddedCharName }}
            </router-link>
          </h3>
        </div>

        <!-- Item (if user) -->
        <div v-else-if="log.itemType.itemTypeId === 2">
          <h3>
            User: 
            <router-link
              :to="{ name: 'ModderDetail', params: { id: log.item?.modderId } }"
              class="unvisitable"
            >
              {{ log.item?.name }}
            </router-link>

            <router-link
              v-if="pendingUser"
              :to="{ name: 'EditModder', params: { id: log.item?.modderId } }"
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
            {{ log.item?.seriesName }}
            <!-- todo v-if? -->
            <router-link
              v-if="true"
              :to="{ name: 'EditSeries', params: { seriesId: log.item?.seriesId } }"
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
        <p v-if="log.notes">"{{ log.notes }}"</p>
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
</style>
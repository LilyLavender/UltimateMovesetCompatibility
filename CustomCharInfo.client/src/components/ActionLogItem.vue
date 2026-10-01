<template>
  <article class="panel log-item">
    <div class="log-item__main">
      <!-- User & Date -->
      <p class="log-item__who">
        <strong>{{ log.user.userName }}</strong>
        <span class="faint">{{ formatDate(log.createdAt) }} UTC</span>
      </p>

      <!-- Item (if moveset) -->
      <h4 v-if="log.itemType.itemTypeId === ItemType.Moveset" class="log-item__what">
        <span class="log-item__kind">Moveset</span>
        <router-link
          v-if="log.item?.movesetId"
          :to="{ name: 'MovesetDetail', params: { movesetId: log.item.movesetId } }"
        >
          {{ log.item.moddedCharName }}
        </router-link>
        <span v-else>{{ log.item?.moddedCharName ?? '(deleted)' }}</span>
      </h4>

      <!-- Item (if user) -->
      <h4 v-else-if="log.itemType.itemTypeId === ItemType.Modder" class="log-item__what">
        <span class="log-item__kind">User</span>
        <router-link
          v-if="log.item?.modderId"
          :to="{ name: 'ModderDetail', params: { id: log.item.modderId } }"
        >
          {{ log.item.name }}
        </router-link>
        <span v-else>{{ log.item?.name ?? '(deleted)' }}</span>
        <router-link
          v-if="pendingUser && log.item?.modderId"
          :to="{ name: 'EditModder', params: { id: log.item.modderId } }"
          class="log-item__edit"
        >
          <v-icon size="14">mdi-pencil</v-icon>
          Edit
        </router-link>
      </h4>

      <!-- Item (if series) -->
      <h4 v-else-if="log.itemType.itemTypeId === ItemType.Series" class="log-item__what">
        <span class="log-item__kind">Series</span>
        {{ log.item?.seriesName ?? '(deleted)' }}
        <router-link
          v-if="log.item?.seriesId"
          :to="{ name: 'EditSeries', params: { seriesId: log.item.seriesId } }"
          class="log-item__edit"
        >
          <v-icon size="14">mdi-pencil</v-icon>
          Edit
        </router-link>
      </h4>

      <!-- Item (if hook) -->
      <h4 v-else-if="log.itemType.itemTypeId === ItemType.Hook" class="log-item__what">
        <span class="log-item__kind">Hook</span>
        <router-link v-if="log.item?.hookId" :to="{ name: 'Hooks' }" class="mono">
          {{ formatOffset(log.item.offset) }}
        </router-link>
        <span v-else class="mono">{{
          log.item?.offset ? formatOffset(log.item.offset) : '(deleted)'
        }}</span>
        <router-link
          v-if="log.item?.hookId"
          :to="{ name: 'EditHook', params: { hookId: log.item.hookId } }"
          class="log-item__edit"
        >
          <v-icon size="14">mdi-pencil</v-icon>
          Edit
        </router-link>
      </h4>

      <!-- Item (if plugin) -->
      <h4 v-else-if="log.itemType.itemTypeId === ItemType.Plugin" class="log-item__what">
        <span class="log-item__kind">Plugin</span>
        {{ log.item?.label ?? '(deleted)' }}
      </h4>
    </div>

    <div class="log-item__side">
      <!-- Acceptance -->
      <StatusTag :state="log.acceptanceState.acceptanceStateId">
        {{ log.acceptanceState.acceptanceStateName }}
      </StatusTag>

      <!-- Notes -->
      <p v-if="log.notes" class="log-item__notes">"{{ log.notes }}"</p>

      <!-- Diff -->
      <div v-if="parsedDiff.length" class="diff-list">
        <div v-for="change in parsedDiff" :key="change.field" class="diff-row">
          <span class="diff-field">{{ change.field }}</span>
          <template v-if="change.old && change.new">
            <span class="diff-old">{{ change.old }}</span>
            <span class="diff-arrow" aria-hidden="true">to</span>
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
    </div>
  </article>
</template>

<script setup>
import { computed } from 'vue'
import { format } from 'date-fns'
import { ItemType, AcceptanceState } from '@/globals'
import { formatOffset } from '@/services/offsets'
import StatusTag from '@/components/StatusTag.vue'

const props = defineProps({
  log: { type: Object, required: true },
  isAdmin: Boolean,
})

const formatDate = (date) => {
  return format(new Date(date), 'PPpp')
}

const pendingUser = computed(() =>
  [AcceptanceState.PendingUserSoft, AcceptanceState.PendingUserHard].includes(
    props.log.acceptanceState.acceptanceStateId
  )
)

const parsedDiff = computed(() => {
  if (!props.log.diff) return []
  try {
    return JSON.parse(props.log.diff)
  } catch {
    return []
  }
})
</script>

<style scoped>
.log-item {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  gap: 8px 20px;
  padding: 12px 14px;
}

.log-item__who {
  margin: 0 0 4px;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  font-size: 13px;
}

.log-item__what {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 8px;
  margin: 0;
  padding-right: 60px;
  font-family: var(--font-condensed);
  font-size: 17px;
  font-weight: 700;
}

.log-item__what a {
  text-decoration: none;
}

.log-item__what a:hover {
  text-decoration: underline;
}

.log-item__kind {
  font-family: var(--font-body);
  font-size: 12px;
  font-weight: 600;
  color: var(--tx-3);
  letter-spacing: 0.03em;
  text-transform: uppercase;
}

.log-item__edit {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  font-family: var(--font-body);
  font-size: 12px;
  font-weight: 500;
  color: var(--tx-2);
}

.log-item__side {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 6px;
}

.log-item__notes {
  margin: 0;
  font-style: italic;
  font-size: 14px;
  color: var(--tx-2);
}

.diff-list {
  font-size: 13px;
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
  font-weight: 600;
  min-width: 90px;
  color: var(--tx-2);
}

.diff-old {
  color: var(--err);
  text-decoration: line-through;
  word-break: break-all;
}

.diff-arrow {
  color: var(--tx-3);
  font-size: 12px;
}

.diff-new {
  color: var(--ok);
  word-break: break-all;
}

@media (max-width: 599px) {
  .log-item {
    grid-template-columns: 1fr;
  }
}
</style>

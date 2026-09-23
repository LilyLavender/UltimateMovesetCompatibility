<template>
  <router-link :to="to" class="tile" :class="{ 'tile--large': large }">
    <v-icon class="tile__icon" :size="large ? 26 : 20">{{ icon }}</v-icon>
    <span class="tile__text">
      <b class="tile__label">{{ label }}</b>
      <span v-if="description" class="tile__description">{{ description }}</span>
    </span>
    <span v-if="badge" class="tile__badge" :class="`tile__badge--${badgeTone}`">{{ badge }}</span>
  </router-link>
</template>

<script setup>
/* A whole-tile link with an icon, a name, a one-line description, and an optional count. */
defineProps({
  to: { type: [Object, String], required: true },
  icon: { type: String, default: 'mdi-arrow-right' },
  label: { type: String, required: true },
  description: { type: String, default: '' },
  badge: { type: [Number, String], default: '' },
  badgeTone: { type: String, default: 'admin' },
  large: { type: Boolean, default: false },
})
</script>

<style scoped>
.tile {
  display: flex;
  gap: 12px;
  padding: 14px;
  border: 1px solid var(--line);
  background: var(--panel);
  color: var(--tx);
  text-decoration: none;
  transition:
    background-color var(--dur-fast) var(--ease),
    color var(--dur-fast) var(--ease);
}

.tile__icon {
  flex: none;
  margin-top: 2px;
  color: var(--white);
}

.tile__text {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.tile__label {
  font-weight: 600;
  font-size: 14px;
}

.tile__description {
  color: var(--tx-3);
  font-size: 12.5px;
  line-height: 1.35;
}

.tile__badge {
  margin-left: auto;
  align-self: flex-start;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 18px;
  height: 18px;
  padding: 0 5px;
  font-size: 11px;
  font-weight: 700;
  line-height: 1;
  color: #141414;
}

.tile__badge--admin {
  background: var(--state-pending-admin-hard);
}

.tile__badge--user {
  background: var(--state-pending-user-hard);
}

.tile:hover,
.tile:focus-visible {
  background: var(--white);
  color: #000;
}

.tile:hover .tile__icon,
.tile:focus-visible .tile__icon {
  color: #000;
}

.tile:hover .tile__description,
.tile:focus-visible .tile__description {
  color: #444;
}

.tile--large {
  align-items: center;
  padding: 18px 20px;
}

.tile--large .tile__label {
  font-size: 18px;
}

.tile--large .tile__description {
  font-size: 13px;
}
</style>

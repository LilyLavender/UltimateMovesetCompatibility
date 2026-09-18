<template>
  <v-snackbar
    v-model="notifyState.visible"
    :color="notifyState.color"
    :timeout="notifyState.timeout"
    location="bottom"
    multi-line
    @update:model-value="onToggle"
  >
    <span class="snackbar-text">{{ notifyState.message }}</span>
    <template #actions>
      <v-btn variant="text" icon="mdi-close" @click="notifyState.visible = false" />
    </template>
  </v-snackbar>
</template>

<script setup>
import { notifyState, showNext } from '@/composables/useNotify'

// Vuetify closes the snackbar on timeout; a short gap before the next one keeps the transition visible.
const onToggle = (visible) => {
  if (!visible) setTimeout(showNext, 250)
}
</script>

<style scoped>
.snackbar-text {
  white-space: pre-line;
}
</style>

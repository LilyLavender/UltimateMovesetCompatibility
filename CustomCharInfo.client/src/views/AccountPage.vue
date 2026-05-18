<template>
  <div class="p-6 mx-auto">
    <h1 class="mb-4 page-title no-select">User Settings</h1>
    <div class="account-sections">
      <!-- Authorization -->
      <AuthPanel />

      <!-- Community note -->
      <blockquote class="community-note">
        <strong>A reminder:</strong> someone's contributions to this community, no matter how celebrated, don't reflect their value as a person. Please treat people accordingly.
      </blockquote>

      <!-- Action Log -->
      <v-card class="pa-4" width="1020px" v-if="isLoggedIn">
        <div>
          <ActionLogList />
        </div>
      </v-card>

      <!-- Account actions: change password etc -->
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import AuthPanel from '@/components/AuthPanel.vue'
import ActionLogList from '@/components/ActionLogList.vue'

const isLoggedIn = computed(() => !!localStorage.getItem('token'))
</script>

<style scoped>
.page-title {
  font-size: 5em;
  margin-top: 0.5em;
}

.account-sections > *:not(:last-child) {
  margin-bottom: 2em;
}

.account-sections > *:last-child {
  margin-bottom: 1em;
}

.v-card {
  margin: 0 auto;
  background-color: #1e1e1e;
  color: #dedede;
}

.community-note {
  max-width: 620px;
  margin: auto;
  padding: 0.6em 1em;
  border-left: 4px solid #555;
  background-color: #1a1a1a80;
  color: #888;
  font-size: 0.85em;
  font-style: italic;
  line-height: 1.6;
  border-radius: 0 4px 4px 0;
}

.community-note > strong {
  font-size: 1.2em;
}
</style>
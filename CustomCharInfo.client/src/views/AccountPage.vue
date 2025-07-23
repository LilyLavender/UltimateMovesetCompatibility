<template>
  <div class="p-6 mx-auto">
    <h1 class="mb-4 page-title no-select">User Settings</h1>
    <div class="account-sections">
      <!-- Authorization -->
      <AuthPanel />

      <!-- Link to photo submissions -->

      <!-- Account actions: change password etc -->
    </div>
  </div>
</template>

<script setup>
import AuthPanel from '@/components/AuthPanel.vue'
</script>

<style scoped>
.page-title {
  font-size: 5em;
  margin-top: 0.5em;
}

.account-sections > *:not(:last-of-type) {
  margin-bottom: 2em;
}

.account-sections > *:last-of-type {
  margin-bottom: 1em;
}
</style>
<template>
  <v-container max-width="1020px">
    <h1 class="mb-4 page-title no-select">Admin Portal</h1>

    <div class="admin-links">
      <router-link
        :to="{ name: 'AdminAccepter' }"
        class="unvisitable ml-2 admin-link"
      >
        <v-icon left>mdi-shield-check</v-icon>
        Admin Accepter
      </router-link>

      <router-link
        :to="{ name: 'AddBlogPost' }"
        class="unvisitable ml-2 admin-link"
      >
        <v-icon left>mdi-post</v-icon>
        Add Blog Post
      </router-link>

      <router-link
        :to="{ name: 'UserList' }"
        class="unvisitable ml-2 admin-link"
      >
        <v-icon left>mdi-account-group</v-icon>
        All users
      </router-link>
    </div>

    <ActionLogList />
  </v-container>
</template>

<script setup>
import ActionLogList from '@/components/ActionLogList.vue'
</script>

<style scoped>
.page-title {
  font-size: 5em;
  margin-top: 0.5em;
}

.admin-links {
  margin: 0 auto;
  width: fit-content;
}

.admin-links > * {
  background-color: #1e1e1e;
  padding: 0.5em 1em;
  margin: 0 1em;
  border-radius: 6px;
  text-decoration: none;
}

.admin-link .v-icon {
  margin-top: -4px;
}
</style>
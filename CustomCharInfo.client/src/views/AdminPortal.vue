<template>
  <v-container max-width="1020px">
    <h1 class="mb-4 page-title no-select">Admin Portal</h1>

    <router-link
      :to="{ name: 'AdminAccepter' }"
      class="unvisitable ml-2 admin-link"
    >
      Admin Accepter
    </router-link>

    <router-link
      :to="{ name: 'AddBlogPost' }"
      class="unvisitable ml-2 admin-link"
    >
      Add Blog Post
    </router-link>


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
</style>
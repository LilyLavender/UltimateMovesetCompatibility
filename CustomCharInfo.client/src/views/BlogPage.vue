<template>
  <v-container>
    <h1 class="mb-4 page-title no-select">Blog</h1>

    <v-alert v-if="error" type="error" class="mb-4">{{ error }}</v-alert>

    <BlogPost v-for="post in blogPosts" :key="post.blogPostId" :post="post" />
  </v-container>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import BlogPost from '@/components/BlogPost.vue'
import api from '@/services/api'

const blogPosts = ref([])
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    const response = await api.get('/blog')
    blogPosts.value = response.data.sort((a, b) => new Date(b.postedDate) - new Date(a.postedDate))
  } catch (err) {
    error.value = 'Failed to load blog posts.'
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.page-title {
  font-size: 5em;
  margin-top: 0.5em;
}
</style>
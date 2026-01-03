<template>
  <v-container>
    <h1 class="mb-4 page-title no-select">Blog</h1>

    <v-alert v-if="error" type="error" class="mb-4">{{ error }}</v-alert>

    <div v-else>
      <p class="router-link">
        <i class="mdi mdi-arrow-right-bottom"></i>
        Want to add to the blog? Contact Lily
      </p>

      <BlogPost v-for="post in blogPosts" :key="post.blogPostId" :post="post" />
    </div>
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

.router-link {
  margin-top: -1.75em;
  margin-bottom: 1em;
}
</style>
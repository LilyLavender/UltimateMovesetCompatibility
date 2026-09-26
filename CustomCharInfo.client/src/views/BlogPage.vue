<template>
  <PageShell title="Blog">
    <template #subnav>
      <SubNav section="blog" label="Blog" />
    </template>

    <p v-if="error" class="note note--err">{{ error }}</p>

    <template v-else>
      <p class="blog-hint">Want to add to the blog? Contact an admin.</p>
      <div v-if="loading" class="blog-list" aria-busy="true">
        <SkeletonPanel :lines="4" />
        <SkeletonPanel :lines="3" />
      </div>
      <div v-else-if="blogPosts.length" class="blog-list reveal">
        <BlogPost v-for="post in blogPosts" :key="post.blogPostId" :post="post" />
      </div>
      <EmptyState v-else message="No posts yet." />
    </template>
  </PageShell>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import PageShell from '@/components/PageShell.vue'
import SubNav from '@/components/SubNav.vue'
import SkeletonPanel from '@/components/SkeletonPanel.vue'
import EmptyState from '@/components/EmptyState.vue'
import BlogPost from '@/components/BlogPost.vue'
import api from '@/services/api'

const blogPosts = ref([])
const loading = ref(true)
const error = ref('')

onMounted(async () => {
  try {
    const response = await api.get('/blog')
    blogPosts.value = response.data.sort((a, b) => new Date(b.postedDate) - new Date(a.postedDate))
  } catch {
    error.value = 'Could not load the blog. Try again in a moment.'
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.blog-hint {
  margin: 0 0 18px;
  color: var(--tx-3);
  font-size: 13px;
}

.blog-list {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.note {
  padding: 10px 14px;
  border: 1px solid var(--line-2);
  border-left: 4px solid var(--tx-3);
  background: var(--panel);
  color: var(--tx-2);
  font-size: 14px;
}

.note--err {
  border-left-color: var(--err);
}
</style>

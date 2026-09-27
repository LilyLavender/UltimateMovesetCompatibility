<template>
  <article class="panel blog-post">
    <h2 class="blog-post__title">{{ props.post.blogTitle }}</h2>
    <p class="blog-post__meta">
      <HudReadout :value="props.post.authorUserName" tone="info" />
      <span>{{ formatDate(props.post.postedDate) }}</span>
    </p>
    <!-- Content is DOMPurify-sanitized -->
    <!-- eslint-disable-next-line vue/no-v-html -->
    <div class="blog-post__text" v-html="renderedText"></div>
    <img
      v-if="props.post.blogImageUrl"
      :src="getFullImageUrl(props.post.blogImageUrl)"
      class="blog-post__image"
      alt=""
    />
  </article>
</template>

<script setup>
import { computed } from 'vue'
import { format } from 'date-fns'
import { marked } from 'marked'
import DOMPurify from 'dompurify'
import HudReadout from '@/components/HudReadout.vue'

const apiUrl = import.meta.env.VITE_API_URL

const props = defineProps({
  post: {
    type: Object,
    required: true,
  },
})

const formatDate = (date) => format(new Date(date), 'PPp')

const getFullImageUrl = (path) => {
  if (!path) return null
  return path.startsWith('/') ? `${apiUrl}${path}` : path
}

const renderedText = computed(() => DOMPurify.sanitize(marked.parse(props.post.blogText || '')))
</script>

<style scoped>
.blog-post {
  padding: 22px 24px;
}

.blog-post__title {
  margin: 0 0 8px;
  font-size: 28px;
  text-transform: none;
}

.blog-post__meta {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 10px;
  margin: 0 0 16px;
  color: var(--tx-3);
  font-size: 13px;
}

.blog-post__text {
  font-size: 16px;
  line-height: 1.6;
}

.blog-post__text :deep(p) {
  margin-bottom: 0.75em;
}

.blog-post__text :deep(ol),
.blog-post__text :deep(ul) {
  padding-left: 1.5em;
  margin-bottom: 0.75em;
}

.blog-post__text :deep(li) {
  margin-bottom: 0.2em;
}

.blog-post__text :deep(a) {
  text-decoration: underline;
}

.blog-post__image {
  display: block;
  margin: 12px auto 4px;
  max-height: 40vh;
  max-width: 100%;
  border: 1px solid var(--line);
}
</style>

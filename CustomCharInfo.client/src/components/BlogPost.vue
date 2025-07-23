<template>
  <v-card class="mb-5 blog-card" color="#161616">
    <v-card-title class="blog-title">
      {{ props.post.blogTitle }}
    </v-card-title>
    <v-card-text>
      <p v-html="props.post.blogText" class="blog-text"></p>
    </v-card-text>
    <div>
      <v-img v-if="props.post.blogImageUrl" :src="getFullImageUrl(props.post.blogImageUrl)" class="blog-image" />
    </div>
    <v-card-subtitle class="blog-subtitle">
      {{ props.post.authorUserName }} | {{ formatDate(props.post.postedDate) }} UTC
    </v-card-subtitle>
  </v-card>
</template>

<script setup>
import { computed } from 'vue'
import { format } from 'date-fns'

const apiUrl = import.meta.env.VITE_API_URL

const props = defineProps({
  post: {
    type: Object,
    required: true,
  },
})

const formatDate = (date) => {
  return format(new Date(date), 'PPpp')
}

const getFullImageUrl = (path) => {
  if (!path) return null
  return path.startsWith('/') ? `${apiUrl}${path}` : path
}
</script>

<style scoped>
.blog-card {
  color: #dedede;
}
.blog-title {
  font-size: 2.8em;
}
.blog-text {
  font-size: 1.3em;
}
.blog-subtitle {
  margin-bottom: 0.4em;
}
.blog-image {
  width: 60%;
  margin: 1em auto;
}
</style>

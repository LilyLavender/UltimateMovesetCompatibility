<template>
  <v-container max-width="1020px">
    <!-- Header -->
    <h1 class="mb-4">Add Blog Post</h1>

    <!-- Blog Form -->
    <section>
      <v-row>
        <!-- Blog Title -->
        <v-col cols="12">
          <v-text-field
            variant="outlined"
            v-model="form.blogTitle"
            label="Post Title"
          />
        </v-col>

        <!-- Blog Text -->
        <v-col cols="12">
          <v-textarea
            variant="outlined"
            v-model="form.blogText"
            label="Post Content"
            auto-grow
            rows="5"
          />
        </v-col>

        <!-- Blog Image Upload -->
        <v-col cols="12" sm="4">
          <v-file-input
            variant="outlined"
            label="Post Image"
            accept="image/*"
            @change="uploadImage"
          />
        </v-col>
        <v-col cols="12" sm="8">
          <v-img
            :src="getFullImageUrl(form.blogImageUrl)"
          />
        </v-col>
      </v-row>
    </section>

    <!-- Submit -->
    <div class="d-flex justify-end">
      <v-btn @click="submit" class="btn submit-button">
        Add Blog Post
      </v-btn>
    </div>
  </v-container>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import api from '@/services/api'

const router = useRouter()
const apiUrl = import.meta.env.VITE_API_URL

const form = ref({
  blogTitle: '',
  blogText: '',
  blogImageUrl: '',
})

const getFullImageUrl = (path) => {
  if (!path) return null
  return path.startsWith('/') ? `${apiUrl}${path}` : path
}

const uploadImage = async (event) => {
  const file = event?.target?.files?.[0]
  if (!file) {
    alert("Please select a valid image.")
    return
  }

  const formData = new FormData()
  formData.append("file", file)

  try {
    const res = await api.post("/upload/blog-image", formData, {
      headers: { "Content-Type": "multipart/form-data" },
    })

    const timestamp = new Date().getTime()
    const imageUrlWithCacheBust = res.data.url + `?t=${timestamp}`
    form.value.blogImageUrl = imageUrlWithCacheBust
  } catch (err) {
    console.error("Image upload failed:", err.response?.data || err.message)
    alert("Failed to upload blog image.")
  }
}

const submit = async () => {
  try {
    await api.post("/blog", {
      blogTitle: form.value.blogTitle,
      blogText: form.value.blogText,
      blogImageUrl: form.value.blogImageUrl,
    })
    router.push("/blog")
  } catch (err) {
    console.error("Blog post failed:", err.response?.data || err.message)
    alert("Failed to post blog.")
  }
}
</script>

<style scoped>
section {
  margin-bottom: 2rem;
  background-color: #1e1e1e;
  padding: 1em;
  border-radius: 10px;
}
h1 {
  font-size: 3.25em;
}
.submit-button {
  background-color: #1e1e1e;
  color: #e2e2e2;
}
</style>

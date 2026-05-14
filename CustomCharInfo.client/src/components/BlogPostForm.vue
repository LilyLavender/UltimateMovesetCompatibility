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
            hide-details
          />
        </v-col>

        <!-- Blog Text + Preview -->
        <v-col cols="12">
          <div class="editor-header">
            <span class="editor-label">Post Content</span>
            <div class="tab-group">
              <button
                class="tab-btn"
                :class="{ active: tab === 'write' }"
                @click="tab = 'write'"
                type="button"
              >Write</button>
              <button
                class="tab-btn"
                :class="{ active: tab === 'preview' }"
                @click="tab = 'preview'"
                type="button"
              >Preview</button>
            </div>
          </div>

          <v-textarea
            v-if="tab === 'write'"
            variant="outlined"
            v-model="form.blogText"
            placeholder="Markdown is supported."
            auto-grow
            rows="5"
            hide-details
          />
          <div v-else class="preview-box">
            <div
              v-if="renderedPreview"
              v-html="renderedPreview"
              class="preview-content"
            />
            <span v-else class="preview-empty">Nothing to preview.</span>
          </div>
        </v-col>

        <!-- Blog Image URL -->
        <v-col cols="12" sm="4">
          <v-text-field
            variant="outlined"
            v-model="form.blogImageUrl"
            label="Post Image URL"
            placeholder="https://example.com/image.png"
          />
        </v-col>
        <v-col cols="12" sm="8">
          <v-img
            v-if="form.blogImageUrl"
            :src="getFullImageUrl(form.blogImageUrl)"
          />
        </v-col>
      </v-row>
    </section>

    <!-- Submit -->
    <div class="d-flex justify-end">
      <v-btn @click="submit" class="submit-button">
        Add Blog Post
      </v-btn>
    </div>
  </v-container>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { marked } from 'marked'
import api from '@/services/api'

const router = useRouter()
const apiUrl = import.meta.env.VITE_API_URL

const tab = ref('write')

const form = ref({
  blogTitle: '',
  blogText: '',
  blogImageUrl: '',
})

const renderedPreview = computed(() => marked.parse(form.value.blogText || ''))

const getFullImageUrl = (path) => {
  if (!path) return null
  return path.startsWith('/') ? `${apiUrl}${path}` : path
}

const submit = async () => {
  if (!form.value.blogTitle?.trim() || !form.value.blogText?.trim()) {
    alert("Blog Title and Blog Text are required.")
    return
  }

  try {
    await api.post("/blog", {
      blogTitle: form.value.blogTitle,
      blogText: form.value.blogText,
      blogImageUrl: form.value.blogImageUrl,
    })
    router.push("/blog")
  } catch (err) {
    console.error("Submit failed:", JSON.stringify(err.response?.data) || err.message)
    alert("Failed to post blog.\n\n" + (JSON.stringify(err.response?.data) || err.message))
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
  background-color: #2e2e2e;
  color: #e2e2e2;
  text-transform: unset;
}

.editor-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 6px;
}
.editor-label {
  font-size: 0.9rem;
  color: #9e9e9e;
}
.tab-group {
  display: flex;
  border: 1px solid #444;
  border-radius: 6px;
  overflow: hidden;
}
.tab-btn {
  background: transparent;
  color: #9e9e9e;
  border: none;
  padding: 4px 14px;
  font-size: 0.85rem;
  cursor: pointer;
  transition: background 150ms, color 150ms;
}
.tab-btn:hover {
  background: #2e2e2e;
  color: #e2e2e2;
}
.tab-btn.active {
  background: #2e2e2e;
  color: #e2e2e2;
}

.preview-box {
  border: 1px solid #444;
  border-radius: 4px;
  min-height: 140px;
  padding: 12px 16px;
}
.preview-empty {
  color: #555;
  font-style: italic;
}
.preview-content :deep(p) {
  margin-bottom: 0.75em;
}
.preview-content :deep(ol),
.preview-content :deep(ul) {
  padding-left: 1.5em;
  margin-bottom: 0.75em;
}
.preview-content :deep(li) {
  margin-bottom: 0.2em;
}
</style>

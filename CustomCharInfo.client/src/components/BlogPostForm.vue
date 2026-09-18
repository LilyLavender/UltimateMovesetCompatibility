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
            v-model="form.blogTitle"
            variant="outlined"
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
                type="button"
                @click="tab = 'write'"
              >
                Write
              </button>
              <button
                class="tab-btn"
                :class="{ active: tab === 'preview' }"
                type="button"
                @click="tab = 'preview'"
              >
                Preview
              </button>
            </div>
          </div>

          <v-textarea
            v-if="tab === 'write'"
            v-model="form.blogText"
            variant="outlined"
            placeholder="Markdown is supported."
            auto-grow
            rows="5"
            hide-details
          />
          <div v-else class="preview-box">
            <div v-if="renderedPreview" class="preview-content" v-html="renderedPreview" />
            <span v-else class="preview-empty">Nothing to preview.</span>
          </div>
        </v-col>

        <!-- Blog Image -->
        <v-col cols="12" sm="6">
          <p class="field-label">Post Image</p>
          <ImageUploadField v-model="form.blogImageUrl" />
        </v-col>
      </v-row>
    </section>

    <!-- Submit -->
    <div class="d-flex justify-end">
      <v-btn class="submit-button" :loading="isSubmitting" :disabled="isSubmitting" @click="submit">
        {{ uploadStatus || 'Add Blog Post' }}
      </v-btn>
    </div>
  </v-container>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { marked } from 'marked'
import DOMPurify from 'dompurify'
import api from '@/services/api'
import ImageUploadField from '@/components/ImageUploadField.vue'
import { useImageUpload, isStagedFile } from '@/composables/useImageUpload'

const router = useRouter()

const tab = ref('write')
const isSubmitting = ref(false)
const uploadStatus = ref('')

const form = ref({
  blogTitle: '',
  blogText: '',
  blogImageUrl: '',
})

const renderedPreview = computed(() => DOMPurify.sanitize(marked.parse(form.value.blogText || '')))

const { uploadIfNeeded } = useImageUpload('/upload/blog-image')

const submit = async () => {
  if (!form.value.blogTitle?.trim() || !form.value.blogText?.trim()) {
    alert('Blog Title and Blog Text are required.')
    return
  }

  // No blog post exists yet to attach an image to, so an upload failure or a save failure after
  // upload would otherwise orphan the image in R2 with nothing referencing it. Create the post
  // first (without a staged file), then upload and attach the image after.
  isSubmitting.value = true
  const stagedImage = isStagedFile(form.value.blogImageUrl) ? form.value.blogImageUrl : null

  uploadStatus.value = 'Posting...'

  let newId
  try {
    const res = await api.post('/blog', {
      blogTitle: form.value.blogTitle,
      blogText: form.value.blogText,
      blogImageUrl: stagedImage ? null : form.value.blogImageUrl,
    })
    newId = res.data.blogPostId
  } catch (err) {
    console.error('Submit failed:', JSON.stringify(err.response?.data) || err.message)
    alert('Failed to post blog.\n\n' + (JSON.stringify(err.response?.data) || err.message))
    isSubmitting.value = false
    uploadStatus.value = ''
    return
  }

  if (stagedImage) {
    uploadStatus.value = 'Uploading image...'
    try {
      const blogImageUrl = await uploadIfNeeded(stagedImage)
      await api.patch(`/blog/${newId}/image`, { blogImageUrl })
    } catch (err) {
      console.error(
        'Image upload failed after blog post creation:',
        JSON.stringify(err.response?.data) || err.message
      )
      alert(
        'Blog post created, but the image failed to upload.\n\n' +
          (JSON.stringify(err.response?.data) || err.message)
      )
    }
  }

  isSubmitting.value = false
  uploadStatus.value = ''
  router.push('/blog')
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
.field-label {
  font-size: 0.85rem;
  color: #b0b0b0;
  margin-bottom: 4px;
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
  transition:
    background 150ms,
    color 150ms;
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

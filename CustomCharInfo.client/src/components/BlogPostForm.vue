<template>
  <PageShell title="Add a blog post" :back-to="{ name: 'AdminPortal' }" back-label="Admin portal">
    <FormLayout>
      <FormSection id="post" title="Post">
        <LabeledField label="Title" required>
          <v-text-field v-model="form.blogTitle" />
        </LabeledField>

        <LabeledField label="Content" required hint="Markdown is supported.">
          <div class="editor-tabs">
            <AppButton
              size="sm"
              :variant="tab === 'write' ? 'primary' : 'ghost'"
              @click="tab = 'write'"
            >
              Write
            </AppButton>
            <AppButton
              size="sm"
              :variant="tab === 'preview' ? 'primary' : 'ghost'"
              @click="tab = 'preview'"
            >
              Preview
            </AppButton>
          </div>
          <v-textarea v-if="tab === 'write'" v-model="form.blogText" auto-grow rows="8" />
          <div v-else class="preview-box">
            <!-- Content is DOMPurify-sanitized -->
            <!-- eslint-disable-next-line vue/no-v-html -->
            <div v-if="renderedPreview" class="preview-content" v-html="renderedPreview" />
            <span v-else class="preview-empty">Nothing to preview.</span>
          </div>
        </LabeledField>

        <LabeledField label="Image" note="optional" class="image-field">
          <ImageUploadField v-model="form.blogImageUrl" />
        </LabeledField>
      </FormSection>

      <template #savebar>
        <span class="savebar-spacer"></span>
        <AppButton variant="primary" icon="mdi-check" :busy="isSubmitting" @click="submit">
          {{ uploadStatus || 'Publish post' }}
        </AppButton>
      </template>
    </FormLayout>
  </PageShell>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { marked } from 'marked'
import DOMPurify from 'dompurify'
import api from '@/services/api'
import PageShell from '@/components/PageShell.vue'
import FormLayout from '@/components/FormLayout.vue'
import FormSection from '@/components/FormSection.vue'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'
import ImageUploadField from '@/components/ImageUploadField.vue'
import { useImageUpload, isStagedFile } from '@/composables/useImageUpload'
import { useNotify } from '@/composables/useNotify'

const notify = useNotify()

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
    notify.warning('A title and content are required.')
    return
  }

  // No blog post exists yet to attach an image to, so an upload failure or a save failure after
  // upload would otherwise orphan the image in R2 with nothing referencing it. Create the post
  // first (without a staged file), then upload and attach the image after.
  isSubmitting.value = true
  const stagedImage = isStagedFile(form.value.blogImageUrl) ? form.value.blogImageUrl : null

  uploadStatus.value = 'Posting'

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
    notify.error('Failed to post blog.', err)
    isSubmitting.value = false
    uploadStatus.value = ''
    return
  }

  if (stagedImage) {
    uploadStatus.value = 'Uploading image'
    try {
      const blogImageUrl = await uploadIfNeeded(stagedImage)
      await api.patch(`/blog/${newId}/image`, { blogImageUrl })
    } catch (err) {
      console.error(
        'Image upload failed after blog post creation:',
        JSON.stringify(err.response?.data) || err.message
      )
      notify.error('Blog post created, but the image failed to upload.', err)
    }
  }

  isSubmitting.value = false
  uploadStatus.value = ''
  router.push('/blog')
}
</script>

<style scoped>
.editor-tabs {
  display: flex;
  gap: 6px;
  margin-bottom: 8px;
}

.preview-box {
  min-height: 160px;
  padding: 12px 16px;
  border: 1px solid var(--line-2);
  background: var(--panel-2);
}

.preview-empty {
  color: var(--tx-3);
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

.image-field {
  max-width: 480px;
}

.savebar-spacer {
  flex: 1;
}
</style>

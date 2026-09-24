<template>
  <div class="list-editor">
    <div class="list-editor__head">
      <a
        href="https://docs.google.com/spreadsheets/d/16SEU3MibrzTJHTjxJb7c5e7JzGgrfWY_c_hqNJtGvNY/"
        target="_blank"
        rel="noopener"
        class="list-editor__hint offsite"
      >
        Learn more about articles
      </a>
      <AppButton
        variant="ghost"
        size="sm"
        :icon="showForm ? 'mdi-close' : 'mdi-plus'"
        @click="toggleForm"
      >
        {{ showForm ? 'Cancel' : 'Add an article' }}
      </AppButton>
    </div>

    <!-- Add / edit -->
    <v-expand-transition>
      <div v-if="showForm" class="list-editor__form">
        <LabeledField label="Article" required class="list-editor__wide">
          <v-autocomplete
            v-model="draft.articleId"
            :items="articleOptions"
            :item-title="(item) => `${item.vanillaCharInternalName}_${item.articleName}`"
            item-value="articleId"
            density="compact"
            hide-details
          />
        </LabeledField>
        <LabeledField label="Modded internal name">
          <v-text-field
            v-model="draft.moddedName"
            placeholder="e.g. shortaxe"
            density="compact"
            hide-details
          />
        </LabeledField>
        <LabeledField label="Display name">
          <v-text-field
            v-model="draft.description"
            placeholder="e.g. Short Axe"
            density="compact"
            hide-details
          />
        </LabeledField>
        <AppButton size="sm" icon="mdi-check" class="list-editor__commit" @click="commitDraft">
          {{ editingIndex !== null ? 'Update article' : 'Add article' }}
        </AppButton>
      </div>
    </v-expand-transition>

    <!-- List -->
    <ul v-if="articles.length" class="list-editor__list">
      <li v-for="(entry, i) in articles" :key="i" class="list-editor__row">
        <span class="list-editor__text">
          <strong>{{ entry.description }}</strong>
          <span class="mono muted">{{ articleName(entry.articleId) }}</span>
          <span class="faint">({{ entry.moddedName }})</span>
        </span>
        <span class="list-editor__actions">
          <button
            type="button"
            class="icon-btn"
            :class="{ 'icon-btn--hidden': i === 0 }"
            aria-label="Move up"
            @click="moveItem(articles, i, -1)"
          >
            <v-icon size="18">mdi-arrow-up</v-icon>
          </button>
          <button
            type="button"
            class="icon-btn"
            :class="{ 'icon-btn--hidden': i === articles.length - 1 }"
            aria-label="Move down"
            @click="moveItem(articles, i, 1)"
          >
            <v-icon size="18">mdi-arrow-down</v-icon>
          </button>
          <button type="button" class="icon-btn" aria-label="Edit" @click="editEntry(i)">
            <v-icon size="18">mdi-pencil</v-icon>
          </button>
          <button
            type="button"
            class="icon-btn icon-btn--danger"
            aria-label="Remove"
            @click="articles.splice(i, 1)"
          >
            <v-icon size="18">mdi-delete</v-icon>
          </button>
        </span>
      </li>
    </ul>
    <p v-else class="list-editor__empty">No cloned articles yet.</p>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { moveItem } from '@/services/listUtils'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'

// Editable list of a moveset's cloned articles: { articleId, moddedName, description }.
// The list is edited in place through v-model; the parent submits it as-is.
const props = defineProps({
  articleOptions: { type: Array, default: () => [] },
})
const articles = defineModel({ type: Array, default: () => [] })

const emptyDraft = () => ({ articleId: null, moddedName: '', description: '' })
const draft = ref(emptyDraft())
const showForm = ref(false)
const editingIndex = ref(null)

const articleName = (id) => {
  const article = props.articleOptions.find((a) => a.articleId === id)
  return article ? `${article.vanillaCharInternalName}_${article.articleName}` : 'Unknown'
}

// Closing the form also discards any in-progress edit.
const toggleForm = () => {
  showForm.value = !showForm.value
  if (!showForm.value) {
    editingIndex.value = null
    draft.value = emptyDraft()
  }
}

const commitDraft = () => {
  if (!draft.value.articleId) return
  if (editingIndex.value !== null) {
    articles.value[editingIndex.value] = { ...draft.value }
    editingIndex.value = null
  } else {
    articles.value.push({ ...draft.value })
  }
  draft.value = emptyDraft()
  showForm.value = false
}

const editEntry = (i) => {
  editingIndex.value = i
  draft.value = { ...articles.value[i] }
  showForm.value = true
}
</script>

<style scoped>
.list-editor {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.list-editor__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.list-editor__hint {
  font-size: 12px;
  color: var(--tx-2);
}

.list-editor__form {
  display: grid;
  grid-template-columns: 2fr 1fr 1fr auto;
  gap: 12px;
  align-items: end;
  padding: 14px;
  border: 1px solid var(--line);
  background: var(--panel-2);
}

.list-editor__commit {
  margin-bottom: 5px;
}

.list-editor__list {
  margin: 0;
  padding: 0;
  list-style: none;
}

.list-editor__row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 8px 0;
  border-bottom: 1px solid var(--line);
  font-size: 14px;
}

.list-editor__row:last-child {
  border-bottom: 0;
}

.list-editor__text {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 8px;
  min-width: 0;
}

.list-editor__actions {
  display: flex;
  gap: 2px;
  flex: none;
}

.list-editor__empty {
  margin: 0;
  color: var(--tx-3);
  font-size: 13px;
}

.icon-btn {
  display: flex;
  padding: 4px;
  border: 0;
  background: none;
  color: var(--tx-2);
  cursor: pointer;
  transition: color var(--dur-fast) var(--ease);
}

.icon-btn:hover {
  color: var(--white);
}

.icon-btn--danger:hover {
  color: var(--err);
}

.icon-btn--hidden {
  visibility: hidden;
  pointer-events: none;
}

@media (max-width: 959px) {
  .list-editor__form {
    grid-template-columns: 1fr;
  }

  .list-editor__commit {
    margin-bottom: 0;
  }
}
</style>

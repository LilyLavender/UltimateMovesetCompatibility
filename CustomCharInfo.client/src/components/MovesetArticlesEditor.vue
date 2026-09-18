<template>
  <section>
    <h2>
      Cloned Articles
      <v-btn
        variant="text"
        density="compact"
        icon="mdi-plus"
        class="rotate-toggle"
        :class="{ rotated: showForm }"
        @click="toggleForm"
      />
    </h2>
    <p class="subheader">
      <a
        href="https://docs.google.com/spreadsheets/d/16SEU3MibrzTJHTjxJb7c5e7JzGgrfWY_c_hqNJtGvNY/"
        target="_blank"
        class="offsite unvisitable"
      >
        Learn more about articles
      </a>
    </p>

    <!-- Add / edit -->
    <v-expand-transition>
      <div v-if="showForm">
        <v-row>
          <v-col cols="12" sm="4">
            <v-autocomplete
              v-model="draft.articleId"
              variant="outlined"
              :items="articleOptions"
              :item-title="(item) => `${item.vanillaCharInternalName}_${item.articleName}`"
              item-value="articleId"
              label="Article"
            />
          </v-col>
          <v-col cols="12" sm="3">
            <v-text-field
              v-model="draft.moddedName"
              variant="outlined"
              label="Modded Internal Name"
              placeholder="eg. shortaxe"
            />
          </v-col>
          <v-col cols="12" sm="3">
            <v-text-field
              v-model="draft.description"
              variant="outlined"
              label="Display Name"
              placeholder="eg. Short Axe"
            />
          </v-col>
          <v-col cols="12" sm="2" class="justify-content-center">
            <v-btn class="btn add-button" @click="commitDraft">
              {{ editingIndex !== null ? 'Update Article' : 'Add Article' }}
            </v-btn>
          </v-col>
        </v-row>
      </div>
    </v-expand-transition>

    <!-- List -->
    <v-list>
      <v-list-item v-for="(entry, i) in articles" :key="i">
        <v-list-item-title>
          <strong>{{ entry.description }}</strong
          >: {{ articleName(entry.articleId) }} ({{ entry.moddedName }})
        </v-list-item-title>
        <template #append>
          <v-icon
            class="reorder-icon"
            :class="{ invisible: i === 0 }"
            @click="moveItem(articles, i, -1)"
            >mdi-arrow-up</v-icon
          >
          <v-icon
            class="reorder-icon"
            :class="{ invisible: i === articles.length - 1 }"
            @click="moveItem(articles, i, 1)"
            >mdi-arrow-down</v-icon
          >
          <v-icon class="edit-icon" @click="editEntry(i)">mdi-pencil</v-icon>
          <v-icon class="delete-icon" @click="articles.splice(i, 1)">mdi-delete</v-icon>
        </template>
      </v-list-item>
    </v-list>
  </section>
</template>

<script setup>
import { ref } from 'vue'
import { moveItem } from '@/services/listUtils'

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
h2 {
  font-size: 2.25em;
  margin-bottom: 10px;
}
.subheader {
  margin-top: -1.5em;
  margin-bottom: 0.5em;
  font-size: 12px;
}
.btn {
  text-transform: unset;
  letter-spacing: 0.009375em;
  font-size: medium;
}
.add-button {
  background-color: #2e2e2e;
  color: #e2e2e2;
  margin-top: 10px;
  margin-left: 10px;
  box-shadow: none;
}
.edit-icon,
.delete-icon,
.reorder-icon {
  background: none;
  font-size: 20px;
  margin-left: 8px;
  color: #aaaaaa;
  transition: color 150ms ease-in-out;
}
.edit-icon:hover,
.delete-icon:hover,
.reorder-icon:hover {
  color: #dddddd;
}
.edit-icon::before,
.delete-icon::before,
.reorder-icon::before {
  margin-top: -4px;
}
.invisible {
  visibility: hidden;
  pointer-events: none;
}
:deep(.rotate-toggle > span > i::before) {
  transition: transform 250ms ease-in-out;
}
:deep(.rotate-toggle.rotated span > i::before) {
  transform: rotate(-45deg);
}
</style>

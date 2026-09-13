<template>
  <div class="lookup-page">
    <h1 class="page-title no-select">Plugin Lookup</h1>
    <p class="subtitle">
      Not sure what a <code>.nro</code> is or whether it's up to date?
      Upload the file below to hash entirely in the browser and check with UMC's database.

      If you have a plugin and wish to add it to the database, go to the <router-link to="/plugins/add" class="unvisitable">submission page</router-link>.
    </p>

    <div class="picker-row">
      <PluginDropZone v-model="file" v-model:hash="hash" :check-duplicate="false" large />
    </div>

    <div v-if="loading" class="status-msg">
      <v-progress-circular indeterminate size="20" />
      Hashing and checking…
    </div>

    <div v-else-if="error" class="result-card result-card--error">
      <v-icon class="result-icon">mdi-alert-circle</v-icon>
      <span>{{ error }}</span>
    </div>

    <div v-else-if="result === 'not-found'" class="result-card result-card--unknown">
      <v-icon class="result-icon">mdi-help-circle</v-icon>
      <div>
        <div class="result-title">Not recognized</div>
        <div class="result-sub">
          This hash doesn't match any plugin in our database!
          If you know what it is, please <router-link to="/plugins/add" class="unvisitable">submit it</router-link>.
        </div>
      </div>
    </div>

    <div v-else-if="result" class="result-card" :class="result.isCurrent ? 'result-card--current' : 'result-card--outdated'">
      <v-icon class="result-icon">{{ result.isCurrent ? 'mdi-check-circle' : 'mdi-alert' }}</v-icon>
      <div class="result-body">
        <div class="result-title">
          <router-link
            v-if="result.attachmentType === 'Moveset'"
            :to="{ name: 'MovesetDetail', params: { movesetId: result.movesetId } }"
            class="unvisitable"
          >{{ result.pluginName }}</router-link>
          <template v-else>{{ result.pluginName }}</template>
        </div>
        <div class="result-sub">Version {{ result.matchedVersionLabel }}</div>
        <p v-if="result.attachmentType === 'Other' && result.pluginDescription" class="result-desc">{{ result.pluginDescription }}</p>

        <div class="result-version-row">
          <span v-if="result.isCurrent">Up to date! This is the most recent version.</span>
          <span v-else>
            There is a
            <a v-if="result.learnMoreUrl" :href="result.learnMoreUrl" target="_blank" rel="noopener" class="unvisitable">newer version of this {{ thingLabel }}</a><template v-else>newer version of this {{ thingLabel }}</template>
            available!<template v-if="result.currentVersionLabel"> ({{ displayVersion(result.currentVersionLabel) }})</template>
          </span>
        </div>

        <!--
          Moveset plugins link the title itself to the moveset's page on UMC;
          dependency/other plugins have no such page,
          so their link needs to stay visible here even when current (when outdated, the sentence above already links to it).
        -->
        <a
          v-if="result.attachmentType !== 'Moveset' && result.isCurrent && result.learnMoreUrl"
          :href="result.learnMoreUrl"
          target="_blank"
          rel="noopener"
          class="unvisitable learn-more-link"
        >
          View mod page <v-icon size="small">mdi-open-in-new</v-icon>
        </a>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import api from '@/services/api'
import PluginDropZone from '@/components/PluginDropZone.vue'
import { displayVersion } from '@/services/pluginVersion'

const file = ref(null)
const hash = ref(null)
const loading = ref(false)
const error = ref(null)
const result = ref(null)

const thingLabel = computed(() => result.value?.attachmentType === 'Dependency' ? 'dependency' : 'mod')

async function checkHash(h) {
  error.value = null
  result.value = null
  if (!h) return

  loading.value = true
  try {
    const res = await api.get('/plugins/identify', { params: { hash: h } })
    result.value = res.data
  } catch (err) {
    if (err.response?.status === 404) {
      result.value = 'not-found'
    } else {
      error.value = 'Something went wrong while checking this file. Please try again.'
    }
  } finally {
    loading.value = false
  }
}

watch(hash, checkHash)
</script>

<style scoped>
.lookup-page {
  max-width: 700px;
  margin: 0 auto;
  padding: 2rem 1.5rem 4rem;
}

.page-title { font-size: 4em; margin-bottom: 0.15em; }
.subtitle { color: #aaa; margin-bottom: 1.5rem; line-height: 1.5; }
.subtitle code { background-color: #222; padding: 0.1em 0.35em; border-radius: 3px; }

.picker-row { margin-bottom: 1.5rem; }

.status-msg {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  color: #aaa;
  padding: 1rem 0;
}

.result-card {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 1rem 1.25rem;
  border-radius: 6px;
}

.result-icon { font-size: 1.6em; flex-shrink: 0; margin-top: 2px; }
.result-title { font-size: 1.15em; font-weight: bold; }
.result-sub { font-size: 0.85em; opacity: 0.75; margin-top: 0.1rem; }
.result-desc { margin: 0.5rem 0 0; font-size: 0.92em; line-height: 1.4; }

.result-version-row {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  flex-wrap: wrap;
  margin-top: 0.6rem;
  font-size: 0.92em;
}

.learn-more-link {
  display: inline-flex;
  align-items: center;
  gap: 0.2rem;
  margin-top: 0.6rem;
  font-size: 0.88em;
  color: #90caf9;
  text-decoration: none;
}
.learn-more-link:hover { text-decoration: underline; }

.result-card--error    { background-color: #3a1010; color: #ef9a9a; border: 1px solid #c62828; }
.result-card--unknown  { background-color: #1a1a1a; color: #ccc; border: 1px solid #333; }
.result-card--current  { background-color: #1b3a1b; color: #c8e6c9; border: 1px solid #388e3c; }
.result-card--outdated { background-color: #3a2f00; color: #ffe082; border: 1px solid #f9a825; }
</style>

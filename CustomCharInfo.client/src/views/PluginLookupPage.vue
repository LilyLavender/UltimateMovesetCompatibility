<template>
  <div class="lookup-page">
    <h1 class="page-title no-select">Plugin Lookup</h1>
    <p class="subtitle">
      Not sure what a <code>.nro</code> is or whether it's up to date? Upload the file below to hash
      entirely in the browser and check with UMC's database. You can also drop several files or your
      whole <code>plugins</code> folder to check everything at once. If you have a plugin and wish
      to add it to the database, go to the
      <router-link to="/plugins/add" class="unvisitable">submission page</router-link>.
    </p>

    <div class="picker-row">
      <PluginBatchDropZone v-model="entries" large />
    </div>

    <div v-if="loading" class="status-msg">
      <v-progress-circular indeterminate size="20" />
      {{ loadingLabel }}
    </div>

    <div v-else-if="error" class="result-card result-card--error">
      <v-icon class="result-icon">mdi-alert-circle</v-icon>
      <span>{{ error }}</span>
    </div>

    <!-- One file: the single result card -->
    <template v-else-if="rows && rows.length === 1">
      <div v-if="!singleResult" class="result-card result-card--unknown">
        <v-icon class="result-icon">mdi-help-circle</v-icon>
        <div>
          <div class="result-title">Not recognized</div>
          <div class="result-sub">
            This hash doesn't match any plugin in our database! If you know what it is, please
            <router-link to="/plugins/add" class="unvisitable">submit it</router-link>.
          </div>
        </div>
      </div>

      <div
        v-else
        class="result-card"
        :class="singleResult.isCurrent ? 'result-card--current' : 'result-card--outdated'"
      >
        <v-icon class="result-icon">{{
          singleResult.isCurrent ? 'mdi-check-circle' : 'mdi-alert'
        }}</v-icon>
        <div class="result-body">
          <div class="result-title">
            <router-link
              v-if="singleResult.attachmentType === 'Moveset'"
              :to="{ name: 'MovesetDetail', params: { movesetId: singleResult.movesetId } }"
              class="unvisitable"
              >{{ singleResult.pluginName }}</router-link
            >
            <template v-else>{{ singleResult.pluginName }}</template>
          </div>
          <div class="result-sub">Version {{ singleResult.matchedVersionLabel }}</div>
          <p
            v-if="singleResult.attachmentType === 'Other' && singleResult.pluginDescription"
            class="result-desc"
          >
            {{ singleResult.pluginDescription }}
          </p>

          <div class="result-version-row">
            <span v-if="singleResult.isCurrent">Up to date! This is the most recent version.</span>
            <span v-else>
              There is a
              <a
                v-if="singleResult.learnMoreUrl"
                :href="singleResult.learnMoreUrl"
                target="_blank"
                rel="noopener"
                class="unvisitable"
                >newer version of this {{ thingLabel(singleResult) }}</a
              ><template v-else>newer version of this {{ thingLabel(singleResult) }}</template>
              available!<template v-if="singleResult.currentVersionLabel">
                ({{ displayVersion(singleResult.currentVersionLabel) }})</template
              >
            </span>
          </div>

          <!--
            Moveset plugins link the title itself to the moveset's page on UMC;
            dependency/other plugins have no such page,
            so their link needs to stay visible here even when current (when outdated, the sentence above already links to it).
          -->
          <a
            v-if="
              singleResult.attachmentType !== 'Moveset' &&
              singleResult.isCurrent &&
              singleResult.learnMoreUrl
            "
            :href="singleResult.learnMoreUrl"
            target="_blank"
            rel="noopener"
            class="unvisitable learn-more-link"
          >
            View mod page <v-icon size="small">mdi-open-in-new</v-icon>
          </a>
        </div>
      </div>
    </template>

    <!-- Several files: summary line and table -->
    <template v-else-if="rows && rows.length > 1">
      <div class="batch-summary">
        <span class="summary-pill summary-pill--current">
          <v-icon size="small">mdi-check-circle</v-icon> {{ summary.current }} up to date
        </span>
        <span class="summary-pill summary-pill--outdated">
          <v-icon size="small">mdi-alert</v-icon> {{ summary.outdated }} outdated
        </span>
        <span class="summary-pill summary-pill--unknown">
          <v-icon size="small">mdi-help-circle</v-icon> {{ summary.unknown }} not recognized
        </span>
      </div>

      <div class="batch-table-wrap">
        <table class="batch-table">
          <thead>
            <tr>
              <th>File</th>
              <th>Plugin</th>
              <th>Version</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="row in rows" :key="row.path + row.hash">
              <td class="cell-file" :title="row.path">{{ row.path }}</td>

              <td v-if="row.found">
                <router-link
                  v-if="row.result.attachmentType === 'Moveset'"
                  :to="{ name: 'MovesetDetail', params: { movesetId: row.result.movesetId } }"
                  class="unvisitable"
                  >{{ row.result.pluginName }}</router-link
                >
                <template v-else>{{ row.result.pluginName }}</template>
              </td>
              <td v-else class="cell-muted">
                Unknown.
                <router-link to="/plugins/add" class="unvisitable">Submit it?</router-link>
              </td>

              <td>
                <template v-if="row.found">{{ row.result.matchedVersionLabel }}</template>
              </td>

              <td>
                <span class="status-pill" :class="`status-pill--${rowStatus(row)}`">
                  <template v-if="rowStatus(row) === 'current'">Up to date</template>
                  <template v-else-if="rowStatus(row) === 'outdated'">
                    Newer available<template v-if="row.result.currentVersionLabel">
                      ({{ displayVersion(row.result.currentVersionLabel) }})</template
                    >
                  </template>
                  <template v-else>Not recognized</template>
                </span>
              </td>

              <td>
                <a
                  v-if="row.found && row.result.learnMoreUrl"
                  :href="row.result.learnMoreUrl"
                  target="_blank"
                  rel="noopener"
                  class="unvisitable learn-more-link learn-more-link--inline"
                  title="View mod page"
                >
                  <v-icon size="small">mdi-open-in-new</v-icon>
                </a>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import api from '@/services/api'
import PluginBatchDropZone from '@/components/PluginBatchDropZone.vue'
import { hashFile } from '@/services/hashFile'
import { displayVersion } from '@/services/pluginVersion'
import {
  chunk,
  groupBatchResults,
  rowStatus,
  summarizeRows,
  BATCH_IDENTIFY_MAX_HASHES,
} from '@/services/pluginBatch'

const MAX_FILES = 200

const entries = ref([])
const loading = ref(false)
const loadingLabel = ref('')
const error = ref(null)
const rows = ref(null)

const singleResult = computed(() => (rows.value?.[0]?.found ? rows.value[0].result : null))
const summary = computed(() => summarizeRows(rows.value ?? []))

const thingLabel = (result) => (result?.attachmentType === 'Dependency' ? 'dependency' : 'mod')

async function runLookup(list) {
  error.value = null
  rows.value = null
  if (!list || list.length === 0) return

  if (list.length > MAX_FILES) {
    error.value = `That's ${list.length} files. Please check at most ${MAX_FILES} at a time.`
    return
  }

  loading.value = true
  loadingLabel.value =
    list.length === 1 ? 'Hashing and checking…' : `Hashing ${list.length} files and checking…`
  try {
    const hashes = await Promise.all(list.map((entry) => hashFile(entry.file)))
    const unique = [...new Set(hashes)]
    const responses = await Promise.all(
      chunk(unique, BATCH_IDENTIFY_MAX_HASHES).map((hashesChunk) =>
        api.post('/plugins/identify/batch', { hashes: hashesChunk })
      )
    )
    const results = responses.flatMap((res) => res.data)

    if (entries.value !== list) return // a newer selection was made while this was in flight
    rows.value = groupBatchResults(list, hashes, results)
  } catch {
    if (entries.value !== list) return
    error.value = 'Something went wrong while checking these files. Please try again.'
  } finally {
    if (entries.value === list) loading.value = false
  }
}

watch(entries, runLookup)
</script>

<style scoped>
.lookup-page {
  max-width: 900px;
  margin: 0 auto;
  padding: 2rem 1.5rem 4rem;
}

.page-title {
  font-size: 4em;
  margin-bottom: 0.15em;
}
.subtitle {
  color: #aaa;
  margin-bottom: 1.5rem;
  line-height: 1.5;
}
.subtitle code {
  background-color: #222;
  padding: 0.1em 0.35em;
  border-radius: 3px;
}

.picker-row {
  margin-bottom: 1.5rem;
}

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

.result-icon {
  font-size: 1.6em;
  flex-shrink: 0;
  margin-top: 2px;
}
.result-title {
  font-size: 1.15em;
  font-weight: bold;
}
.result-sub {
  font-size: 0.85em;
  opacity: 0.75;
  margin-top: 0.1rem;
}
.result-desc {
  margin: 0.5rem 0 0;
  font-size: 0.92em;
  line-height: 1.4;
}

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
.learn-more-link:hover {
  text-decoration: underline;
}
.learn-more-link--inline {
  margin-top: 0;
}

.result-card--error {
  background-color: #3a1010;
  color: #ef9a9a;
  border: 1px solid #c62828;
}
.result-card--unknown {
  background-color: #1a1a1a;
  color: #ccc;
  border: 1px solid #333;
}
.result-card--current {
  background-color: #1b3a1b;
  color: #c8e6c9;
  border: 1px solid #388e3c;
}
.result-card--outdated {
  background-color: #3a2f00;
  color: #ffe082;
  border: 1px solid #f9a825;
}

/* Batch summary */
.batch-summary {
  display: flex;
  flex-wrap: wrap;
  gap: 0.6rem;
  margin-bottom: 0.9rem;
}
.summary-pill {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.25rem 0.7rem;
  border-radius: 999px;
  font-size: 0.88em;
  border: 1px solid;
}
.summary-pill--current {
  background-color: #1b3a1b;
  color: #c8e6c9;
  border-color: #388e3c;
}
.summary-pill--outdated {
  background-color: #3a2f00;
  color: #ffe082;
  border-color: #f9a825;
}
.summary-pill--unknown {
  background-color: #1a1a1a;
  color: #ccc;
  border-color: #333;
}

/* Batch table */
.batch-table-wrap {
  border: 1px solid #333;
  border-radius: 6px;
  overflow-x: auto;
}
.batch-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9em;
}
.batch-table th,
.batch-table td {
  padding: 0.45rem 0.75rem;
  text-align: left;
  border-bottom: 1px solid #252525;
  vertical-align: middle;
  white-space: nowrap;
}
.batch-table th {
  background-color: #121212;
  color: #fff;
  font-weight: 600;
  border-bottom: 1px solid #333;
}
.batch-table tbody tr:hover td {
  background-color: #181818;
}
.batch-table tbody tr:last-child td {
  border-bottom: none;
}
.cell-file {
  max-width: 320px;
  overflow: hidden;
  text-overflow: ellipsis;
  font-family: monospace;
  font-size: 0.92em;
}
.cell-muted {
  color: #888;
}

.status-pill {
  display: inline-block;
  padding: 1px 8px;
  border-radius: 3px;
  font-size: 0.9em;
  white-space: nowrap;
}
.status-pill--current {
  background-color: #2e7d32;
  color: #fff;
}
.status-pill--outdated {
  background-color: #fbc02d;
  color: #000;
}
.status-pill--unknown {
  background-color: #333;
  color: #ccc;
}
</style>

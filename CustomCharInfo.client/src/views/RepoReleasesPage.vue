<template>
  <v-container max-width="1400px">
    <h1 class="mb-2 page-title no-select">Repo Releases</h1>
    <p class="helper-text mb-5">
      Reads a GitHub repository's releases and checks them against the database. 
      Limit of 60 requests an hour per IP.
    </p>

    <!-- Repo input -->
    <div class="d-flex align-start ga-3 flex-wrap mb-2">
      <v-text-field
        v-model="repoInput"
        variant="outlined"
        density="comfortable"
        label="GitHub repository"
        placeholder="owner/repo or https://github.com/owner/repo"
        hide-details
        class="repo-input"
        @keyup.enter="checkInput"
      />
      <v-btn class="btn action-button mt-1" :disabled="!parsedInput" @click="checkInput">
        Check
      </v-btn>
      <v-btn
        class="btn action-button mt-1"
        :disabled="!parsedInput || adding"
        :loading="adding"
        @click="addToList"
      >
        Add to list
      </v-btn>
    </div>

    <!-- Watched repos -->
    <h2 class="mt-6 mb-2">Watched repositories</h2>
    <div v-if="watchedRepos.length === 0" class="helper-text mb-4">None yet.</div>
    <v-table v-else class="dark-table mb-3" density="comfortable">
      <thead>
        <tr>
          <th>Repository</th>
          <th>Added by</th>
          <th class="text-right">
            <v-btn
              size="small"
              class="btn action-button"
              :disabled="checkingAll"
              :loading="checkingAll"
              @click="checkAll"
            >
              Check all
            </v-btn>
          </th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="watched in watchedRepos" :key="watched.watchedRepoId">
          <td>
            <a :href="repoUrl(watched)" target="_blank" rel="noopener" class="unvisitable">
              {{ watched.owner }}/{{ watched.repo }}
            </a>
          </td>
          <td class="helper-text">{{ watched.addedByUsername ?? 'unknown' }}</td>
          <td class="text-right">
            <v-btn
              size="small"
              variant="text"
              :loading="panelFor(watched)?.loading"
              @click="checkRepo(watched)"
            >
              Check
            </v-btn>
            <v-btn
              size="small"
              variant="text"
              icon="mdi-delete"
              title="Remove from list"
              @click="removeWatched(watched)"
            />
          </td>
        </tr>
      </tbody>
    </v-table>

    <!-- Results -->
    <template v-if="panels.length > 0">
      <h2 class="mt-8 mb-2">Results</h2>

      <v-card v-for="panel in panels" :key="panel.key" class="mb-6" color="#1e1e1e">
        <v-card-title class="d-flex align-center flex-wrap ga-2">
          <a :href="repoUrl(panel)" target="_blank" rel="noopener" class="panel-link">
            {{ panel.owner }}/{{ panel.repo }}
          </a>
          <span v-if="panel.checkedAt" class="checked-at">
            checked {{ panel.checkedAt.toLocaleTimeString() }}
          </span>
          <v-spacer />
          <template v-if="panel.rows">
            <span class="status-pill status-pill--registered">
              {{ panel.summary.registered }} registered
            </span>
            <span class="status-pill status-pill--unregistered">
              {{ panel.summary.unregistered }} looked up
            </span>
            <span class="status-pill status-pill--unseen">{{ panel.summary.unseen }} unseen</span>
            <span v-if="panel.summary.noDigest" class="status-pill status-pill--muted">
              {{ panel.summary.noDigest }} unhashed
            </span>
            <v-btn
              size="small"
              class="btn action-button ml-2"
              :disabled="registrableFor(panel).length === 0"
              @click="openRegister(panel)"
            >
              Register unregistered ({{ registrableFor(panel).length }})
            </v-btn>
          </template>
          <v-btn
            size="small"
            variant="text"
            icon="mdi-close"
            title="Close"
            color="#e2e2e2"
            @click="closePanel(panel)"
          />
        </v-card-title>

        <v-card-text>
          <div v-if="panel.loading" class="helper-text">{{ panel.progress }}</div>
          <div v-else-if="panel.error" class="error-text">{{ panel.error }}</div>
          <div v-else-if="visibleRows(panel).length === 0" class="helper-text">
            No .nro assets in this repository's releases.
          </div>
          <v-data-table
            v-else
            class="dark-table"
            :items="visibleRows(panel)"
            :headers="headers"
            item-value="key"
            density="compact"
            :items-per-page="25"
          >
            <template #item.releaseDate="{ item }">
              {{ item.releaseDate ? new Date(item.releaseDate).toLocaleDateString() : '' }}
            </template>
            <template #item.tag="{ item }">
              <span class="mono">{{ item.tag }}</span>
              <span v-if="item.prerelease" class="pre-marker" title="Prerelease">pre</span>
            </template>
            <template #item.name="{ item }">
              <a
                v-if="item.releaseUrl"
                :href="item.releaseUrl"
                target="_blank"
                rel="noopener"
                class="unvisitable"
              >
                {{ item.name }}
              </a>
              <span v-else>{{ item.name }}</span>
            </template>
            <template #item.asset="{ item }">
              <a
                v-if="item.downloadUrl"
                :href="item.downloadUrl"
                target="_blank"
                rel="noopener"
                class="unvisitable mono"
              >
                {{ item.asset }}
              </a>
              <span v-else class="helper-text">none</span>
            </template>
            <template #item.downloads="{ item }">
              {{ item.downloads == null ? '' : item.downloads.toLocaleString() }}
            </template>
            <template #item.hash="{ item }">
              <span v-if="item.hash" class="mono hash-cell" :title="hashTitle(item)">
                {{ item.hash.slice(0, 12) }}…
                <span v-if="item.hashSource === 'server'" class="hash-source">server</span>
                <v-btn
                  size="x-small"
                  variant="text"
                  icon="mdi-content-copy"
                  title="Copy hash"
                  @click="copyHash(item.hash)"
                />
              </span>
            </template>
            <template #item.status="{ item }">
              <span class="status-pill" :class="`status-pill--${item.status}`">
                {{ statusLabel(item) }}
              </span>
              <span v-if="statusDetail(item)" class="status-detail">{{ statusDetail(item) }}</span>
            </template>
          </v-data-table>
        </v-card-text>
      </v-card>
    </template>

    <PluginBatchRegisterDialog
      v-model="registerOpen"
      :rows="registerRows"
      :repo="registerRepo"
      @registered="onRegistered"
    />
  </v-container>
</template>

<script setup>
import { ref, computed, reactive, onMounted } from 'vue'
import api from '@/services/api'
import { useNotify } from '@/composables/useNotify'
import { PENDING_ADMIN_STATES } from '@/globals'
import {
  parseRepoInput,
  repoUrl,
  fetchAllReleases,
  flattenReleaseAssets,
  rowsNeedingHash,
  withServerHash,
  withHashError,
  applyMatches,
  registrableRows,
  summarizeStatuses,
  GitHubApiError,
  RowStatus,
  STATUS_LABELS,
} from '@/services/githubReleases'
import { hasExtension } from '@/services/fileEntries'
import { chunk } from '@/services/pluginBatch'
import PluginBatchRegisterDialog from '@/components/PluginBatchRegisterDialog.vue'

const MATCH_HASHES_MAX = 200

const notify = useNotify()

const repoInput = ref('')
const adding = ref(false)
const watchedRepos = ref([])
const panels = ref([])
const checkingAll = ref(false)

const registerOpen = ref(false)
const registerRows = ref([])
const registerRepo = ref(null)
let registerPanelKey = null

const headers = [
  { title: 'Release Date', key: 'releaseDate', width: 120 },
  { title: 'Tag', key: 'tag' },
  { title: 'Name', key: 'name' },
  { title: 'Asset', key: 'asset' },
  { title: 'Downloads', key: 'downloads', align: 'end' },
  { title: 'Hash', key: 'hash', sortable: false },
  { title: 'Status', key: 'status' },
]

const parsedInput = computed(() => parseRepoInput(repoInput.value))

function keyFor({ owner, repo }) {
  return `${owner}/${repo}`.toLowerCase()
}

function panelFor(target) {
  return panels.value.find((p) => p.key === keyFor(target))
}

// Only plugin files matter here. Releases with no assets at all still get their placeholder row.
function isNroRow(row) {
  return row.asset == null || hasExtension(row.asset, '.nro')
}

function visibleRows(panel) {
  return panel.rows ?? []
}

function registrableFor(panel) {
  return panel.rows ? registrableRows(panel.rows) : []
}

function statusLabel(row) {
  if (row.status === RowStatus.NoDigest && row.hashError) return 'Could not hash'
  return STATUS_LABELS[row.status]
}

// Second line under the status pill: what a registered hash is,
// how often an unregistered one was looked up,
// or why an asset could not be hashed.
function statusDetail(row) {
  const match = row.match
  if (row.status === RowStatus.Registered) {
    const flags = []
    if (match.isCurrent) flags.push('current')
    if (match.acceptanceStateId != null && PENDING_ADMIN_STATES.includes(match.acceptanceStateId))
      flags.push('pending review')
    const suffix = flags.length ? ` (${flags.join(', ')})` : ''
    return `${match.pluginName} v${match.versionLabel}${suffix}`
  }
  if (row.status === RowStatus.Unregistered) {
    const times = match.checkCount === 1 ? '1 lookup' : `${match.checkCount} lookups`
    return `${times}, last ${new Date(match.lastCheckedAt).toLocaleDateString()}`
  }
  if (row.status === RowStatus.NoDigest && row.hashError) return row.hashError
  return ''
}

function hashTitle(row) {
  return row.hashSource === 'server'
    ? `${row.hash} (downloaded and hashed by the server)`
    : `${row.hash} (GitHub's digest)`
}

async function loadWatched() {
  try {
    const res = await api.get('/watched-repos')
    watchedRepos.value = res.data
  } catch (err) {
    notify.error('Failed to load the watched repositories.', err)
  }
}

async function addToList() {
  if (!parsedInput.value) return
  adding.value = true
  try {
    await api.post('/watched-repos', { input: repoInput.value.trim() })
    notify.success('Added to the list.')
    repoInput.value = ''
    await loadWatched()
  } catch (err) {
    notify.error('Could not add that repository.', err)
  } finally {
    adding.value = false
  }
}

async function removeWatched(watched) {
  try {
    await api.delete(`/watched-repos/${watched.watchedRepoId}`)
    watchedRepos.value = watchedRepos.value.filter((w) => w.watchedRepoId !== watched.watchedRepoId)
  } catch (err) {
    notify.error('Could not remove that repository.', err)
  }
}

function checkInput() {
  if (!parsedInput.value) return
  checkRepo(parsedInput.value)
}

async function matchHashes(rows) {
  const hashes = [...new Set(rows.map((r) => r.hash).filter(Boolean))]
  const matches = []
  for (const group of chunk(hashes, MATCH_HASHES_MAX)) {
    const res = await api.post('/plugins/match-hashes', { hashes: group })
    matches.push(...res.data)
  }
  return matches
}

// Assets uploaded before GitHub computed digests are hashed by the server one at a time.
async function fillMissingHashes(rows, panel) {
  const missing = rowsNeedingHash(rows)
  if (missing.length === 0) return rows
  const byKey = new Map(rows.map((r) => [r.key, r]))
  for (let i = 0; i < missing.length; i++) {
    const row = missing[i]
    panel.progress = `Hashing asset ${i + 1} of ${missing.length} without a GitHub digest…`
    try {
      const res = await api.get('/plugins/hash-asset', { params: { url: row.downloadUrl } })
      byKey.set(row.key, withServerHash(row, res.data.hash))
    } catch (err) {
      const detail = typeof err.response?.data === 'string' ? err.response.data : err.message
      byKey.set(row.key, withHashError(row, detail))
    }
  }
  return rows.map((r) => byKey.get(r.key))
}

// Returns false when the check hit GitHub's rate limit so Check All can stop early.
async function checkRepo(target) {
  const key = keyFor(target)
  let panel = panelFor(target)
  if (!panel) {
    panel = reactive({
      key,
      owner: target.owner,
      repo: target.repo,
      loading: false,
      progress: '',
      error: null,
      rows: null,
      summary: null,
      checkedAt: null,
    })
    panels.value.unshift(panel)
  }
  panel.loading = true
  panel.progress = 'Reading releases…'
  panel.error = null
  try {
    const releases = await fetchAllReleases(target.owner, target.repo)
    let rows = flattenReleaseAssets(releases).filter(isNroRow)
    rows = await fillMissingHashes(rows, panel)
    panel.progress = 'Matching against registered plugins…'
    const matches = await matchHashes(rows)
    panel.rows = applyMatches(rows, matches)
    panel.summary = summarizeStatuses(panel.rows)
    panel.checkedAt = new Date()
    return true
  } catch (err) {
    if (err instanceof GitHubApiError) {
      panel.error = err.message
      if (err.kind === 'rate-limit') notify.warning(err.message)
      return err.kind !== 'rate-limit'
    }
    panel.error = 'Could not match the hashes against registered plugins.'
    notify.error(panel.error, err)
    return true
  } finally {
    panel.loading = false
  }
}

async function checkAll() {
  checkingAll.value = true
  try {
    for (const watched of watchedRepos.value) {
      const keepGoing = await checkRepo(watched)
      if (!keepGoing) break
    }
  } finally {
    checkingAll.value = false
  }
}

function closePanel(panel) {
  panels.value = panels.value.filter((p) => p.key !== panel.key)
}

async function refreshMatches(panel) {
  if (!panel.rows) return
  try {
    const matches = await matchHashes(panel.rows)
    panel.rows = applyMatches(panel.rows, matches)
    panel.summary = summarizeStatuses(panel.rows)
  } catch (err) {
    notify.error('Could not refresh the statuses.', err)
  }
}

function openRegister(panel) {
  registerRows.value = registrableFor(panel)
  registerRepo.value = { owner: panel.owner, repo: panel.repo }
  registerPanelKey = panel.key
  registerOpen.value = true
}

async function onRegistered() {
  const panel = panels.value.find((p) => p.key === registerPanelKey)
  if (panel) await refreshMatches(panel)
}

async function copyHash(hash) {
  try {
    await navigator.clipboard.writeText(hash)
    notify.info('Hash copied.')
  } catch {
    notify.warning('Could not copy to the clipboard.')
  }
}

onMounted(loadWatched)
</script>

<style scoped>
.page-title {
  font-size: 2.5rem;
}
.helper-text {
  color: #b0b0b0;
}
.error-text {
  color: #ef9a9a;
}
.repo-input {
  max-width: 520px;
  flex: 1 1 320px;
}
.panel-link {
  color: #e2e2e2;
  text-decoration: none;
}
.panel-link:hover {
  text-decoration: underline;
}
.checked-at {
  font-size: 0.75em;
  color: #9e9e9e;
  font-weight: normal;
}
.mono {
  font-family: monospace;
  font-size: 0.9em;
}
.hash-cell {
  color: #9e9e9e;
  white-space: nowrap;
}
.hash-source {
  margin-left: 0.3em;
  padding: 0 0.4em;
  border-radius: 4px;
  background-color: #2e2e2e;
  color: #9e9e9e;
  font-size: 0.75em;
  font-family: sans-serif;
}
.pre-marker {
  margin-left: 0.4em;
  padding: 0 0.4em;
  border-radius: 4px;
  background-color: #4a3f1f;
  color: #ffd54f;
  font-size: 0.7em;
  text-transform: uppercase;
}
.status-detail {
  display: block;
  font-size: 0.8em;
  color: #9e9e9e;
}
.status-pill {
  display: inline-block;
  padding: 0.1em 0.6em;
  border-radius: 999px;
  font-size: 0.8em;
  white-space: nowrap;
  background-color: #2e2e2e;
  color: #e2e2e2;
}
.status-pill--registered {
  background-color: #1f3d2a;
  color: #81c784;
}
.status-pill--unregistered {
  background-color: #4a3f1f;
  color: #ffd54f;
}
.status-pill--unseen {
  background-color: #263544;
  color: #90caf9;
}
.status-pill--no-digest,
.status-pill--archive,
.status-pill--no-asset,
.status-pill--muted {
  background-color: #2e2e2e;
  color: #9e9e9e;
}
.action-button {
  background-color: #2e2e2e;
  color: #e2e2e2;
}
.btn {
  text-transform: unset;
  letter-spacing: 0.009375em;
  font-size: medium;
}
</style>

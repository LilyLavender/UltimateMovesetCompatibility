<template>
  <PageShell
    title="Repo releases"
    tier="wide"
    :back-to="{ name: 'AdminPortal' }"
    back-label="Admin portal"
    lede="Reads a GitHub repository's releases and checks them against the database. GitHub allows 60 requests an hour per IP."
  >
    <!-- Repo input -->
    <div class="repo-form">
      <LabeledField label="GitHub repository" class="repo-form__input">
        <v-text-field
          v-model="repoInput"
          placeholder="owner/repo or https://github.com/owner/repo"
          density="comfortable"
          hide-details
          @keyup.enter="checkInput"
        />
      </LabeledField>
      <AppButton icon="mdi-magnify" :disabled="!parsedInput" @click="checkInput">Check</AppButton>
      <AppButton
        variant="ghost"
        icon="mdi-playlist-plus"
        :disabled="!parsedInput"
        :busy="adding"
        @click="addToList"
      >
        Add to list
      </AppButton>
    </div>

    <!-- Watched repos -->
    <SectionHeading title="Watched repositories" :count="watchedRepos.length">
      <AppButton
        v-if="watchedRepos.length"
        size="sm"
        icon="mdi-refresh"
        :busy="checkingAll"
        class="ml-auto"
        @click="checkAll"
      >
        Check all
      </AppButton>
    </SectionHeading>
    <EmptyState
      v-if="watchedRepos.length === 0"
      message="No watched repositories yet."
      icon="mdi-github"
    />
    <TableScroll v-else min-width="560px">
      <v-table density="comfortable">
        <thead>
          <tr>
            <th>Repository</th>
            <th>Added by</th>
            <th class="text-right"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="watched in watchedRepos" :key="watched.watchedRepoId">
            <td>
              <a :href="repoUrl(watched)" target="_blank" rel="noopener" class="mono">
                {{ watched.owner }}/{{ watched.repo }}
              </a>
            </td>
            <td class="muted">{{ watched.addedByUsername ?? 'unknown' }}</td>
            <td class="text-right">
              <span class="row-actions">
                <AppButton
                  size="sm"
                  variant="ghost"
                  :busy="panelFor(watched)?.loading"
                  @click="checkRepo(watched)"
                >
                  Check
                </AppButton>
                <AppButton
                  size="sm"
                  variant="ghost"
                  icon="mdi-delete"
                  aria-label="Remove from list"
                  @click="removeWatched(watched)"
                />
              </span>
            </td>
          </tr>
        </tbody>
      </v-table>
    </TableScroll>

    <!-- Results -->
    <template v-if="panels.length > 0">
      <SectionHeading title="Results" :count="panels.length" />

      <section v-for="panel in panels" :key="panel.key" class="panel result">
        <div class="result__head">
          <a :href="repoUrl(panel)" target="_blank" rel="noopener" class="result__repo mono">
            {{ panel.owner }}/{{ panel.repo }}
          </a>
          <span v-if="panel.checkedAt" class="result__checked">
            checked {{ panel.checkedAt.toLocaleTimeString() }}
          </span>
          <span class="result__spacer"></span>
          <template v-if="panel.rows">
            <StatusTag variant="ok">{{ panel.summary.registered }} registered</StatusTag>
            <StatusTag variant="warn">{{ panel.summary.unregistered }} looked up</StatusTag>
            <StatusTag variant="info">{{ panel.summary.unseen }} unseen</StatusTag>
            <StatusTag v-if="panel.summary.noDigest" variant="outline">
              {{ panel.summary.noDigest }} unhashed
            </StatusTag>
            <AppButton
              size="sm"
              icon="mdi-plus"
              :disabled="registrableFor(panel).length === 0"
              @click="openRegister(panel)"
            >
              Register unregistered ({{ registrableFor(panel).length }})
            </AppButton>
          </template>
          <AppButton
            size="sm"
            variant="ghost"
            icon="mdi-close"
            aria-label="Close"
            @click="closePanel(panel)"
          />
        </div>

        <AppLoading v-if="panel.loading" size="sm" :label="panel.progress" />
        <p v-else-if="panel.error" class="note note--err">{{ panel.error }}</p>
        <p v-else-if="visibleRows(panel).length === 0" class="faint">
          No .nro assets in this repository's releases.
        </p>
        <TableScroll v-else min-width="900px">
          <v-data-table
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
              <StatusTag v-if="item.prerelease" variant="outline" class="ml-2">pre</StatusTag>
            </template>
            <template #item.name="{ item }">
              <a v-if="item.releaseUrl" :href="item.releaseUrl" target="_blank" rel="noopener">
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
                class="mono"
              >
                {{ item.asset }}
              </a>
              <span v-else class="faint">none</span>
            </template>
            <template #item.downloads="{ item }">
              {{ item.downloads == null ? '' : item.downloads.toLocaleString() }}
            </template>
            <template #item.hash="{ item }">
              <span v-if="item.hash" class="hash-cell mono" :title="hashTitle(item)">
                {{ item.hash.slice(0, 12) }}&hellip;
                <StatusTag v-if="item.hashSource === 'server'" variant="outline">server</StatusTag>
                <button
                  type="button"
                  class="copy-btn"
                  aria-label="Copy hash"
                  @click="copyHash(item.hash)"
                >
                  <v-icon size="14">mdi-content-copy</v-icon>
                </button>
              </span>
            </template>
            <template #item.status="{ item }">
              <StatusTag :variant="STATUS_TONES[item.status] ?? 'outline'">
                {{ statusLabel(item) }}
              </StatusTag>
              <span v-if="statusDetail(item)" class="status-detail">{{ statusDetail(item) }}</span>
            </template>
          </v-data-table>
        </TableScroll>
      </section>
    </template>

    <PluginBatchRegisterDialog
      v-model="registerOpen"
      :rows="registerRows"
      :repo="registerRepo"
      @registered="onRegistered"
    />
  </PageShell>
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
import PageShell from '@/components/PageShell.vue'
import SectionHeading from '@/components/SectionHeading.vue'
import LabeledField from '@/components/LabeledField.vue'
import AppButton from '@/components/AppButton.vue'
import AppLoading from '@/components/AppLoading.vue'
import StatusTag from '@/components/StatusTag.vue'
import TableScroll from '@/components/TableScroll.vue'
import EmptyState from '@/components/EmptyState.vue'
import PluginBatchRegisterDialog from '@/components/PluginBatchRegisterDialog.vue'

const MATCH_HASHES_MAX = 200

const STATUS_TONES = {
  [RowStatus.Registered]: 'ok',
  [RowStatus.Unregistered]: 'warn',
  [RowStatus.Unseen]: 'info',
}

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
  { title: 'Release date', key: 'releaseDate', width: 120 },
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

// Second line under the status tag: what a registered hash is,
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
    panel.progress = `Hashing asset ${i + 1} of ${missing.length} without a GitHub digest`
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
  panel.progress = 'Reading releases'
  panel.error = null
  try {
    const releases = await fetchAllReleases(target.owner, target.repo)
    let rows = flattenReleaseAssets(releases).filter(isNroRow)
    rows = await fillMissingHashes(rows, panel)
    panel.progress = 'Matching against registered plugins'
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
.repo-form {
  display: flex;
  align-items: flex-end;
  flex-wrap: wrap;
  gap: 10px;
}

.repo-form__input {
  flex: 1 1 320px;
  max-width: 520px;
}

.ml-auto {
  margin-left: auto;
}

.row-actions {
  display: inline-flex;
  gap: 4px;
}

.result {
  margin-bottom: 20px;
}

.result__head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  margin-bottom: 14px;
}

.result__repo {
  font-size: 15px;
  font-weight: 600;
  text-decoration: none;
}

.result__repo:hover {
  text-decoration: underline;
}

.result__checked {
  font-size: 12px;
  color: var(--tx-3);
}

.result__spacer {
  flex: 1;
}

.hash-cell {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  color: var(--tx-2);
  white-space: nowrap;
}

.copy-btn {
  display: inline-flex;
  padding: 2px;
  border: 0;
  background: none;
  color: var(--tx-2);
  cursor: pointer;
}

.copy-btn:hover {
  color: var(--white);
}

.status-detail {
  display: block;
  margin-top: 3px;
  font-size: 12px;
  color: var(--tx-3);
}

.note {
  margin: 0;
  padding: 10px 14px;
  border: 1px solid var(--line-2);
  border-left: 4px solid var(--err);
  background: var(--panel-2);
  font-size: 14px;
}
</style>

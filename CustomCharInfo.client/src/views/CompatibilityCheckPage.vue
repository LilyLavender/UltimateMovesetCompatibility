<template>
  <div class="compat-page">
    <h1 class="page-title no-select">Compatibility Check</h1>

    <!-- Mode switch -->
    <div class="mode-row">
      <button
        type="button"
        :class="['mode-btn', mode === 'check' ? 'mode-btn--active' : '']"
        @click="mode = 'check'"
      >
        <span class="mdi mdi-swap-horizontal" /> Check compatibility
      </button>
      <button
        type="button"
        :class="['mode-btn', mode === 'group' ? 'mode-btn--active' : '']"
        @click="mode = 'group'"
      >
        <span class="mdi mdi-checkbox-multiple-marked-outline" /> Report a group
      </button>
    </div>

    <p v-if="mode === 'check'" class="subtitle">
      Click two movesets to check if they work together, or select up to {{ MAX_PREVIEW }} to
      compare every pair at once.
    </p>
    <p v-else class="subtitle">
      Select every moveset you run together (up to {{ MAX_GROUP }}) and report all of them as
      compatible with each other in one go.
    </p>

    <!-- Group selection bar -->
    <div v-if="mode === 'group'" class="group-bar">
      <span class="group-bar__count">
        <strong>{{ selection.length }}</strong> selected
        <span v-if="selection.length >= 2" class="group-bar__pairs">· {{ pairCount }} pairs</span>
      </span>
      <div class="group-bar__actions">
        <v-btn
          size="small"
          variant="text"
          :disabled="selection.length === 0"
          @click="clearGroupSelection"
        >
          Clear
        </v-btn>
        <v-btn
          size="small"
          variant="tonal"
          color="primary"
          :disabled="selection.length < 2 || groupReview.loading"
          :loading="groupReview.loading"
          @click="startGroupReview"
        >
          <v-icon start size="small">mdi-clipboard-check-outline</v-icon> Review and submit
        </v-btn>
      </div>
    </div>

    <!-- Moveset grid -->
    <div v-if="loading" class="loading-msg">Loading movesets…</div>
    <div v-else class="moveset-grid">
      <button
        v-for="m in visibleMovesets"
        :key="m.movesetId"
        class="ms-card"
        :class="{
          'ms-card--selected': isSelected(m),
          'ms-card--dimmed': isDimmed(m),
          'ms-card--preview': showVotePreview(m),
          'ms-card--vote-preview': showVotePreview(m) && !pairSummary(m.movesetId),
        }"
        :style="{
          '--bg-color': showVotePreview(m) ? '#808080' : `#${normalizedBgColor(m)}`,
        }"
        @click="toggleSelect(m)"
      >
        <div class="ms-card__thumb" :style="thumbStyle(m)" />
        <div class="ms-card__overlay" :style="overlayStyle(m)" />
        <div class="ms-card__shade" />
        <span class="ms-card__name"
          >{{ m.moddedCharName
          }}<span v-if="m.subtitle" class="ms-card__subtitle"> ({{ m.subtitle }})</span></span
        >
        <!-- Vote bar: shown when one moveset is selected and this card has vote data -->
        <div v-if="showVotePreview(m) && pairSummary(m.movesetId)" class="ms-card__vote-bar">
          <div class="ms-card__vote-bar-compat" :style="{ width: compatPct(m.movesetId) + '%' }" />
          <div class="ms-card__vote-bar-incompat" />
        </div>
      </button>
    </div>

    <!-- Result area: the panels swap with a short slide and cross-blur -->
    <Transition name="panel" mode="out-in">
      <!-- Predicted pair list: three or more selected in check mode, or the group review step -->
      <div v-if="pairPanel" key="pairs" class="result-section group-review">
        <Transition name="panel" mode="out-in">
          <div v-if="pairPanel.loading" key="loading" class="checking-msg">
            <v-progress-circular indeterminate size="24" />
            Checking…
          </div>

          <div v-else-if="pairPanel.error" key="error" class="verdict-banner verdict--bad">
            <v-icon class="verdict-icon">mdi-alert-circle</v-icon>
            <div class="verdict-body">
              <span class="verdict-text">{{ pairPanel.error }}</span>
            </div>
          </div>

          <div v-else key="content">
            <p v-if="pairPanel.pairs === null && mode === 'check'" class="group-review__note">
              Predicted results are available for up to {{ MAX_PREVIEW }} movesets. Deselect a few
              to see them.
            </p>
            <p v-else-if="pairPanel.pairs === null" class="group-review__note">
              Groups larger than {{ MAX_PREVIEW }} skip the predicted-conflict preview.
            </p>
            <template v-else>
              <p v-if="mode === 'group'" class="group-review__note">
                Predicted results for each pair. Only report the group if you actually ran these
                movesets together.
              </p>
              <p v-else class="group-review__note">
                Predicted results for each pair. Select exactly two movesets for the full breakdown
                and community votes.
              </p>
              <div class="group-pairs">
                <div
                  v-for="pair in panelConflicts"
                  :key="pair.key"
                  :class="['group-pair', GROUP_SEVERITY[pair.severity].cls]"
                >
                  <v-icon size="small" class="group-pair__icon">{{
                    GROUP_SEVERITY[pair.severity].icon
                  }}</v-icon>
                  <span class="group-pair__names">{{ pair.nameA }} + {{ pair.nameB }}</span>
                  <span class="group-pair__label">{{ GROUP_SEVERITY[pair.severity].label }}</span>
                  <span v-if="pair.hookOffsets.length" class="group-pair__hooks">
                    shared hook{{ pair.hookOffsets.length === 1 ? '' : 's' }}:
                    {{ pair.hookOffsets.map((o) => `0x${o}`).join(', ') }}
                  </span>
                  <span v-else-if="pair.articleCount" class="group-pair__hooks">
                    {{ pair.articleCount }} shared article{{ pair.articleCount === 1 ? '' : 's' }}
                  </span>
                </div>

                <!-- Every clean pair collapses into one block -->
                <div v-if="panelClear.length" class="group-pair group-clear verdict--good">
                  <div class="group-clear__header">
                    <v-icon size="small" class="group-pair__icon">mdi-check-circle</v-icon>
                    <span class="group-pair__names">No issues between:</span>
                  </div>
                  <div class="group-clear__chips">
                    <span v-for="pair in panelClear" :key="pair.key" class="group-chip">
                      {{ pair.nameA }} + {{ pair.nameB }}
                    </span>
                  </div>
                </div>
              </div>
            </template>

            <template v-if="mode === 'group'">
              <div v-if="user" class="group-review__confirm">
                <v-btn
                  :class="[
                    'report-btn',
                    hasIncompatiblePair ? 'report-btn--muted' : 'report-btn--active-compat',
                  ]"
                  size="small"
                  variant="tonal"
                  :loading="reportLoading"
                  @click="confirmOrSubmitGroup"
                >
                  <v-icon start>mdi-thumb-up-outline</v-icon>
                  Mark all {{ pairCount }} pairs compatible
                </v-btn>
                <span v-if="hasIncompatiblePair" class="group-review__warning">
                  At least one pair is predicted not compatible.
                </span>
              </div>
              <p v-else class="sign-in-note">Sign in to submit a report.</p>
            </template>
          </div>
        </Transition>
      </div>

      <!-- Checking spinner -->
      <div v-else-if="mode === 'check' && checking" key="checking" class="checking-msg">
        <v-progress-circular indeterminate size="24" />
        Checking…
      </div>

      <!-- Results -->
      <div v-else-if="mode === 'check' && result" key="result" class="result-section">
        <!-- Verdict banner -->
        <div :class="['verdict-banner', finalVerdictClass]">
          <v-icon class="verdict-icon">{{ finalVerdictIcon }}</v-icon>
          <div class="verdict-body">
            <span class="verdict-text">{{ finalVerdictText }}</span>
            <span v-if="communityFactored" class="verdict-note">{{ communityNote }}</span>
          </div>
        </div>

        <!-- Issues list -->
        <div v-if="result.issues.length > 0" class="issues-list">
          <div
            v-for="(issue, i) in result.issues"
            :key="i"
            :class="['issue-item', `issue-item--${issue.severity}`]"
          >
            <v-icon class="issue-icon" size="small">{{ severityIcon(issue.severity) }}</v-icon>
            <span>{{ issue.message }}</span>
          </div>
        </div>

        <!-- Community reports -->
        <div class="community-section">
          <!-- Vote banner -->
          <div v-if="totalVotes > 0" :class="['community-banner', communityBannerClass]">
            <v-icon class="community-banner-icon">{{ communityBannerIcon }}</v-icon>
            <div class="community-banner-body">
              <span class="community-banner-text">{{ communityBannerText }}</span>
              <span class="community-banner-sub"
                >{{ reports.compatibleCount }} compatible · {{ reports.incompatibleCount }} not
                compatible</span
              >
            </div>
          </div>
          <p v-else class="community-no-votes">No community reports yet for this combination.</p>

          <!-- User vote -->
          <div v-if="user" class="community-vote-row">
            <span class="report-label">Have you tested this combination?</span>
            <div class="report-btn-row">
              <v-btn
                :class="[
                  'report-btn',
                  reports.userVote === true ? 'report-btn--active-compat' : '',
                ]"
                size="small"
                variant="tonal"
                :loading="reportLoading"
                @click="submitReport(true)"
              >
                <v-icon start>mdi-thumb-up-outline</v-icon> Works Together
              </v-btn>
              <v-btn
                :class="[
                  'report-btn',
                  reports.userVote === false ? 'report-btn--active-incompat' : '',
                ]"
                size="small"
                variant="tonal"
                :loading="reportLoading"
                @click="submitReport(false)"
              >
                <v-icon start>mdi-thumb-down-outline</v-icon> Doesn't Work
              </v-btn>
            </div>
          </div>
          <p v-else class="sign-in-note">Sign in to submit a report.</p>
        </div>

        <!-- Moveset comparison panel -->
        <div v-if="result.a && result.b" class="compare-panel">
          <div class="compare-col">
            <div class="compare-header">
              <strong
                >{{ result.a.moddedCharName
                }}<span v-if="result.a.subtitle" class="compare-subtitle">
                  ({{ result.a.subtitle }})</span
                ></strong
              >
            </div>
            <div class="compare-meta">
              <img
                v-if="result.a.vanillaChar"
                :src="iconUrl(result.a.vanillaChar.vanillaCharInternalName)"
                class="meta-char-icon"
              />
              {{ result.a.vanillaChar?.displayName ?? '-' }}
              <span v-if="result.a.slotsStart != null" class="meta-slots"
                >(c{{ pad(result.a.slotsStart) }}–c{{ pad(result.a.slotsEnd) }})</span
              >
            </div>
            <div class="compare-section">
              <span class="compare-label">Articles ({{ result.a.movesetArticles.length }})</span>
              <ul class="compare-list">
                <li
                  v-for="ma in result.a.movesetArticles"
                  :key="ma.article.articleId"
                  :class="{
                    'compare-conflict': result.conflictingArticleIds.has(ma.article.articleId),
                  }"
                >
                  <v-icon
                    v-if="result.conflictingArticleIds.has(ma.article.articleId)"
                    size="x-small"
                    class="conflict-icon"
                    >mdi-alert</v-icon
                  >
                  {{ ma.article.vanillaCharInternalName }}_{{ ma.article.articleName }}
                </li>
                <li v-if="!result.a.movesetArticles.length" class="compare-none">none</li>
              </ul>
            </div>
            <div class="compare-section">
              <span class="compare-label">Hooks ({{ result.a.movesetHooks.length }})</span>
              <ul class="compare-list">
                <li
                  v-for="mh in result.a.movesetHooks"
                  :key="mh.hook.hookId"
                  :class="{ 'compare-conflict': result.conflictingHookIds.has(mh.hook.hookId) }"
                >
                  <v-icon
                    v-if="result.conflictingHookIds.has(mh.hook.hookId)"
                    size="x-small"
                    class="conflict-icon"
                    >mdi-alert</v-icon
                  >
                  0x{{ mh.hook.offset }}
                </li>
                <li v-if="!result.a.movesetHooks.length" class="compare-none">none</li>
              </ul>
            </div>
          </div>

          <div class="compare-divider" />

          <div class="compare-col">
            <div class="compare-header">
              <strong
                >{{ result.b.moddedCharName
                }}<span v-if="result.b.subtitle" class="compare-subtitle">
                  ({{ result.b.subtitle }})</span
                ></strong
              >
            </div>
            <div class="compare-meta">
              <img
                v-if="result.b.vanillaChar"
                :src="iconUrl(result.b.vanillaChar.vanillaCharInternalName)"
                class="meta-char-icon"
              />
              {{ result.b.vanillaChar?.displayName ?? '-' }}
              <span v-if="result.b.slotsStart != null" class="meta-slots"
                >(c{{ pad(result.b.slotsStart) }}–c{{ pad(result.b.slotsEnd) }})</span
              >
            </div>
            <div class="compare-section">
              <span class="compare-label">Articles ({{ result.b.movesetArticles.length }})</span>
              <ul class="compare-list">
                <li
                  v-for="ma in result.b.movesetArticles"
                  :key="ma.article.articleId"
                  :class="{
                    'compare-conflict': result.conflictingArticleIds.has(ma.article.articleId),
                  }"
                >
                  <v-icon
                    v-if="result.conflictingArticleIds.has(ma.article.articleId)"
                    size="x-small"
                    class="conflict-icon"
                    >mdi-alert</v-icon
                  >
                  {{ ma.article.vanillaCharInternalName }}_{{ ma.article.articleName }}
                </li>
                <li v-if="!result.b.movesetArticles.length" class="compare-none">none</li>
              </ul>
            </div>
            <div class="compare-section">
              <span class="compare-label">Hooks ({{ result.b.movesetHooks.length }})</span>
              <ul class="compare-list">
                <li
                  v-for="mh in result.b.movesetHooks"
                  :key="mh.hook.hookId"
                  :class="{ 'compare-conflict': result.conflictingHookIds.has(mh.hook.hookId) }"
                >
                  <v-icon
                    v-if="result.conflictingHookIds.has(mh.hook.hookId)"
                    size="x-small"
                    class="conflict-icon"
                    >mdi-alert</v-icon
                  >
                  0x{{ mh.hook.offset }}
                </li>
                <li v-if="!result.b.movesetHooks.length" class="compare-none">none</li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </Transition>

    <!-- Confirm a group report that includes a predicted-incompatible pair -->
    <v-dialog v-model="confirmDialog" max-width="480">
      <v-card color="#2e2e2e">
        <v-card-title>
          <v-icon class="mr-1">mdi-alert</v-icon>
          Report anyway?
        </v-card-title>
        <v-card-text>
          At least one pair in this group is predicted not compatible. Only mark all
          {{ pairCount }} pairs compatible if you have actually run every one of these movesets
          together without problems.
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="confirmDialog = false">Cancel</v-btn>
          <v-btn class="report-btn--active-compat" :loading="reportLoading" @click="submitGroup">
            Mark all compatible
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import api from '@/services/api'
import { useNotify } from '@/composables/useNotify'
import {
  ItemType,
  ReleaseState,
  RELEASE_STATE_NAMES,
  ALL_ACCEPTANCE_STATES,
  HookableStatus,
} from '@/globals'

const apiUrl = import.meta.env.VITE_API_URL

const ALLOWED_STATES = new Set(
  [ReleaseState.Released, ReleaseState.PendingUpdate, ReleaseState.OpenBeta].map(
    (id) => RELEASE_STATE_NAMES[id]
  )
)
const COMMUNITY_MIN_VOTES = 3
const COMMUNITY_THRESHOLD = 0.65

// Group mode limits should mirror CompatibilityController
const MAX_GROUP = 25
const MAX_PREVIEW = 10
const SEVERITY_RANK = { compatible: 0, warning: 1, 'predicted-incompat': 2, incompatible: 3 }
const GROUP_SEVERITY = {
  compatible: { cls: 'verdict--good', icon: 'mdi-check-circle', label: 'No issues' },
  warning: { cls: 'verdict--warn', icon: 'mdi-alert', label: 'Caveats' },
  'predicted-incompat': {
    cls: 'verdict--predicted-bad',
    icon: 'mdi-alert-circle',
    label: 'Likely not compatible',
  },
  incompatible: { cls: 'verdict--bad', icon: 'mdi-close-circle', label: 'Not compatible' },
}

const notify = useNotify()

const mode = ref('check')
const groupReview = ref({ open: false, loading: false, pairs: null, error: null })
// Predicted pairs for three or more selected in check mode.
const multiCheck = ref({ loading: false, pairs: null, error: null })
const hooksById = ref(null)

const loading = ref(false)
const checking = ref(false)
const reportLoading = ref(false)

const movesets = ref([])
const hardHeldIds = ref(new Set())
const user = ref(null)

const selection = ref([])
const result = ref(null)
const reports = ref({ compatibleCount: 0, incompatibleCount: 0, userVote: null })

// Map<partnerMovesetId, {compatibleCount, incompatibleCount}>
const pairSummaries = ref(new Map())

const visibleMovesets = computed(() =>
  movesets.value.filter(
    (m) =>
      !m.privateMoveset && !hardHeldIds.value.has(m.movesetId) && ALLOWED_STATES.has(m.releaseState)
  )
)

const isSelected = (m) => selection.value.some((s) => s.movesetId === m.movesetId)

const pairSummary = (movesetId) => pairSummaries.value.get(movesetId) ?? null

const compatPct = (movesetId) => {
  const s = pairSummary(movesetId)
  if (!s) return 0
  const total = s.compatibleCount + s.incompatibleCount
  return total === 0 ? 0 : Math.round((s.compatibleCount / total) * 100)
}

const pairCount = computed(() => (selection.value.length * (selection.value.length - 1)) / 2)

// Check mode stops at the predict endpoint's limit; group mode at the batch report's.
const selectionCap = computed(() => (mode.value === 'check' ? MAX_PREVIEW : MAX_GROUP))

const isDimmed = (m) => !isSelected(m) && selection.value.length >= selectionCap.value

const showVotePreview = (m) =>
  mode.value === 'check' && selection.value.length === 1 && !isSelected(m)

// The predicted pair list shown for three or more in check mode, or during the group review step.
const pairPanel = computed(() => {
  if (mode.value === 'check') return selection.value.length > 2 ? multiCheck.value : null
  return groupReview.value.open ? groupReview.value : null
})

// Conflicting pairs list individually; clean pairs collapse into one "no issues" block.
const panelConflicts = computed(() =>
  (pairPanel.value?.pairs ?? []).filter((p) => p.severity !== 'compatible')
)
const panelClear = computed(() =>
  (pairPanel.value?.pairs ?? []).filter((p) => p.severity === 'compatible')
)
const hasIncompatiblePair = computed(() =>
  (pairPanel.value?.pairs ?? []).some((p) => p.severity === 'incompatible')
)

const confirmDialog = ref(false)

const resetGroupReview = () => {
  groupReview.value = { open: false, loading: false, pairs: null, error: null }
}

const toggleSelect = (m) => {
  const idx = selection.value.findIndex((s) => s.movesetId === m.movesetId)
  resetGroupReview()
  if (idx !== -1) selection.value.splice(idx, 1)
  else if (selection.value.length < selectionCap.value) selection.value.push(m)
}

const clearGroupSelection = () => {
  selection.value = []
  resetGroupReview()
}

const loadHooksById = async () => {
  if (hooksById.value) return hooksById.value
  const res = await api.get('/hooks')
  hooksById.value = new Map(res.data.map((h) => [h.hookId, h]))
  return hooksById.value
}

// Runs the N-way prediction for the current selection and shapes it for the pair list.
// Returns null when the selection is too large for the predict endpoint.
const buildPredictedPairs = async () => {
  if (selection.value.length > MAX_PREVIEW) return null

  const ids = selection.value.map((s) => s.movesetId)
  const [predictRes, hooks] = await Promise.all([
    api.get('/compatibility/predict', { params: { movesets: ids.join(',') } }),
    loadHooksById(),
  ])
  const nameOf = (id) => {
    const m = selection.value.find((s) => s.movesetId === id)
    return m ? `${m.moddedCharName}${m.subtitle ? ` (${m.subtitle})` : ''}` : `#${id}`
  }
  const pairs = predictRes.data.pairs.map((p) => ({
    key: `${p.moveset1.movesetId}-${p.moveset2.movesetId}`,
    nameA: nameOf(p.moveset1.movesetId),
    nameB: nameOf(p.moveset2.movesetId),
    severity: p.severity,
    hookOffsets: p.conflictingHookIds.map((id) => hooks.get(id)?.offset ?? String(id)),
    articleCount: p.conflictingArticleIds.length,
  }))
  pairs.sort((a, b) => SEVERITY_RANK[b.severity] - SEVERITY_RANK[a.severity])
  return pairs
}

const PREDICT_ERROR = 'Could not load the predicted results. Please try again.'

const runMultiCheck = async () => {
  const ids = selection.value.map((s) => s.movesetId).join(',')
  multiCheck.value = { loading: true, pairs: null, error: null }
  try {
    const pairs = await buildPredictedPairs()
    if (selection.value.map((s) => s.movesetId).join(',') !== ids) return
    multiCheck.value = { loading: false, pairs, error: null }
  } catch {
    multiCheck.value = { loading: false, pairs: null, error: PREDICT_ERROR }
  }
}

const startGroupReview = async () => {
  if (selection.value.length < 2) return
  groupReview.value = { open: true, loading: true, pairs: null, error: null }
  try {
    const pairs = await buildPredictedPairs()
    groupReview.value = { open: true, loading: false, pairs, error: null }
  } catch {
    groupReview.value = { open: true, loading: false, pairs: null, error: PREDICT_ERROR }
  }
}

// A predicted-incompatible pair in the group asks for a second look before the report goes in.
const confirmOrSubmitGroup = () => {
  if (hasIncompatiblePair.value) confirmDialog.value = true
  else submitGroup()
}

const submitGroup = async () => {
  if (selection.value.length < 2) return
  reportLoading.value = true
  try {
    const res = await api.post('/compatibility/batch', {
      movesetIds: selection.value.map((s) => s.movesetId),
    })
    notify.success(`Marked ${res.data.pairCount} pairs as compatible. Thanks for reporting!`)
    confirmDialog.value = false
    clearGroupSelection()
  } catch (err) {
    notify.error('Could not submit the group report.', err)
  } finally {
    reportLoading.value = false
  }
}

const thumbStyle = (m) => {
  const raw = m.thumbhImageUrl
  if (!raw) return {}
  const url = raw.startsWith('/') ? `${apiUrl}${raw}` : raw
  return {
    backgroundImage: `url('${url}')`,
    backgroundSize: 'auto 100%',
    backgroundPosition: 'right center',
    backgroundRepeat: 'no-repeat',
  }
}

const normalizedBgColor = (m) => {
  const c = m.backgroundColor?.toLowerCase()
  if (!c || c === 'ffffff' || c === '000000') return '808080'
  return c
}

const overlayStyle = (m) => {
  const c = normalizedBgColor(m)
  return {
    background: `linear-gradient(to right, #${c}f2 0%, #${c}c8 50%, #${c}50 80%, #${c}10 100%)`,
  }
}

function pad(n) {
  return String(n).padStart(3, '0')
}
function iconUrl(internalName) {
  return `${import.meta.env.BASE_URL}vanilla-stock-icons/chara_2_${internalName}.png`
}

const runCheck = async () => {
  if (selection.value.length < 2) return
  checking.value = true
  result.value = null

  try {
    const [resA, resB, reportsRes, predictRes] = await Promise.all([
      api.get(`/movesets/${selection.value[0].movesetId}`),
      api.get(`/movesets/${selection.value[1].movesetId}`),
      api
        .get('/compatibility', {
          params: {
            moveset1: selection.value[0].movesetId,
            moveset2: selection.value[1].movesetId,
          },
        })
        .catch(() => ({ data: { compatibleCount: 0, incompatibleCount: 0, userVote: null } })),
      api.get('/compatibility/predict', {
        params: { movesets: `${selection.value[0].movesetId},${selection.value[1].movesetId}` },
      }),
    ])

    const a = resA.data
    const b = resB.data
    reports.value = reportsRes.data

    // The predict endpoint is the source of truth for which hooks/articles conflict and the overall severity.
    // Message text is built here from data already in `a`/`b`.
    const pair = predictRes.data.pairs[0]
    const conflictingHookIds = new Set(pair.conflictingHookIds)
    const conflictingArticleIds = new Set(pair.conflictingArticleIds)

    const issues = []

    for (const hookId of conflictingHookIds) {
      const mhA = (a.movesetHooks ?? []).find((h) => h.hook.hookId === hookId)
      if (!mhA) continue
      const sid = mhA.hook.hookableStatusId
      if (sid === HookableStatus.MoreThanOnce) {
        issues.push({
          severity: 'warning',
          message: `Both movesets use hook 0x${mhA.hook.offset}. This hook supports multiple uses, but too many at the same offset may still cause issues.`,
        })
      } else if (sid === HookableStatus.OnlyOnce) {
        issues.push({
          severity: 'incompatible',
          message: `Both movesets use hook 0x${mhA.hook.offset} - this hook can only be used once and will cause a crash.`,
        })
      } else {
        issues.push({
          severity: 'predicted-incompat',
          message: `Both movesets use hook 0x${mhA.hook.offset}. This hook's behavior with multiple users is untested. It will likely crash.`,
        })
      }
    }

    const sameChar =
      a.vanillaChar?.vanillaCharInternalName === b.vanillaChar?.vanillaCharInternalName
    for (const articleId of conflictingArticleIds) {
      const maA = (a.movesetArticles ?? []).find((art) => art.article.articleId === articleId)
      if (!maA) continue
      if (sameChar) {
        issues.push({
          severity: 'incompatible',
          message: `Both movesets are on ${a.vanillaChar?.displayName ?? 'the same character'} and clone the same article (${maA.article.vanillaCharInternalName}_${maA.article.articleName}). The article will not work correctly.`,
        })
      } else {
        issues.push({
          severity: 'warning',
          message: `Both movesets clone the article ${maA.article.vanillaCharInternalName}_${maA.article.articleName} and use overlapping slots. It's recommended to switch the slots of one moveset to reduce article conflicts.`,
        })
      }
    }

    result.value = { issues, a, b, conflictingHookIds, conflictingArticleIds }
  } catch {
    result.value = {
      issues: [{ severity: 'error', message: 'Failed to load moveset data. Please try again.' }],
      a: null,
      b: null,
      conflictingHookIds: new Set(),
      conflictingArticleIds: new Set(),
    }
  } finally {
    checking.value = false
  }
}

// Reacts to the selection in check mode:
// 1 -> nothing
// 2 -> full breakdown
// 3+ -> pair list
// Group mode waits for the review button instead.
const syncCheckMode = async () => {
  if (mode.value !== 'check') return
  if (selection.value.length > 2) {
    result.value = null
    pairSummaries.value = new Map()
    runMultiCheck()
  } else if (selection.value.length === 2) {
    runCheck()
  } else if (selection.value.length === 1) {
    result.value = null
    // Fetch vote summaries for the selected moveset
    try {
      const res = await api.get('/compatibility/summary', {
        params: { moveset: selection.value[0].movesetId },
      })
      const map = new Map()
      for (const entry of res.data) {
        map.set(entry.movesetId, {
          compatibleCount: entry.compatibleCount,
          incompatibleCount: entry.incompatibleCount,
        })
      }
      pairSummaries.value = map
    } catch {
      pairSummaries.value = new Map()
    }
  } else {
    result.value = null
    pairSummaries.value = new Map()
  }
}

// Watch the IDs so any change to the selection triggers a re-check
watch(() => selection.value.map((s) => s.movesetId).join(','), syncCheckMode)

// Switching modes keeps the selection and re-derives what to show for it.
watch(mode, () => {
  result.value = null
  pairSummaries.value = new Map()
  multiCheck.value = { loading: false, pairs: null, error: null }
  resetGroupReview()
  syncCheckMode()
})

// Auto-detected severity
const autoSeverity = computed(() => {
  if (!result.value) return null
  const issues = result.value.issues
  if (issues.some((i) => i.severity === 'incompatible')) return 'incompatible'
  if (issues.some((i) => i.severity === 'predicted-incompat')) return 'predicted-incompat'
  if (issues.some((i) => i.severity === 'warning')) return 'warning'
  return 'compatible'
})

const communitySignal = computed(() => {
  const total = reports.value.compatibleCount + reports.value.incompatibleCount
  if (total < COMMUNITY_MIN_VOTES) return null
  const ratio = reports.value.compatibleCount / total
  if (ratio >= COMMUNITY_THRESHOLD) return 'compatible'
  if (ratio <= 1 - COMMUNITY_THRESHOLD) return 'incompatible'
  return null
})

const autoAsSignal = (s) => (s === 'compatible' || s === 'warning' ? 'compatible' : 'incompatible')

const finalSeverity = computed(() => {
  const auto = autoSeverity.value
  const community = communitySignal.value
  if (!auto) return null
  if (!community || community === autoAsSignal(auto)) return auto
  if (community === 'compatible') {
    if (auto === 'predicted-incompat') return 'community-compat'
    if (auto === 'incompatible') return 'community-compat'
    if (auto === 'warning') return 'compatible'
  }
  if (community === 'incompatible') {
    if (auto === 'compatible' || auto === 'warning') return 'community-incompat'
  }
  return auto
})

const communityFactored = computed(
  () => communitySignal.value !== null && communitySignal.value !== autoAsSignal(autoSeverity.value)
)

const communityNote = computed(() => {
  const total = reports.value.compatibleCount + reports.value.incompatibleCount
  if (communitySignal.value === 'compatible')
    return `${reports.value.compatibleCount} of ${total} users report it works.`
  if (communitySignal.value === 'incompatible')
    return `${reports.value.incompatibleCount} of ${total} users report issues.`
  return ''
})

const totalVotes = computed(() => reports.value.compatibleCount + reports.value.incompatibleCount)

const communityBannerClass = computed(() => {
  if (reports.value.compatibleCount > reports.value.incompatibleCount)
    return 'community-banner--compat'
  if (reports.value.incompatibleCount > reports.value.compatibleCount)
    return 'community-banner--incompat'
  return 'community-banner--neutral'
})

const communityBannerIcon = computed(() => {
  if (reports.value.compatibleCount > reports.value.incompatibleCount) return 'mdi-thumb-up'
  if (reports.value.incompatibleCount > reports.value.compatibleCount) return 'mdi-thumb-down'
  return 'mdi-scale-balance'
})

const communityBannerText = computed(() => {
  if (reports.value.compatibleCount > reports.value.incompatibleCount)
    return 'Users are reporting this combination as compatible'
  if (reports.value.incompatibleCount > reports.value.compatibleCount)
    return 'Users are reporting this combination as not compatible'
  return 'Community reports are evenly split'
})

const VERDICT_MAP = {
  compatible: {
    cls: 'verdict--good',
    icon: 'mdi-check-circle',
    text: 'Most likely compatible. No Issues Detected',
  },
  warning: { cls: 'verdict--warn', icon: 'mdi-alert', text: 'Compatible with Caveats' },
  'predicted-incompat': {
    cls: 'verdict--predicted-bad',
    icon: 'mdi-alert-circle',
    text: 'Likely Not Compatible',
  },
  incompatible: { cls: 'verdict--bad', icon: 'mdi-close-circle', text: 'Not Compatible' },
  'community-compat': {
    cls: 'verdict--community-compat',
    icon: 'mdi-account-check',
    text: 'Users Report Compatible',
  },
  'community-incompat': {
    cls: 'verdict--community-incompat',
    icon: 'mdi-account-cancel',
    text: 'Users Report Issues',
  },
}

const finalVerdictClass = computed(() => VERDICT_MAP[finalSeverity.value]?.cls ?? '')
const finalVerdictIcon = computed(() => VERDICT_MAP[finalSeverity.value]?.icon ?? 'mdi-help-circle')
const finalVerdictText = computed(() => VERDICT_MAP[finalSeverity.value]?.text ?? '')
const severityIcon = (s) =>
  ({
    incompatible: 'mdi-close-circle',
    'predicted-incompat': 'mdi-alert-circle',
    warning: 'mdi-alert',
  })[s] ?? 'mdi-information'

const submitReport = async (isCompatible) => {
  if (!result.value || selection.value.length < 2) return
  reportLoading.value = true
  try {
    const res = await api.post('/compatibility', {
      movesetId1: selection.value[0].movesetId,
      movesetId2: selection.value[1].movesetId,
      isCompatible,
    })
    reports.value = res.data
  } catch {
    /**/
  } finally {
    reportLoading.value = false
  }
}

onMounted(async () => {
  loading.value = true
  try {
    const [msRes, userRes, logsRes] = await Promise.allSettled([
      api.get('/movesets'),
      api.get('/auth/me'),
      api.get('/logs', {
        params: { acceptanceStates: ALL_ACCEPTANCE_STATES, itemTypes: [ItemType.Moveset] },
      }),
    ])
    if (msRes.status === 'fulfilled') movesets.value = msRes.value.data
    if (userRes.status === 'fulfilled') user.value = userRes.value.data
    if (logsRes.status === 'fulfilled') {
      const latest = new Map()
      for (const log of logsRes.value.data) {
        const id = log.item?.movesetId
        if (id == null) continue
        const cur = latest.get(id)
        if (!cur || new Date(log.createdAt) > new Date(cur.createdAt)) latest.set(id, log)
      }
      const held = new Set()
      for (const [id, log] of latest) {
        if ([2, 4].includes(log.acceptanceState?.acceptanceStateId)) held.add(id)
      }
      hardHeldIds.value = held
    }
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.compat-page {
  max-width: 1100px;
  margin: 0 auto;
  padding: 2rem 1.5rem 4rem;

  /* Motion tokens shared by every transition on this page */
  --duration-quick: 150ms;
  --duration-fast: 250ms;
  --duration-medium: 350ms;
  --duration-slow: 400ms;
  --ease-smooth-out: cubic-bezier(0.22, 1, 0.36, 1);
  --panel-open-dur: var(--duration-slow);
  --panel-close-dur: var(--duration-medium);
  --panel-translate-y: 12px;
  --panel-blur: 2px;
  --panel-ease: var(--ease-smooth-out);
}

/* Panel reveal */
.panel-enter-active {
  transition:
    transform var(--panel-open-dur) var(--panel-ease),
    opacity var(--panel-open-dur) var(--panel-ease),
    filter var(--panel-open-dur) var(--panel-ease);
  will-change: transform, opacity, filter;
}
.panel-leave-active {
  transition:
    transform var(--panel-close-dur) var(--panel-ease),
    opacity var(--panel-close-dur) var(--panel-ease),
    filter var(--panel-close-dur) var(--panel-ease);
  will-change: transform, opacity, filter;
  pointer-events: none;
}
.panel-enter-from,
.panel-leave-to {
  transform: translateY(var(--panel-translate-y));
  opacity: 0;
  filter: blur(var(--panel-blur));
}

@media (prefers-reduced-motion: reduce) {
  .panel-enter-active,
  .panel-leave-active,
  .ms-card,
  .ms-card__overlay,
  .ms-card__shade,
  .ms-card__name,
  .mode-btn,
  .report-btn {
    transition: none !important;
  }
}

.page-title {
  font-size: 4em;
  margin-bottom: 0.15em;
}
.subtitle {
  color: #aaa;
  margin-bottom: 1.5rem;
}

/* Mode switch and group bar */
.mode-row {
  display: flex;
  gap: 0.4rem;
  margin-bottom: 0.9rem;
}
.mode-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  padding: 5px 14px;
  border-radius: 4px;
  border: 1px solid #444;
  background: #1e1e1e;
  color: #ccc;
  cursor: pointer;
  transition:
    background-color var(--duration-quick) var(--ease-smooth-out),
    border-color var(--duration-quick) var(--ease-smooth-out),
    color var(--duration-quick) var(--ease-smooth-out);
}
.mode-btn:hover {
  background: #2a2a2a;
}
.mode-btn--active {
  background: #e2e2e2;
  border-color: #e2e2e2;
  color: #111;
}

.group-bar {
  position: sticky;
  top: 0;
  z-index: 2;
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 1.25rem;
  padding: 0.5rem 0.75rem;
  background-color: #1a1a1a;
  border: 1px solid #333;
  border-radius: 6px;
}
.group-bar__count {
  font-size: 0.92em;
  color: #ccc;
}
.group-bar__pairs {
  color: #888;
}
.group-bar__actions {
  display: flex;
  gap: 0.4rem;
  flex-wrap: wrap;
}

.group-review__note {
  font-size: 0.85em;
  color: #888;
  margin: 0 0 0.75rem;
}
.group-pairs {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  margin-bottom: 1rem;
}
.group-pair {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.5rem;
  padding: 0.45rem 0.75rem;
  border-radius: 6px;
  font-size: 0.9em;
}
.group-pair__icon {
  flex-shrink: 0;
}
.group-pair__names {
  font-weight: bold;
}
.group-pair__label {
  opacity: 0.85;
}
.group-pair__hooks {
  font-size: 0.85em;
  opacity: 0.75;
  margin-left: auto;
}
.group-review__confirm {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-wrap: wrap;
}
.group-review__warning {
  font-size: 0.82em;
  color: #ffb74d;
}

/* One block for every pair with nothing to report */
.group-clear {
  flex-direction: column;
  align-items: stretch;
  gap: 0.4rem;
}
.group-clear__header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}
.group-clear__chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.3rem;
}
.group-chip {
  padding: 0.15rem 0.55rem;
  border-radius: 999px;
  font-size: 0.85em;
  background-color: rgba(129, 199, 132, 0.12);
  border: 1px solid rgba(129, 199, 132, 0.35);
}

/* ── Selection bar ── */
.selection-status {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-bottom: 1.25rem;
  padding: 0.5rem 0.75rem;
  background-color: #1a1a1a;
  border-radius: 6px;
  min-height: 42px;
}

.selection-pill {
  display: flex;
  align-items: center;
  gap: 0.25rem;
  background-color: #2a2a2a;
  border: 1px solid #fff3;
  border-radius: 4px;
  padding: 0.15rem 0.4rem 0.15rem 0.6rem;
  font-size: 0.88em;
}

.slot-empty {
  color: #555;
  font-style: italic;
  font-size: 0.88em;
}

/* ── Grid ── */
.loading-msg {
  color: #888;
  padding: 1rem 0;
}

.moveset-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  margin-bottom: 2rem;
  justify-content: center;
}

.ms-card {
  position: relative;
  width: 163px;
  height: 50px;
  background-color: var(--bg-color, #111);
  border: 3px solid transparent;
  border-radius: 3px;
  overflow: hidden;
  cursor: pointer;
  text-align: left;
  transition:
    background-color var(--duration-fast) var(--ease-smooth-out),
    border-color var(--duration-fast) var(--ease-smooth-out),
    outline-color var(--duration-fast) var(--ease-smooth-out),
    border-radius var(--duration-fast) var(--ease-smooth-out),
    box-shadow var(--duration-fast) var(--ease-smooth-out),
    opacity var(--duration-fast) var(--ease-smooth-out),
    filter var(--duration-fast) var(--ease-smooth-out);
  flex-shrink: 0;
  outline: 2px solid transparent;
}

.ms-card:hover:not(.ms-card--dimmed) {
  filter: brightness(0.9);
  border-radius: 11px;
}
.ms-card--dimmed {
  opacity: 0.35;
  cursor: default;
}

.ms-card--selected {
  border-color: #ffffff;
  outline-color: #fff;
}

.ms-card--vote-preview {
  background-color: #808080;
}
.ms-card--vote-preview .ms-card__name {
  color: #111;
  text-shadow: 0 0px 2px #ffffff60;
}

.ms-card__thumb {
  position: absolute;
  inset: 0;
  z-index: 0;
}
.ms-card__overlay,
.ms-card__shade {
  position: absolute;
  inset: 0;
  z-index: 1;
  transition: opacity var(--duration-fast) var(--ease-smooth-out);
}
/* Dark shade that fades in over the colored gradient while one card is selected */
.ms-card__shade {
  opacity: 0;
  background: linear-gradient(
    to right,
    rgba(0, 0, 0, 0.35) 0%,
    rgba(0, 0, 0, 0.15) 50%,
    rgba(0, 0, 0, 0.02) 100%
  );
}
.ms-card--preview .ms-card__overlay {
  opacity: 0;
}
.ms-card--preview .ms-card__shade {
  opacity: 1;
}

.ms-card__name {
  position: absolute;
  z-index: 2;
  left: 0;
  right: 0;
  top: 50%;
  transform: translateY(-50%);
  padding: 2px 0.5rem 2px 0.4rem;
  font-size: 0.82em;
  font-weight: bold;
  line-height: 1.2;
  letter-spacing: 0.25px;
  color: #fff;
  text-shadow: 0 1px 5px #000;
  pointer-events: none;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  transition:
    color var(--duration-fast) var(--ease-smooth-out),
    text-shadow var(--duration-fast) var(--ease-smooth-out);
}
.ms-card__subtitle {
  font-size: 0.8em;
  opacity: 0.65;
  font-weight: normal;
}

/* Vote bar — full card background, behind overlay */
.ms-card__vote-bar {
  position: absolute;
  inset: 0;
  z-index: 0;
  display: flex;
}

.ms-card__vote-bar-compat {
  background-color: rgba(46, 125, 50, 0.65);
  height: 100%;
  flex-shrink: 0;
  transition: width 0.3s ease;
}

.ms-card__vote-bar-incompat {
  background-color: rgba(198, 40, 40, 0.65);
  flex: 1;
}

/* ── Checking ── */
.checking-msg {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  color: #aaa;
  padding: 1rem 0;
}

/* ── Compare panel ── */
.compare-panel {
  display: flex;
  background-color: #141414;
  border-radius: 6px;
  overflow: hidden;
  font-size: 1.05em;
}

.compare-col {
  flex: 1;
  padding: 0.875rem 1rem;
  min-width: 0;
}
.compare-header {
  margin-bottom: 0.2rem;
}
.compare-header strong {
  font-size: 1em;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  display: block;
}
.compare-subtitle {
  font-size: 0.75em;
  opacity: 0.6;
  font-weight: normal;
}
.compare-meta {
  font-size: 0.9em;
  color: #aaa;
  margin-bottom: 0.6rem;
  display: flex;
  align-items: center;
  gap: 5px;
  flex-wrap: wrap;
}

.meta-char-icon {
  width: 22px;
  height: 22px;
  object-fit: contain;
  flex-shrink: 0;
}

.meta-slots {
  color: #777;
}
.compare-divider {
  width: 1px;
  background-color: #2a2a2a;
  margin: 0.5rem 0;
  flex-shrink: 0;
}
.compare-section {
  margin-bottom: 0.5rem;
}
.compare-label {
  font-size: 0.8em;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: #666;
  display: block;
  margin-bottom: 0.2rem;
}

.compare-list {
  list-style: none;
  padding: 0;
  margin: 0;
  font-size: 0.88em;
  color: #ccc;
  font-family: monospace;
}

.compare-list li {
  line-height: 1.6;
  display: flex;
  align-items: center;
  gap: 0.3rem;
}

.compare-conflict {
  color: #ffb74d;
}

.conflict-icon {
  color: #ffb74d;
  flex-shrink: 0;
}

.compare-none {
  color: #555;
  font-style: italic;
  font-family: inherit;
}

/* ── Verdict ── */
.verdict-banner {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.75rem 1.25rem;
  border-radius: 6px;
  font-size: 1.1em;
  font-weight: bold;
  margin-bottom: 1.25rem;
}

.verdict-body {
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
}
.verdict-text {
  line-height: 1.2;
}
.verdict-note {
  font-size: 0.72em;
  font-weight: normal;
  opacity: 0.8;
}
.verdict-icon {
  font-size: 1.4em;
  flex-shrink: 0;
}

.verdict--good {
  background-color: #1b3a1b;
  color: #81c784;
  border: 1px solid #388e3c;
}
.verdict--warn {
  background-color: #3a2f00;
  color: #ffd54f;
  border: 1px solid #f9a825;
}
.verdict--predicted-bad {
  background-color: #2e1f00;
  color: #ffb74d;
  border: 1px solid #e65100;
}
.verdict--bad {
  background-color: #3a1010;
  color: #ef9a9a;
  border: 1px solid #c62828;
}
.verdict--community-compat {
  background-color: #0d2e2e;
  color: #80cbc4;
  border: 1px solid #00897b;
}
.verdict--community-incompat {
  background-color: #2a1030;
  color: #ce93d8;
  border: 1px solid #8e24aa;
}

.issues-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-bottom: 1.75rem;
}

.issue-item {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  padding: 0.5rem 0.75rem;
  border-radius: 4px;
  font-size: 0.92em;
  line-height: 1.5;
}

.issue-icon {
  margin-top: 2px;
  flex-shrink: 0;
}
.issue-item--incompatible {
  background-color: #2a0a0a;
  color: #ef9a9a;
}
.issue-item--predicted-incompat {
  background-color: #271500;
  color: #ffcc80;
}
.issue-item--warning {
  background-color: #1e1a00;
  color: #fff176;
}
.issue-item--error {
  background-color: #1a1a1a;
  color: #ccc;
}

/* ── Community ── */
.community-section {
  background-color: #12121280;
  padding: 0.875rem 1rem;
  margin-bottom: 1.25rem;
  border-radius: 6px;
  backdrop-filter: blur(2px);
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.community-banner {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.6rem 0.9rem;
  border-radius: 5px;
  font-weight: bold;
}

.community-banner-icon {
  font-size: 1.2em;
  flex-shrink: 0;
}

.community-banner-body {
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
}

.community-banner-text {
  font-size: 0.92em;
  line-height: 1.2;
}
.community-banner-sub {
  font-size: 0.75em;
  font-weight: normal;
  opacity: 0.8;
}

.community-banner--compat {
  background-color: #1b3a1b;
  color: #81c784;
  border: 1px solid #388e3c;
}
.community-banner--incompat {
  background-color: #3a1010;
  color: #ef9a9a;
  border: 1px solid #c62828;
}
.community-banner--neutral {
  background-color: #2a2a1a;
  color: #ffd54f;
  border: 1px solid #f9a825;
}

.community-no-votes {
  font-size: 0.85em;
  color: #555;
  margin: 0;
}

.community-vote-row {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.report-label {
  font-size: 0.82em;
  color: #888;
}

.report-btn-row {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.report-btn {
  transition:
    background-color var(--duration-fast) var(--ease-smooth-out),
    color var(--duration-fast) var(--ease-smooth-out);
}
.report-btn--active-compat {
  background-color: #2e7d32 !important;
  color: #fff !important;
}
.report-btn--muted {
  background-color: #555 !important;
  color: #ddd !important;
}
.report-btn--active-incompat {
  background-color: #c62828 !important;
  color: #fff !important;
}

.vote-note {
  font-size: 0.78em;
  color: #666;
  margin: 0;
}
.sign-in-note {
  font-size: 0.82em;
  color: #555;
}

@media (max-width: 680px) {
  .ms-card {
    width: calc(33.333% - 4px);
  }
  .compare-panel {
    flex-direction: column;
  }
  .compare-divider {
    width: auto;
    height: 1px;
    margin: 0;
  }
}
</style>

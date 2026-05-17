<template>
  <div class="compat-page">
    <h1 class="page-title no-select">Compatibility Check</h1>
    <p class="subtitle">Click two movesets to check if they work together.</p>

    <!-- Moveset grid -->
    <div v-if="loading" class="loading-msg">Loading movesets…</div>
    <div v-else class="moveset-grid">
      <button
        v-for="m in visibleMovesets"
        :key="m.movesetId"
        class="ms-card"
        :class="{
          'ms-card--selected': isSelected(m),
          'ms-card--dimmed': selection.length >= 2 && !isSelected(m),
          'ms-card--vote-preview': selection.length === 1 && !isSelected(m) && !pairSummary(m.movesetId),
        }"
        :style="{ '--bg-color': (selection.length === 1 && !isSelected(m)) ? '#808080' : `#${normalizedBgColor(m)}` }"
        @click="toggleSelect(m)"
      >
        <div class="ms-card__thumb" :style="thumbStyle(m)" />
        <div class="ms-card__overlay" :style="overlayStyle(m)" />
        <span class="ms-card__name">{{ m.moddedCharName }}</span>
        <!-- Vote bar: shown when one moveset is selected and this card has vote data -->
        <div
          v-if="selection.length === 1 && !isSelected(m) && pairSummary(m.movesetId)"
          class="ms-card__vote-bar"
        >
          <div
            class="ms-card__vote-bar-compat"
            :style="{ width: compatPct(m.movesetId) + '%' }"
          />
          <div class="ms-card__vote-bar-incompat" />
        </div>
      </button>
    </div>

    <!-- Checking spinner -->
    <div v-if="checking" class="checking-msg">
      <v-progress-circular indeterminate size="24" />
      Checking…
    </div>

    <!-- Results -->
    <div v-else-if="result" class="result-section">
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
            <span class="community-banner-sub">{{ reports.compatibleCount }} compatible · {{ reports.incompatibleCount }} not compatible</span>
          </div>
        </div>
        <p v-else class="community-no-votes">No community reports yet for this combination.</p>

        <!-- User vote -->
        <div v-if="user" class="community-vote-row">
          <span class="report-label">Have you tested this combination?</span>
          <div class="report-btn-row">
            <v-btn
              :class="['report-btn', reports.userVote === true ? 'report-btn--active-compat' : '']"
              size="small" variant="tonal"
              @click="submitReport(true)" :loading="reportLoading"
            >
              <v-icon start>mdi-thumb-up-outline</v-icon> Works Together
            </v-btn>
            <v-btn
              :class="['report-btn', reports.userVote === false ? 'report-btn--active-incompat' : '']"
              size="small" variant="tonal"
              @click="submitReport(false)" :loading="reportLoading"
            >
              <v-icon start>mdi-thumb-down-outline</v-icon> Doesn't Work
            </v-btn>
          </div>
        </div>
        <p v-else class="sign-in-note">Sign in to submit a report.</p>
      </div>

      <!-- Moveset comparison panel -->
      <div class="compare-panel" v-if="result.a && result.b">
        <div class="compare-col">
          <div class="compare-header"><strong>{{ result.a.moddedCharName }}</strong></div>
          <div class="compare-meta">
            <img v-if="result.a.vanillaChar" :src="iconUrl(result.a.vanillaChar.vanillaCharInternalName)" class="meta-char-icon" />
            {{ result.a.vanillaChar?.displayName ?? '-' }}
            <span v-if="result.a.slotsStart != null" class="meta-slots">(c{{ pad(result.a.slotsStart) }}–c{{ pad(result.a.slotsEnd) }})</span>
          </div>
          <div class="compare-section">
            <span class="compare-label">Articles ({{ result.a.movesetArticles.length }})</span>
            <ul class="compare-list">
              <li
                v-for="ma in result.a.movesetArticles"
                :key="ma.article.articleId"
                :class="{ 'compare-conflict': result.conflictingArticleIds.has(ma.article.articleId) }"
              >
                <v-icon v-if="result.conflictingArticleIds.has(ma.article.articleId)" size="x-small" class="conflict-icon">mdi-alert</v-icon>
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
                <v-icon v-if="result.conflictingHookIds.has(mh.hook.hookId)" size="x-small" class="conflict-icon">mdi-alert</v-icon>
                0x{{ mh.hook.offset }}
              </li>
              <li v-if="!result.a.movesetHooks.length" class="compare-none">none</li>
            </ul>
          </div>
        </div>

        <div class="compare-divider" />

        <div class="compare-col">
          <div class="compare-header"><strong>{{ result.b.moddedCharName }}</strong></div>
          <div class="compare-meta">
            <img v-if="result.b.vanillaChar" :src="iconUrl(result.b.vanillaChar.vanillaCharInternalName)" class="meta-char-icon" />
            {{ result.b.vanillaChar?.displayName ?? '-' }}
            <span v-if="result.b.slotsStart != null" class="meta-slots">(c{{ pad(result.b.slotsStart) }}–c{{ pad(result.b.slotsEnd) }})</span>
          </div>
          <div class="compare-section">
            <span class="compare-label">Articles ({{ result.b.movesetArticles.length }})</span>
            <ul class="compare-list">
              <li
                v-for="ma in result.b.movesetArticles"
                :key="ma.article.articleId"
                :class="{ 'compare-conflict': result.conflictingArticleIds.has(ma.article.articleId) }"
              >
                <v-icon v-if="result.conflictingArticleIds.has(ma.article.articleId)" size="x-small" class="conflict-icon">mdi-alert</v-icon>
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
                <v-icon v-if="result.conflictingHookIds.has(mh.hook.hookId)" size="x-small" class="conflict-icon">mdi-alert</v-icon>
                0x{{ mh.hook.offset }}
              </li>
              <li v-if="!result.b.movesetHooks.length" class="compare-none">none</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import api from '@/services/api'

const apiUrl = import.meta.env.VITE_API_URL

const ALLOWED_STATES = new Set(['Released', 'Pending Update', 'Open for Beta Testing'])
const COMMUNITY_MIN_VOTES = 3
const COMMUNITY_THRESHOLD = 0.65

const loading = ref(false)
const checking = ref(false)
const reportLoading = ref(false)

const movesets = ref([])
const hookableStatuses = ref([])
const hardHeldIds = ref(new Set())
const user = ref(null)

const selection = ref([])
const result = ref(null)
const reports = ref({ compatibleCount: 0, incompatibleCount: 0, userVote: null })

// Map<partnerMovesetId, {compatibleCount, incompatibleCount}>
const pairSummaries = ref(new Map())

const visibleMovesets = computed(() =>
  movesets.value.filter(m =>
    !m.privateMoveset &&
    !hardHeldIds.value.has(m.movesetId) &&
    ALLOWED_STATES.has(m.releaseState)
  )
)

const isSelected = (m) => selection.value.some(s => s.movesetId === m.movesetId)

const pairSummary = (movesetId) => pairSummaries.value.get(movesetId) ?? null

const compatPct = (movesetId) => {
  const s = pairSummary(movesetId)
  if (!s) return 0
  const total = s.compatibleCount + s.incompatibleCount
  return total === 0 ? 0 : Math.round((s.compatibleCount / total) * 100)
}

const toggleSelect = (m) => {
  const idx = selection.value.findIndex(s => s.movesetId === m.movesetId)
  if (idx !== -1) {
    selection.value.splice(idx, 1)
    result.value = null
    return
  }
  if (selection.value.length >= 2) {
    // Replace the most recently selected
    selection.value.splice(1, 1, m)
    return
  }
  selection.value.push(m)
}

const deselect = (m) => {
  const idx = selection.value.findIndex(s => s.movesetId === m.movesetId)
  if (idx !== -1) {
    selection.value.splice(idx, 1)
    result.value = null
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
  if (selection.value.length === 1 && !isSelected(m)) {
    return { background: 'linear-gradient(to right, rgba(0,0,0,0.35) 0%, rgba(0,0,0,0.15) 50%, rgba(0,0,0,0.02) 100%)' }
  }
  const c = normalizedBgColor(m)
  return { background: `linear-gradient(to right, #${c}f2 0%, #${c}c8 50%, #${c}50 80%, #${c}10 100%)` }
}

function pad(n) { return String(n).padStart(3, '0') }
function iconUrl(internalName) {
  return `${import.meta.env.BASE_URL}vanilla-stock-icons/chara_2_${internalName}.png`
}

const statusName = (id) => hookableStatuses.value.find(s => s.hookableStatusId === id)?.name ?? ''
const isOnceOnly = (id) => statusName(id).toLowerCase().includes('once')
const isMultiOk = (id) => statusName(id).toLowerCase().includes('more than once')

const slotsOverlap = (a, b) => {
  const aS = a.slotsStart ?? 0, aE = a.slotsEnd ?? 0
  const bS = b.slotsStart ?? 0, bE = b.slotsEnd ?? 0
  if (!aS && !aE && !bS && !bE) return false
  return aS <= bE && bS <= aE
}

const runCheck = async () => {
  if (selection.value.length < 2) return
  checking.value = true
  result.value = null

  try {
    const [resA, resB, reportsRes] = await Promise.all([
      api.get(`/movesets/${selection.value[0].movesetId}`),
      api.get(`/movesets/${selection.value[1].movesetId}`),
      api.get('/compatibility', {
        params: { movesetId1: selection.value[0].movesetId, movesetId2: selection.value[1].movesetId }
      }).catch(() => ({ data: { compatibleCount: 0, incompatibleCount: 0, userVote: null } }))
    ])

    const a = resA.data
    const b = resB.data
    reports.value = reportsRes.data

    const issues = []
    const conflictingHookIds = new Set()
    const conflictingArticleIds = new Set()

    for (const mhA of (a.movesetHooks ?? [])) {
      const mhB = (b.movesetHooks ?? []).find(h => h.hook.hookId === mhA.hook.hookId)
      if (!mhB) continue
      const sid = mhA.hook.hookableStatusId
      conflictingHookIds.add(mhA.hook.hookId)
      if (isOnceOnly(sid)) {
        issues.push({ severity: 'incompatible', message: `Both movesets use hook 0x${mhA.hook.offset} - this hook can only be used once and will cause a crash.` })
      } else if (isMultiOk(sid)) {
        issues.push({ severity: 'warning', message: `Both movesets use hook 0x${mhA.hook.offset}. This hook supports multiple uses, but too many at the same offset may still cause issues.` })
      } else {
        issues.push({ severity: 'predicted-incompat', message: `Both movesets use hook 0x${mhA.hook.offset}. This hook's behavior with multiple users is untested. It will likely crash.` })
      }
    }

    for (const maA of (a.movesetArticles ?? [])) {
      const maB = (b.movesetArticles ?? []).find(art => art.article.articleId === maA.article.articleId)
      if (!maB) continue
      const sameChar = a.vanillaChar?.vanillaCharInternalName === b.vanillaChar?.vanillaCharInternalName
      if (sameChar) {
        conflictingArticleIds.add(maA.article.articleId)
        issues.push({ severity: 'incompatible', message: `Both movesets are on ${a.vanillaChar?.displayName ?? 'the same character'} and clone the same article (${maA.article.vanillaCharInternalName}_${maA.article.articleName}). The article will not work correctly.` })
      } else if (slotsOverlap(a, b)) {
        conflictingArticleIds.add(maA.article.articleId)
        issues.push({ severity: 'warning', message: `Both movesets clone the article ${maA.article.vanillaCharInternalName}_${maA.article.articleName} and use overlapping slots. It's recommended to switch the slots of one moveset to reduce article conflicts.` })
      }
    }

    result.value = { issues, a, b, conflictingHookIds, conflictingArticleIds }
  } catch {
    result.value = {
      issues: [{ severity: 'error', message: 'Failed to load moveset data. Please try again.' }],
      a: null, b: null,
      conflictingHookIds: new Set(),
      conflictingArticleIds: new Set()
    }
  } finally {
    checking.value = false
  }
}

// Watch the actual IDs so replacing selection[1] triggers a re-check
watch(
  () => selection.value.map(s => s.movesetId).join(','),
  async (val) => {
    if (selection.value.length === 2) {
      runCheck()
    } else if (selection.value.length === 1) {
      result.value = null
      // Fetch vote summaries for the selected moveset
      try {
        const res = await api.get('/compatibility/summary', {
          params: { movesetId: selection.value[0].movesetId }
        })
        const map = new Map()
        for (const entry of res.data) {
          map.set(entry.movesetId, { compatibleCount: entry.compatibleCount, incompatibleCount: entry.incompatibleCount })
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
)

// Auto-detected severity
const autoSeverity = computed(() => {
  if (!result.value) return null
  const issues = result.value.issues
  if (issues.some(i => i.severity === 'incompatible')) return 'incompatible'
  if (issues.some(i => i.severity === 'predicted-incompat')) return 'predicted-incompat'
  if (issues.some(i => i.severity === 'warning')) return 'warning'
  return 'compatible'
})

const communitySignal = computed(() => {
  const total = reports.value.compatibleCount + reports.value.incompatibleCount
  if (total < COMMUNITY_MIN_VOTES) return null
  const ratio = reports.value.compatibleCount / total
  if (ratio >= COMMUNITY_THRESHOLD) return 'compatible'
  if (ratio <= (1 - COMMUNITY_THRESHOLD)) return 'incompatible'
  return null
})

const autoAsSignal = (s) => (s === 'compatible' || s === 'warning') ? 'compatible' : 'incompatible'

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

const communityFactored = computed(() =>
  communitySignal.value !== null && communitySignal.value !== autoAsSignal(autoSeverity.value)
)

const communityNote = computed(() => {
  const total = reports.value.compatibleCount + reports.value.incompatibleCount
  if (communitySignal.value === 'compatible') return `${reports.value.compatibleCount} of ${total} users report it works.`
  if (communitySignal.value === 'incompatible') return `${reports.value.incompatibleCount} of ${total} users report issues.`
  return ''
})

const totalVotes = computed(() => reports.value.compatibleCount + reports.value.incompatibleCount)

const communityBannerClass = computed(() => {
  if (reports.value.compatibleCount > reports.value.incompatibleCount) return 'community-banner--compat'
  if (reports.value.incompatibleCount > reports.value.compatibleCount) return 'community-banner--incompat'
  return 'community-banner--neutral'
})

const communityBannerIcon = computed(() => {
  if (reports.value.compatibleCount > reports.value.incompatibleCount) return 'mdi-thumb-up'
  if (reports.value.incompatibleCount > reports.value.compatibleCount) return 'mdi-thumb-down'
  return 'mdi-scale-balance'
})

const communityBannerText = computed(() => {
  if (reports.value.compatibleCount > reports.value.incompatibleCount) return 'Users are reporting this combination as compatible'
  if (reports.value.incompatibleCount > reports.value.compatibleCount) return 'Users are reporting this combination as not compatible'
  return 'Community reports are evenly split'
})

const VERDICT_MAP = {
  compatible:            { cls: 'verdict--good',             icon: 'mdi-check-circle',   text: 'Most likely compatible. No Issues Detected' },
  warning:               { cls: 'verdict--warn',             icon: 'mdi-alert',           text: 'Compatible with Caveats' },
  'predicted-incompat':  { cls: 'verdict--predicted-bad',    icon: 'mdi-alert-circle',    text: 'Likely Not Compatible' },
  incompatible:          { cls: 'verdict--bad',              icon: 'mdi-close-circle',    text: 'Not Compatible' },
  'community-compat':    { cls: 'verdict--community-compat', icon: 'mdi-account-check',   text: 'Users Report Compatible' },
  'community-incompat':  { cls: 'verdict--community-incompat', icon: 'mdi-account-cancel', text: 'Users Report Issues' },
}

const finalVerdictClass = computed(() => VERDICT_MAP[finalSeverity.value]?.cls ?? '')
const finalVerdictIcon  = computed(() => VERDICT_MAP[finalSeverity.value]?.icon ?? 'mdi-help-circle')
const finalVerdictText  = computed(() => VERDICT_MAP[finalSeverity.value]?.text ?? '')
const severityIcon = (s) => ({ incompatible: 'mdi-close-circle', 'predicted-incompat': 'mdi-alert-circle', warning: 'mdi-alert' }[s] ?? 'mdi-information')

const submitReport = async (isCompatible) => {
  if (!result.value || selection.value.length < 2) return
  reportLoading.value = true
  try {
    const res = await api.post('/compatibility', {
      movesetId1: selection.value[0].movesetId,
      movesetId2: selection.value[1].movesetId,
      isCompatible
    })
    reports.value = res.data
  } catch { /**/ } finally {
    reportLoading.value = false
  }
}

onMounted(async () => {
  loading.value = true
  try {
    const [msRes, statusRes, userRes, logsRes] = await Promise.allSettled([
      api.get('/movesets'),
      api.get('/hookablestatuses'),
      api.get('/auth/me'),
      api.get('/logs', { params: { acceptanceStates: [1, 2, 3, 4, 5, 6, 7], itemTypes: [1] } })
    ])
    if (msRes.status === 'fulfilled') movesets.value = msRes.value.data
    if (statusRes.status === 'fulfilled') hookableStatuses.value = statusRes.value.data
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
}

.page-title { font-size: 4em; margin-bottom: 0.15em; }
.subtitle { color: #aaa; margin-bottom: 1.5rem; }

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

.slot-empty { color: #555; font-style: italic; font-size: 0.88em; }

/* ── Grid ── */
.loading-msg { color: #888; padding: 1rem 0; }

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
  transition: border-color 0.12s, box-shadow 0.12s, opacity 0.12s, filter 0.12s;
  flex-shrink: 0;
  outline: none;
}

.ms-card:hover:not(.ms-card--dimmed) { filter: brightness(1.18); }
.ms-card--dimmed { opacity: 0.35; cursor: default; }

.ms-card--selected {
  border-color: #ffffff;
  outline: 2px solid #fff;
}

.ms-card--vote-preview {
  background-color: #808080;
}
.ms-card--vote-preview .ms-card__name {
  color: #111;
  text-shadow: 0 0px 2px #ffffff60;
}

.ms-card__thumb { position: absolute; inset: 0; z-index: 0; }
.ms-card__overlay { position: absolute; inset: 0; z-index: 1; }

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

.compare-col { flex: 1; padding: 0.875rem 1rem; min-width: 0; }
.compare-header { margin-bottom: 0.2rem; }
.compare-header strong { font-size: 1em; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; display: block; }
.compare-meta { font-size: 0.9em; color: #aaa; margin-bottom: 0.6rem; display: flex; align-items: center; gap: 5px; flex-wrap: wrap; }

.meta-char-icon {
  width: 22px;
  height: 22px;
  object-fit: contain;
  flex-shrink: 0;
}

.meta-slots { color: #777; }
.compare-divider { width: 1px; background-color: #2a2a2a; margin: 0.5rem 0; flex-shrink: 0; }
.compare-section { margin-bottom: 0.5rem; }
.compare-label { font-size: 0.8em; text-transform: uppercase; letter-spacing: 0.04em; color: #666; display: block; margin-bottom: 0.2rem; }

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

.compare-none { color: #555; font-style: italic; font-family: inherit; }

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

.verdict-body { display: flex; flex-direction: column; gap: 0.1rem; }
.verdict-text { line-height: 1.2; }
.verdict-note { font-size: 0.72em; font-weight: normal; opacity: 0.8; }
.verdict-icon { font-size: 1.4em; flex-shrink: 0; }

.verdict--good              { background-color: #1b3a1b; color: #81c784; border: 1px solid #388e3c; }
.verdict--warn              { background-color: #3a2f00; color: #ffd54f; border: 1px solid #f9a825; }
.verdict--predicted-bad     { background-color: #2e1f00; color: #ffb74d; border: 1px solid #e65100; }
.verdict--bad               { background-color: #3a1010; color: #ef9a9a; border: 1px solid #c62828; }
.verdict--community-compat  { background-color: #0d2e2e; color: #80cbc4; border: 1px solid #00897b; }
.verdict--community-incompat { background-color: #2a1030; color: #ce93d8; border: 1px solid #8e24aa; }

.issues-list { display: flex; flex-direction: column; gap: 0.5rem; margin-bottom: 1.75rem; }

.issue-item {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  padding: 0.5rem 0.75rem;
  border-radius: 4px;
  font-size: 0.92em;
  line-height: 1.5;
}

.issue-icon { margin-top: 2px; flex-shrink: 0; }
.issue-item--incompatible     { background-color: #2a0a0a; color: #ef9a9a; }
.issue-item--predicted-incompat { background-color: #271500; color: #ffcc80; }
.issue-item--warning          { background-color: #1e1a00; color: #fff176; }
.issue-item--error            { background-color: #1a1a1a; color: #ccc; }

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

.community-banner-icon { font-size: 1.2em; flex-shrink: 0; }

.community-banner-body {
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
}

.community-banner-text { font-size: 0.92em; line-height: 1.2; }
.community-banner-sub  { font-size: 0.75em; font-weight: normal; opacity: 0.8; }

.community-banner--compat   { background-color: #1b3a1b; color: #81c784; border: 1px solid #388e3c; }
.community-banner--incompat { background-color: #3a1010; color: #ef9a9a; border: 1px solid #c62828; }
.community-banner--neutral  { background-color: #2a2a1a; color: #ffd54f; border: 1px solid #f9a825; }

.community-no-votes { font-size: 0.85em; color: #555; margin: 0; }

.community-vote-row {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.report-label { font-size: 0.82em; color: #888; }

.report-btn-row { display: flex; gap: 0.5rem; flex-wrap: wrap; }

.report-btn--active-compat   { background-color: #2e7d32 !important; color: #fff !important; }
.report-btn--active-incompat { background-color: #c62828 !important; color: #fff !important; }

.vote-note { font-size: 0.78em; color: #666; margin: 0; }
.sign-in-note { font-size: 0.82em; color: #555; }

@media (max-width: 680px) {
  .ms-card { width: calc(33.333% - 4px); }
  .compare-panel { flex-direction: column; }
  .compare-divider { width: auto; height: 1px; margin: 0; }
}
</style>

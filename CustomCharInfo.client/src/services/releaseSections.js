import { ReleaseState, RELEASE_STATE_NAMES } from '@/globals'
import { compareDateOnlyStrings } from '@/services/dateOnly'

// Orders moveset list rows (from GET /movesets, which carries the release state by name) for the release-date sort.

// Deprecated only reaches the list when the Release state filter asks for it
const RELEASED_NAMES = new Set(
  [ReleaseState.Released, ReleaseState.PendingUpdate, ReleaseState.Deprecated].map(
    (id) => RELEASE_STATE_NAMES[id]
  )
)
const OPEN_BETA = RELEASE_STATE_NAMES[ReleaseState.OpenBeta]

// Public first. Public by name, private by first credited modder since their names are hidden.
export function compareByName(a, b) {
  if (!!a.privateMoveset !== !!b.privateMoveset) return a.privateMoveset ? 1 : -1
  if (!a.privateMoveset) return a.moddedCharName.localeCompare(b.moddedCharName)
  const modderA = (a.modders?.[0] || '').toLowerCase()
  const modderB = (b.modders?.[0] || '').toLowerCase()
  return modderA.localeCompare(modderB)
}

// Released: newest first, undated last.
function compareReleased(a, b) {
  if (a.releaseDate && b.releaseDate) return compareDateOnlyStrings(b.releaseDate, a.releaseDate)
  if (a.releaseDate || b.releaseDate) return a.releaseDate ? -1 : 1
  return compareByName(a, b)
}

// Unreleased: dated ones oldest first, then undated open beta, then undated upcoming (and no state).
const unreleasedRank = (m) => (m.releaseDate ? 0 : m.releaseState === OPEN_BETA ? 1 : 2)

function compareUnreleased(a, b) {
  const rank = unreleasedRank(a) - unreleasedRank(b)
  if (rank) return rank
  if (a.releaseDate) return compareDateOnlyStrings(a.releaseDate, b.releaseDate)
  return compareByName(a, b)
}

export function releaseDateSections(movesets) {
  const released = movesets.filter((m) => RELEASED_NAMES.has(m.releaseState)).sort(compareReleased)
  const unreleased = movesets
    .filter((m) => !RELEASED_NAMES.has(m.releaseState))
    .sort(compareUnreleased)
  return { released, unreleased }
}

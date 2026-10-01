// Pill labels and colors for an item's current acceptance state,
// shared by the pages that show a submitter or admin where something sits in review.
// Colors are the --state-* tokens in src/styles/tokens.css, usable directly in inline styles.
import { AcceptanceState } from '@/globals'

export const PILL_COLORS = Object.freeze({
  [AcceptanceState.PendingAdminSoft]: 'var(--state-pending-admin-soft)',
  [AcceptanceState.PendingAdminHard]: 'var(--state-pending-admin-hard)',
  [AcceptanceState.PendingUserSoft]: 'var(--state-pending-user-soft)',
  [AcceptanceState.PendingUserHard]: 'var(--state-pending-user-hard)',
  [AcceptanceState.Accepted]: 'var(--state-accepted)',
  [AcceptanceState.AutoAccepted]: 'var(--state-accepted)',
  [AcceptanceState.Rejected]: 'var(--state-rejected)',
})

export const PILL_LABELS = Object.freeze({
  [AcceptanceState.PendingAdminSoft]: 'Pending Admin Action (Soft)',
  [AcceptanceState.PendingAdminHard]: 'Pending Admin Action (Hard)',
  [AcceptanceState.PendingUserSoft]: 'Pending User Action (Soft)',
  [AcceptanceState.PendingUserHard]: 'Pending User Action (Hard)',
  [AcceptanceState.Rejected]: 'Rejected',
})

export const PRIVATE_COLOR = 'var(--err)'
export const CURRENT_COLOR = 'var(--ok)'

// { label, color } for states that need a pill, null for accepted or unknown states.
export function statusPillFor(stateId) {
  if (!PILL_LABELS[stateId]) return null
  return { label: PILL_LABELS[stateId], color: PILL_COLORS[stateId] }
}

export function pillsFor(stateId) {
  const pill = statusPillFor(stateId)
  return pill ? [pill] : []
}

// Map of item id to its newest acceptance state id for one item type, from a /logs/latest response.
export function latestStatesByItem(rows, itemTypeId) {
  const states = new Map()
  for (const row of rows) {
    if (row.itemTypeId !== itemTypeId) continue
    states.set(row.itemId, row.acceptanceStateId)
  }
  return states
}

// Newest log per item id for one item type, from a /logs response.
export function latestLogsByItem(logs, itemTypeId, idOf) {
  const latest = new Map()
  for (const log of logs) {
    if (log.itemType?.itemTypeId !== itemTypeId) continue
    const id = idOf(log.item)
    if (id == null) continue
    const cur = latest.get(id)
    if (!cur || new Date(log.createdAt) > new Date(cur.createdAt)) latest.set(id, log)
  }
  return latest
}

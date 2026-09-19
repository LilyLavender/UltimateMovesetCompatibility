// Pill labels and colors for an item's current acceptance state,
// shared by the pages that show a submitter or admin where something sits in review.
// Colors match ActionLogItem.vue
import { AcceptanceState } from '@/globals'

export const PILL_COLORS = Object.freeze({
  [AcceptanceState.PendingAdminSoft]: 'rgb(187, 224, 236)',
  [AcceptanceState.PendingAdminHard]: 'rgb(52, 194, 241)',
  [AcceptanceState.PendingUserSoft]: 'rgb(241, 241, 142)',
  [AcceptanceState.PendingUserHard]: 'rgb(241, 241, 52)',
  [AcceptanceState.Rejected]: 'rgb(241, 52, 52)',
})

export const PILL_LABELS = Object.freeze({
  [AcceptanceState.PendingAdminSoft]: 'Pending Admin Action (Soft)',
  [AcceptanceState.PendingAdminHard]: 'Pending Admin Action (Hard)',
  [AcceptanceState.PendingUserSoft]: 'Pending User Action (Soft)',
  [AcceptanceState.PendingUserHard]: 'Pending User Action (Hard)',
  [AcceptanceState.Rejected]: 'Rejected',
})

export const PRIVATE_COLOR = 'rgb(241, 52, 52)'
export const CURRENT_COLOR = 'rgb(129, 199, 132)'

// { label, color } for states that need a pill, null for accepted or unknown states.
export function statusPillFor(stateId) {
  if (!PILL_LABELS[stateId]) return null
  return { label: PILL_LABELS[stateId], color: PILL_COLORS[stateId] }
}

export function pillsFor(stateId) {
  const pill = statusPillFor(stateId)
  return pill ? [pill] : []
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

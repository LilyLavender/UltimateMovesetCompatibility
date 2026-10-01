import {
  CONTRIBUTION_ROLE_NAMES,
  CONTRIBUTION_ROLE_ORDER,
  PROFILE_CONTRIBUTION_ROLES,
} from '@/globals'

// Role names in list order, for the creator subtitle on a moveset page. Empty string for no roles.
export function formatRoles(roleIds) {
  const ids = new Set(roleIds ?? [])
  return CONTRIBUTION_ROLE_ORDER.filter((id) => ids.has(id))
    .map((id) => CONTRIBUTION_ROLE_NAMES[id])
    .join(', ')
}

// Profile readouts: how many of the listed movesets carry each role for the filtered modder.
// Reads modderRoleIds, which the list API fills only when asked for one modder.
// Other never counts. Zeros are dropped. Sorted by count, ties in list order.
export function roleCounts(movesets) {
  const counts = new Map(PROFILE_CONTRIBUTION_ROLES.map((id) => [id, 0]))
  for (const moveset of movesets ?? []) {
    for (const id of new Set(moveset.modderRoleIds ?? [])) {
      if (counts.has(id)) counts.set(id, counts.get(id) + 1)
    }
  }
  return PROFILE_CONTRIBUTION_ROLES.map((id, index) => ({
    roleId: id,
    name: CONTRIBUTION_ROLE_NAMES[id],
    count: counts.get(id),
    index,
  }))
    .filter((r) => r.count > 0)
    .sort((a, b) => b.count - a.count || a.index - b.index)
    .map(({ roleId, name, count }) => ({ roleId, name, count }))
}

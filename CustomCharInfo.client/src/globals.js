export const GB_PAGE_URL = 'https://gamebanana.com/mods/'
export const GB_WIP_URL = 'https://gamebanana.com/wips/'
export const GB_MEMBER_URL = 'https://gamebanana.com/members/'
export const MODS_WIKI_URL = 'https://smashmods.choille.name/wiki/'

// Mirrors the `allowedTypes` dimension requirements enforced server-side in UploadController.cs
export const IMAGE_UPLOAD_SPECS = {
  thumb_h: { width: 340, height: 82 },
  moveset_hero: { width: 1200, height: 1200 },
  series_icon: { width: 800, height: 800 },
}

// Mirrors ApplicationUser.UserTypeId server-side (seeded in UserType lookup table)
export const UserType = Object.freeze({
  User: 1,
  Modder: 2,
  Admin: 3,
})

// Mirrors AcceptanceState.AcceptanceStateId server-side (seeded in AcceptanceState lookup table).
// Names follow the soft/hard review-edit model documented in ActionLogItem.vue's acceptanceStyle
// map and MovesetController's newState comments: a "soft" edit stays visible while pending; a
// "hard" edit is hidden until an admin acts on it. States 1/2 await admin action; 3/4 are the
// outcome shown to the submitter after a soft/hard edit is acted on.
export const AcceptanceState = Object.freeze({
  PendingAdminSoft: 1,
  PendingAdminHard: 2,
  PendingUserSoft: 3,
  PendingUserHard: 4,
  Accepted: 5,
  Rejected: 6,
  AutoAccepted: 7,
})

// AcceptanceState values under which a moveset/modder/series is hidden from non-owners
export const BLOCKED_ACCEPTANCE_STATES = [
  AcceptanceState.PendingAdminHard,
  AcceptanceState.PendingUserHard,
  AcceptanceState.Rejected,
]

// Mirrors ItemType.ItemTypeId server-side (seeded in ItemType lookup table)
export const ItemType = Object.freeze({
  Moveset: 1,
  Modder: 2,
  Series: 3,
  Hook: 4,
})
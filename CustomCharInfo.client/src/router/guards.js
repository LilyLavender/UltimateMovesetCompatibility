import api from '@/services/api'
import {
  UserType,
  ItemType,
  BLOCKED_ACCEPTANCE_STATES,
  PENDING_ADMIN_STATES,
  PENDING_USER_STATES,
} from '@/globals'

// Redirects to ErrorPage with the given status/reason/extra.
export function redirectError(next, httpCode, reason, extra) {
  next({
    name: 'ErrorPage',
    query: { httpCode, reason, extra },
  })
}

// Fetches the current user, redirecting to ErrorPage 401 and returning null on failure.
// Guards that require auth should bail out (`if (!user) return`) when this returns null.
export async function fetchAuthUser(next) {
  try {
    return (await api.get('/auth/me')).data
  } catch {
    redirectError(
      next,
      '401 Unauthorized',
      'Authentication failed.',
      'Try signing in or refreshing the page.'
    )
    return null
  }
}

// Fetches all logs and returns the most recent one matching itemTypeId and matchItem(log.item).
export async function getLatestLog(itemTypeId, matchItem) {
  const logsRes = await api.get('/logs')
  return logsRes.data
    .filter((log) => log.itemType?.itemTypeId === itemTypeId && matchItem(log.item))
    .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))[0]
}

// Builds a route guard that fetches the current user and allows navigation
// only when `predicate(user)` is true. Redirects to ErrorPage with 403 when
// the predicate fails, or 401 when the user can't be fetched (not signed in).
export function createAuthGuard(predicate) {
  return async (to, from, next) => {
    const user = await fetchAuthUser(next)
    if (!user) return

    if (predicate(user)) {
      next()
    } else {
      redirectError(next, '403 Forbidden', 'You do not have permission to access this page.')
    }
  }
}

// Public moveset page. Anonymous visitors are allowed; a moveset whose newest review state is
// blocked is only shown to its own modders and admins.
export function createMovesetViewGuard() {
  return async (to, from, next) => {
    const movesetId = parseInt(to.params.movesetId)

    try {
      // Logged-out users can't read logs, which just means "no blocked state known"
      let latestLog = null
      try {
        latestLog = await getLatestLog(ItemType.Moveset, (item) => item?.movesetId === movesetId)
      } catch {
        latestLog = null
      }

      const moveset = (await api.get(`/movesets/${movesetId}`)).data
      const modderIds = moveset.movesetModders.map((m) => m.modder.modderId)

      let user = null
      try {
        user = (await api.get('/auth/me')).data
      } catch {
        user = null
      }

      const isBlocked =
        latestLog && BLOCKED_ACCEPTANCE_STATES.includes(latestLog.acceptanceState.acceptanceStateId)

      if (isBlocked) {
        const isAdmin = user?.userTypeId === UserType.Admin
        const isOwner = user && modderIds.includes(user.modderId)
        if (!isAdmin && !isOwner) {
          return redirectError(
            next,
            '403 Forbidden',
            'This moveset is currently private.',
            'Check back later, or try signing in.'
          )
        }
      }

      return next()
    } catch (err) {
      return redirectError(
        next,
        '500 Server Error',
        'Could not load the moveset.',
        err.message || 'Please try again later.'
      )
    }
  }
}

// Moveset edit page: one of the moveset's modders, or an admin.
export function createMovesetOwnerGuard() {
  return async (to, from, next) => {
    let moveset
    try {
      moveset = (await api.get(`/movesets/${to.params.movesetId}`)).data
    } catch (err) {
      return redirectError(
        next,
        '500 Server Error',
        'Could not load the moveset.',
        err.message || 'Please try again later.'
      )
    }

    const user = await fetchAuthUser(next)
    if (!user) return

    const modderIds = moveset.movesetModders.map((m) => m.modder.modderId)
    if (modderIds.includes(user.modderId) || user.userTypeId === UserType.Admin) {
      next()
    } else {
      redirectError(
        next,
        '403 Forbidden',
        'You do not have permission to access this page.',
        'Try signing in?'
      )
    }
  }
}

// Series edit page. Allowed when the series is pending the user's own edit, when an admin is
// reviewing it, or when the requester is a modder of a moveset in the series (any modder if the
// series has none yet).
export function createSeriesEditGuard() {
  return async (to, from, next) => {
    const seriesId = parseInt(to.params.seriesId)

    const user = await fetchAuthUser(next)
    if (!user) return

    const denyForbidden = (extra = '') =>
      redirectError(next, '403 Forbidden', 'You do not have permission to edit this series.', extra)

    try {
      const latestLog = await getLatestLog(ItemType.Series, (item) => item?.seriesId === seriesId)
      const stateId = latestLog?.acceptanceState?.acceptanceStateId

      if (PENDING_USER_STATES.includes(stateId)) return next()
      if (PENDING_ADMIN_STATES.includes(stateId) && user.userTypeId === UserType.Admin)
        return next()

      const movesets = (await api.get('/movesets', { params: { seriesId } })).data
      if (movesets.length === 0) {
        return user.userTypeId >= UserType.Modder ? next() : denyForbidden()
      }

      // The moveset list endpoint only exposes modder display names, not IDs, so match by name.
      const modderNames = movesets.flatMap((m) => m.modders)
      const modderName = (await api.get(`/modders/${user.modderId}`)).data.name
      if (modderNames.includes(modderName)) return next()

      return denyForbidden('Only modders of movesets in this series can edit it.')
    } catch (err) {
      return redirectError(
        next,
        '500 Server Error',
        'Could not load this series.',
        err.message || 'Please try again later.'
      )
    }
  }
}

// Modder application page: signed in and not yet a modder.
export function createModderApplyGuard() {
  return async (to, from, next) => {
    const user = await fetchAuthUser(next)
    if (!user) return

    if (!user.modderId) {
      next()
    } else {
      redirectError(next, '403 Forbidden', 'You have already applied for modder.')
    }
  }
}

// Modder profile edit page: the modder themself, or the user whose application created the
// profile (they may not be linked as a modder yet while the application is pending).
export function createModderEditGuard() {
  return async (to, from, next) => {
    const user = await fetchAuthUser(next)
    if (!user) return

    const modderId = parseInt(to.params.id)
    if (user.modderId === modderId) return next()

    try {
      const logsRes = await api.get('/logs', { params: { userId: user.id } })
      const submitted = logsRes.data.find(
        (log) => log.itemType?.itemTypeId === ItemType.Modder && log.item?.modderId === modderId
      )
      if (submitted) {
        next()
      } else {
        redirectError(
          next,
          '403 Forbidden',
          'You do not have permission to access this page.',
          'Try signing in?'
        )
      }
    } catch (err) {
      redirectError(
        next,
        '500 Server Error',
        'Could not load this modder.',
        err.message || 'Please try again later.'
      )
    }
  }
}

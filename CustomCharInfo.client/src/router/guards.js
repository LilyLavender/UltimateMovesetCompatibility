import api from '@/services/api'

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

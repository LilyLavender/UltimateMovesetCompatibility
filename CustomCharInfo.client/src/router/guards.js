import api from '@/services/api'

// Builds a route guard that fetches the current user and allows navigation
// only when `predicate(user)` is true. Redirects to ErrorPage with 403 when
// the predicate fails, or 401 when the user can't be fetched (not signed in).
export function createAuthGuard(predicate) {
  return async (to, from, next) => {
    try {
      const user = (await api.get('/auth/me')).data
      if (predicate(user)) {
        next()
      } else {
        next({
          name: 'ErrorPage',
          query: {
            httpCode: '403 Forbidden',
            reason: 'You do not have permission to access this page.',
          }
        })
      }
    } catch (err) {
      next({
        name: 'ErrorPage',
        query: {
          httpCode: '401 Unauthorized',
          reason: 'Authentication failed.',
          extra: 'Try signing in or refreshing the page.',
        }
      })
    }
  }
}

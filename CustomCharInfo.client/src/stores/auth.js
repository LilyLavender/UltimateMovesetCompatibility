import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '@/services/api'

// Single source of truth for auth state. Previously AuthPanel.vue, SiteHeader.vue, and
// AccountPage.vue each read localStorage independently, so login/logout could desync between
// them until a manual reload. Everything reactive now goes through this store instead;
// localStorage stays the persistence layer underneath (api.js's interceptor still reads/writes
// the same 'token'/'refreshToken' keys directly for request auth and silent refresh).
export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('token'))
  const user = ref(null)

  const isLoggedIn = computed(() => !!token.value)

  // api.js's request interceptor reads the 'token' key from localStorage on every request,
  // so this only needs to keep that key and the reactive ref in sync.
  function setToken(newToken) {
    token.value = newToken
    if (newToken) {
      localStorage.setItem('token', newToken)
    } else {
      localStorage.removeItem('token')
    }
  }

  async function fetchCurrentUser() {
    const res = await api.get('/auth/me')
    user.value = res.data
    return user.value
  }

  async function login(email, password) {
    const res = await api.post('/auth/login', { email, password })
    setToken(res.data.token)
    localStorage.setItem('refreshToken', res.data.refreshToken)
    await fetchCurrentUser()
  }

  async function register(email, password) {
    await api.post('/auth/register', { email, password })
  }

  async function logout() {
    const storedRefreshToken = localStorage.getItem('refreshToken')
    if (storedRefreshToken) {
      try {
        await api.post('/auth/logout', { refreshToken: storedRefreshToken })
      } catch {
        // Revocation is best-effort; proceed regardless
      }
    }
    localStorage.removeItem('refreshToken')
    setToken(null)
    user.value = null
  }

  // The api.js interceptor dispatches this when a refresh attempt fails outright.
  window.addEventListener('auth:expired', () => {
    token.value = null
    user.value = null
  })

  // Pick up a token already in localStorage (e.g. from a previous session) on first load.
  // Consumers that need `user` populated before acting should await ensureLoaded() first.
  let initialLoad = token.value
    ? fetchCurrentUser().catch(() => {
        // Interceptor will have cleared tokens if refresh also failed
      })
    : null

  async function ensureLoaded() {
    if (initialLoad) await initialLoad
    return user.value
  }

  return { token, user, isLoggedIn, setToken, fetchCurrentUser, ensureLoaded, login, register, logout }
})

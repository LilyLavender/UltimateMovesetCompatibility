import axios from 'axios'

const apiUrl = import.meta.env.VITE_API_URL

const api = axios.create({
  baseURL: `${apiUrl}/api`,
})

// Attach access token to every request
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Refresh access token on 401
let isRefreshing = false
let failedQueue = []

const processQueue = (error, token = null) => {
  failedQueue.forEach(({ resolve, reject }) => {
    if (error) reject(error)
    else resolve(token)
  })
  failedQueue = []
}

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config

    if (error.response?.status !== 401 || original._retry) {
      return Promise.reject(error)
    }

    const refreshToken = localStorage.getItem('refreshToken')
    if (!refreshToken) {
      return Promise.reject(error)
    }

    if (isRefreshing) {
      // Queue this request until the in-flight refresh completes
      return new Promise((resolve, reject) => {
        failedQueue.push({ resolve, reject })
      }).then((token) => {
        original.headers.Authorization = `Bearer ${token}`
        return api(original)
      }).catch((err) => Promise.reject(err))
    }

    original._retry = true
    isRefreshing = true

    try {
      const res = await axios.post(`${apiUrl}/api/auth/refresh`, { refreshToken })
      const { token, refreshToken: newRefreshToken } = res.data

      localStorage.setItem('token', token)
      localStorage.setItem('refreshToken', newRefreshToken)
      api.defaults.headers.common['Authorization'] = `Bearer ${token}`

      processQueue(null, token)
      original.headers.Authorization = `Bearer ${token}`
      return api(original)
    } catch (err) {
      processQueue(err, null)
      localStorage.removeItem('token')
      localStorage.removeItem('refreshToken')
      delete api.defaults.headers.common['Authorization']
      window.dispatchEvent(new Event('auth:expired'))
      return Promise.reject(err)
    } finally {
      isRefreshing = false
    }
  }
)

export default api

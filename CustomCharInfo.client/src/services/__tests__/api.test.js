import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest'
import axios from 'axios'
import api from '@/services/api'

// axios exposes registered interceptors via interceptors.<type>.handlers, which lets us
// exercise the interceptor logic directly without making real network calls.
const requestInterceptor = api.interceptors.request.handlers[0].fulfilled
const responseRejectedInterceptor = api.interceptors.response.handlers[0].rejected

describe('api request interceptor', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('attaches the Authorization header when a token is stored', () => {
    localStorage.setItem('token', 'abc123')

    const config = requestInterceptor({ headers: {} })

    expect(config.headers.Authorization).toBe('Bearer abc123')
  })

  it('does not attach an Authorization header when no token is stored', () => {
    const config = requestInterceptor({ headers: {} })

    expect(config.headers.Authorization).toBeUndefined()
  })
})

describe('api response interceptor (401 refresh flow)', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.restoreAllMocks()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('passes through non-401 errors unchanged', async () => {
    const error = { response: { status: 500 }, config: {} }

    await expect(responseRejectedInterceptor(error)).rejects.toBe(error)
  })

  it('does not retry a request that has already been retried', async () => {
    const error = { response: { status: 401 }, config: { _retry: true } }

    await expect(responseRejectedInterceptor(error)).rejects.toBe(error)
  })

  it('dispatches auth:expired and clears storage when there is no refresh token', async () => {
    localStorage.setItem('token', 'stale')
    const dispatchSpy = vi.spyOn(window, 'dispatchEvent')
    const error = { response: { status: 401 }, config: {} }

    await expect(responseRejectedInterceptor(error)).rejects.toBe(error)

    expect(localStorage.getItem('token')).toBeNull()
    expect(dispatchSpy).toHaveBeenCalledWith(expect.objectContaining({ type: 'auth:expired' }))
  })

  it('refreshes the token and retries the original request on success', async () => {
    localStorage.setItem('refreshToken', 'refresh-1')
    vi.spyOn(axios, 'post').mockResolvedValue({
      data: { token: 'new-token', refreshToken: 'new-refresh' },
    })

    // Override the instance's transport so the retried request resolves without a real
    // network call, instead of relying on jsdom's XHR to reach a live server.
    const originalAdapter = api.defaults.adapter
    api.defaults.adapter = vi.fn().mockResolvedValue({
      data: 'retried-ok', status: 200, statusText: 'OK', headers: {}, config: {},
    })

    try {
      const error = { response: { status: 401 }, config: { headers: {} } }

      const result = await responseRejectedInterceptor(error)

      expect(localStorage.getItem('token')).toBe('new-token')
      expect(localStorage.getItem('refreshToken')).toBe('new-refresh')
      expect(result.data).toBe('retried-ok')
    } finally {
      api.defaults.adapter = originalAdapter
    }
  })

  it('clears storage and dispatches auth:expired when the refresh call itself fails', async () => {
    localStorage.setItem('refreshToken', 'refresh-1')
    localStorage.setItem('token', 'stale')
    vi.spyOn(axios, 'post').mockRejectedValue(new Error('refresh failed'))
    const dispatchSpy = vi.spyOn(window, 'dispatchEvent')

    const error = { response: { status: 401 }, config: { headers: {} } }

    await expect(responseRejectedInterceptor(error)).rejects.toThrow('refresh failed')

    expect(localStorage.getItem('token')).toBeNull()
    expect(localStorage.getItem('refreshToken')).toBeNull()
    expect(dispatchSpy).toHaveBeenCalledWith(expect.objectContaining({ type: 'auth:expired' }))
  })
})

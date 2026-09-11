import { describe, it, expect, vi, beforeEach } from 'vitest'

vi.mock('@/services/api', () => ({
  default: { get: vi.fn() },
}))

import api from '@/services/api'
import router from '@/router/index'

describe('router role guards', () => {
  beforeEach(async () => {
    api.get.mockReset()
    // Reset back to a neutral route between tests so each push exercises the guard fresh.
    if (router.currentRoute.value.name !== 'Home') {
      await router.push({ name: 'Home' })
    }
  })

  describe('/moveset/add (requires userTypeId >= 2)', () => {
    it('redirects anonymous/failed-auth users to ErrorPage 401', async () => {
      api.get.mockRejectedValue(new Error('401'))

      await router.push('/moveset/add')

      expect(router.currentRoute.value.name).toBe('ErrorPage')
      expect(router.currentRoute.value.query.httpCode).toBe('401 Unauthorized')
    })

    it('redirects a plain user (userTypeId 1) to ErrorPage 403', async () => {
      api.get.mockResolvedValue({ data: { userTypeId: 1 } })

      await router.push('/moveset/add')

      expect(router.currentRoute.value.name).toBe('ErrorPage')
      expect(router.currentRoute.value.query.httpCode).toBe('403 Forbidden')
    })

    it('allows a modder (userTypeId 2) through', async () => {
      api.get.mockResolvedValue({ data: { userTypeId: 2 } })

      await router.push('/moveset/add')

      expect(router.currentRoute.value.name).toBe('AddMoveset')
    })

    it('allows an admin (userTypeId 3) through', async () => {
      api.get.mockResolvedValue({ data: { userTypeId: 3 } })

      await router.push('/moveset/add')

      expect(router.currentRoute.value.name).toBe('AddMoveset')
    })
  })

  describe('/series/edit/:seriesId (modders must own a moveset in the series)', () => {
    const mockSeriesEditApi = ({ user, movesets = [], modderName = '' }) => {
      api.get.mockImplementation((url) => {
        if (url === '/auth/me') return Promise.resolve({ data: user })
        if (url === '/logs') return Promise.resolve({ data: [] })
        if (url === '/movesets') return Promise.resolve({ data: movesets })
        if (url.startsWith('/modders/')) return Promise.resolve({ data: { name: modderName } })
        return Promise.reject(new Error(`unexpected url: ${url}`))
      })
    }

    it('redirects a modder who does not own any moveset in the series to ErrorPage 403', async () => {
      mockSeriesEditApi({
        user: { userTypeId: 2, modderId: 5 },
        movesets: [{ modders: ['SomeoneElse'] }],
        modderName: 'RequestingModder',
      })

      await router.push('/series/edit/1')

      expect(router.currentRoute.value.name).toBe('ErrorPage')
      expect(router.currentRoute.value.query.httpCode).toBe('403 Forbidden')
    })

    it('allows a modder who owns a moveset in the series through', async () => {
      mockSeriesEditApi({
        user: { userTypeId: 2, modderId: 5 },
        movesets: [{ modders: ['RequestingModder'] }],
        modderName: 'RequestingModder',
      })

      await router.push('/series/edit/1')

      expect(router.currentRoute.value.name).toBe('EditSeries')
    })
  })

  describe('/admin-portal (requires userTypeId === 3)', () => {
    it('redirects anonymous/failed-auth users to ErrorPage 401', async () => {
      api.get.mockRejectedValue(new Error('401'))

      await router.push('/admin-portal')

      expect(router.currentRoute.value.name).toBe('ErrorPage')
      expect(router.currentRoute.value.query.httpCode).toBe('401 Unauthorized')
    })

    it('redirects a modder (userTypeId 2) to ErrorPage 403', async () => {
      api.get.mockResolvedValue({ data: { userTypeId: 2 } })

      await router.push('/admin-portal')

      expect(router.currentRoute.value.name).toBe('ErrorPage')
      expect(router.currentRoute.value.query.httpCode).toBe('403 Forbidden')
    })

    it('allows an admin (userTypeId 3) through', async () => {
      api.get.mockResolvedValue({ data: { userTypeId: 3 } })

      await router.push('/admin-portal')

      expect(router.currentRoute.value.name).toBe('AdminPortal')
    })
  })
})

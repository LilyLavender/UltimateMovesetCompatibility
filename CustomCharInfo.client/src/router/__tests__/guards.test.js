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

  describe('/series/edit/:seriesId (modders must own or edit a moveset in the series)', () => {
    const mockSeriesEditApi = ({ user, userOwnsMoveset, seriesStatus = 200 }) => {
      api.get.mockImplementation((url) => {
        if (url === '/auth/me') return Promise.resolve({ data: user })
        if (url === '/logs') return Promise.resolve({ data: [] })
        if (url === '/series/1') {
          if (seriesStatus !== 200) return Promise.reject({ response: { status: seriesStatus } })
          return Promise.resolve({ data: { seriesId: 1, userOwnsMoveset } })
        }
        return Promise.reject(new Error(`unexpected url: ${url}`))
      })
    }

    it('redirects a modder who does not own any moveset in the series to ErrorPage 403', async () => {
      mockSeriesEditApi({ user: { userTypeId: 2, modderId: 5 }, userOwnsMoveset: false })

      await router.push('/series/edit/1')

      expect(router.currentRoute.value.name).toBe('ErrorPage')
      expect(router.currentRoute.value.query.httpCode).toBe('403 Forbidden')
    })

    it('allows a modder who owns or edits a moveset in the series through', async () => {
      mockSeriesEditApi({ user: { userTypeId: 2, modderId: 5 }, userOwnsMoveset: true })

      await router.push('/series/edit/1')

      expect(router.currentRoute.value.name).toBe('EditSeries')
    })

    it('treats a 403 from the series endpoint as forbidden rather than a server error', async () => {
      mockSeriesEditApi({ user: { userTypeId: 2, modderId: 5 }, seriesStatus: 403 })

      await router.push('/series/edit/1')

      expect(router.currentRoute.value.name).toBe('ErrorPage')
      expect(router.currentRoute.value.query.httpCode).toBe('403 Forbidden')
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

  describe('/moveset/:movesetId (blocked review states hide the page from non-owners)', () => {
    const blockedLog = {
      itemType: { itemTypeId: 1 },
      item: { movesetId: 1 },
      acceptanceState: { acceptanceStateId: 2 },
      createdAt: '2026-01-01T00:00:00Z',
    }
    const mockMovesetApi = ({ user = null, logs = [], modderIds = [] }) => {
      api.get.mockImplementation((url) => {
        if (url === '/auth/me')
          return user ? Promise.resolve({ data: user }) : Promise.reject(new Error('401'))
        if (url === '/logs')
          return user ? Promise.resolve({ data: logs }) : Promise.reject(new Error('401'))
        if (url === '/movesets/1')
          return Promise.resolve({
            data: { movesetModders: modderIds.map((id) => ({ modder: { modderId: id } })) },
          })
        return Promise.reject(new Error(`unexpected url: ${url}`))
      })
    }

    it('allows an anonymous visitor to a moveset with no blocked state', async () => {
      mockMovesetApi({})

      await router.push('/moveset/1')

      expect(router.currentRoute.value.name).toBe('MovesetDetail')
    })

    it('redirects a signed-in non-owner away from a blocked moveset with 403', async () => {
      mockMovesetApi({ user: { userTypeId: 2, modderId: 9 }, logs: [blockedLog], modderIds: [5] })

      await router.push('/moveset/1')

      expect(router.currentRoute.value.name).toBe('ErrorPage')
      expect(router.currentRoute.value.query.httpCode).toBe('403 Forbidden')
    })

    it('allows the owning modder into a blocked moveset', async () => {
      mockMovesetApi({ user: { userTypeId: 2, modderId: 5 }, logs: [blockedLog], modderIds: [5] })

      await router.push('/moveset/1')

      expect(router.currentRoute.value.name).toBe('MovesetDetail')
    })

    it('allows an admin into a blocked moveset', async () => {
      mockMovesetApi({
        user: { userTypeId: 3, modderId: null },
        logs: [blockedLog],
        modderIds: [5],
      })

      await router.push('/moveset/1')

      expect(router.currentRoute.value.name).toBe('MovesetDetail')
    })
  })

  describe('/moveset/edit/:movesetId (credited modder or editor, as decided by the API)', () => {
    const mockEditApi = (user, canEdit) => {
      api.get.mockImplementation((url) => {
        if (url === '/auth/me') return Promise.resolve({ data: user })
        if (url === '/movesets/1')
          return Promise.resolve({
            data: { canEdit, movesetModders: [{ modder: { modderId: 5 } }] },
          })
        return Promise.reject(new Error(`unexpected url: ${url}`))
      })
    }

    it('redirects a modder who is not on the moveset to ErrorPage 403', async () => {
      mockEditApi({ userTypeId: 2, modderId: 9 }, false)

      await router.push('/moveset/edit/1')

      expect(router.currentRoute.value.name).toBe('ErrorPage')
      expect(router.currentRoute.value.query.httpCode).toBe('403 Forbidden')
    })

    it('allows a modder on the moveset through', async () => {
      mockEditApi({ userTypeId: 2, modderId: 5 }, true)

      await router.push('/moveset/edit/1')

      expect(router.currentRoute.value.name).toBe('EditMoveset')
    })

    it('allows an editor of the moveset through', async () => {
      mockEditApi({ userTypeId: 2, modderId: 7 }, true)

      await router.push('/moveset/edit/1')

      expect(router.currentRoute.value.name).toBe('EditMoveset')
    })

    it('redirects an admin who is not on the moveset to ErrorPage 403', async () => {
      mockEditApi({ userTypeId: 3, modderId: null }, false)

      await router.push('/moveset/edit/1')

      expect(router.currentRoute.value.name).toBe('ErrorPage')
      expect(router.currentRoute.value.query.httpCode).toBe('403 Forbidden')
    })
  })

  describe('/modder/apply (signed in, not yet a modder)', () => {
    it('allows a user without a modder profile through', async () => {
      api.get.mockResolvedValue({ data: { userTypeId: 1, modderId: null } })

      await router.push('/modder/apply')

      expect(router.currentRoute.value.name).toBe('ApplyModder')
    })

    it('redirects an existing modder to ErrorPage 403', async () => {
      api.get.mockResolvedValue({ data: { userTypeId: 2, modderId: 5 } })

      await router.push('/modder/apply')

      expect(router.currentRoute.value.name).toBe('ErrorPage')
      expect(router.currentRoute.value.query.httpCode).toBe('403 Forbidden')
    })
  })

  describe('/modder/edit/:id (own profile, or the pending application you submitted)', () => {
    const mockModderEditApi = ({ user, logs = [] }) => {
      api.get.mockImplementation((url) => {
        if (url === '/auth/me') return Promise.resolve({ data: user })
        if (url === '/logs') return Promise.resolve({ data: logs })
        return Promise.reject(new Error(`unexpected url: ${url}`))
      })
    }

    it('allows a modder to edit their own profile', async () => {
      mockModderEditApi({ user: { id: 'u1', userTypeId: 2, modderId: 5 } })

      await router.push('/modder/edit/5')

      expect(router.currentRoute.value.name).toBe('EditModder')
    })

    it('allows a user whose pending application created the profile', async () => {
      mockModderEditApi({
        user: { id: 'u1', userTypeId: 1, modderId: null },
        logs: [{ itemType: { itemTypeId: 2 }, item: { modderId: 5 } }],
      })

      await router.push('/modder/edit/5')

      expect(router.currentRoute.value.name).toBe('EditModder')
    })

    it("redirects a user editing someone else's profile to ErrorPage 403", async () => {
      mockModderEditApi({ user: { id: 'u1', userTypeId: 2, modderId: 9 } })

      await router.push('/modder/edit/5')

      expect(router.currentRoute.value.name).toBe('ErrorPage')
      expect(router.currentRoute.value.query.httpCode).toBe('403 Forbidden')
    })
  })
})

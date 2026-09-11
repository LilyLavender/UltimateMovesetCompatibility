import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createVuetify } from 'vuetify'

vi.mock('@/services/api', () => ({
  default: { get: vi.fn() },
}))

import api from '@/services/api'
import ActionLogList from '@/components/ActionLogList.vue'
import ActionLogGroup from '@/components/ActionLogGroup.vue'

const vuetify = createVuetify()

const logFor = (itemId, acceptanceStateId, createdAt) => ({
  actionLogId: `${itemId}-${acceptanceStateId}-${createdAt}`,
  itemId,
  itemType: { itemTypeId: 1 },
  item: { movesetId: itemId },
  acceptanceState: { acceptanceStateId },
  createdAt,
})

function mountList(props = {}) {
  return mount(ActionLogList, {
    props,
    global: {
      plugins: [vuetify],
      stubs: { ActionLogGroup: true },
    },
  })
}

describe('ActionLogList', () => {
  beforeEach(() => {
    api.get.mockReset()
    api.get.mockImplementation((url) => {
      if (url === '/auth/me') return Promise.resolve({ data: { userTypeId: 1 } })
      return Promise.resolve({ data: [] })
    })
  })

  it('shows "No logs found." when there are no logs', async () => {
    const wrapper = mountList()
    await flushPromises()

    expect(wrapper.text()).toContain('No logs found.')
    expect(wrapper.findAllComponents(ActionLogGroup)).toHaveLength(0)
  })

  it('groups logs by item and hides groups whose latest state is filtered out by default', async () => {
    api.get.mockImplementation((url) => {
      if (url === '/auth/me') return Promise.resolve({ data: { userTypeId: 1 } })
      return Promise.resolve({
        data: [
          // Item 1: two logs, latest is state 2 (Pending Admin Hard) - visible by default.
          logFor(1, 1, '2024-01-01'),
          logFor(1, 2, '2024-01-02'),
          // Item 2: latest is state 5 (Accepted) - filtered out by the default "only relevant" view.
          logFor(2, 5, '2024-01-03'),
        ],
      })
    })

    const wrapper = mountList()
    await flushPromises()

    const groups = wrapper.findAllComponents(ActionLogGroup)
    expect(groups).toHaveLength(1)
    expect(groups[0].props('logs')).toHaveLength(2)
  })

  it('"Enable All" reveals groups hidden by the default acceptance-state filter', async () => {
    api.get.mockImplementation((url) => {
      if (url === '/auth/me') return Promise.resolve({ data: { userTypeId: 1 } })
      return Promise.resolve({
        data: [
          logFor(1, 2, '2024-01-01'),
          logFor(2, 5, '2024-01-03'),
        ],
      })
    })

    const wrapper = mountList()
    await flushPromises()
    expect(wrapper.findAllComponents(ActionLogGroup)).toHaveLength(1)

    await wrapper.findAll('button').find(b => b.text() === 'Enable All').trigger('click')
    await flushPromises()

    expect(wrapper.findAllComponents(ActionLogGroup)).toHaveLength(2)
  })

  it('"Only Relevant" excludes hooks for a regular user', async () => {
    api.get.mockImplementation((url) => {
      if (url === '/auth/me') return Promise.resolve({ data: { userTypeId: 1 } })
      return Promise.resolve({ data: [] })
    })

    const wrapper = mountList()
    await flushPromises()

    await wrapper.findAll('button').find(b => b.text() === 'Only Relevant').trigger('click')
    await flushPromises()

    const logsCall = api.get.mock.calls.filter(([url]) => url === '/logs').pop()
    expect(logsCall[1].params.itemTypes).toEqual([1, 2, 3])
  })

  it('"Only Relevant" includes hooks for an admin', async () => {
    api.get.mockImplementation((url) => {
      if (url === '/auth/me') return Promise.resolve({ data: { userTypeId: 3 } })
      return Promise.resolve({ data: [] })
    })

    const wrapper = mountList()
    await flushPromises()

    await wrapper.findAll('button').find(b => b.text() === 'Only Relevant').trigger('click')
    await flushPromises()

    const logsCall = api.get.mock.calls.filter(([url]) => url === '/logs').pop()
    expect(logsCall[1].params.itemTypes).toEqual([1, 2, 3, 4])
  })

  it('requests scoped logs for a given userId instead of viewAll', async () => {
    mountList({ userId: 'user-42', viewAll: true })
    await flushPromises()

    const logsCall = api.get.mock.calls.find(([url]) => url === '/logs')
    expect(logsCall[1].params.targetUserId).toBe('user-42')
    expect(logsCall[1].params.viewAll).toBe(false)
  })
})

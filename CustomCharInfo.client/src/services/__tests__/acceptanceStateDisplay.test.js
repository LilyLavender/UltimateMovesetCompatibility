import { describe, it, expect } from 'vitest'
import { latestStatesByItem, latestLogsByItem } from '@/services/acceptanceStateDisplay'
import { AcceptanceState, ItemType } from '@/globals'

describe('latestStatesByItem', () => {
  const rows = [
    { itemTypeId: ItemType.Moveset, itemId: 1, acceptanceStateId: AcceptanceState.Accepted },
    {
      itemTypeId: ItemType.Moveset,
      itemId: 2,
      acceptanceStateId: AcceptanceState.PendingAdminHard,
    },
    { itemTypeId: ItemType.Series, itemId: 1, acceptanceStateId: AcceptanceState.Rejected },
  ]

  it('maps item id to state for one item type only', () => {
    const states = latestStatesByItem(rows, ItemType.Moveset)
    expect([...states]).toEqual([
      [1, AcceptanceState.Accepted],
      [2, AcceptanceState.PendingAdminHard],
    ])
  })

  it('returns an empty map when nothing matches', () => {
    expect(latestStatesByItem(rows, ItemType.Hook).size).toBe(0)
  })
})

describe('latestLogsByItem', () => {
  it('keeps the newest log per item', () => {
    const logs = [
      {
        itemType: { itemTypeId: ItemType.Moveset },
        item: { movesetId: 1 },
        createdAt: '2026-09-01T00:00:00Z',
        acceptanceState: { acceptanceStateId: AcceptanceState.PendingAdminSoft },
      },
      {
        itemType: { itemTypeId: ItemType.Moveset },
        item: { movesetId: 1 },
        createdAt: '2026-09-02T00:00:00Z',
        acceptanceState: { acceptanceStateId: AcceptanceState.Accepted },
      },
    ]
    const latest = latestLogsByItem(logs, ItemType.Moveset, (i) => i?.movesetId)
    expect(latest.get(1).acceptanceState.acceptanceStateId).toBe(AcceptanceState.Accepted)
  })
})

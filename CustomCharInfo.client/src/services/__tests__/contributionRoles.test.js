import { describe, it, expect } from 'vitest'
import { formatRoles, roleCounts } from '@/services/contributionRoles'
import { ContributionRole } from '@/globals'

describe('formatRoles', () => {
  it('joins names in list order regardless of input order', () => {
    expect(formatRoles([ContributionRole.Animation, ContributionRole.Coding])).toBe(
      'Coding, Animation'
    )
  })

  it('spells Other out', () => {
    expect(formatRoles([ContributionRole.Other])).toBe('Other')
  })

  it('returns an empty string for no roles', () => {
    expect(formatRoles([])).toBe('')
    expect(formatRoles(undefined)).toBe('')
  })
})

describe('roleCounts', () => {
  const movesets = [
    { modderRoleIds: [ContributionRole.Animation, ContributionRole.Coding] },
    { modderRoleIds: [ContributionRole.Animation, ContributionRole.Other] },
    { modderRoleIds: [ContributionRole.Animation] },
    { modderRoleIds: [ContributionRole.Modelling] },
    { modderRoleIds: [] },
    { modderRoleIds: null },
  ]

  it('counts per role, drops zeros, sorts by count then list order', () => {
    expect(roleCounts(movesets)).toEqual([
      { roleId: ContributionRole.Animation, name: 'Animation', count: 3 },
      { roleId: ContributionRole.Coding, name: 'Coding', count: 1 },
      { roleId: ContributionRole.Modelling, name: 'Modelling', count: 1 },
    ])
  })

  it('never counts Other', () => {
    expect(roleCounts([{ modderRoleIds: [ContributionRole.Other] }])).toEqual([])
  })

  it('counts a moveset once per role even if the id repeats', () => {
    expect(
      roleCounts([{ modderRoleIds: [ContributionRole.Coding, ContributionRole.Coding] }])
    ).toEqual([{ roleId: ContributionRole.Coding, name: 'Coding', count: 1 }])
  })

  it('handles an empty list', () => {
    expect(roleCounts([])).toEqual([])
    expect(roleCounts(undefined)).toEqual([])
  })
})

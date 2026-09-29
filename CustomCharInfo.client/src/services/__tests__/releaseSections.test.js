import { describe, it, expect } from 'vitest'
import { releaseDateSections, compareByName } from '@/services/releaseSections'

const m = (moddedCharName, releaseState, releaseDate = null, extra = {}) => ({
  moddedCharName,
  releaseState,
  releaseDate,
  privateMoveset: false,
  modders: [],
  ...extra,
})

const names = (list) => list.map((x) => x.moddedCharName)

describe('releaseDateSections', () => {
  it('puts released and pending update in released, newest first, undated last', () => {
    const { released } = releaseDateSections([
      m('Old', 'Released', '2024-01-01'),
      m('Undated', 'Released'),
      m('New', 'Pending Update', '2026-05-01'),
      m('Mid', 'Released', '2025-03-10'),
    ])
    expect(names(released)).toEqual(['New', 'Mid', 'Old', 'Undated'])
  })

  it('mixes dated beta and upcoming oldest first, then undated beta, then undated upcoming', () => {
    const { unreleased } = releaseDateSections([
      m('Upcoming undated', 'Upcoming'),
      m('Beta late', 'Open Beta', '2026-11-15'),
      m('Beta undated', 'Open Beta'),
      m('Upcoming soon', 'Upcoming', '2026-10-01'),
      m('Upcoming later', 'Upcoming', '2027-01-01'),
    ])
    expect(names(unreleased)).toEqual([
      'Upcoming soon',
      'Beta late',
      'Upcoming later',
      'Beta undated',
      'Upcoming undated',
    ])
  })

  it('treats a missing release state as upcoming', () => {
    const { released, unreleased } = releaseDateSections([
      m('No state undated', null),
      m('No state dated', null, '2026-12-01'),
      m('Beta undated', 'Open Beta'),
    ])
    expect(released).toEqual([])
    expect(names(unreleased)).toEqual(['No state dated', 'Beta undated', 'No state undated'])
  })

  it('treats deprecated as released', () => {
    const { released, unreleased } = releaseDateSections([
      m('Gone', 'Deprecated', '2023-02-02'),
      m('Gone undated', 'Deprecated'),
    ])
    expect(names(released)).toEqual(['Gone', 'Gone undated'])
    expect(unreleased).toEqual([])
  })

  it('orders undated ties public first, then by name or first modder', () => {
    const { released } = releaseDateSections([
      m('???', 'Released', null, { privateMoveset: true, modders: ['zed'] }),
      m('Beta', 'Released'),
      m('???', 'Released', null, { privateMoveset: true, modders: ['amy'] }),
      m('Alpha', 'Released'),
    ])
    expect(released.map((x) => x.modders[0] ?? x.moddedCharName)).toEqual([
      'Alpha',
      'Beta',
      'amy',
      'zed',
    ])
  })
})

describe('compareByName', () => {
  it('sorts public before private', () => {
    const list = [
      m('A', 'Released', null, { privateMoveset: true, modders: ['a'] }),
      m('B', 'Released'),
    ]
    expect(names(list.sort(compareByName))).toEqual(['B', 'A'])
  })
})

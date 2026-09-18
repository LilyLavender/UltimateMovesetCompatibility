import { describe, it, expect } from 'vitest'
import {
  dateOnlyStringToLocalDate,
  localDateToDateOnlyString,
  compareDateOnlyStrings,
} from '@/services/dateOnly'

// These exercise the fix for the "release date saved a day off" bug: a Date
// constructed from *local* year/month/day components must always round-trip
// to the identical "yyyy-MM-dd" string, regardless of what the host
// timezone's offset from UTC is. The bug happened because the old code went
// through toISOString()/new Date(dateOnlyString), both of which are UTC -
// these tests never touch either.

describe('localDateToDateOnlyString', () => {
  it('formats a local date as yyyy-MM-dd using local components, not UTC', () => {
    const date = new Date(2026, 0, 15) // Jan 15 2026, local midnight
    expect(localDateToDateOnlyString(date)).toBe('2026-01-15')
  })

  it('zero-pads single-digit months and days', () => {
    const date = new Date(2026, 8, 5) // Sep 5 2026
    expect(localDateToDateOnlyString(date)).toBe('2026-09-05')
  })

  it('returns null for a nullish input', () => {
    expect(localDateToDateOnlyString(null)).toBeNull()
    expect(localDateToDateOnlyString(undefined)).toBeNull()
  })
})

describe('dateOnlyStringToLocalDate', () => {
  it('builds a local-midnight Date from a yyyy-MM-dd string using local components', () => {
    const date = dateOnlyStringToLocalDate('2026-09-10')
    expect(date.getFullYear()).toBe(2026)
    expect(date.getMonth()).toBe(8) // 0-indexed
    expect(date.getDate()).toBe(10)
    expect(date.getHours()).toBe(0)
  })

  it('returns null for a nullish or empty input', () => {
    expect(dateOnlyStringToLocalDate(null)).toBeNull()
    expect(dateOnlyStringToLocalDate('')).toBeNull()
  })
})

describe('round-trip (the actual bug this fixes)', () => {
  it('a picked calendar day survives string -> Date -> string unchanged', () => {
    const original = '2026-09-10'
    const asDate = dateOnlyStringToLocalDate(original)
    const backToString = localDateToDateOnlyString(asDate)
    expect(backToString).toBe(original)
  })

  it('never produces a date one day off, unlike the old toISOString()-based path', () => {
    const picked = new Date(2026, 8, 10) // user picks Sep 10 locally
    const submitted = localDateToDateOnlyString(picked)
    // The old buggy code used picked.toISOString().split('T')[0] here, which
    // shifts this back to '2026-09-09' for any positive UTC offset - this
    // must always be '2026-09-10' regardless of the host timezone.
    expect(submitted).toBe('2026-09-10')
  })
})

describe('compareDateOnlyStrings', () => {
  it('orders chronologically', () => {
    expect(compareDateOnlyStrings('2026-01-01', '2026-06-01')).toBeLessThan(0)
    expect(compareDateOnlyStrings('2026-06-01', '2026-01-01')).toBeGreaterThan(0)
    expect(compareDateOnlyStrings('2026-06-01', '2026-06-01')).toBe(0)
  })

  it('sorts null/undefined dates after real dates', () => {
    expect(compareDateOnlyStrings(null, '2026-06-01')).toBeGreaterThan(0)
    expect(compareDateOnlyStrings('2026-06-01', null)).toBeLessThan(0)
    expect(compareDateOnlyStrings(null, null)).toBe(0)
  })

  it('produces a correctly-ordered sort, unlike numeric subtraction on strings', () => {
    const dates = ['2026-12-01', null, '2026-01-01', '2026-06-01']
    const sorted = [...dates].sort(compareDateOnlyStrings)
    expect(sorted).toEqual(['2026-01-01', '2026-06-01', '2026-12-01', null])
  })
})

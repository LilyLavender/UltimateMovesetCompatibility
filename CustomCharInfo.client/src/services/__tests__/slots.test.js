import { describe, it, expect } from 'vitest'
import { formatSlot, formatSlotRange } from '@/services/slots'

describe('formatSlot', () => {
  it('pads to at least two digits with a c prefix', () => {
    expect(formatSlot(8)).toBe('c08')
    expect(formatSlot(45)).toBe('c45')
    expect(formatSlot(128)).toBe('c128')
    expect(formatSlot(0)).toBe('c00')
  })

  it('accepts numeric strings', () => {
    expect(formatSlot('8')).toBe('c08')
    expect(formatSlot('120')).toBe('c120')
  })

  it('returns an empty string for nothing', () => {
    expect(formatSlot(null)).toBe('')
    expect(formatSlot(undefined)).toBe('')
    expect(formatSlot('')).toBe('')
  })

  it('leaves values it cannot read as a number alone', () => {
    expect(formatSlot('abc')).toBe('cabc')
  })
})

describe('formatSlotRange', () => {
  it('joins two formatted slots with a hyphen', () => {
    expect(formatSlotRange(64, 71)).toBe('c64-c71')
    expect(formatSlotRange(8, 15)).toBe('c08-c15')
    expect(formatSlotRange(120, 127)).toBe('c120-c127')
  })

  it('returns an empty string when either end is missing', () => {
    expect(formatSlotRange(null, 71)).toBe('')
    expect(formatSlotRange(64, undefined)).toBe('')
  })
})

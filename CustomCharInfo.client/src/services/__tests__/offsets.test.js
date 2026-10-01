import { describe, it, expect } from 'vitest'
import { formatOffset } from '@/services/offsets'

describe('formatOffset', () => {
  it('prefixes a stored offset with 0x', () => {
    expect(formatOffset('4B640')).toBe('0x4B640')
    expect(formatOffset('11D7480')).toBe('0x11D7480')
  })

  it('does not double a prefix that is already there', () => {
    expect(formatOffset('0x4B640')).toBe('0x4B640')
    expect(formatOffset('0X4B640')).toBe('0x4B640')
  })

  it('returns an empty string for nothing', () => {
    expect(formatOffset(null)).toBe('')
    expect(formatOffset(undefined)).toBe('')
    expect(formatOffset('')).toBe('')
  })
})

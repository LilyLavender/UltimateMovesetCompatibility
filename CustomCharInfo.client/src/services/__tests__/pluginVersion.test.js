import { describe, it, expect } from 'vitest'
import { normalizeVersionLabel, displayVersion } from '@/services/pluginVersion'

describe('normalizeVersionLabel', () => {
  it('strips a leading "v"', () => {
    expect(normalizeVersionLabel('v1.2.3')).toBe('1.2.3')
    expect(normalizeVersionLabel('V1.2.3')).toBe('1.2.3')
  })

  it('strips a leading "version" and any spaces left behind', () => {
    expect(normalizeVersionLabel('Version 2.0')).toBe('2.0')
    expect(normalizeVersionLabel('VERSION   2.0')).toBe('2.0')
  })

  it('leaves labels with no v/version prefix untouched', () => {
    expect(normalizeVersionLabel('Beta 2.0')).toBe('Beta 2.0')
    expect(normalizeVersionLabel('3.0.2 (standalone)')).toBe('3.0.2 (standalone)')
  })

  it('trims surrounding whitespace', () => {
    expect(normalizeVersionLabel('  1.0  ')).toBe('1.0')
  })

  it('handles null/undefined gracefully', () => {
    expect(normalizeVersionLabel(null)).toBe('')
    expect(normalizeVersionLabel(undefined)).toBe('')
  })
})

describe('displayVersion', () => {
  it('adds a "v" prefix when the label starts with a numeric x.x pattern', () => {
    expect(displayVersion('3.0.2 (standalone)')).toBe('v3.0.2 (standalone)')
    expect(displayVersion('4.0.10-beta.0.1')).toBe('v4.0.10-beta.0.1')
    expect(displayVersion('1.0')).toBe('v1.0')
  })

  it('does not add a "v" prefix when the label does not start with a numeric pattern', () => {
    expect(displayVersion('Beta 2.0')).toBe('Beta 2.0')
    expect(displayVersion('RC1')).toBe('RC1')
  })
})

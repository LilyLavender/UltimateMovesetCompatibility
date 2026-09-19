import { describe, it, expect } from 'vitest'
import { chunk, groupBatchResults, rowStatus, summarizeRows } from '@/services/pluginBatch'

const entry = (path) => ({ file: new File(['x'], path.split('/').pop()), path })

describe('chunk', () => {
  it('splits into fixed-size pieces with a shorter tail', () => {
    expect(chunk([1, 2, 3, 4, 5], 2)).toEqual([[1, 2], [3, 4], [5]])
  })

  it('returns no chunks for an empty array', () => {
    expect(chunk([], 50)).toEqual([])
  })
})

describe('groupBatchResults', () => {
  const found = { attachmentType: 'Moveset', pluginName: 'Waluigi', isCurrent: true }

  it('joins each entry to its result by hash, keeping entry order', () => {
    const entries = [entry('a.nro'), entry('sub/b.nro')]
    const hashes = ['aaa', 'bbb']
    const results = [
      { hash: 'bbb', found: false, result: null },
      { hash: 'aaa', found: true, result: found },
    ]

    const rows = groupBatchResults(entries, hashes, results)
    expect(rows.map((r) => r.path)).toEqual(['a.nro', 'sub/b.nro'])
    expect(rows[0]).toMatchObject({ name: 'a.nro', hash: 'aaa', found: true, result: found })
    expect(rows[1]).toMatchObject({ name: 'b.nro', hash: 'bbb', found: false, result: null })
  })

  it('gives two files with the same hash the same result', () => {
    const entries = [entry('one/plugin.nro'), entry('two/plugin.nro')]
    const rows = groupBatchResults(
      entries,
      ['same', 'same'],
      [{ hash: 'same', found: true, result: found }]
    )
    expect(rows.every((r) => r.found && r.result === found)).toBe(true)
  })

  it('treats a hash the API never answered for as not found', () => {
    const rows = groupBatchResults([entry('a.nro')], ['aaa'], [])
    expect(rows[0]).toMatchObject({ found: false, result: null })
  })

  it('matches hashes regardless of case', () => {
    const rows = groupBatchResults(
      [entry('a.nro')],
      ['ABC'],
      [{ hash: 'abc', found: true, result: found }]
    )
    expect(rows[0].found).toBe(true)
  })
})

describe('rowStatus and summarizeRows', () => {
  it('classifies rows and counts them', () => {
    const rows = [
      { found: true, result: { isCurrent: true } },
      { found: true, result: { isCurrent: false } },
      { found: false, result: null },
      { found: true, result: { isCurrent: true } },
    ]
    expect(rows.map(rowStatus)).toEqual(['current', 'outdated', 'unknown', 'current'])
    expect(summarizeRows(rows)).toEqual({ current: 2, outdated: 1, unknown: 1 })
  })
})

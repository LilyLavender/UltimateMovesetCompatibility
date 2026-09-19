// Pure helpers behind the batch plugin lookup: splitting hashes into request-sized chunks
// and joining the API's per-hash answers back onto the files the user dropped.

export const BATCH_IDENTIFY_MAX_HASHES = 50

export function chunk(array, size) {
  const out = []
  for (let i = 0; i < array.length; i += size) out.push(array.slice(i, i + size))
  return out
}

// entries: [{ file, path }],
// hashes: one hex digest per entry in the same order,
// results: the merged BatchIdentifyResultDto list ({ hash, found, result }).
export function groupBatchResults(entries, hashes, results) {
  const byHash = new Map()
  for (const r of results ?? []) {
    if (r?.hash) byHash.set(r.hash.toLowerCase(), r)
  }

  return entries.map((entry, i) => {
    const hash = hashes[i]
    const match = byHash.get(hash?.toLowerCase())
    return {
      path: entry.path,
      name: entry.file?.name ?? entry.path,
      hash,
      found: Boolean(match?.found && match.result),
      result: match?.found ? (match.result ?? null) : null,
    }
  })
}

// 'current' | 'outdated' | 'unknown'
export function rowStatus(row) {
  if (!row.found || !row.result) return 'unknown'
  return row.result.isCurrent ? 'current' : 'outdated'
}

export function summarizeRows(rows) {
  const summary = { current: 0, outdated: 0, unknown: 0 }
  for (const row of rows) summary[rowStatus(row)]++
  return summary
}

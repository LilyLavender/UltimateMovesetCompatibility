import { describe, it, expect, vi } from 'vitest'
import {
  parseRepoInput,
  parseLinkHeader,
  fetchAllReleases,
  flattenReleaseAssets,
  digestToHash,
  applyMatches,
  registrableRows,
  summarizeStatuses,
  isArchiveAsset,
  rowsNeedingHash,
  withServerHash,
  withHashError,
  GitHubApiError,
  RowStatus,
} from '@/services/githubReleases'

const hashA = 'a'.repeat(64)
const hashB = 'b'.repeat(64)

function fakeResponse(body, { status = 200, headers = {} } = {}) {
  return {
    ok: status >= 200 && status < 300,
    status,
    headers: { get: (name) => headers[name.toLowerCase()] ?? null },
    json: async () => body,
  }
}

describe('parseRepoInput', () => {
  it.each([
    ['owner/repo', 'owner', 'repo'],
    ['  Owner/Repo.Name  ', 'Owner', 'Repo.Name'],
    ['github.com/owner/repo', 'owner', 'repo'],
    ['https://github.com/owner/repo', 'owner', 'repo'],
    ['https://www.github.com/owner/repo.git', 'owner', 'repo'],
    ['https://github.com/owner/repo/releases/tag/v1.0', 'owner', 'repo'],
    ['https://github.com/owner/repo/', 'owner', 'repo'],
  ])('accepts %s', (input, owner, repo) => {
    expect(parseRepoInput(input)).toEqual({ owner, repo })
  })

  it.each([
    '',
    '   ',
    'owner',
    'https://gitlab.com/owner/repo',
    'owner/re po',
    '-owner/repo',
    null,
  ])('rejects %s', (input) => {
    expect(parseRepoInput(input)).toBeNull()
  })
})

describe('parseLinkHeader', () => {
  it('reads every rel', () => {
    const header =
      '<https://api.github.com/x?page=2>; rel="next", <https://api.github.com/x?page=9>; rel="last"'
    expect(parseLinkHeader(header)).toEqual({
      next: 'https://api.github.com/x?page=2',
      last: 'https://api.github.com/x?page=9',
    })
  })

  it('returns nothing for a missing header', () => {
    expect(parseLinkHeader(null)).toEqual({})
  })
})

describe('fetchAllReleases', () => {
  it('follows next links and concatenates pages', async () => {
    const fetchImpl = vi
      .fn()
      .mockResolvedValueOnce(
        fakeResponse([{ id: 1 }], {
          headers: { link: '<https://api.github.com/repos/o/r/releases?page=2>; rel="next"' },
        })
      )
      .mockResolvedValueOnce(fakeResponse([{ id: 2 }]))

    const releases = await fetchAllReleases('o', 'r', fetchImpl)

    expect(releases.map((r) => r.id)).toEqual([1, 2])
    expect(fetchImpl).toHaveBeenCalledTimes(2)
    expect(fetchImpl.mock.calls[0][0]).toBe(
      'https://api.github.com/repos/o/r/releases?per_page=100'
    )
    expect(fetchImpl.mock.calls[1][0]).toBe('https://api.github.com/repos/o/r/releases?page=2')
  })

  it('never sends an Authorization header', async () => {
    const fetchImpl = vi.fn().mockResolvedValue(fakeResponse([]))

    await fetchAllReleases('o', 'r', fetchImpl)

    const headers = fetchImpl.mock.calls[0][1].headers
    expect(Object.keys(headers).map((k) => k.toLowerCase())).not.toContain('authorization')
  })

  it('turns a 404 into a not-found error', async () => {
    const fetchImpl = vi.fn().mockResolvedValue(fakeResponse(null, { status: 404 }))

    const err = await fetchAllReleases('o', 'r', fetchImpl).catch((e) => e)

    expect(err).toBeInstanceOf(GitHubApiError)
    expect(err.kind).toBe('not-found')
  })

  it('turns an exhausted rate limit into a rate-limit error with the reset time', async () => {
    const reset = Math.floor(Date.now() / 1000) + 600
    const fetchImpl = vi.fn().mockResolvedValue(
      fakeResponse(null, {
        status: 403,
        headers: { 'x-ratelimit-remaining': '0', 'x-ratelimit-reset': String(reset) },
      })
    )

    const err = await fetchAllReleases('o', 'r', fetchImpl).catch((e) => e)

    expect(err.kind).toBe('rate-limit')
    expect(err.resetAt.getTime()).toBe(reset * 1000)
  })

  it('treats a 403 with remaining quota as a plain http error', async () => {
    const fetchImpl = vi
      .fn()
      .mockResolvedValue(
        fakeResponse(null, { status: 403, headers: { 'x-ratelimit-remaining': '12' } })
      )

    const err = await fetchAllReleases('o', 'r', fetchImpl).catch((e) => e)

    expect(err.kind).toBe('http')
    expect(err.status).toBe(403)
  })
})

describe('digestToHash', () => {
  it('strips the sha256 prefix and lowercases', () => {
    expect(digestToHash(`sha256:${hashA.toUpperCase()}`)).toBe(hashA)
  })

  it('rejects other algorithms, malformed values, and null', () => {
    expect(digestToHash(`sha512:${hashA}`)).toBeNull()
    expect(digestToHash('sha256:abc')).toBeNull()
    expect(digestToHash(null)).toBeNull()
  })
})

describe('isArchiveAsset', () => {
  it('spots common archive extensions regardless of case', () => {
    expect(isArchiveAsset('Plugin.ZIP')).toBe(true)
    expect(isArchiveAsset('mod.tar.gz')).toBe(true)
    expect(isArchiveAsset('plugin.nro')).toBe(false)
  })
})

describe('flattenReleaseAssets', () => {
  const releases = [
    {
      id: 10,
      tag_name: 'v2.0',
      name: 'Two',
      prerelease: true,
      published_at: '2026-09-01T00:00:00Z',
      html_url: 'https://github.com/o/r/releases/tag/v2.0',
      assets: [
        {
          id: 1,
          name: 'plugin.nro',
          size: 1234,
          download_count: 5,
          digest: `sha256:${hashA}`,
          browser_download_url: 'https://github.com/o/r/releases/download/v2.0/plugin.nro',
        },
        { id: 2, name: 'bundle.zip', size: 99, download_count: 1, digest: `sha256:${hashB}` },
        { id: 3, name: 'old.nro', size: 1, download_count: 0, digest: null },
      ],
    },
    { id: 11, tag_name: 'v1.0', name: '', published_at: '2026-08-01T00:00:00Z', assets: [] },
    { id: 12, tag_name: 'v0.9', draft: true, assets: [{ id: 4, name: 'x.nro' }] },
  ]

  it('makes one row per asset, a placeholder for an empty release, and skips drafts', () => {
    const rows = flattenReleaseAssets(releases)

    expect(rows.map((r) => r.key)).toEqual(['10:1', '10:2', '10:3', '11:none'])
    expect(rows[0]).toMatchObject({
      tag: 'v2.0',
      name: 'Two',
      prerelease: true,
      asset: 'plugin.nro',
      downloads: 5,
      hash: hashA,
      isArchive: false,
      releaseUrl: 'https://github.com/o/r/releases/tag/v2.0',
    })
    expect(rows[0].hashSource).toBe('github')
    expect(rows[1].isArchive).toBe(true)
    expect(rows[2].hash).toBeNull()
    expect(rows[2].hashSource).toBeNull()
    expect(rows[3]).toMatchObject({ asset: null, hash: null, name: 'v1.0' })
  })
})

describe('server-side hashing helpers', () => {
  const rows = flattenReleaseAssets([
    {
      id: 1,
      tag_name: 'v1',
      assets: [
        { id: 1, name: 'with.nro', digest: `sha256:${hashA}`, browser_download_url: 'u1' },
        { id: 2, name: 'old.nro', digest: null, browser_download_url: 'u2' },
        { id: 3, name: 'old.zip', digest: null, browser_download_url: 'u3' },
        { id: 4, name: 'nolink.nro', digest: null },
      ],
    },
    { id: 2, tag_name: 'v0', assets: [] },
  ])

  it('picks only digest-less, downloadable, non-archive assets', () => {
    expect(rowsNeedingHash(rows).map((r) => r.asset)).toEqual(['old.nro'])
  })

  it('records a server hash or an error without touching other fields', () => {
    const hashed = withServerHash(rows[1], hashB.toUpperCase())
    expect(hashed).toMatchObject({ asset: 'old.nro', hash: hashB, hashSource: 'server' })

    const failed = withHashError(rows[1], 'boom')
    expect(failed).toMatchObject({ asset: 'old.nro', hash: null, hashError: 'boom' })
    expect(withHashError(rows[1], '').hashError).toBe('Could not hash this asset.')
  })

  it('lets a server hash flow into matching like a GitHub digest', () => {
    const hashed = rows.map((r) => (r.asset === 'old.nro' ? withServerHash(r, hashB) : r))
    const result = applyMatches(hashed, [{ hash: hashB, status: 'registered', pluginName: 'P' }])
    expect(result[1].status).toBe(RowStatus.Registered)
  })
})

describe('applyMatches and friends', () => {
  const rows = flattenReleaseAssets([
    {
      id: 1,
      tag_name: 'v1',
      assets: [
        { id: 1, name: 'a.nro', digest: `sha256:${hashA}` },
        { id: 2, name: 'b.nro', digest: `sha256:${hashB}` },
        { id: 3, name: 'c.nro', digest: `sha256:${'c'.repeat(64)}` },
        { id: 4, name: 'd.zip', digest: `sha256:${'d'.repeat(64)}` },
        { id: 5, name: 'e.nro', digest: null },
      ],
    },
    { id: 2, tag_name: 'v0', assets: [] },
  ])
  const matches = [
    { hash: hashA, status: 'registered', pluginName: 'P', versionLabel: '1.0' },
    { hash: hashB, status: 'unregistered', checkCount: 4 },
    { hash: 'c'.repeat(64), status: 'unseen' },
    { hash: 'd'.repeat(64), status: 'unseen' },
  ]

  it('derives a status per row and keeps the match', () => {
    const result = applyMatches(rows, matches)

    expect(result.map((r) => r.status)).toEqual([
      RowStatus.Registered,
      RowStatus.Unregistered,
      RowStatus.Unseen,
      RowStatus.Archive,
      RowStatus.NoDigest,
      RowStatus.NoAsset,
    ])
    expect(result[0].match.pluginName).toBe('P')
    expect(result[1].match.checkCount).toBe(4)
  })

  it('reports a registered archive as registered rather than archive', () => {
    const result = applyMatches(rows, [{ hash: 'd'.repeat(64), status: 'registered' }])

    expect(result[3].status).toBe(RowStatus.Registered)
  })

  it('offers only unregistered and unseen rows for batch registration', () => {
    const result = registrableRows(applyMatches(rows, matches))

    expect(result.map((r) => r.asset)).toEqual(['b.nro', 'c.nro'])
  })

  it('counts each status', () => {
    expect(summarizeStatuses(applyMatches(rows, matches))).toEqual({
      registered: 1,
      unregistered: 1,
      unseen: 1,
      noDigest: 1,
      archive: 1,
    })
  })
})

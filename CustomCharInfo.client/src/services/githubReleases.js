// Reads a GitHub repository's releases straight from the browser for the Repo Releases admin page.
// Uses plain fetch on purpose: the site's axios instance would attach the UMC bearer token to
// every request, and that token should never be sent to github.com.

export const GITHUB_API = 'https://api.github.com'
export const RELEASES_PER_PAGE = 100

const REPO_PATTERN =
  /^(?:https?:\/\/)?(?:www\.)?(?:github\.com\/)?([A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?)\/([A-Za-z0-9_.-]+?)(?:\.git)?(?:\/.*)?$/

const ARCHIVE_EXTENSIONS = ['.zip', '.7z', '.rar', '.tar.gz', '.tgz', '.tar']

export const RowStatus = Object.freeze({
  Registered: 'registered',
  Unregistered: 'unregistered',
  Unseen: 'unseen',
  NoDigest: 'no-digest',
  Archive: 'archive',
  NoAsset: 'no-asset',
  Manual: 'manual',
})

export const STATUS_LABELS = Object.freeze({
  [RowStatus.Registered]: 'Registered',
  [RowStatus.Unregistered]: 'Looked up, not registered',
  [RowStatus.Unseen]: 'Never seen',
  [RowStatus.NoDigest]: 'No digest',
  [RowStatus.Archive]: 'Archive, inner file not hashed',
  [RowStatus.NoAsset]: 'No assets',
  [RowStatus.Manual]: 'No plugin.nro, add by hand',
})

const NRO_EXTENSION = '.nro'

// Mirrors GitHubRepoRef.TryParse on the server. Accepts "owner/repo", "github.com/owner/repo/anything"
// and full https URLs with or without ".git". Returns null when the input is not a repository.
export function parseRepoInput(input) {
  const match = REPO_PATTERN.exec((input ?? '').trim())
  if (!match) return null
  const [, owner, repo] = match
  if (!repo || repo === '.' || repo === '..') return null
  return { owner, repo }
}

export function repoUrl({ owner, repo }) {
  return `https://github.com/${owner}/${repo}`
}

// GitHub paginates with a Link header like: <url>; rel="next", <url>; rel="last"
export function parseLinkHeader(header) {
  const links = {}
  if (!header) return links
  for (const part of header.split(',')) {
    const match = /<([^>]+)>\s*;\s*rel="([^"]+)"/.exec(part.trim())
    if (match) links[match[2]] = match[1]
  }
  return links
}

export class GitHubApiError extends Error {
  constructor(message, { kind, status, resetAt = null } = {}) {
    super(message)
    this.name = 'GitHubApiError'
    this.kind = kind
    this.status = status
    this.resetAt = resetAt
  }
}

function toApiError(response) {
  const status = response.status
  const remaining = response.headers.get('x-ratelimit-remaining')
  if ((status === 403 || status === 429) && remaining === '0') {
    const resetHeader = response.headers.get('x-ratelimit-reset')
    const resetAt = resetHeader ? new Date(Number(resetHeader) * 1000) : null
    const when = resetAt ? ` It resets at ${resetAt.toLocaleTimeString()}.` : ''
    return new GitHubApiError(`GitHub's rate limit is used up.${when}`, {
      kind: 'rate-limit',
      status,
      resetAt,
    })
  }
  if (status === 404) {
    return new GitHubApiError('Repository not found, or it has no public releases.', {
      kind: 'not-found',
      status,
    })
  }
  return new GitHubApiError(`GitHub returned ${status}.`, { kind: 'http', status })
}

// Follows the Link header until every page of releases has been read.
export async function fetchAllReleases(owner, repo, fetchImpl = globalThis.fetch) {
  let url = `${GITHUB_API}/repos/${owner}/${repo}/releases?per_page=${RELEASES_PER_PAGE}`
  const releases = []
  while (url) {
    const response = await fetchImpl(url, {
      headers: { Accept: 'application/vnd.github+json' },
    })
    if (!response.ok) throw toApiError(response)
    releases.push(...(await response.json()))
    url = parseLinkHeader(response.headers.get('link')).next ?? null
  }
  return releases
}

export function isArchiveAsset(name) {
  const lower = (name ?? '').toLowerCase()
  return ARCHIVE_EXTENSIONS.some((ext) => lower.endsWith(ext))
}

// GitHub reports digests as "sha256:<hex>". Only SHA-256 is comparable to a registered version.
export function digestToHash(digest) {
  if (typeof digest !== 'string') return null
  const match = /^sha256:([0-9a-fA-F]{64})$/.exec(digest.trim())
  return match ? match[1].toLowerCase() : null
}

// The most recently published non-draft release, prereleases included, as the watched list reports it.
// Null when the repository has no releases.
export function newestRelease(releases) {
  let newest = null
  for (const release of releases ?? []) {
    if (release.draft) continue
    const at = release.published_at ?? release.created_at ?? null
    if (!at) continue
    if (!newest || new Date(at) > new Date(newest.lastReleaseAt)) {
      newest = {
        lastReleaseAt: at,
        lastReleaseTag: release.tag_name ?? '',
        lastReleaseUrl: release.html_url ?? null,
      }
    }
  }
  return newest
}

// One row per asset, with a placeholder row for a release that has no assets so it is not silently dropped.
export function flattenReleaseAssets(releases) {
  const rows = []
  for (const release of releases ?? []) {
    if (release.draft) continue
    const base = {
      releaseId: release.id,
      releaseDate: release.published_at ?? release.created_at ?? null,
      tag: release.tag_name ?? '',
      name: release.name || release.tag_name || '',
      prerelease: Boolean(release.prerelease),
      releaseUrl: release.html_url ?? null,
    }
    const assets = release.assets ?? []
    if (assets.length === 0) {
      rows.push({
        ...base,
        key: `${release.id}:none`,
        asset: null,
        size: null,
        downloads: null,
        hash: null,
        hashSource: null,
        hashError: null,
        downloadUrl: null,
        isArchive: false,
      })
      continue
    }
    for (const asset of assets) {
      const hash = digestToHash(asset.digest)
      rows.push({
        ...base,
        key: `${release.id}:${asset.id}`,
        asset: asset.name ?? '',
        size: asset.size ?? null,
        downloads: asset.download_count ?? 0,
        hash,
        hashSource: hash ? 'github' : null,
        hashError: null,
        downloadUrl: asset.browser_download_url ?? null,
        isArchive: isArchiveAsset(asset.name),
      })
    }
  }
  return rows
}

// Keeps the plugin rows and the no-asset placeholders, and stands in one row for each release
// whose files are all something else, naming them so the release is still visible.
// Those rows have no hash and can never be registered from here.
export function nroRowsWithPlaceholders(rows) {
  const byRelease = new Map()
  for (const row of rows ?? []) {
    if (!byRelease.has(row.releaseId)) byRelease.set(row.releaseId, [])
    byRelease.get(row.releaseId).push(row)
  }
  const result = []
  for (const releaseRows of byRelease.values()) {
    const kept = releaseRows.filter(
      (row) => row.asset == null || row.asset.toLowerCase().endsWith(NRO_EXTENSION)
    )
    if (kept.length > 0) {
      result.push(...kept)
      continue
    }
    const first = releaseRows[0]
    result.push({
      ...first,
      key: `${first.releaseId}:manual`,
      asset: null,
      assetNames: releaseRows.map((row) => row.asset),
      size: null,
      downloads: null,
      hash: null,
      hashSource: null,
      hashError: null,
      downloadUrl: null,
      isArchive: false,
    })
  }
  return result
}

// Assets GitHub has no digest for, which the server can download and hash instead.
// Archives are skipped since their hash would never match a plugin.nro anyway.
export function rowsNeedingHash(rows) {
  return rows.filter((row) => row.asset != null && !row.hash && row.downloadUrl && !row.isArchive)
}

export function withServerHash(row, hash) {
  return { ...row, hash: (hash ?? '').toLowerCase(), hashSource: 'server', hashError: null }
}

export function withHashError(row, message) {
  return { ...row, hashError: message || 'Could not hash this asset.' }
}

// Joins flattened rows to the match-hashes response and derives a display status for each.
export function applyMatches(rows, matches) {
  const byHash = new Map((matches ?? []).map((m) => [m.hash, m]))
  return rows.map((row) => {
    const match = row.hash ? (byHash.get(row.hash) ?? null) : null
    return { ...row, match, status: statusFor(row, match) }
  })
}

function statusFor(row, match) {
  if (row.asset == null) return row.assetNames?.length ? RowStatus.Manual : RowStatus.NoAsset
  if (!row.hash) return RowStatus.NoDigest
  if (match?.status === 'registered') return RowStatus.Registered
  if (match?.status === 'unregistered') return RowStatus.Unregistered
  if (row.isArchive) return RowStatus.Archive
  return RowStatus.Unseen
}

// Rows that the batch register dialog can act on: a real digest, not yet registered, not an archive.
export function registrableRows(rows) {
  return rows.filter(
    (row) => row.status === RowStatus.Unregistered || row.status === RowStatus.Unseen
  )
}

export function summarizeStatuses(rows) {
  const summary = {
    registered: 0,
    unregistered: 0,
    unseen: 0,
    noDigest: 0,
    archive: 0,
    manual: 0,
  }
  for (const row of rows) {
    if (row.status === RowStatus.Registered) summary.registered++
    else if (row.status === RowStatus.Unregistered) summary.unregistered++
    else if (row.status === RowStatus.Unseen) summary.unseen++
    else if (row.status === RowStatus.NoDigest) summary.noDigest++
    else if (row.status === RowStatus.Archive) summary.archive++
    else if (row.status === RowStatus.Manual) summary.manual++
  }
  return summary
}

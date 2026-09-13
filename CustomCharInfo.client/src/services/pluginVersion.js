// Mirrors the backend's PluginController version-label handling (see NormalizeVersionLabel / VersionNumberAtStartPattern)
// The frontend's live previews match what actually gets stored and displayed after submission.

const VERSION_PREFIX = /^v/i
const NUMERIC_START = /^\d+(?:\.\d+)+/

// Strips a leading "version" or "v" (and any leading spaces left behind) from raw user input.
// eg "v1.2.3" -> "1.2.3", "Version 2.0" -> "2.0".
// Used before composing a preview from live input; values already read back from the API are already normalized.
export function normalizeVersionLabel(label) {
  let result = (label ?? '').trimStart()
  if (/^version/i.test(result)) {
    result = result.slice('version'.length)
  } else if (VERSION_PREFIX.test(result)) {
    result = result.slice(1)
  }
  return result.trim()
}

// Adds a "v" prefix only when the label itself starts with a numeric x.x/x.x.x pattern.
// "3.0.2 (standalone)" and "4.0.10-beta.0.1" get one, "Beta 2.0" doesn't.
export function displayVersion(label) {
  const value = label ?? ''
  return NUMERIC_START.test(value) ? `v${value}` : value
}

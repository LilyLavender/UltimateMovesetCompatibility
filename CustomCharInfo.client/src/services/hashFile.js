// Hashes a File entirely client-side (SHA-256). Only the resulting hex digest needs to leave the browser (never the actual file or name).
// .nro plugin binaries are relatively small, so the whole file is read into memory rather than streamed/chunked.
export async function hashFile(file) {
  const buffer = await file.arrayBuffer()
  const digest = await crypto.subtle.digest('SHA-256', buffer)
  return Array.from(new Uint8Array(digest))
    .map((b) => b.toString(16).padStart(2, '0'))
    .join('')
}

// Collects the files behind a drop or a picker and walks dropped folders through the directory-entry API.
// Every result is { file, path } where path is the file's location relative to what was dropped,
// so a plugins folder lists as "plugins/foo/plugin.nro" rather than a pile of identical names.

export function hasExtension(name, extension) {
  if (!extension) return true
  return name.toLowerCase().endsWith(extension.toLowerCase())
}

function stripLeadingSlash(path) {
  return path.startsWith('/') ? path.slice(1) : path
}

function entryToFile(entry) {
  return new Promise((resolve, reject) => entry.file(resolve, reject))
}

// readEntries returns results in batches and must be called until it hands back an empty array.
function readAllEntries(reader) {
  return new Promise((resolve, reject) => {
    const collected = []
    const readBatch = () => {
      reader.readEntries((batch) => {
        if (batch.length === 0) return resolve(collected)
        collected.push(...batch)
        readBatch()
      }, reject)
    }
    readBatch()
  })
}

async function walkEntry(entry, out) {
  if (entry.isFile) {
    const file = await entryToFile(entry)
    out.push({ file, path: stripLeadingSlash(entry.fullPath || file.name) })
    return
  }
  if (entry.isDirectory) {
    const children = await readAllEntries(entry.createReader())
    for (const child of children) await walkEntry(child, out)
  }
}

function fromFileList(fileList) {
  return Array.from(fileList ?? []).map((file) => ({
    file,
    path: file.webkitRelativePath || file.name,
  }))
}

function splitByExtension(entries, extension) {
  const files = []
  let skipped = 0
  for (const entry of entries) {
    if (hasExtension(entry.path, extension)) files.push(entry)
    else skipped++
  }
  return { files, skipped }
}

// For picker input. webkitdirectory pickers fill webkitRelativePath, plain pickers leave empty.
export function collectFilesFromList(fileList, { extension } = {}) {
  return splitByExtension(fromFileList(fileList), extension)
}

// For drop. Falls back to the flat file list when the browser has no entry API.
export async function collectFilesFromDataTransfer(dataTransfer, { extension } = {}) {
  const items = dataTransfer?.items
  const entries = items ? Array.from(items).map((item) => item.webkitGetAsEntry?.()) : []
  const usable = entries.filter(Boolean)

  if (usable.length === 0) {
    return splitByExtension(fromFileList(dataTransfer?.files), extension)
  }

  const out = []
  for (const entry of usable) await walkEntry(entry, out)
  return splitByExtension(out, extension)
}

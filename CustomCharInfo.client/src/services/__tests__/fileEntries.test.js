import { describe, it, expect } from 'vitest'
import {
  hasExtension,
  collectFilesFromList,
  collectFilesFromDataTransfer,
} from '@/services/fileEntries'

function fileEntry(name, fullPath) {
  const file = new File(['x'], name)
  return {
    isFile: true,
    isDirectory: false,
    fullPath,
    file: (resolve) => resolve(file),
  }
}

// Serves children in two batches then an empty one
function dirEntry(fullPath, children) {
  return {
    isFile: false,
    isDirectory: true,
    fullPath,
    createReader: () => {
      let served = 0
      return {
        readEntries: (cb) => {
          if (served >= children.length) return cb([])
          const batch = children.slice(served, served + 1)
          served += batch.length
          cb(batch)
        },
      }
    },
  }
}

function dataTransferWithEntries(entries) {
  return { items: entries.map((e) => ({ webkitGetAsEntry: () => e })), files: [] }
}

describe('hasExtension', () => {
  it('matches case-insensitively and passes everything when no extension is given', () => {
    expect(hasExtension('plugin.NRO', '.nro')).toBe(true)
    expect(hasExtension('readme.txt', '.nro')).toBe(false)
    expect(hasExtension('anything', undefined)).toBe(true)
  })
})

describe('collectFilesFromList', () => {
  it('keeps matching files, counts the rest, and prefers webkitRelativePath', () => {
    const a = new File(['a'], 'a.nro')
    const b = new File(['b'], 'notes.txt')
    const c = new File(['c'], 'c.nro')
    Object.defineProperty(c, 'webkitRelativePath', { value: 'plugins/sub/c.nro' })

    const { files, skipped } = collectFilesFromList([a, b, c], { extension: '.nro' })
    expect(files.map((f) => f.path)).toEqual(['a.nro', 'plugins/sub/c.nro'])
    expect(skipped).toBe(1)
  })
})

describe('collectFilesFromDataTransfer', () => {
  it('walks nested folders through the entry API and strips the leading slash', async () => {
    const tree = dirEntry('/plugins', [
      fileEntry('one.nro', '/plugins/one.nro'),
      dirEntry('/plugins/sub', [
        fileEntry('two.nro', '/plugins/sub/two.nro'),
        fileEntry('skip.txt', '/plugins/sub/skip.txt'),
      ]),
      fileEntry('three.nro', '/plugins/three.nro'),
    ])

    const { files, skipped } = await collectFilesFromDataTransfer(dataTransferWithEntries([tree]), {
      extension: '.nro',
    })
    expect(files.map((f) => f.path)).toEqual([
      'plugins/one.nro',
      'plugins/sub/two.nro',
      'plugins/three.nro',
    ])
    expect(skipped).toBe(1)
  })

  it('falls back to the flat file list when there is no entry API', async () => {
    const dt = { files: [new File(['a'], 'a.nro'), new File(['b'], 'b.bin')] }
    const { files, skipped } = await collectFilesFromDataTransfer(dt, { extension: '.nro' })
    expect(files.map((f) => f.path)).toEqual(['a.nro'])
    expect(skipped).toBe(1)
  })

  it('returns nothing for an empty or missing transfer', async () => {
    expect(await collectFilesFromDataTransfer(null, { extension: '.nro' })).toEqual({
      files: [],
      skipped: 0,
    })
  })
})

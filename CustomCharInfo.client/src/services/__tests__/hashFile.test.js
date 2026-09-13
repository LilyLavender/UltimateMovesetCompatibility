import { describe, it, expect } from 'vitest'
import { hashFile } from '@/services/hashFile'

// Known SHA-256 test vector: sha256("abc") = ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad
describe('hashFile', () => {
  it('hashes a file to the correct lowercase hex SHA-256 digest', async () => {
    const file = new File(['abc'], 'test.nro')
    const hash = await hashFile(file)
    expect(hash).toBe('ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad')
  })

  it('produces a 64-character lowercase hex string', async () => {
    const file = new File(['some plugin bytes'], 'plugin.nro')
    const hash = await hashFile(file)
    expect(hash).toMatch(/^[0-9a-f]{64}$/)
  })

  it('produces different hashes for different content', async () => {
    const a = await hashFile(new File(['one'], 'a.nro'))
    const b = await hashFile(new File(['two'], 'b.nro'))
    expect(a).not.toBe(b)
  })
})

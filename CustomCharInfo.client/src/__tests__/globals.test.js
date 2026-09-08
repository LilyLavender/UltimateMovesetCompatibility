import { describe, it, expect } from 'vitest'
import { IMAGE_UPLOAD_SPECS } from '@/globals'

describe('IMAGE_UPLOAD_SPECS', () => {
  it('defines positive width/height for every upload type', () => {
    for (const [key, spec] of Object.entries(IMAGE_UPLOAD_SPECS)) {
      expect(spec.width, `${key}.width`).toBeGreaterThan(0)
      expect(spec.height, `${key}.height`).toBeGreaterThan(0)
    }
  })

  it('matches the dimensions UploadController.cs enforces server-side', () => {
    // Keep this in sync with allowedTypes in Controllers/UploadController.cs manually -
    // there's no shared source of truth between client and server for these dimensions.
    expect(IMAGE_UPLOAD_SPECS).toEqual({
      thumb_h: { width: 340, height: 82 },
      moveset_hero: { width: 1200, height: 1200 },
      series_icon: { width: 800, height: 800 },
    })
  })
})

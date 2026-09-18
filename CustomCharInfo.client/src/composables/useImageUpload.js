import api from '@/services/api'

// True when an ImageUploadField holds a file the user picked but that has not been sent to R2 yet.
export function isStagedFile(value) {
  return value instanceof File
}

// Uploads a staged File and returns its public URL; passes any other value (an existing URL, empty, null) straight through.
// Moveset-image uploads need the server-side size preset (`type`) and a name for the object key; blog images take neither.
export function useImageUpload(endpoint = '/upload/moveset-image') {
  async function uploadIfNeeded(value, { type, itemName } = {}) {
    if (!isStagedFile(value)) return value

    const formData = new FormData()
    formData.append('File', value)
    if (type) formData.append('Type', type)
    if (type) formData.append('ItemName', itemName || 'unnamed')

    const res = await api.post(endpoint, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    return res.data.url
  }

  return { uploadIfNeeded }
}

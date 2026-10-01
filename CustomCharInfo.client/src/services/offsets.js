// Hook addresses are stored without a prefix (see OffsetFormat on the server) and always shown with 0x.

export function formatOffset(offset) {
  if (offset === null || offset === undefined || offset === '') return ''
  return `0x${String(offset).replace(/^0x/i, '')}`
}

// Slot labels always read as "c" plus at least two digits: c08, c45, c128.
// Every place a slot number is shown goes through these two functions.

export function formatSlot(slot) {
  if (slot === null || slot === undefined || slot === '') return ''
  const n = Number(slot)
  if (!Number.isInteger(n) || n < 0) return `c${slot}`
  return `c${String(n).padStart(2, '0')}`
}

export function formatSlotRange(start, end) {
  if (start === null || start === undefined || end === null || end === undefined) return ''
  return `${formatSlot(start)}-${formatSlot(end)}`
}

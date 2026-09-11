// Converts between a "yyyy-MM-dd" date-only string and a JS Date,
// always using local calendar components on both sides.
// Never route through toISOString()/new Date(dateOnlyString),
// as those parse/format as UTC midnight and shift the calendar day for anyone off UTC+0.

export function dateOnlyStringToLocalDate(dateOnlyString) {
  if (!dateOnlyString) return null
  const [year, month, day] = dateOnlyString.split('-').map(Number)
  return new Date(year, month - 1, day)
}

export function localDateToDateOnlyString(date) {
  if (!date) return null
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

// Compares two "yyyy-MM-dd" strings (or nullish) chronologically without constructing Date objects.
// Returns <0, 0, or >0 like a normal comparator.
export function compareDateOnlyStrings(a, b) {
  if (!a && !b) return 0
  if (!a) return 1
  if (!b) return -1
  return a < b ? -1 : a > b ? 1 : 0
}

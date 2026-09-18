// Swaps an item with its neighbour in place; dir is -1 (up) or 1 (down). No-op at the ends.
export function moveItem(arr, i, dir) {
  const j = i + dir
  if (j < 0 || j >= arr.length) return
  const tmp = arr[i]
  arr[i] = arr[j]
  arr[j] = tmp
}

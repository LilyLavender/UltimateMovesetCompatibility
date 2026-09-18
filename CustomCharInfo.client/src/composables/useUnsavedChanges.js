import { ref, watch, onMounted, onUnmounted } from 'vue'
import { onBeforeRouteLeave } from 'vue-router'

// Warns before navigating away from a form with unsaved edits, both in-app (router) and on tab close.
// Call takeSnapshot() once the form holds its initial data (after any async load), and markSaved() after a successful submit.
export function useUnsavedChanges(
  form,
  { message = 'You have unsaved changes. Are you sure you want to leave?' } = {}
) {
  const isDirty = ref(false)
  let snapshot = null
  let saved = false

  function takeSnapshot() {
    snapshot = JSON.stringify(form.value)
    isDirty.value = false
  }

  function markSaved() {
    isDirty.value = false
    saved = true
  }

  watch(
    form,
    () => {
      if (snapshot !== null) isDirty.value = JSON.stringify(form.value) !== snapshot
    },
    { deep: true }
  )

  const shouldWarn = () => isDirty.value && !saved

  const handleBeforeUnload = (e) => {
    if (shouldWarn()) {
      e.preventDefault()
      e.returnValue = ''
    }
  }
  onMounted(() => window.addEventListener('beforeunload', handleBeforeUnload))
  onUnmounted(() => window.removeEventListener('beforeunload', handleBeforeUnload))

  onBeforeRouteLeave((to, from, next) => {
    next(shouldWarn() ? window.confirm(message) : true)
  })

  return { isDirty, takeSnapshot, markSaved }
}

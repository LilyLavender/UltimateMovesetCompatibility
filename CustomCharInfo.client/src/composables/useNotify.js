import { reactive } from 'vue'

// App-wide toast messages, rendered once by AppSnackbar in App.vue.
// Any component calls useNotify() and pushes a message; the host shows them one at a time.
const state = reactive({
  visible: false,
  message: '',
  color: 'info',
  timeout: 4000,
  queue: [],
})

const TIMEOUTS = { success: 4000, info: 4000, warning: 6000, error: 8000 }

function push(color, message) {
  const entry = { message, color, timeout: TIMEOUTS[color] }
  if (state.visible) {
    state.queue.push(entry)
  } else {
    show(entry)
  }
}

function show(entry) {
  state.message = entry.message
  state.color = entry.color
  state.timeout = entry.timeout
  state.visible = true
}

// Called by the host when the current message closes.
function showNext() {
  const next = state.queue.shift()
  if (next) show(next)
}

// Mirrors what the old alert() calls appended: the server's response body when there is one, else the error message.
function describeError(err) {
  if (!err) return ''
  const data = err.response?.data
  if (data === undefined) return err.message || ''
  return typeof data === 'string' ? data : JSON.stringify(data)
}

export function useNotify() {
  return {
    success: (message) => push('success', message),
    info: (message) => push('info', message),
    warning: (message) => push('warning', message),
    // Pass the caught error to append its detail on a second line.
    error: (message, err) => {
      const detail = describeError(err)
      push('error', detail ? `${message}\n${detail}` : message)
    },
  }
}

export { state as notifyState, showNext }

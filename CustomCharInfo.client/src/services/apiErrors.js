// Turns a failed axios request into readable messages, one per line.
export function extractErrorMessages(err) {
  const data = err.response?.data

  // The 'auth' rate limit rejects with an empty body
  if (err.response?.status === 429) {
    return ['Too many attempts. Wait a minute and try again.']
  }

  // ASP.NET Identity validation errors (array)
  if (Array.isArray(data)) {
    return data.map((e) => e.description || e.message || String(e))
  }

  // Plain-string bodies such as BadRequest("Invalid user")
  if (typeof data === 'string' && data.trim()) {
    return [data]
  }

  // ASP.NET ProblemDetails / custom object
  if (typeof data === 'object' && data !== null) {
    if (data.message) {
      return [data.message]
    }

    if (data.title) {
      return [data.title]
    }
  }

  // Axios error
  if (err.message) {
    return [err.message]
  }

  // Fallback
  return ['An error occurred.']
}

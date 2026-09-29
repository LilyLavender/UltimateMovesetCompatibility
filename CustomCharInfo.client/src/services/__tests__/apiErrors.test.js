import { describe, it, expect } from 'vitest'
import { extractErrorMessages } from '@/services/apiErrors'

const failed = (status, data, message = `Request failed with status code ${status}`) => ({
  message,
  response: { status, data },
})

describe('extractErrorMessages', () => {
  it('returns each Identity error description', () => {
    const err = failed(400, [
      { code: 'PasswordTooShort', description: 'Passwords must be at least 6 characters.' },
      {
        code: 'PasswordRequiresDigit',
        description: "Passwords must have at least one digit ('0'-'9').",
      },
    ])
    expect(extractErrorMessages(err)).toEqual([
      'Passwords must be at least 6 characters.',
      "Passwords must have at least one digit ('0'-'9').",
    ])
  })

  it('returns a plain-string body as is', () => {
    expect(extractErrorMessages(failed(400, 'Invalid user'))).toEqual(['Invalid user'])
  })

  it('ignores a blank string body', () => {
    expect(extractErrorMessages(failed(401, ''))).toEqual(['Request failed with status code 401'])
  })

  it('prefers message, then title, on an object body', () => {
    expect(extractErrorMessages(failed(400, { message: 'Nope', title: 'Bad' }))).toEqual(['Nope'])
    expect(extractErrorMessages(failed(400, { title: 'Bad Request' }))).toEqual(['Bad Request'])
  })

  it('explains a rate-limited request', () => {
    expect(extractErrorMessages(failed(429, ''))).toEqual([
      'Too many attempts. Wait a minute and try again.',
    ])
  })

  it('falls back to the axios message, then a generic one', () => {
    expect(extractErrorMessages({ message: 'Network Error' })).toEqual(['Network Error'])
    expect(extractErrorMessages({})).toEqual(['An error occurred.'])
  })
})

import { describe, it, expect } from 'vitest'
import router from '@/router/index'
import {
  headerItems,
  menus,
  subnavs,
  adminTiles,
  reachableRouteNames,
  visibleItems,
  resolveTo,
  sectionOf,
} from '@/navigation'
import { UserType } from '@/globals'

const anonymous = null
const plainUser = { userTypeId: UserType.User, modderId: null, modderIdFuture: null }
const applicant = { userTypeId: UserType.User, modderId: null, modderIdFuture: 9 }
const modder = { userTypeId: UserType.Modder, modderId: 7, modderIdFuture: null }
const admin = { userTypeId: UserType.Admin, modderId: 3, modderIdFuture: null }

const labels = (items) => items.filter((i) => i.label).map((i) => i.label)

describe('every named route is reachable', () => {
  const reachable = reachableRouteNames()
  const namedRoutes = router.getRoutes().filter((r) => r.name)

  it.each(namedRoutes.map((r) => [r.name]))('%s', (name) => {
    expect(reachable.has(name)).toBe(true)
  })

  it('names only routes that exist', () => {
    const existing = new Set(namedRoutes.map((r) => r.name))
    for (const name of reachable) expect(existing.has(name)).toBe(true)
  })

  it('assigns every named route a header section, or marks it contextual', () => {
    const unsectioned = namedRoutes
      .map((r) => r.name)
      .filter((name) => !sectionOf(name))
      .filter((name) => !['ErrorPage', 'ForgotPasswordPage', 'ResetPasswordPage'].includes(name))
      .filter(
        (name) =>
          ![
            'AboutPage',
            'PhotoSubmissions',
            'PrivacyPolicyPage',
            'ApiPage',
            'OpenSource',
            'MovesetSubmissionGuide',
            'ModderCreditGuide',
            'ImageHostingPage',
          ].includes(name)
      )
    expect(unsectioned).toEqual([])
  })
})

describe('role predicates', () => {
  it('signed out: the account item has no menu and the movesets menu has no submit link', () => {
    const account = headerItems.find((i) => i.key === 'account')
    expect(account.menuWhen(anonymous)).toBe(false)
    expect(labels(visibleItems(menus.movesets, anonymous))).not.toContain('Submit a moveset')
  })

  it('plain user: can apply for modder, sees no submit links or admin portal', () => {
    const items = labels(visibleItems(menus.account, plainUser))
    expect(items).toContain('Apply for modder')
    expect(items).toContain('My likes')
    expect(items).not.toContain('My content')
    expect(items).not.toContain('Submit a moveset')
    expect(items).not.toContain('Admin portal')
  })

  it('applicant: sees the pending note instead of the apply link', () => {
    const items = visibleItems(menus.account, applicant)
    expect(labels(items)).not.toContain('Apply for modder')
    expect(items.some((i) => i.note)).toBe(true)
  })

  it('modder: sees content, profile, and submit links, no admin portal', () => {
    const items = labels(visibleItems(menus.account, modder))
    expect(items[0]).toBe('My content')
    expect(items).toContain('View my profile')
    expect(items).toContain('Submit a hook')
    expect(items).not.toContain('Admin portal')
    expect(labels(visibleItems(subnavs.movesets.actions, modder))).toContain('Submit a moveset')
  })

  it('admin: sees everything including the admin portal', () => {
    const items = labels(visibleItems(menus.account, admin))
    expect(items).toContain('Admin portal')
    expect(items).toContain('Submit a moveset')
    expect(labels(visibleItems(subnavs.blog.actions, admin))).toContain('Add post')
    expect(adminTiles.flatMap((g) => g.items).length).toBe(13)
  })

  it('resolves profile links against the user', () => {
    const view = menus.account.find((i) => i.label === 'View my profile')
    expect(resolveTo(view, modder)).toEqual({ name: 'ModderDetail', params: { id: 7 } })
  })

  it('never leaves a divider at an edge or doubled', () => {
    const items = visibleItems(menus.account, anonymous)
    expect(items[0].divider).toBeFalsy()
    expect(items[items.length - 1].divider).toBeFalsy()
    for (let i = 1; i < items.length; i++) {
      expect(items[i].divider && items[i - 1].divider).toBeFalsy()
    }
  })
})

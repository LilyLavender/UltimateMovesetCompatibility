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
  canApplyForModder,
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

  it('plain user: account, likes, and sign out only', () => {
    const items = labels(visibleItems(menus.account, plainUser))
    expect(items).toEqual(['Account', 'My likes', 'Sign out'])
  })

  it('applicant: the menu carries no apply link or pending note; the account page does', () => {
    const items = visibleItems(menus.account, applicant)
    expect(labels(items)).not.toContain('Apply for modder')
    expect(items.some((i) => i.note)).toBe(false)
    expect(canApplyForModder(plainUser)).toBe(true)
  })

  it('modder: account, content, likes, profile links, sign out, no admin portal', () => {
    const items = labels(visibleItems(menus.account, modder))
    expect(items).toEqual([
      'Account',
      'My content',
      'My likes',
      'View my profile',
      'Edit my profile',
      'Sign out',
    ])
    expect(labels(visibleItems(subnavs.movesets.actions, modder))).toContain('Submit a moveset')
    expect(labels(visibleItems(subnavs.movesets.actions, modder))).toContain('Submit a plugin')
  })

  it('admin: the same menu plus the admin portal before sign out', () => {
    const items = labels(visibleItems(menus.account, admin))
    expect(items.slice(-2)).toEqual(['Admin portal', 'Sign out'])
    expect(items).not.toContain('Submit a moveset')
    expect(labels(visibleItems(subnavs.blog.actions, admin))).toContain('Add post')
    expect(adminTiles.flatMap((g) => g.items).length).toBe(13)
  })

  it('hub actions can be limited to certain pages', () => {
    const onlyOn = (name) =>
      subnavs.movesets.actions.filter((a) => !a.on || a.on.includes(name)).map((a) => a.label)
    expect(onlyOn('Movesets')).toEqual(['Submit a moveset'])
    expect(onlyOn('PluginLookup')).toEqual(['Submit a plugin'])
    expect(onlyOn('Hooks')).toEqual(['Submit a hook'])
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

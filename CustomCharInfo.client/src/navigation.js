/*
  The single source of every link between pages: header items, dropdown menus, hub sub-navs,
  admin tiles, footer links, and the routes only reached from inside a page.
  navigation.test.js fails when a named route is reachable from none of these.
  Each item may carry `show(user)`; `user` is the auth store's user or null when signed out.
  `to` is a route object, or a function of the user for routes that need the user's own ids.
*/
import { UserType } from '@/globals'

export const isSignedIn = (user) => !!user
export const isModder = (user) => !!user && user.userTypeId >= UserType.Modder
export const isAdmin = (user) => !!user && user.userTypeId === UserType.Admin
export const hasModderPage = (user) => !!user?.modderId
export const hasPendingApplication = (user) => !!user && !user.modderId && !!user.modderIdFuture
export const canApplyForModder = (user) =>
  !!user && !user.modderId && !user.modderIdFuture && user.userTypeId === UserType.User

export const headerItems = [
  { key: 'home', label: 'Home', to: { name: 'Home' } },
  { key: 'movesets', label: 'Movesets', to: { name: 'Movesets' }, menu: 'movesets' },
  { key: 'series', label: 'Series', to: { name: 'Series' } },
  { key: 'modders', label: 'Modders', to: { name: 'ModdersPage' } },
  { key: 'blog', label: 'Blog', to: { name: 'Blog' } },
  {
    key: 'account',
    label: 'Account',
    icon: 'mdi-account',
    to: { name: 'UserActions' },
    menu: 'account',
    menuWhen: isSignedIn,
  },
]

export const menus = {
  movesets: [
    { label: 'All movesets', to: { name: 'Movesets' }, icon: 'mdi-view-list' },
    { label: 'Moveset table', to: { name: 'MovesetsList' }, icon: 'mdi-table' },
    { label: 'Slot grid', to: { name: 'SlotGrid' }, icon: 'mdi-grid' },
    {
      label: 'Compatibility check',
      to: { name: 'CompatibilityCheck' },
      icon: 'mdi-swap-horizontal',
    },
    { label: 'Plugin lookup', to: { name: 'PluginLookup' }, icon: 'mdi-file-search' },
    { label: 'Hooks', to: { name: 'Hooks' }, icon: 'mdi-hook' },
    { divider: true, show: isModder },
    { label: 'Submit a moveset', to: { name: 'AddMoveset' }, icon: 'mdi-plus', show: isModder },
  ],
  account: [
    {
      label: 'My content',
      to: { name: 'MyContent' },
      icon: 'mdi-view-list',
      show: isModder,
      strong: true,
    },
    { label: 'My likes', to: { name: 'MyLikes' }, icon: 'mdi-heart', show: isSignedIn },
    { divider: true, show: hasModderPage },
    {
      label: 'View my profile',
      to: (user) => ({ name: 'ModderDetail', params: { id: user.modderId } }),
      icon: 'mdi-account-eye',
      show: hasModderPage,
    },
    {
      label: 'Edit my profile',
      to: (user) => ({ name: 'EditModder', params: { id: user.modderId } }),
      icon: 'mdi-account-edit',
      show: hasModderPage,
    },
    { divider: true, show: canApplyForModder },
    {
      label: 'Apply for modder',
      to: { name: 'ApplyModder' },
      icon: 'mdi-account-plus',
      show: canApplyForModder,
    },
    { note: 'Modder application pending', icon: 'mdi-account-clock', show: hasPendingApplication },
    { divider: true, show: isModder },
    { label: 'Submit a moveset', to: { name: 'AddMoveset' }, icon: 'mdi-plus', show: isModder },
    { label: 'Submit a series', to: { name: 'AddSeries' }, icon: 'mdi-plus', show: isModder },
    { label: 'Submit a hook', to: { name: 'AddHook' }, icon: 'mdi-plus', show: isModder },
    { label: 'Submit a plugin', to: { name: 'AddPlugin' }, icon: 'mdi-plus', show: isModder },
    { divider: true, show: isAdmin },
    {
      label: 'Admin portal',
      to: { name: 'AdminPortal' },
      icon: 'mdi-shield-account',
      show: isAdmin,
    },
    { divider: true },
    { label: 'Settings', to: { name: 'UserActions' }, icon: 'mdi-cog' },
    { label: 'Log out', action: 'logout', icon: 'mdi-logout' },
  ],
}

export const subnavs = {
  movesets: {
    items: [
      { label: 'All movesets', to: { name: 'Movesets' }, icon: 'mdi-view-list' },
      { label: 'Table', to: { name: 'MovesetsList' }, icon: 'mdi-table' },
      { label: 'Slot grid', to: { name: 'SlotGrid' }, icon: 'mdi-grid' },
      {
        label: 'Compatibility check',
        to: { name: 'CompatibilityCheck' },
        icon: 'mdi-swap-horizontal',
      },
      { label: 'Plugin lookup', to: { name: 'PluginLookup' }, icon: 'mdi-file-search' },
      { label: 'Hooks', to: { name: 'Hooks' }, icon: 'mdi-hook' },
    ],
    actions: [
      { label: 'Submit a moveset', to: { name: 'AddMoveset' }, icon: 'mdi-plus', show: isModder },
    ],
  },
  series: {
    items: [{ label: 'All series', to: { name: 'Series' }, icon: 'mdi-view-list' }],
    actions: [
      { label: 'Submit a series', to: { name: 'AddSeries' }, icon: 'mdi-plus', show: isModder },
      {
        label: 'Request an edit',
        to: { name: 'RequestEditSeries' },
        icon: 'mdi-pencil',
        show: isModder,
      },
    ],
  },
  modders: {
    items: [{ label: 'All modders', to: { name: 'ModdersPage' }, icon: 'mdi-account-group' }],
    actions: [
      {
        label: 'Apply for modder',
        to: { name: 'ApplyModder' },
        icon: 'mdi-account-plus',
        show: canApplyForModder,
      },
    ],
  },
  blog: {
    items: [{ label: 'Posts', to: { name: 'Blog' }, icon: 'mdi-post' }],
    actions: [{ label: 'Add post', to: { name: 'AddBlogPost' }, icon: 'mdi-plus', show: isAdmin }],
  },
  account: {
    items: [
      { label: 'Account', to: { name: 'UserActions' }, icon: 'mdi-cog' },
      { label: 'My content', to: { name: 'MyContent' }, icon: 'mdi-view-list', show: isModder },
      { label: 'My likes', to: { name: 'MyLikes' }, icon: 'mdi-heart', show: isSignedIn },
    ],
    actions: [],
  },
}

export const adminTiles = [
  {
    group: 'Review',
    items: [
      {
        label: 'Action log manager',
        to: { name: 'AdminAccepter' },
        icon: 'mdi-shield-check',
        description: 'Accept, reject, and comment on submissions.',
        badge: 'pendingAdmin',
      },
      {
        label: 'Hidden content',
        to: { name: 'HiddenContent' },
        icon: 'mdi-eye-off',
        description: 'Everything blocked from public listings.',
      },
      {
        label: 'Admin picks',
        to: { name: 'AdminPicks' },
        icon: 'mdi-star',
        description: 'Choose the featured movesets on the home page.',
      },
      {
        label: 'Delete movesets',
        to: { name: 'MovesetDeleteManager' },
        icon: 'mdi-delete-sweep',
        description: 'Remove a moveset and its images for good.',
      },
    ],
  },
  {
    group: 'Content',
    items: [
      {
        label: 'Add blog post',
        to: { name: 'AddBlogPost' },
        icon: 'mdi-post',
        description: 'Write and publish a post.',
      },
      {
        label: 'Banner images',
        to: { name: 'BannerImageManager' },
        icon: 'mdi-image-multiple',
        description: 'Manage the home page hero collage.',
      },
      {
        label: 'Game versions',
        to: { name: 'GameVersions' },
        icon: 'mdi-update',
        description: 'Add a game version and shift hook offsets.',
      },
      {
        label: 'All plugins',
        to: { name: 'AllPlugins' },
        icon: 'mdi-puzzle',
        description: 'Every registered plugin and its hashes.',
      },
      {
        label: 'Repo releases',
        to: { name: 'RepoReleases' },
        icon: 'mdi-github',
        description: 'Match GitHub release assets to plugins.',
      },
    ],
  },
  {
    group: 'Users and maintenance',
    items: [
      {
        label: 'All users',
        to: { name: 'UserList' },
        icon: 'mdi-account-group',
        description: 'Roles, modder links, and sign-in history.',
      },
      {
        label: 'Reset passwords',
        to: { name: 'AdminPasswordResetter' },
        icon: 'mdi-account-lock-open',
        description: 'Generate a reset link for a user.',
      },
      {
        label: 'Image garbage collector',
        to: { name: 'ImageGarbageCollector' },
        icon: 'mdi-image-remove',
        description: 'Find and delete unreferenced images.',
      },
      {
        label: 'Notification simulator',
        to: { name: 'NotificationSimulator' },
        icon: 'mdi-bell-cog',
        description: 'Preview the badges a user would see.',
      },
    ],
  },
]

export const footerLinks = [
  { label: 'About UMC', to: { name: 'AboutPage' } },
  { label: 'Photo submissions', to: { name: 'PhotoSubmissions' } },
  { label: 'Privacy policy', to: { name: 'PrivacyPolicyPage' } },
  { label: 'API', to: { name: 'ApiPage' } },
  { label: 'Open source', to: { name: 'OpenSource' } },
]

export const footerGuides = [
  { label: 'Moveset submission guide', to: { name: 'MovesetSubmissionGuide' } },
  { label: 'Who should I credit?', to: { name: 'ModderCreditGuide' } },
  { label: 'Image hosting', to: { name: 'ImageHostingPage' } },
]

/* Routes reached from inside a page (a card, a form, a login panel), never from navigation. */
export const contextualRoutes = [
  'MovesetDetail',
  'EditMoveset',
  'SeriesDetail',
  'EditSeries',
  'ModderDetail',
  'EditModder',
  'EditHook',
  'ForgotPasswordPage',
  'ResetPasswordPage',
  'ErrorPage',
]

/* Which header item lights up for a route. */
export const sections = {
  home: ['Home'],
  movesets: [
    'Movesets',
    'MovesetsList',
    'SlotGrid',
    'CompatibilityCheck',
    'PluginLookup',
    'AddPlugin',
    'Hooks',
    'AddHook',
    'EditHook',
    'MovesetDetail',
    'AddMoveset',
    'EditMoveset',
  ],
  series: ['Series', 'SeriesDetail', 'AddSeries', 'EditSeries', 'RequestEditSeries'],
  modders: ['ModdersPage', 'ModderDetail', 'ApplyModder', 'EditModder'],
  blog: ['Blog', 'AddBlogPost'],
  account: [
    'UserActions',
    'MyContent',
    'MyLikes',
    'AdminPortal',
    ...adminTiles.flatMap((g) => g.items.map((i) => i.to.name)),
  ],
}

export function sectionOf(routeName) {
  for (const [key, names] of Object.entries(sections)) {
    if (names.includes(routeName)) return key
  }
  return null
}

/* Resolves an item's route for the given user. */
export function resolveTo(item, user) {
  return typeof item.to === 'function' ? item.to(user) : item.to
}

/* Applies each item's `show` predicate and drops dividers that would sit at an edge or double up. */
export function visibleItems(items, user) {
  const shown = items.filter((item) => !item.show || item.show(user))
  const out = []
  for (const item of shown) {
    if (item.divider) {
      if (out.length === 0 || out[out.length - 1].divider) continue
    }
    out.push(item)
  }
  while (out.length && out[out.length - 1].divider) out.pop()
  return out
}

/* Every route name that navigation can reach, for the test and for future audits. */
export function reachableRouteNames() {
  const names = new Set()
  const add = (item) => {
    if (!item || item.divider || item.note || item.action) return
    const to = resolveTo(item, { modderId: 0 })
    if (to?.name) names.add(to.name)
  }
  headerItems.forEach(add)
  Object.values(menus).forEach((list) => list.forEach(add))
  Object.values(subnavs).forEach((s) => {
    s.items.forEach(add)
    s.actions.forEach(add)
  })
  adminTiles.forEach((g) => g.items.forEach(add))
  footerLinks.forEach(add)
  footerGuides.forEach(add)
  contextualRoutes.forEach((n) => names.add(n))
  return names
}

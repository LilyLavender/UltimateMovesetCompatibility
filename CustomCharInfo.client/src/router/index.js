import { createRouter, createWebHashHistory } from 'vue-router'
import {
  createAuthGuard,
  createMovesetViewGuard,
  createMovesetOwnerGuard,
  createSeriesEditGuard,
  createModderApplyGuard,
  createModderEditGuard,
} from '@/router/guards'
import { UserType } from '@/globals'
// Basic
import HomePage from '@/views/HomePage.vue'
import ErrorPage from '@/views/ErrorPage.vue'
import AboutPage from '@/views/AboutPage.vue'
import PhotoSubmissionPage from '@/views/PhotoSubmissionPage.vue'
import ImageHostingPage from '@/views/ImageHostingPage.vue'
import MovesetSubmissionGuide from '@/views/MovesetSubmissionGuide.vue'
import ModderCreditGuide from '@/views/ModderCreditGuide.vue'
import OpenSourcePage from '@/views/OpenSourcePage.vue'
import PrivacyPolicyPage from '@/views/PrivacyPolicyPage.vue'
import ForgotPasswordPage from '@/views/ForgotPasswordPage.vue'
import ApiPage from '@/views/ApiPage.vue'
// Movesets
import MovesetsPage from '@/views/MovesetsPage.vue'
import MovesetsListPage from '@/views/MovesetsListPage.vue'
import SlotGridPage from '@/views/SlotGridPage.vue'
import MovesetDetail from '@/views/MovesetDetail.vue'
import AddMoveset from '@/views/AddMoveset.vue'
import EditMoveset from '@/views/EditMoveset.vue'
// Modders
import ModdersPage from '@/views/ModdersPage.vue'
import ModderDetail from '@/views/ModderDetail.vue'
import ApplyModder from '@/views/ApplyModder.vue'
import EditModder from '@/views/EditModder.vue'
// Series
import SeriesPage from '@/views/SeriesPage.vue'
import SeriesDetail from '@/views/SeriesDetail.vue'
import AddSeries from '@/views/AddSeries.vue'
import EditSeries from '@/views/EditSeries.vue'
import RequestEditSeries from '@/views/RequestEditSeries.vue'
// Tools
import CompatibilityCheckPage from '@/views/CompatibilityCheckPage.vue'
import PluginLookupPage from '@/views/PluginLookupPage.vue'
import AddPluginPage from '@/views/AddPluginPage.vue'
// Hooks
import HooksPage from '@/views/HooksPage.vue'
import AddHook from '@/views/AddHook.vue'
import EditHook from '@/views/EditHook.vue'
// Blog
import BlogPage from '@/views/BlogPage.vue'
import BlogPostForm from '@/components/BlogPostForm.vue'
// User
import AccountPage from '@/views/AccountPage.vue'
import MyContentPage from '@/views/MyContentPage.vue'
import MyLikesPage from '@/views/MyLikesPage.vue'
import ResetPasswordPage from '@/views/ResetPasswordPage.vue'
// Admin
import AdminPortal from '@/views/AdminPortal.vue'
import AdminAccepter from '@/views/AdminAccepter.vue'
import NotificationSimulator from '@/views/NotificationSimulator.vue'
import AdminPicks from '@/views/AdminPicks.vue'
import HiddenContentPage from '@/views/HiddenContentPage.vue'
import AdminPasswordResetter from '@/views/AdminPasswordResetter.vue'
import UserList from '@/views/UserList.vue'
import ImageGarbageCollector from '@/views/ImageGarbageCollector.vue'
import BannerImageManager from '@/views/BannerImageManager.vue'
import MovesetDeleteManager from '@/views/MovesetDeleteManager.vue'
import AllPluginsPage from '@/views/AllPluginsPage.vue'

const routes = [
  {
    path: '/',
    name: 'Home',
    component: HomePage,
  },
  {
    path: '/movesets',
    name: 'Movesets',
    component: MovesetsPage,
    meta: { title: 'Movesets' },
  },
  {
    path: '/moveset-table',
    name: 'MovesetsList',
    component: MovesetsListPage,
    meta: { title: 'Movesets' },
  },
  {
    path: '/slot-grid',
    name: 'SlotGrid',
    component: SlotGridPage,
    meta: { title: 'Slot Grid' },
  },
  {
    path: '/compatibility',
    name: 'CompatibilityCheck',
    component: CompatibilityCheckPage,
    meta: { title: 'Compatibility Check' },
  },
  {
    path: '/plugin-lookup',
    name: 'PluginLookup',
    component: PluginLookupPage,
    meta: { title: 'Plugin Lookup' },
  },
  {
    path: '/plugins/add',
    name: 'AddPlugin',
    component: AddPluginPage,
    meta: { title: 'Submit a Plugin' },
    beforeEnter: createAuthGuard((user) => user.userTypeId >= UserType.Modder),
  },
  {
    path: '/blog',
    name: 'Blog',
    component: BlogPage,
    meta: { title: 'Blog' },
  },
  {
    path: '/series',
    name: 'Series',
    component: SeriesPage,
    meta: { title: 'Series' },
  },
  {
    path: '/series/request-edit',
    name: 'RequestEditSeries',
    component: RequestEditSeries,
    meta: { title: 'Edit a Series' },
    beforeEnter: createAuthGuard((user) => user.userTypeId >= UserType.Modder),
  },
  {
    path: '/series/:seriesId',
    name: 'SeriesDetail',
    component: SeriesDetail,
    props: true,
  },
  {
    path: '/moveset/:movesetId',
    name: 'MovesetDetail',
    component: MovesetDetail,
    props: true,
    beforeEnter: createMovesetViewGuard(),
  },
  {
    path: '/moveset/edit/:movesetId',
    name: 'EditMoveset',
    component: EditMoveset,
    props: true,
    beforeEnter: createMovesetOwnerGuard(),
  },
  {
    path: '/series/edit/:seriesId',
    name: 'EditSeries',
    component: EditSeries,
    props: true,
    beforeEnter: createSeriesEditGuard(),
  },
  {
    path: '/moveset/add',
    name: 'AddMoveset',
    component: AddMoveset,
    meta: { title: 'Submit Moveset' },
    beforeEnter: createAuthGuard((user) => user.userTypeId >= UserType.Modder),
  },
  {
    path: '/series/add',
    name: 'AddSeries',
    component: AddSeries,
    meta: { title: 'Submit Series' },
    beforeEnter: createAuthGuard((user) => user.userTypeId >= UserType.Modder),
  },
  {
    path: '/modders',
    name: 'ModdersPage',
    component: ModdersPage,
    meta: { title: 'Modders' },
  },
  {
    path: '/modder/:id',
    name: 'ModderDetail',
    component: ModderDetail,
    props: true,
  },
  {
    path: '/modder/apply',
    name: 'ApplyModder',
    component: ApplyModder,
    props: true,
    beforeEnter: createModderApplyGuard(),
  },
  {
    path: '/modder/edit/:id',
    name: 'EditModder',
    component: EditModder,
    meta: { title: 'Editing Modder Page' },
    props: true,
    beforeEnter: createModderEditGuard(),
  },
  {
    path: '/hooks',
    name: 'Hooks',
    component: HooksPage,
    meta: { title: 'Hooks' },
  },
  {
    path: '/hooks/add',
    name: 'AddHook',
    component: AddHook,
    meta: { title: 'Submit Hook' },
    beforeEnter: createAuthGuard((user) => user.userTypeId >= UserType.Modder),
  },
  {
    path: '/hooks/edit/:hookId',
    name: 'EditHook',
    component: EditHook,
    props: true,
    beforeEnter: createAuthGuard((user) => user.userTypeId >= UserType.Modder),
  },
  {
    path: '/user-actions',
    name: 'UserActions',
    component: AccountPage,
    meta: { title: 'User Settings' },
  },
  {
    path: '/my-content',
    name: 'MyContent',
    component: MyContentPage,
    meta: { title: 'My Content' },
  },
  {
    path: '/my-likes',
    name: 'MyLikes',
    component: MyLikesPage,
    meta: { title: 'My Likes' },
    beforeEnter: createAuthGuard(() => true),
  },
  {
    path: '/admin-portal',
    name: 'AdminPortal',
    component: AdminPortal,
    meta: { title: 'Admin portal' },
    beforeEnter: createAuthGuard((user) => user.userTypeId === UserType.Admin),
  },
  {
    path: '/action-log-manager',
    name: 'AdminAccepter',
    component: AdminAccepter,
    meta: { title: 'Action Log Manager' },
    beforeEnter: createAuthGuard((user) => user.userTypeId === UserType.Admin),
  },
  {
    path: '/notification-simulator',
    name: 'NotificationSimulator',
    component: NotificationSimulator,
    meta: { title: 'Notification Simulator' },
    beforeEnter: createAuthGuard((user) => user.userTypeId === UserType.Admin),
  },
  {
    path: '/hidden-content',
    name: 'HiddenContent',
    component: HiddenContentPage,
    meta: { title: 'Hidden Content' },
    beforeEnter: createAuthGuard((user) => user.userTypeId === UserType.Admin),
  },
  {
    path: '/admin-picks',
    name: 'AdminPicks',
    component: AdminPicks,
    meta: { title: 'Admin Picks' },
    beforeEnter: createAuthGuard((user) => user.userTypeId === UserType.Admin),
  },
  {
    path: '/admin-password-resetter',
    name: 'AdminPasswordResetter',
    component: AdminPasswordResetter,
    meta: { title: 'Password Resetter' },
    beforeEnter: createAuthGuard((user) => user.userTypeId === UserType.Admin),
  },
  {
    path: '/add-blog-post',
    name: 'AddBlogPost',
    component: BlogPostForm,
    meta: { title: 'Add Blog Post' },
    beforeEnter: createAuthGuard((user) => user.userTypeId === UserType.Admin),
  },
  {
    path: '/user-list',
    name: 'UserList',
    component: UserList,
    meta: { title: 'User List' },
    beforeEnter: createAuthGuard((user) => user.userTypeId === UserType.Admin),
  },
  {
    path: '/image-garbage-collector',
    name: 'ImageGarbageCollector',
    component: ImageGarbageCollector,
    meta: { title: 'Image Garbage Collector' },
    beforeEnter: createAuthGuard((user) => user.userTypeId === UserType.Admin),
  },
  {
    path: '/banner-image-manager',
    name: 'BannerImageManager',
    component: BannerImageManager,
    meta: { title: 'Banner Image Manager' },
    beforeEnter: createAuthGuard((user) => user.userTypeId === UserType.Admin),
  },
  {
    path: '/moveset-delete-manager',
    name: 'MovesetDeleteManager',
    component: MovesetDeleteManager,
    meta: { title: 'Moveset Delete Manager' },
    beforeEnter: createAuthGuard((user) => user.userTypeId === UserType.Admin),
  },
  {
    path: '/all-plugins',
    name: 'AllPlugins',
    component: AllPluginsPage,
    meta: { title: 'All Plugins' },
    beforeEnter: createAuthGuard((user) => user.userTypeId === UserType.Admin),
  },
  {
    path: '/about',
    name: 'AboutPage',
    component: AboutPage,
    meta: { title: 'About' },
  },
  {
    path: '/reset-password',
    name: 'ResetPasswordPage',
    component: ResetPasswordPage,
    meta: { title: 'Reset Password' },
  },
  {
    path: '/photo-submissions',
    name: 'PhotoSubmissions',
    component: PhotoSubmissionPage,
    meta: { title: 'Photo Submissions' },
  },
  {
    path: '/image-hosting',
    name: 'ImageHostingPage',
    component: ImageHostingPage,
    meta: { title: 'Image Hosting Info' },
  },
  {
    path: '/open-source',
    name: 'OpenSource',
    component: OpenSourcePage,
    meta: { title: 'Why Open-Source Your Moveset?' },
  },
  {
    path: '/moveset-submission-guide',
    name: 'MovesetSubmissionGuide',
    component: MovesetSubmissionGuide,
    meta: { title: 'Moveset Submission Guide' },
  },
  {
    path: '/modder-credit-guide',
    name: 'ModderCreditGuide',
    component: ModderCreditGuide,
    meta: { title: 'Who Should I Include?' },
  },
  {
    path: '/privacy-policy',
    name: 'PrivacyPolicyPage',
    component: PrivacyPolicyPage,
    meta: { title: 'Privacy Policy' },
  },
  {
    path: '/api',
    name: 'ApiPage',
    component: ApiPage,
    meta: { title: 'Public API' },
  },
  {
    path: '/forgot-password',
    name: 'ForgotPasswordPage',
    component: ForgotPasswordPage,
    meta: { title: 'Forgot Password?' },
  },
  {
    path: '/error',
    name: 'ErrorPage',
    component: ErrorPage,
    props: (route) => ({
      httpCode: route.query.httpCode,
      reason: route.query.reason,
      extra: route.query.extra,
    }),
  },
  {
    path: '/:catchAll(.*)*',
    component: ErrorPage,
    props: {
      extra: 'Try the navigation links in the header?',
    },
  },
]

export default createRouter({
  history: createWebHashHistory('/UltimateMovesetCompatibility/'),
  routes,
})

import { createRouter, createWebHashHistory  } from 'vue-router';
import api from '@/services/api'
// Basic
import HomePage from '@/views/HomePage.vue';
import ErrorPage from '@/views/ErrorPage.vue';
import AboutPage from '@/views/AboutPage.vue';
import PhotoSubmissionPage from '@/views/PhotoSubmissionPage.vue';
// Movesets
import MovesetsPage from '@/views/MovesetsPage.vue';
import MovesetsListPage from '@/views/MovesetsListPage.vue';
import MovesetDetail from '@/views/MovesetDetail.vue';
import AddMoveset from '@/views/AddMoveset.vue';
import EditMoveset from '@/views/EditMoveset.vue';
// Modders
import ModderDetail from '@/views/ModderDetail.vue';
import ApplyModder from '@/views/ApplyModder.vue';
import EditModder from '@/views/EditModder.vue';
// Series
import SeriesPage from '@/views/SeriesPage.vue';
import AddSeries from '@/views/AddSeries.vue';
import EditSeries from '@/views/EditSeries.vue';
// Blog
import BlogPage from '@/views/BlogPage.vue';
import BlogPostForm from '@/components/BlogPostForm.vue';
// By user type
import AccountPage from '@/views/AccountPage.vue';
import AdminPortal from '@/views/AdminPortal.vue';
import AdminAccepter from '@/views/AdminAccepter.vue';
import UserList from '@/views/UserList.vue';

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
    path: '/moveset/:movesetId',
    name: 'MovesetDetail',
    component: MovesetDetail,
    props: true,
    beforeEnter: async (to, from, next) => {
      const movesetId = parseInt(to.params.movesetId);
      const blockedStates = [2, 4, 6];
    
      try {
        const logsRes = await api.get('/logs', { params: { userId: null } });
        const latestLog = logsRes.data
          .filter(log => log.itemType?.itemTypeId === 1 && log.item?.movesetId === movesetId)
          .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))[0];
      
        const moveset = await api.get(`/movesets/${movesetId}`);
        const modderIds = moveset.data.movesetModders.map(m => m.modderId);
      
        let user = null;
        try {
          user = (await api.get('/auth/me')).data;
        } catch (err) {
          // Not logged in
        }
      
        const isPrivate = latestLog && blockedStates.includes(latestLog.acceptanceState.acceptanceStateId);
      
        if (isPrivate) {
          const isAdmin = user?.userTypeId === 3;
          const isOwner = user && modderIds.includes(user.modderId);
        
          if (!isAdmin && !isOwner) {
            return next({
              name: 'ErrorPage',
              query: {
                httpCode: '403 Forbidden',
                reason: 'This moveset is currently private.',
                extra: 'Check back later, or try signing in.',
              },
            });
          }
        }
      
        return next();
      } catch (err) {
        return next({
          name: 'ErrorPage',
          query: {
            httpCode: '500 Server Error',
            reason: 'Could not load the moveset.',
            extra: err.message || 'Please try again later.',
          },
        });
      }
    }
  },
  {
    path: '/moveset/edit/:movesetId',
    name: 'EditMoveset',
    component: EditMoveset,
    props: true,
    beforeEnter: async (to, from, next) => {
      try {
        const moveset = await api.get(`/movesets/${to.params.movesetId}`);
        const modderIds = moveset.data.movesetModders.map(m => m.modderId);
        const user = (await api.get('/auth/me')).data;

        if (modderIds.includes(user.modderId) || user.userTypeId === 3) {
          next();
        } else {
          next({
            name: 'ErrorPage',
            query: {
              httpCode: '403 Forbidden',
              reason: 'You do not have permission to access this page.',
              extra: 'Try signing in?',
            }
          })
        }
      } catch {
        next({
          name: 'ErrorPage',
          query: {
            httpCode: '401 Unauthorized',
            reason: 'Authentication failed.',
            extra: 'Try signing in or refreshing the page.',
          }
        })
      }
    }
  },
  {
    path: '/series/edit/:seriesId',
    name: 'EditSeries',
    component: EditSeries,
    props: true,
    beforeEnter: async (to, from, next) => {
      try {
        const seriesId = parseInt(to.params.seriesId);
        const user = (await api.get('/auth/me')).data;

        // Get all logs for series
        const logsRes = await api.get('/logs', { params: { userId: null } });
        const latestLog = logsRes.data
          .filter(log => log.itemType?.itemTypeId === 3 && log.item?.seriesId === seriesId)
          .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))[0];
        
        // Restrict if the latest acceptanceState is not 3 or 4
        const stateId = latestLog?.acceptanceState?.acceptanceStateId;
        if (stateId !== 3 && stateId !== 4) {
          if (user.userTypeId == 2) {
            next();
          } else if ((stateId === 1 || stateId === 2) && user.userTypeId === 3) {
            next();
          } else {
            next({
              name: 'ErrorPage',
              query: {
                httpCode: '403 Forbidden',
                reason: 'You do not have permission to edit this series.',
                extra: '',
              }
            });
            return;
          }
        }

        // Get movesets from series
        const movesets = (await api.get('/movesets', { params: { seriesId } })).data;

        // No movesets, only modders or admins
        if (movesets.length === 0) {
          if (user.userTypeId === 2 || user.userTypeId === 3) {
            next();
          } else {
            next({
              name: 'ErrorPage',
              query: {
                httpCode: '403 Forbidden',
                reason: 'You do not have permission to edit this series.',
                extra: '',
              }
            });
          }
          return;
        }

        // Check if user is a modder of a moveset in this series
        const modderNames = movesets.flatMap(m => m.modders);
        const modderName = (await api.get(`/modders/${user.modderId}`)).data.name;
        if (modderNames.includes(modderName)) {
          next();
        } else {
          next({
            name: 'ErrorPage',
            query: {
              httpCode: '403 Forbidden',
              reason: 'You do not have permission to edit this series.',
              extra: 'Only modders of movesets in this series can edit it.',
            }
          });
        }
      } catch (err) {
        next({
          name: 'ErrorPage',
          query: {
            httpCode: '401 Unauthorized',
            reason: 'Authentication failed.',
            extra: 'Try signing in or refreshing the page.',
          }
        });
      }
    }
  },
  {
    path: '/moveset/add',
    name: 'AddMoveset',
    component: AddMoveset,
    meta: { title: 'Submit Moveset' },
    beforeEnter: async (to, from, next) => {
      try {
        const user = (await api.get('/auth/me')).data;
        if (user.userTypeId >= 2) {
          next();
        } else {
          next({
            name: 'ErrorPage',
            query: {
              httpCode: '403 Forbidden',
              reason: 'You do not have permission to access this page.',
            }
          })
        }
      } catch (err) {
        next({
          name: 'ErrorPage',
          query: {
            httpCode: '401 Unauthorized',
            reason: 'Authentication failed.',
            extra: 'Try signing in or refreshing the page.',
          }
        })
      }
    }
  },
  {
    path: '/series/add',
    name: 'AddSeries',
    component: AddSeries,
    meta: { title: 'Submit Series' },
    beforeEnter: async (to, from, next) => {
      try {
        const user = (await api.get('/auth/me')).data;
        if (user.userTypeId >= 2) {
          next();
        } else {
          next({
            name: 'ErrorPage',
            query: {
              httpCode: '403 Forbidden',
              reason: 'You do not have permission to access this page.',
            }
          })
        }
      } catch (err) {
        next({
          name: 'ErrorPage',
          query: {
            httpCode: '401 Unauthorized',
            reason: 'Authentication failed.',
            extra: 'Try signing in or refreshing the page.',
          }
        })
      }
    }
  },
  {
    path: '/modder/:id',
    name: 'ModderDetail',
    component: ModderDetail,
    props: true,
    beforeEnter: async (to, from, next) => {
      const modderId = parseInt(to.params.id);
      const blockedStates = [2, 4, 6];
    
      try {
        const res = await api.get('/logs', { params: { userId: null } });
        const logs = res.data;
      
        const latestLog = logs
          .filter(log => log.itemType?.itemTypeId === 2 && log.item?.modderId === modderId)
          .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))[0];
      
        const submitted = logs.some(log =>
          log.itemType?.itemTypeId === 2 &&
          log.item?.modderId === modderId
        );
      
        let user = null;
        try {
          user = (await api.get('/auth/me')).data;
        } catch (err) {
          // Unauthenticated
        }
      
        const isPrivate = latestLog && blockedStates.includes(latestLog.acceptanceState.acceptanceStateId);
        const isAdmin = user?.userTypeId === 3;
        const isSelf = user?.modderId === modderId;
      
        if (isPrivate && !isAdmin && !isSelf && !submitted) {
          return next({
            name: 'ErrorPage',
            query: {
              httpCode: '403 Forbidden',
              reason: 'This modder page is currently private.',
              extra: 'Check back later, or try signing in.',
            }
          });
        }
      
        return next();
      } catch (err) {
        return next({
          name: 'ErrorPage',
          query: {
            httpCode: '500 Server Error',
            reason: 'Could not load the modder page.',
            extra: err.message || 'Please try again later.',
          }
        });
      }
    }
  },
  {
    path: '/modder/apply',
    name: 'ApplyModder',
    component: ApplyModder,
    props: true,
    beforeEnter: async (to, from, next) => {
      try {
        const user = (await api.get('/auth/me')).data;
        if ((!user.modderId || user.modderId === null)) {
          next();
        } else {
          next({
          name: 'ErrorPage',
            query: {
              httpCode: '403 Forbidden',
              reason: 'You have already applied for modder.',
            }
          })
        }
      } catch {
        next({
          name: 'ErrorPage',
          query: {
            httpCode: '401 Unauthorized',
            reason: 'Authentication failed.',
            extra: 'Try signing in or refreshing the page.',
          }
        })
      }
    }
  },
  {
    path: '/modder/edit/:id',
    name: 'EditModder',
    component: EditModder,
    meta: { title: 'Editing Modder Page' },
    props: true,
    beforeEnter: async (to, from, next) => {
      try {
        const user = (await api.get('/auth/me')).data;
        if (user.modderId === parseInt(to.params.id)) {
          next();
        } else {
          const logsRes = await api.get(`/logs`, { params: { userId: user.id } });
          const submitted = logsRes.data.find(log =>
            log.itemType?.itemTypeId === 2 &&
            log.item?.modderId === parseInt(to.params.id)
          );
          if (submitted) {
            next();
          } else {
            next({
              name: 'ErrorPage',
              query: {
                httpCode: '403 Forbidden',
                reason: 'You do not have permission to access this page.',
                extra: 'Try signing in?',
              }
            })
          }
        }
      } catch {
        next({
          name: 'ErrorPage',
          query: {
            httpCode: '401 Unauthorized',
            reason: 'Authentication failed.',
            extra: 'Try signing in or refreshing the page.',
          }
        })
      }
    },
  },
  {
    path: '/user-actions',
    name: 'UserActions',
    component: AccountPage,
    meta: { title: 'User Settings' },
  },
  {
    path: '/admin-portal',
    name: 'AdminPortal',
    component: AdminPortal,
    meta: { title: 'Admin portal' },
    beforeEnter: async (to, from, next) => {
      try {
        const user = (await api.get('/auth/me')).data;
        if (user.userTypeId === 3) {
          next();
        } else {
          next({
            name: 'ErrorPage',
            query: {
              httpCode: '403 Forbidden',
              reason: 'You do not have permission to access this page.',
            }
          })
        }
      } catch (err) {
        next({
          name: 'ErrorPage',
          query: {
            httpCode: '401 Unauthorized',
            reason: 'Authentication failed.',
            extra: 'Try signing in or refreshing the page.',
          }
        })
      }
    }
  },
  {
    path: '/action-log-manager',
    name: 'AdminAccepter',
    component: AdminAccepter,
    meta: { title: 'Admin Accepter' },
    beforeEnter: async (to, from, next) => {
      try {
        const user = (await api.get('/auth/me')).data;
        if (user.userTypeId === 3) {
          next();
        } else {
          next({
            name: 'ErrorPage',
            query: {
              httpCode: '403 Forbidden',
              reason: 'You do not have permission to access this page.',
            }
          })
        }
      } catch (err) {
        next({
          name: 'ErrorPage',
          query: {
            httpCode: '401 Unauthorized',
            reason: 'Authentication failed.',
            extra: 'Try signing in or refreshing the page.',
          }
        })
      }
    }
  },
  {
    path: '/add-blog-post',
    name: 'AddBlogPost',
    component: BlogPostForm,
    meta: { title: 'Add Blog Post' },
    beforeEnter: async (to, from, next) => {
      try {
        const user = (await api.get('/auth/me')).data;
        if (user.userTypeId === 3) {
          next();
        } else {
          next({
            name: 'ErrorPage',
            query: {
              httpCode: '403 Forbidden',
              reason: 'You do not have permission to access this page.',
            }
          })
        }
      } catch (err) {
        next({
          name: 'ErrorPage',
          query: {
            httpCode: '401 Unauthorized',
            reason: 'Authentication failed.',
            extra: 'Try signing in or refreshing the page.',
          }
        })
      }
    }
  },
  {
    path: '/user-list',
    name: 'UserList',
    component: UserList,
    meta: { title: 'User List' },
    beforeEnter: async (to, from, next) => {
      try {
        const user = (await api.get('/auth/me')).data;
        if (user.userTypeId === 3) {
          next();
        } else {
          next({
            name: 'ErrorPage',
            query: {
              httpCode: '403 Forbidden',
              reason: 'You do not have permission to access this page.',
            }
          })
        }
      } catch (err) {
        next({
          name: 'ErrorPage',
          query: {
            httpCode: '401 Unauthorized',
            reason: 'Authentication failed.',
            extra: 'Try signing in or refreshing the page.',
          }
        })
      }
    },
  },
  {
    path: '/about',
    name: 'AboutPage',
    component: AboutPage,
    meta: { title: 'About' },
  },
  {
    path: '/photo-submissions',
    name: 'PhotoSubmissions',
    component: PhotoSubmissionPage,
    meta: { title: 'Photo Submissions' },
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
});
/**
 * Vue Router
 * Route guards:
 *  - requiresAuth: redirect to /login if not authenticated
 *  - requiresPermission: redirect to /403 if permission missing
 *  - requiresGuest: redirect to / if already authenticated (login page)
 * Business module routes are lazy-loaded and added dynamically via
 * addBusinessModuleRoutes() as modules are enabled per tenant.
 */
import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '@/stores/auth.store'

const routes: RouteRecordRaw[] = [
  // ── Public routes ─────────────────────────────────────────────────────────
  {
    path: '/login',
    name: 'login',
    component: () => import('@/views/auth/LoginView.vue'),
    meta: { requiresGuest: true, layout: 'auth' },
  },
  {
    path: '/403',
    name: 'forbidden',
    component: () => import('@/views/errors/ForbiddenView.vue'),
    meta: { layout: 'blank' },
  },
  {
    path: '/404',
    name: 'not-found',
    component: () => import('@/views/errors/NotFoundView.vue'),
    meta: { layout: 'blank' },
  },

  // ── Authenticated shell ───────────────────────────────────────────────────
  {
    path: '/',
    component: () => import('@/layouts/DefaultLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'dashboard',
        component: () => import('@/views/DashboardView.vue'),
      },
      {
        path: 'settings',
        name: 'settings',
        component: () => import('@/views/settings/SettingsView.vue'),
        meta: { permission: 'settings:read' },
      },

      // ── Corporation & Campus ───────────────────────────────────────────────
      {
        path: 'corporations',
        name: 'corporations',
        component: () => import('@/views/corporations/CorporationListView.vue'),
        meta: { permission: 'corporation:read' },
      },
      {
        path: 'corporations/:id',
        name: 'corporation-detail',
        component: () => import('@/views/corporations/CorporationDetailView.vue'),
        meta: { permission: 'corporation:read' },
      },
      {
        path: 'corporations/:id/settings',
        name: 'corporation-settings',
        component: () => import('@/views/corporations/CorporationSettingsView.vue'),
        meta: { permission: 'corporation:read' },
      },
      {
        path: 'campuses',
        name: 'campuses',
        component: () => import('@/views/campuses/CampusListView.vue'),
        meta: { permission: 'campus:read' },
      },

      // ── User Management ───────────────────────────────────────────────────
      {
        path: 'users',
        name: 'users',
        component: () => import('@/views/users/UsersView.vue'),
        meta: { permission: 'user:read' },
      },
      {
        path: 'users/:id',
        name: 'user-detail',
        component: () => import('@/views/users/UserDetailView.vue'),
        meta: { permission: 'user:read' },
      },

      // ── Role & Permission Management ──────────────────────────────────────
      {
        path: 'roles',
        name: 'roles',
        component: () => import('@/views/roles/RolesView.vue'),
        meta: { permission: 'role:read' },
      },
      {
        path: 'roles/:id',
        name: 'role-detail',
        component: () => import('@/views/roles/RoleDetailView.vue'),
        meta: { permission: 'role:read' },
      },
      {
        path: 'permissions',
        name: 'permissions',
        component: () => import('@/views/permissions/PermissionsView.vue'),
        meta: { permission: 'role:read' },
      },

      // ── Dynamic Menu Management ───────────────────────────────────────────
      {
        path: 'menus',
        name: 'menus',
        component: () => import('@/views/menus/MenusView.vue'),
        meta: { permission: 'menu:read' },
      },

      // ── Reference Data ────────────────────────────────────────────────────
      {
        path: 'reference-data',
        name: 'reference-data',
        component: () => import('@/views/refdata/RefDataView.vue'),
        meta: { permission: 'ref_data:read' },
      },
    ],
  },

  // ── Catch-all ─────────────────────────────────────────────────────────────
  { path: '/:pathMatch(.*)*', redirect: '/404' },
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior: () => ({ top: 0 }),
})

// ── Navigation guards ─────────────────────────────────────────────────────────
router.beforeEach(async (to) => {
  const auth = useAuthStore()

  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.meta.requiresGuest && auth.isAuthenticated) {
    return { path: '/' }
  }

  if (to.meta.permission && !auth.hasPermission(to.meta.permission as string)) {
    return { name: 'forbidden' }
  }
})

export default router

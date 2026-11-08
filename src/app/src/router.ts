import { createRouter, createWebHistory } from 'vue-router'
import AppShell from '@/layouts/AppShell.vue'
import { ensureSession, hasSession, onSessionExpired } from '@/auth/session'

declare module 'vue-router' {
  interface RouteMeta {
    title?: string
    /** Reachable without signing in. */
    public?: boolean
  }
}

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', name: 'login', component: () => import('@/views/LoginView.vue'), meta: { title: 'Sign in', public: true } },
    {
      path: '/',
      component: AppShell,
      children: [
        { path: '', name: 'dashboard', component: () => import('@/views/DashboardView.vue'), meta: { title: 'Dashboard' } },
        { path: 'pages', name: 'pages', component: () => import('@/views/PagesView.vue'), meta: { title: 'Pages' } },
        { path: 'websites', name: 'websites', component: () => import('@/views/WebsitesView.vue'), meta: { title: 'Websites' } },
        {
          path: 'websites/:id',
          name: 'website',
          component: () => import('@/views/WebsitesView.vue'),
          meta: { title: 'Websites' },
        },
        // Sections that are designed but not built yet share one placeholder.
        ...(
          [
            ['studio', 'Design Studio'],
            ['templates', 'Templates'],
            ['blocks', 'Blocks'],
            ['media', 'Media'],
            ['styles', 'Global Styles'],
            ['settings', 'Settings'],
            ['plugins', 'Plugins'],
            ['users', 'Users'],
          ] as const
        ).map(([path, title]) => ({
          path,
          name: path,
          component: () => import('@/views/ComingSoonView.vue'),
          meta: { title },
        })),
        { path: ':pathMatch(.*)*', name: 'not-found', component: () => import('@/views/NotFoundView.vue'), meta: { title: 'Not found' } },
      ],
    },
  ],
  scrollBehavior: () => ({ top: 0 }),
})

// Every route needs a session unless it's marked public; signed-in users skip the login page.
router.beforeEach(async (to) => {
  if (to.meta.public) {
    return to.name === 'login' && hasSession() && (await ensureSession()) ? { path: '/' } : true
  }
  if (await ensureSession()) return true
  return { name: 'login', query: to.fullPath === '/' ? {} : { redirect: to.fullPath } }
})

// The API rejected the session mid-use (revoked, or expired while idle): back to the login page.
onSessionExpired(() => {
  const current = router.currentRoute.value
  if (!current.meta.public) {
    router.replace({ name: 'login', query: { redirect: current.fullPath } })
  }
})

router.afterEach((to) => {
  document.title = to.meta.title ? `${to.meta.title} · Forma` : 'Forma'
})

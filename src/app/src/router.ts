import { createRouter, createWebHistory } from 'vue-router'
import AppShell from '@/layouts/AppShell.vue'

declare module 'vue-router' {
  interface RouteMeta {
    title?: string
  }
}

export const router = createRouter({
  history: createWebHistory(),
  routes: [
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
        {
          path: 'studio/:pageId?',
          name: 'studio',
          component: () => import('@/views/StudioView.vue'),
          meta: { title: 'Design Studio' },
        },
        // Sections that are designed but not built yet share one placeholder.
        ...(
          [
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

router.afterEach((to) => {
  document.title = to.meta.title ? `${to.meta.title} · Forma` : 'Forma'
})

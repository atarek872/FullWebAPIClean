import { createRouter, createWebHistory } from 'vue-router';
import { tokenManager } from '@/services/tokenManager';

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/auth/LoginView.vue'),
      meta: { guestOnly: true }
    },
    {
      path: '/',
      component: () => import('@/views/ShellView.vue'),
      meta: { requiresAuth: true },
      children: [
        { path: '', redirect: '/dashboard' },
        { path: 'dashboard', name: 'dashboard', component: () => import('@/views/dashboard/DashboardView.vue') },
        { path: 'products', name: 'products', component: () => import('@/views/ecommerce/ProductsView.vue') }
      ]
    }
  ]
});

router.beforeEach((to) => {
  const isAuthenticated = Boolean(tokenManager.getSession()?.accessToken);

  if (to.meta.requiresAuth && !isAuthenticated) {
    return { name: 'login' };
  }

  if (to.meta.guestOnly && isAuthenticated) {
    return { name: 'dashboard' };
  }

  return true;
});

export default router;

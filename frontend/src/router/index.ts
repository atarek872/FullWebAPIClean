import { createRouter, createWebHistory } from 'vue-router';
import { tokenManager } from '@/services/tokenManager';
import { adminModules } from '@/modules/adminModules';

const hasPermission = (requiredPermission?: string) => {
  if (!requiredPermission) {
    return true;
  }

  const permissions = tokenManager.getPermissions();
  return permissions.includes('full_access') || permissions.includes(requiredPermission);
};

const adminRoutes = adminModules.flatMap((module) => [
  {
    path: module.routeBase.replace('/admin/', ''),
    name: `${module.key}-list`,
    component: () => import('@/views/admin/CrudListView.vue'),
    meta: { requiresAuth: true, moduleKey: module.key, permission: module.permission }
  },
  {
    path: `${module.routeBase.replace('/admin/', '')}/create`,
    name: `${module.key}-create`,
    component: () => import('@/views/admin/CrudFormView.vue'),
    meta: { requiresAuth: true, moduleKey: module.key, permission: module.permission }
  },
  {
    path: `${module.routeBase.replace('/admin/', '')}/:id/edit`,
    name: `${module.key}-edit`,
    component: () => import('@/views/admin/CrudFormView.vue'),
    meta: { requiresAuth: true, moduleKey: module.key, permission: module.permission }
  }
]);

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/auth/LoginView.vue'),
      meta: { guestOnly: true }
    },
    { path: '/401', name: 'error-401', component: () => import('@/views/errors/Error401View.vue') },
    { path: '/403', name: 'error-403', component: () => import('@/views/errors/Error403View.vue') },
    { path: '/404', name: 'error-404', component: () => import('@/views/errors/Error404View.vue') },
    { path: '/500', name: 'error-500', component: () => import('@/views/errors/Error500View.vue') },
    {
      path: '/',
      component: () => import('@/views/ShellView.vue'),
      meta: { requiresAuth: true },
      children: [
        { path: '', redirect: '/dashboard' },
        { path: 'dashboard', name: 'dashboard', component: () => import('@/views/dashboard/DashboardView.vue') },
        ...adminRoutes
      ]
    },
    { path: '/:pathMatch(.*)*', redirect: '/404' }
  ]
});

router.beforeEach((to) => {
  const isAuthenticated = Boolean(tokenManager.getSession()?.accessToken);

  if (to.meta.requiresAuth && !isAuthenticated) {
    return { name: 'error-401' };
  }

  if (to.meta.guestOnly && isAuthenticated) {
    return { name: 'dashboard' };
  }

  if (to.meta.requiresAuth && !hasPermission(to.meta.permission as string | undefined)) {
    return { name: 'error-403' };
  }

  return true;
});

export default router;

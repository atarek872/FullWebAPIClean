import { createRouter, createWebHistory } from 'vue-router';
import { tokenManager } from '@/services/tokenManager';
import { adminModules } from '@/modules/adminModules';

const modulePermissions: Record<string, string | undefined> = {
  users: 'users.view',
  roles: 'roles.manage'
};

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
    meta: { requiresAuth: true, moduleKey: module.key, permission: modulePermissions[module.key] }
  },
  {
    path: `${module.routeBase.replace('/admin/', '')}/create`,
    name: `${module.key}-create`,
    component: () => import('@/views/admin/CrudFormView.vue'),
    meta: { requiresAuth: true, moduleKey: module.key, permission: modulePermissions[module.key] }
  },
  {
    path: `${module.routeBase.replace('/admin/', '')}/:id/edit`,
    name: `${module.key}-edit`,
    component: () => import('@/views/admin/CrudFormView.vue'),
    meta: { requiresAuth: true, moduleKey: module.key, permission: modulePermissions[module.key] }
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
    {
      path: '/',
      component: () => import('@/views/ShellView.vue'),
      meta: { requiresAuth: true },
      children: [
        { path: '', redirect: '/dashboard' },
        { path: 'dashboard', name: 'dashboard', component: () => import('@/views/dashboard/DashboardView.vue') },
        ...adminRoutes
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

  if (to.meta.requiresAuth && !hasPermission(to.meta.permission as string | undefined)) {
    return { name: 'dashboard' };
  }

  return true;
});

export default router;

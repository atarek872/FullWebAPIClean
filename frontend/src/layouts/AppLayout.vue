<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import { useTenantStore } from '@/stores/tenant';
import { adminModules } from '@/modules/adminModules';
import { TenantsService } from '@/services/api/TenantsService';
import { tokenManager } from '@/services/tokenManager';

interface SidebarItem {
  label: string;
  to: string;
  permission?: string;
}

const modulePermissions: Record<string, string | undefined> = {
  users: 'users.view',
  roles: 'roles.manage'
};

const router = useRouter();
const authStore = useAuthStore();
const tenantStore = useTenantStore();

const profileOpen = ref(false);
const notificationsOpen = ref(false);
const darkMode = ref(localStorage.getItem('fw-theme') === 'dark');
const tenantOptions = ref<Array<{ tenantId: string; name: string }>>([]);

const sidebarItems = computed<SidebarItem[]>(() => {
  const baseItems: SidebarItem[] = [{ label: 'Dashboard', to: '/dashboard' }];

  const moduleItems = adminModules.map((module) => ({
    label: module.title,
    to: module.routeBase.replace('/admin', ''),
    permission: modulePermissions[module.key]
  }));

  const permissions = tokenManager.getPermissions();

  return [...baseItems, ...moduleItems].filter((item) => {
    if (!item.permission) {
      return true;
    }

    return permissions.includes('full_access') || permissions.includes(item.permission);
  });
});

const userInitials = computed(() => {
  if (!authStore.user) {
    return '?';
  }

  return `${authStore.user.firstName[0] ?? ''}${authStore.user.lastName[0] ?? ''}`.toUpperCase();
});

const toggleDarkMode = () => {
  darkMode.value = !darkMode.value;
  document.documentElement.setAttribute('data-theme', darkMode.value ? 'dark' : 'light');
  localStorage.setItem('fw-theme', darkMode.value ? 'dark' : 'light');
};

const onLogout = async () => {
  await authStore.logout();
  await router.push({ name: 'login' });
};

const onSelectTenant = (event: Event) => {
  const value = (event.target as HTMLSelectElement).value;
  tenantStore.setTenantId(value);
};

onMounted(async () => {
  document.documentElement.setAttribute('data-theme', darkMode.value ? 'dark' : 'light');

  try {
    const { data } = await TenantsService.getAll();
    tenantOptions.value = data.map((tenant) => ({ tenantId: tenant.tenantId, name: tenant.name }));
  } catch {
    tenantOptions.value = tenantStore.tenantId
      ? [{ tenantId: tenantStore.tenantId, name: tenantStore.tenantId }]
      : [];
  }
});
</script>

<template>
  <div class="dashboard-shell">
    <aside class="sidebar card">
      <h2>FullWebAPI</h2>
      <nav>
        <RouterLink
          v-for="item in sidebarItems"
          :key="item.to"
          :to="item.to"
          class="sidebar-link"
          active-class="is-active"
        >
          {{ item.label }}
        </RouterLink>
      </nav>
    </aside>

    <section class="workspace">
      <header class="topbar card">
        <div class="topbar-left">
          <label class="sr-only" for="tenant-selector">Tenant</label>
          <select id="tenant-selector" class="input" :value="tenantStore.tenantId" @change="onSelectTenant">
            <option value="">Select tenant</option>
            <option v-for="tenant in tenantOptions" :key="tenant.tenantId" :value="tenant.tenantId">
              {{ tenant.name }}
            </option>
          </select>
        </div>

        <div class="topbar-actions">
          <button class="btn" @click="toggleDarkMode">{{ darkMode ? '☀️ Light' : '🌙 Dark' }}</button>

          <div class="menu-wrapper">
            <button class="btn" @click="notificationsOpen = !notificationsOpen">🔔 Notifications</button>
            <div v-if="notificationsOpen" class="dropdown card">
              <p>No new notifications</p>
            </div>
          </div>

          <div class="menu-wrapper">
            <button class="btn" @click="profileOpen = !profileOpen">{{ userInitials }} Profile</button>
            <div v-if="profileOpen" class="dropdown card">
              <p>{{ authStore.user?.email ?? 'Anonymous' }}</p>
              <button class="btn btn-danger" @click="onLogout">Logout</button>
            </div>
          </div>
        </div>
      </header>

      <main class="content">
        <slot />
      </main>
    </section>
  </div>
</template>

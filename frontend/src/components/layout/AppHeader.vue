<script setup lang="ts">
import { useAuthStore } from '@/stores/auth';
import { useRouter } from 'vue-router';
import { adminModules } from '@/modules/adminModules';

const authStore = useAuthStore();
const router = useRouter();

const onLogout = async () => {
  await authStore.logout();
  router.push({ name: 'login' });
};
</script>

<template>
  <header style="background:#0f172a;color:white;">
    <div class="container" style="display:flex;justify-content:space-between;align-items:center;gap:1rem;">
      <strong>FullWebAPI Console</strong>
      <nav style="display:flex;gap:0.75rem;align-items:center;flex-wrap:wrap;justify-content:flex-end;">
        <RouterLink to="/dashboard">Dashboard</RouterLink>
        <RouterLink v-for="module in adminModules" :key="module.key" :to="module.routeBase.replace('/admin', '')">
          {{ module.title }}
        </RouterLink>
        <button class="btn" @click="onLogout">Logout</button>
      </nav>
    </div>
  </header>
</template>

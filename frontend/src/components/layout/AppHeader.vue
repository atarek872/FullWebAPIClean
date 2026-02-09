<script setup lang="ts">
import { useAuthStore } from '@/stores/auth';
import { useRouter } from 'vue-router';

const authStore = useAuthStore();
const router = useRouter();

const onLogout = async () => {
  await authStore.logout();
  router.push({ name: 'login' });
};
</script>

<template>
  <header style="background:#0f172a;color:white;">
    <div class="container" style="display:flex;justify-content:space-between;align-items:center;">
      <strong>FullWebAPI Console</strong>
      <nav style="display:flex;gap:1rem;align-items:center;">
        <RouterLink to="/dashboard">Dashboard</RouterLink>
        <RouterLink to="/products">Products</RouterLink>
        <button class="btn" @click="onLogout">Logout</button>
      </nav>
    </div>
  </header>
</template>

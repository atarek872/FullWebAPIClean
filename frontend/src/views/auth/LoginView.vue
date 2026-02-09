<script setup lang="ts">
import { reactive } from 'vue';
import { useRouter } from 'vue-router';
import AuthLayout from '@/layouts/AuthLayout.vue';
import { useAuthStore } from '@/stores/auth';
import { useTenantStore } from '@/stores/tenant';

const authStore = useAuthStore();
const tenantStore = useTenantStore();
const router = useRouter();

const form = reactive({
  email: '',
  password: '',
  tenantId: tenantStore.tenantId
});

const submit = async () => {
  await authStore.login(form);
  tenantStore.setTenantId(form.tenantId);
  router.push({ name: 'dashboard' });
};
</script>

<template>
  <AuthLayout>
    <div class="card">
      <h1 style="margin-top:0">Sign in</h1>
      <p>Use your tenant and credentials from the ASP.NET Core API.</p>
      <form @submit.prevent="submit" style="display:grid;gap:0.75rem">
        <input v-model="form.tenantId" class="input" type="text" placeholder="Tenant GUID" required />
        <input v-model="form.email" class="input" type="email" placeholder="Email" required />
        <input v-model="form.password" class="input" type="password" placeholder="Password" required />
        <button class="btn btn-primary" :disabled="authStore.loading">{{ authStore.loading ? 'Signing in...' : 'Sign in' }}</button>
      </form>
    </div>
  </AuthLayout>
</template>

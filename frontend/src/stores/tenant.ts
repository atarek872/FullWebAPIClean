import { defineStore } from 'pinia';
import { tokenManager } from '@/services/tokenManager';

export const useTenantStore = defineStore('tenant', {
  state: () => ({
    tenantId: tokenManager.getTenantId() ?? ''
  }),
  actions: {
    setTenantId(tenantId: string) {
      this.tenantId = tenantId;
      tokenManager.setTenantId(tenantId);
    }
  }
});

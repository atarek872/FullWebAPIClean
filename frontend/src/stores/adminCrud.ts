import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import { adminModulesMap, type AdminModuleKey } from '@/modules/adminModules';
import { UsersService, RolesService, TenantsService, ProductsService, OrdersService, AuthService } from '@/services/api';

export type CrudRecord = Record<string, unknown> & { id: string };

const byModule = ref<Record<AdminModuleKey, CrudRecord[]>>({
  users: [],
  auth: [],
  roles: [],
  tenants: [],
  products: [],
  orders: [],
  settings: [],
  logs: [],
  notifications: [],
  'audit-trail': [],
  'multi-tenant-selector': []
});

const loading = ref(false);
const error = ref<string | null>(null);

const mapUsers = (item: {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  roles: string[];
}): CrudRecord => ({ ...item, roles: item.roles.join(', ') });

const mapTenant = (item: { tenantId: string; name: string; slug: string; plan: string; status: string }): CrudRecord => ({
  id: item.tenantId,
  tenantId: item.tenantId,
  name: item.name,
  slug: item.slug,
  planId: item.plan,
  status: item.status
});

const genId = () => crypto.randomUUID();

export const useAdminCrudStore = defineStore('adminCrud', () => {
  const getRecords = (moduleKey: AdminModuleKey) => computed(() => byModule.value[moduleKey]);

  const load = async (moduleKey: AdminModuleKey) => {
    loading.value = true;
    error.value = null;
    try {
      switch (moduleKey) {
        case 'users': {
          const { data } = await UsersService.getAll();
          byModule.value.users = data.map(mapUsers);
          break;
        }
        case 'roles': {
          const { data } = await RolesService.getAll();
          byModule.value.roles = data.map((role) => ({ ...role }));
          break;
        }
        case 'tenants': {
          const { data } = await TenantsService.getAll();
          byModule.value.tenants = data.map(mapTenant);
          break;
        }
        case 'products': {
          const { data } = await ProductsService.search('', 1, 100);
          byModule.value.products = data.items.map((item) => ({ ...item }));
          break;
        }
        default:
          byModule.value[moduleKey] = [];
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to load records';
      throw err;
    } finally {
      loading.value = false;
    }
  };

  const create = async (moduleKey: AdminModuleKey, payload: Record<string, unknown>) => {
    loading.value = true;
    error.value = null;
    try {
      switch (moduleKey) {
        case 'users': {
          const roles = String(payload.roles ?? '')
            .split(',')
            .map((it) => it.trim())
            .filter(Boolean);

          const { data } = await UsersService.create({
            email: String(payload.email ?? ''),
            password: String(payload.password ?? ''),
            firstName: String(payload.firstName ?? ''),
            lastName: String(payload.lastName ?? ''),
            isActive: Boolean(payload.isActive),
            roles
          });

          byModule.value.users.push({
            id: data.id,
            email: String(payload.email ?? ''),
            firstName: String(payload.firstName ?? ''),
            lastName: String(payload.lastName ?? ''),
            isActive: Boolean(payload.isActive),
            roles: roles.join(', ')
          });
          break;
        }
        case 'roles': {
          const { data } = await RolesService.create({
            name: String(payload.name ?? ''),
            description: String(payload.description ?? '')
          });

          byModule.value.roles.push({ ...payload, id: data.id });
          break;
        }
        case 'tenants': {
          const { data } = await TenantsService.create({
            name: String(payload.name ?? ''),
            slug: String(payload.slug ?? ''),
            subdomain: String(payload.subdomain ?? ''),
            planId: String(payload.planId ?? '')
          });

          byModule.value.tenants.push({ id: data.tenantId, ...payload });
          break;
        }
        case 'products': {
          await ProductsService.create({
            name: String(payload.name ?? ''),
            category: String(payload.category ?? ''),
            description: String(payload.description ?? ''),
            basePrice: Number(payload.basePrice ?? 0)
          });
          await load('products');
          break;
        }
        case 'orders': {
          await OrdersService.checkout({
            buyerName: String(payload.buyerName ?? ''),
            buyerEmail: String(payload.buyerEmail ?? ''),
            items: [{ productId: String(payload.productId ?? ''), quantity: Number(payload.quantity ?? 1) }]
          });
          byModule.value.orders.unshift({ id: genId(), ...payload });
          break;
        }
        case 'auth': {
          await AuthService.register({
            email: String(payload.email ?? ''),
            password: String(payload.password ?? ''),
            tenantId: String(payload.tenantId ?? ''),
            firstName: 'Portal',
            lastName: 'User'
          });
          byModule.value.auth.unshift({ id: genId(), ...payload });
          break;
        }
        case 'settings': {
          await TenantsService.updateSettings({
            tenantId: String(payload.tenantId ?? ''),
            settingsJson: String(payload.settingsJson ?? ''),
            apiRequestLimitPerDay: Number(payload.apiRequestLimitPerDay ?? 0) || undefined,
            storageLimitMb: Number(payload.storageLimitMb ?? 0) || undefined
          });
          byModule.value.settings.unshift({ id: genId(), ...payload });
          break;
        }
        default:
          throw new Error(`No create API is configured for module '${moduleKey}'.`);
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to create record';
      throw err;
    } finally {
      loading.value = false;
    }
  };

  const update = async (moduleKey: AdminModuleKey, id: string, payload: Record<string, unknown>) => {
    loading.value = true;
    error.value = null;
    try {
      switch (moduleKey) {
        case 'users': {
          await UsersService.update(id, {
            email: String(payload.email ?? ''),
            firstName: String(payload.firstName ?? ''),
            lastName: String(payload.lastName ?? ''),
            isActive: Boolean(payload.isActive)
          });

          const roles = String(payload.roles ?? '')
            .split(',')
            .map((it) => it.trim())
            .filter(Boolean);
          await UsersService.setRoles(id, { roles });
          break;
        }
        case 'roles': {
          await RolesService.update(id, {
            name: String(payload.name ?? ''),
            description: String(payload.description ?? ''),
            isActive: Boolean(payload.isActive)
          });
          break;
        }
        default:
          throw new Error(`No update API is configured for module '${moduleKey}'.`);
      }

      const index = byModule.value[moduleKey].findIndex((item) => item.id === id);
      if (index >= 0) {
        byModule.value[moduleKey][index] = { ...byModule.value[moduleKey][index], ...payload };
      }
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to update record';
      throw err;
    } finally {
      loading.value = false;
    }
  };

  const remove = async (moduleKey: AdminModuleKey, id: string) => {
    loading.value = true;
    error.value = null;
    try {
      switch (moduleKey) {
        case 'users':
          await UsersService.remove(id);
          break;
        case 'roles':
          await RolesService.remove(id);
          break;
        default:
          throw new Error(`No delete API is configured for module '${moduleKey}'.`);
      }

      byModule.value[moduleKey] = byModule.value[moduleKey].filter((item) => item.id !== id);
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to delete record';
      throw err;
    } finally {
      loading.value = false;
    }
  };

  const getById = (moduleKey: AdminModuleKey, id: string) => byModule.value[moduleKey].find((item) => item.id === id);

  const buildInitialForm = (moduleKey: AdminModuleKey) => {
    const fields = adminModulesMap[moduleKey].fields;
    const result: Record<string, unknown> = {};
    fields.forEach((field) => {
      result[field.key] = field.type === 'checkbox' ? false : '';
    });
    return result;
  };

  return {
    byModule,
    loading,
    error,
    getRecords,
    load,
    create,
    update,
    remove,
    getById,
    buildInitialForm
  };
});

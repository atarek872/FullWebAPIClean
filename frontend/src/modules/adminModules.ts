export type AdminModuleKey =
  | 'users'
  | 'auth'
  | 'roles'
  | 'tenants'
  | 'products'
  | 'orders'
  | 'settings'
  | 'logs'
  | 'notifications'
  | 'audit-trail'
  | 'multi-tenant-selector';

export interface ModuleField {
  key: string;
  label: string;
  required?: boolean;
  type?: 'text' | 'email' | 'password' | 'number' | 'textarea' | 'checkbox';
}

export interface AdminModuleDefinition {
  key: AdminModuleKey;
  title: string;
  routeBase: string;
  fields: ModuleField[];
}

export const adminModules: AdminModuleDefinition[] = [
  {
    key: 'users',
    title: 'Users',
    routeBase: '/admin/users',
    fields: [
      { key: 'email', label: 'Email', required: true, type: 'email' },
      { key: 'password', label: 'Password', required: true, type: 'password' },
      { key: 'firstName', label: 'First Name', required: true },
      { key: 'lastName', label: 'Last Name', required: true },
      { key: 'isActive', label: 'Active', type: 'checkbox' },
      { key: 'roles', label: 'Roles (comma separated)' }
    ]
  },
  {
    key: 'auth',
    title: 'Auth',
    routeBase: '/admin/auth',
    fields: [
      { key: 'email', label: 'Email', required: true, type: 'email' },
      { key: 'password', label: 'Password', required: true, type: 'password' },
      { key: 'tenantId', label: 'Tenant ID', required: true }
    ]
  },
  {
    key: 'roles',
    title: 'Roles',
    routeBase: '/admin/roles',
    fields: [
      { key: 'name', label: 'Name', required: true },
      { key: 'description', label: 'Description', type: 'textarea' },
      { key: 'isActive', label: 'Active', type: 'checkbox' }
    ]
  },
  {
    key: 'tenants',
    title: 'Tenants',
    routeBase: '/admin/tenants',
    fields: [
      { key: 'name', label: 'Name', required: true },
      { key: 'slug', label: 'Slug', required: true },
      { key: 'subdomain', label: 'Subdomain', required: true },
      { key: 'planId', label: 'Plan ID', required: true }
    ]
  },
  {
    key: 'products',
    title: 'Products',
    routeBase: '/admin/products',
    fields: [
      { key: 'name', label: 'Name', required: true },
      { key: 'category', label: 'Category', required: true },
      { key: 'basePrice', label: 'Base Price', required: true, type: 'number' },
      { key: 'description', label: 'Description', type: 'textarea' }
    ]
  },
  {
    key: 'orders',
    title: 'Orders',
    routeBase: '/admin/orders',
    fields: [
      { key: 'buyerName', label: 'Buyer Name', required: true },
      { key: 'buyerEmail', label: 'Buyer Email', required: true, type: 'email' },
      { key: 'productId', label: 'Product ID', required: true },
      { key: 'quantity', label: 'Quantity', required: true, type: 'number' }
    ]
  },
  {
    key: 'settings',
    title: 'Settings',
    routeBase: '/admin/settings',
    fields: [
      { key: 'name', label: 'Setting Name', required: true },
      { key: 'value', label: 'Value', required: true, type: 'textarea' }
    ]
  },
  {
    key: 'logs',
    title: 'Logs',
    routeBase: '/admin/logs',
    fields: [
      { key: 'level', label: 'Level', required: true },
      { key: 'message', label: 'Message', required: true, type: 'textarea' },
      { key: 'source', label: 'Source', required: true }
    ]
  },
  {
    key: 'notifications',
    title: 'Notifications',
    routeBase: '/admin/notifications',
    fields: [
      { key: 'title', label: 'Title', required: true },
      { key: 'message', label: 'Message', required: true, type: 'textarea' },
      { key: 'channel', label: 'Channel', required: true }
    ]
  },
  {
    key: 'audit-trail',
    title: 'Audit Trail',
    routeBase: '/admin/audit-trail',
    fields: [
      { key: 'action', label: 'Action', required: true },
      { key: 'entity', label: 'Entity', required: true },
      { key: 'actor', label: 'Actor', required: true }
    ]
  },
  {
    key: 'multi-tenant-selector',
    title: 'Multi-tenant Selector',
    routeBase: '/admin/multi-tenant-selector',
    fields: [
      { key: 'tenantId', label: 'Tenant ID', required: true },
      { key: 'tenantName', label: 'Tenant Name', required: true },
      { key: 'isDefault', label: 'Default Tenant', type: 'checkbox' }
    ]
  }
];

export const adminModulesMap = Object.fromEntries(adminModules.map((module) => [module.key, module])) as Record<AdminModuleKey, AdminModuleDefinition>;

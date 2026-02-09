export type AdminModuleKey =
  | 'users'
  | 'roles'
  | 'tenants'
  | 'products'
  | 'orders'
  | 'auth-registration'
  | 'seller-profile';

export interface ModuleField {
  key: string;
  label: string;
  required?: boolean;
  type?: 'text' | 'email' | 'password' | 'number' | 'textarea' | 'checkbox';
}

export interface ModuleCapabilities {
  list?: boolean;
  create?: boolean;
  update?: boolean;
  remove?: boolean;
}

export interface AdminModuleDefinition {
  key: AdminModuleKey;
  title: string;
  routeBase: string;
  fields: ModuleField[];
  capabilities: ModuleCapabilities;
  permission?: string;
}

export const adminModules: AdminModuleDefinition[] = [
  {
    key: 'users',
    title: 'Users',
    routeBase: '/admin/users',
    permission: 'users.view',
    capabilities: { list: true, create: true, update: true, remove: true },
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
    key: 'roles',
    title: 'Roles',
    routeBase: '/admin/roles',
    permission: 'roles.manage',
    capabilities: { list: true, create: true, update: true, remove: true },
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
    permission: 'full_access',
    capabilities: { list: true, create: true },
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
    capabilities: { list: true, create: true },
    fields: [
      { key: 'name', label: 'Name', required: true },
      { key: 'category', label: 'Category', required: true },
      { key: 'basePrice', label: 'Base Price', required: true, type: 'number' },
      { key: 'description', label: 'Description', type: 'textarea' }
    ]
  },
  {
    key: 'orders',
    title: 'Checkout Orders',
    routeBase: '/admin/orders',
    capabilities: { create: true },
    fields: [
      { key: 'buyerName', label: 'Buyer Name', required: true },
      { key: 'buyerEmail', label: 'Buyer Email', required: true, type: 'email' },
      { key: 'productId', label: 'Product ID', required: true },
      { key: 'quantity', label: 'Quantity', required: true, type: 'number' }
    ]
  },
  {
    key: 'auth-registration',
    title: 'Register User',
    routeBase: '/admin/auth-registration',
    capabilities: { create: true },
    fields: [
      { key: 'email', label: 'Email', required: true, type: 'email' },
      { key: 'password', label: 'Password', required: true, type: 'password' },
      { key: 'firstName', label: 'First Name', required: true },
      { key: 'lastName', label: 'Last Name', required: true },
      { key: 'tenantId', label: 'Tenant ID', required: true },
      { key: 'roles', label: 'Roles (comma separated)' }
    ]
  },
  {
    key: 'seller-profile',
    title: 'Seller Profile',
    routeBase: '/admin/seller-profile',
    capabilities: { create: true },
    fields: [
      { key: 'storeName', label: 'Store Name', required: true },
      { key: 'storeDescription', label: 'Description', type: 'textarea' },
      { key: 'slug', label: 'Slug' },
      { key: 'isPublished', label: 'Published', type: 'checkbox' }
    ]
  }
];

export const adminModulesMap = Object.fromEntries(adminModules.map((module) => [module.key, module])) as Record<AdminModuleKey, AdminModuleDefinition>;

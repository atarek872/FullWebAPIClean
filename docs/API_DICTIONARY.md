# FullWebAPI — Complete API Dictionary (Phase 1)

> Scope note: analysis was generated from the local backend repository content in this workspace.
>
> ملاحظة: التحليل اتعمل من نسخة الـ Backend الموجودة محليًا داخل الـ workspace.

This dictionary is generated from all API controllers, endpoint attributes, in-controller DTOs, tenant/auth middleware, and authorization policy setup.

## Global Cross-Cutting Rules

- **Tenant resolution is required for all API endpoints** unless route is `/`, `/swagger*`, or `/health`.
  - Resolution order: `X-Tenant-ID` header (GUID) first, then subdomain fallback.
  - Failure modes: tenant not found -> `400`; tenant inactive/suspended -> `403`.
- **Plan limit enforcement** runs before controllers for resolved tenants:
  - When daily `api_requests` reaches tenant limit -> `429` with `{"error":"Plan request limit reached"}`.
- **JWT bearer auth** is configured globally; secured endpoints require valid token.
- **JWT tenant claim validation**: token must include valid `tenant_id` claim referencing an existing tenant.
- **Identity password rules** (important for frontend validation on registration/admin user creation):
  - min length `10`, at least `1` digit, lowercase, uppercase, non-alphanumeric, and `4` unique chars.

## Authorization Policies Used by Endpoints

- `Admin` => roles `Admin` or `TenantAdmin`
- `User` => roles `User` or `TenantUser`
- `ManageUsers` => `Admin` role OR claims `permissions.full_access` OR `roles.manage`
- `Seller` => required by seller/product endpoints
- Endpoint attributes also reference: `CanEditUsers`, `CanDeleteUsers`, `CanExportUsers` (used on specific user endpoints)

---

## Endpoint Dictionary (100% Coverage)

> Notes:
> - "Tenant required" below means request must resolve tenant via middleware (`X-Tenant-ID` or subdomain).
> - DTO field names use API JSON casing equivalent of C# property names.

### System / Infra

| Domain | Method | URL | Request DTO | Response DTO | Auth | Tenant | Validation / Domain rules |
|---|---|---|---|---|---|---|---|
| System | GET | `/` | None | Redirect to `/swagger` | No | No | N/A |
| System | GET | `/health` | None | Health check payload | No | No | N/A |

### Hello

| Domain | Method | URL | Request DTO | Response DTO | Auth | Tenant | Validation / Domain rules |
|---|---|---|---|---|---|---|---|
| Hello | GET | `/api/hello` | None | `{ message: string, timestamp: datetime }` | No | Yes | Standard tenant middleware checks |
| Hello | GET | `/api/hello/{name}` | Route: `name:string` | `{ message: string, timestamp: datetime }` | No | Yes | Standard tenant middleware checks |

### Auth (`/api/auth`)

| Domain | Method | URL | Request DTO (required *) | Response DTO | Auth | Tenant | Validation / Domain rules |
|---|---|---|---|---|---|---|---|
| Auth | POST | `/api/auth/register` | `RegisterRequest`: `email*` (email), `password*`, `firstName*`, `lastName*`, `tenantId*` (guid), `roles[]` optional | `200 { message }` | No | Yes | Tenant in body must exist and not deleted; if roles empty defaults to `User`; identity/user-role creation errors return `400` |
| Auth | POST | `/api/auth/login` | `LoginRequest`: `email*` (email), `password*`, `tenantId*` (guid) | `200 { accessToken, refreshToken, user:{id,email,firstName,lastName} }` | No | Yes | User must exist and be active; password sign-in must succeed, else `401 Invalid credentials` |
| Auth | POST | `/api/auth/refresh` | `RefreshTokenRequest`: `refreshToken*`, `tenantId*` | `200 { accessToken, refreshToken }` | No | Yes | Invalid/expired token -> `401` |
| Auth | POST | `/api/auth/revoke` | `RevokeTokenRequest`: `refreshToken*` | `200` or `404` | Yes (`[Authorize]`) | Yes | Revokes one refresh token |
| Auth | POST | `/api/auth/revoke-all` | None | `200` | Yes (`[Authorize]`) | Yes | Parses current user id from token; invalid id -> `401` |

### Ecommerce (`/api/ecommerce`)

| Domain | Method | URL | Request DTO (required *) | Response DTO | Auth | Tenant | Validation / Domain rules |
|---|---|---|---|---|---|---|---|
| Ecommerce/Seller | POST | `/api/ecommerce/seller/profile` | `UpsertSellerProfileRequest`: `storeName*` max200, `storeDescription?` max2000, `slug?` max300, `isPublished` | `200 { id, storeName, slug, isPublished }` | Yes (`Seller` policy) | Yes | Uses authenticated user as seller owner; upsert behavior |
| Ecommerce/Product | POST | `/api/ecommerce/products` | `CreateProductRequest`: `name*` max200, `description?` max5000, `category*` max100, `basePrice` range 0.01..100000000, `discountPercentage?` range 0..90, `images[]`, `customFields[]`, `promoCodes[]`; nested validations apply | `200 { id, name, basePrice, images, customFields }` | Yes (`Seller`) | Yes | Seller profile must exist and be published; promo code stored uppercase |
| Ecommerce/Search | GET | `/api/ecommerce/products/search?query=&page=&pageSize=` | Query: `query?`, `page` default1 min1, `pageSize` default20 clamped1..100 | `200 { total, page, pageSize, items[] }` where item has `id,name,description,category,basePrice,discountPercentage,seller,thumbnail,searchScore` | No | Yes | Full-text-like scoring over product/store text; only active products with published sellers |
| Ecommerce/Checkout | POST | `/api/ecommerce/checkout` | `CheckoutRequest`: `buyerName*` max200, `buyerEmail*` email max300, `items[]`; `CheckoutItemRequest`: `productId*`, `quantity` range1..1000, `promoCode?` max50, `customFieldValues[]`; `CheckoutCustomFieldValueRequest`: `fieldKey*` max100, `value*` max2000 | `200 { id, subtotal, discountAmount, total, status }` | No | Yes | At least 1 item; all products active/exist; discount cap=90%; promo must be active+date-valid; custom field keys must be valid; required custom fields must be present |

### Identity Administration (`/api/identity-admin`) 

Controller-level auth: `[Authorize(Policy="ManageUsers")]`.

| Domain | Method | URL | Request DTO (required *) | Response DTO | Auth | Tenant | Validation / Domain rules |
|---|---|---|---|---|---|---|---|
| Identity/Permissions | GET | `/api/identity-admin/permissions/catalog` | None | `200 { permissions:string[], groups:{name,permissions[]}[] }` | Yes (`ManageUsers`) | Yes | Built from `PermissionConstants` |
| Identity/Users | GET | `/api/identity-admin/users` | None | `200 UserDetailsResponse[]` (`id,email,firstName,lastName,isActive,roles[]`) | Yes (`ManageUsers`) | Yes | Returns non-deleted users |
| Identity/Users | GET | `/api/identity-admin/users/{userId}` | Route `userId:guid` | `200 UserDetailsResponse` | Yes (`ManageUsers`) | Yes | `404` if missing/deleted |
| Identity/Users | POST | `/api/identity-admin/users` | `CreateManagedUserRequest`: `email*` email, `password*`, `firstName*`, `lastName*`, `isActive`, `roles[]` | `201 { message, id }` | Yes (`ManageUsers`) | Yes | Unknown roles -> `400`; identity errors -> `400` |
| Identity/Users | PUT | `/api/identity-admin/users/{userId}` | `UpdateUserRequest`: `email*` email, `firstName*`, `lastName*`, `isActive` | `200 { message }` | Yes (`ManageUsers` + `CanEditUsers`) | Yes | `404` if missing/deleted |
| Identity/Users | DELETE | `/api/identity-admin/users/{userId}` | None | `200 { message }` | Yes (`ManageUsers` + `CanDeleteUsers`) | Yes | Soft delete (`IsDeleted`, `IsActive=false`) |
| Identity/Users | PUT | `/api/identity-admin/users/{userId}/roles` | `SetUserRolesRequest`: `roles[]` | `200 { message }` | Yes (`ManageUsers`) | Yes | Unknown roles -> `400`; replaces all existing roles |
| Identity/Users | GET | `/api/identity-admin/users/export` | None | CSV file download | Yes (`ManageUsers` + `CanExportUsers`) | Yes | Exports non-deleted users with role list |
| Identity/Roles | GET | `/api/identity-admin/roles` | None | `200 RoleSummaryResponse[]` (`id,name,description,isActive`) | Yes (`ManageUsers`) | Yes | Excludes deleted roles |
| Identity/Roles | GET | `/api/identity-admin/roles/{roleId}` | Route `roleId:guid` | `200 RoleDetailsResponse` (`id,name,description,isActive,permissions[],groups[]`) | Yes (`ManageUsers`) | Yes | `404` if missing/deleted |
| Identity/Roles | POST | `/api/identity-admin/roles` | `CreateRoleRequest`: `name*`, `description` | `201 { message, id }` | Yes (`ManageUsers`) | Yes | Existing role name -> `409 Conflict` |
| Identity/Roles | PUT | `/api/identity-admin/roles/{roleId}` | `UpdateRoleRequest`: `name*`, `description`, `isActive` | `200 { message }` | Yes (`ManageUsers`) | Yes | `404` if missing/deleted |
| Identity/Roles | DELETE | `/api/identity-admin/roles/{roleId}` | None | `200 { message }` | Yes (`ManageUsers`) | Yes | Soft delete role |
| Identity/Roles | PUT | `/api/identity-admin/roles/{roleId}/permissions` | `SetRolePermissionsRequest`: `permissions[]` | `200 { message }` | Yes (`ManageUsers`) | Yes | Rejects permissions not in `PermissionConstants.All`; replaces existing permission claims |
| Identity/Roles | POST | `/api/identity-admin/roles/{roleId}/groups` | `AddGroupToRoleRequest`: `groupName*` | `200 { message }` | Yes (`ManageUsers`) | Yes | Group must exist in permission groups; adds group claim + implied permissions |
| Identity/Roles | DELETE | `/api/identity-admin/roles/{roleId}/permissions/{permission}` | Route: `permission:string` | `200 { message }` | Yes (`ManageUsers`) | Yes | `404` if permission claim not assigned |

### Tenant Administration (`/api/admin/tenants`)

Controller-level auth: `[Authorize(Policy="Admin")]`.

| Domain | Method | URL | Request DTO (required *) | Response DTO | Auth | Tenant | Validation / Domain rules |
|---|---|---|---|---|---|---|---|
| Tenant Admin | GET | `/api/admin/tenants` | None | `200 TenantSummaryDto[]` (`tenantId,name,slug,schema,plan,status`) | Yes (`Admin`) | Yes | Uses `GetTenantsQuery` |
| Tenant Admin | POST | `/api/admin/tenants` | `CreateTenantRequest`: `name`, `slug`, `subdomain`, `planId` | `200 { tenantId }` | Yes (`Admin`) | Yes | `planId` must exist (`FirstAsync`) |
| Tenant Admin | PUT | `/api/admin/tenants/settings` | `UpdateTenantSettingsRequest`: `tenantId`, `settingsJson`, `apiRequestLimitPerDay`, `storageLimitMb` | `204 NoContent` | Yes (`Admin`) | Yes | Tenant must exist (`FirstAsync`) |
| Tenant Admin | PUT | `/api/admin/tenants/assign-plan` | `AssignPlanRequest`: `tenantId`, `planId` | `204 NoContent` | Yes (`Admin`) | Yes | Tenant+plan must exist; also creates active `TenantSubscription` |
| Tenant Admin | PUT | `/api/admin/tenants/suspend` | `SuspendTenantRequest`: `tenantId`, `reason` | `204 NoContent` | Yes (`Admin`) | Yes | Marks tenant status as `Suspended`; saves reason into `SettingsJson` |
| Tenant Admin | POST | `/api/admin/tenants/{tenantId}/onboard` | Route `tenantId:guid`; body `OnboardingRequest(adminEmail, adminPassword)` | `200 OK` | Yes (`Admin`) | Yes | Invokes onboarding service to provision tenant admin user |

---

## DTO Index (Quick Reference)

- **Auth DTOs**: `RegisterRequest`, `LoginRequest`, `RefreshTokenRequest`, `RevokeTokenRequest`
- **Ecommerce DTOs**: `UpsertSellerProfileRequest`, `CreateProductRequest`, `CreateProductImageRequest`, `CreateProductCustomFieldRequest`, `CreatePromoCodeRequest`, `CheckoutRequest`, `CheckoutItemRequest`, `CheckoutCustomFieldValueRequest`
- **Identity DTOs**: `CreateManagedUserRequest`, `UpdateUserRequest`, `SetUserRolesRequest`, `CreateRoleRequest`, `UpdateRoleRequest`, `SetRolePermissionsRequest`, `AddGroupToRoleRequest`
- **Tenant DTOs**: `CreateTenantRequest`, `UpdateTenantSettingsRequest`, `AssignPlanRequest`, `SuspendTenantRequest`, `OnboardingRequest`

---

## Phase Control

Phase 1 is complete. Stopping here per your instruction. Waiting for your confirmation before generating frontend architecture/code.

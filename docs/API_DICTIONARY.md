# FullWebAPI API Dictionary

Generated from API controllers, middleware, application DTOs, and authorization setup.

## 1) Global Rules

- **Tenant is mandatory** for almost all endpoints through `TenantResolverMiddleware` (`X-Tenant-ID` header or subdomain). Exclusions: `/`, `/swagger*`, `/health`. Missing/invalid tenant -> `400`.
- **Tenant status gate**: inactive/suspended tenant -> `403`.
- **Per-plan request cap** via `PlanLimitMiddleware`: when daily `api_requests` reaches the tenant plan limit, request is blocked with `429`.
- **JWT bearer auth** is default; `[Authorize]` endpoints require a valid token and tenant claim validation.

## 2) Authentication / Authorization Flows

### Access + Refresh flow
1. `POST /api/auth/login` validates credentials and tenant membership.
2. API returns `{ accessToken, refreshToken, user }`.
3. Frontend sends bearer token on secured routes and `X-Tenant-ID` on every request.
4. On `401`, Axios interceptor calls `POST /api/auth/refresh`.
5. If refresh fails, local session is cleared and user is redirected to login/401 UX.

### Policies used by controllers
- `Admin`: required by tenant admin endpoints.
- `ManageUsers`: required by identity admin controller.
- `CanEditUsers`, `CanDeleteUsers`, `CanExportUsers`: extra policy restrictions on specific user endpoints.
- `Seller`: required by seller profile/product creation endpoints.

## 3) Multi-tenant Rules

- Tenant is resolved before controller execution.
- Tenant ID is propagated into tenant context and EF query filters/interceptors.
- Tenant-owned entities are automatically scoped by current tenant ID.
- Usage tracking is per tenant and metric key (`api_requests`).

## 4) Endpoint Dictionary

## System
- `GET /` -> redirect to `/swagger`.
- `GET /health` -> ASP.NET health response.

## Hello
- `GET /api/hello` -> `{ message, timestamp }`.
- `GET /api/hello/{name}` -> `{ message, timestamp }` personalized.

## Auth (`/api/auth`)

### `POST /register`
- **Request DTO** `RegisterRequest`
  - `email` required + email format
  - `password` required
  - `firstName` required
  - `lastName` required
  - `tenantId` required GUID
  - `roles` optional string[] (defaults to `User`)
- **Responses**
  - `200 { message }`
  - `400` invalid tenant / identity errors

### `POST /login`
- **Request DTO** `LoginRequest`
  - `email` required + email format
  - `password` required
  - `tenantId` required GUID
- **Responses**
  - `200 { accessToken, refreshToken, user }`
  - `401 "Invalid credentials"`

### `POST /refresh`
- **Request DTO** `RefreshTokenRequest`
  - `refreshToken` required
  - `tenantId` required GUID
- **Responses**
  - `200 { accessToken, refreshToken }`
  - `401 invalid/expired refresh token`

### `POST /revoke` (authorized)
- **Request DTO** `RevokeTokenRequest`
  - `refreshToken` required
- **Responses** `200` or `404`.

### `POST /revoke-all` (authorized)
- **Request DTO** none
- **Responses** `200`, or `401` when token user id is invalid.

## Ecommerce (`/api/ecommerce`)

### `POST /seller/profile` (Seller)
- **Request DTO** `UpsertSellerProfileRequest`
  - `storeName` required, max 200
  - `storeDescription` max 2000
  - `slug` max 300
  - `isPublished` bool
- **Response** `{ id, storeName, slug, isPublished }`

### `POST /products` (Seller)
- **Request DTO** `CreateProductRequest`
  - `name` required max 200
  - `description` max 5000
  - `category` required max 100
  - `basePrice` range `0.01..100000000`
  - `discountPercentage` range `0..90`
  - `images[]` `CreateProductImageRequest` (`imageUrl` required max 1000, `altText` max 250)
  - `customFields[]` `CreateProductCustomFieldRequest`
    - `key` required max 100
    - `label` required max 150
    - `inputType` required max 30
    - `placeholder` max 500
    - `isRequired` bool
    - `allowedOptions` optional string[]
  - `promoCodes[]` `CreatePromoCodeRequest`
    - `code` required max 50
    - `discountPercentage` range `0.01..90`
    - `startsAtUtc`, `endsAtUtc`, `isActive`
- **Response** `{ id, name, basePrice, images, customFields }`

### `GET /products/search`
- Query: `query`, `page` (min 1), `pageSize` (clamp 1..100)
- **Response** `{ total, page, pageSize, items[] }`

### `POST /checkout`
- **Request DTO** `CheckoutRequest`
  - `buyerName` required max 200
  - `buyerEmail` required email max 300
  - `items[]` required, at least 1 item
- `CheckoutItemRequest`
  - `productId` required GUID
  - `quantity` range `1..1000`
  - `promoCode` max 50
  - `customFieldValues[]`
- `CheckoutCustomFieldValueRequest`
  - `fieldKey` required max 100
  - `value` required max 2000
- **Business validations**
  - all product IDs must exist and be active
  - promo + product discount capped at 90%
  - custom field keys must be valid for the product
  - all required custom fields must be present
- **Response** `{ id, subtotal, discountAmount, total, status }`

## Identity Administration (`/api/identity-admin`, `ManageUsers`)

### Permissions catalog
- `GET /permissions/catalog` -> `{ permissions[], groups[] }`

### Users
- `GET /users` -> `UserDetailsResponse[]`
- `GET /users/{userId}` -> `UserDetailsResponse`
- `POST /users` with `CreateManagedUserRequest`
  - `email` required + email
  - `password` required
  - `firstName` required
  - `lastName` required
  - `isActive` bool
  - `roles[]`
- `PUT /users/{userId}` (`CanEditUsers`) with `UpdateUserRequest`
  - `email` required + email
  - `firstName` required
  - `lastName` required
  - `isActive` bool
- `DELETE /users/{userId}` (`CanDeleteUsers`)
- `PUT /users/{userId}/roles` with `SetUserRolesRequest { roles[] }`
- `GET /users/export` (`CanExportUsers`) -> CSV file

### Roles
- `GET /roles` -> `RoleSummaryResponse[]`
- `GET /roles/{roleId}` -> `RoleDetailsResponse`
- `POST /roles` with `CreateRoleRequest { name(required), description }`
- `PUT /roles/{roleId}` with `UpdateRoleRequest { name(required), description, isActive }`
- `DELETE /roles/{roleId}`
- `PUT /roles/{roleId}/permissions` with `SetRolePermissionsRequest { permissions[] }`
- `POST /roles/{roleId}/groups` with `AddGroupToRoleRequest { groupName(required) }`
- `DELETE /roles/{roleId}/permissions/{permission}`

## Tenant Administration (`/api/admin/tenants`, `Admin`)

- `GET /api/admin/tenants` -> `TenantSummaryDto[]`
- `POST /api/admin/tenants` with `CreateTenantRequest(name, slug, subdomain, planId)` -> `{ tenantId }`
- `PUT /api/admin/tenants/settings` with `UpdateTenantSettingsRequest(tenantId, settingsJson, apiRequestLimitPerDay, storageLimitMb)` -> `204`
- `PUT /api/admin/tenants/assign-plan` with `AssignPlanRequest(tenantId, planId)` -> `204`
- `PUT /api/admin/tenants/suspend` with `SuspendTenantRequest(tenantId, reason)` -> `204`
- `POST /api/admin/tenants/{tenantId}/onboard` with `OnboardingRequest(adminEmail, adminPassword)` -> `200`

## 5) Frontend Integration Notes

- Frontend service files map to all controller endpoint groups under `frontend/src/services/api`.
- Axios request interceptor injects `Authorization` and `X-Tenant-ID`.
- Axios response interceptor performs refresh-token flow automatically.
- Route guards enforce auth + permission checks and navigate to `401/403/404/500` views.

# API Dictionary

This dictionary is derived from controllers, DTOs, domain models, middleware, and auth/service configuration.

## Global API behavior

- **Tenant resolution is required for almost all routes**: request must provide either `X-Tenant-ID` header (GUID) or a resolvable subdomain, except `/`, `/swagger*`, and `/health`. Otherwise request is rejected with `400 { error: "Tenant not found." }`.
- **Tenant must be active**: inactive/suspended tenants are rejected with `403 { error: "Tenant is not active." }`.
- **Per-tenant plan usage limit**: each request increments `UsageRecords` metric `api_requests`; when daily usage reaches tenant `ApiRequestLimitPerDay`, API returns `429 { error: "Plan request limit reached" }`.
- **Global fixed-window limiter**: 100 requests/minute partitioned by `tenant_id` claim or remote IP.
- **JWT auth**: bearer token required for `[Authorize]` routes. Token validation additionally enforces valid `tenant_id` claim mapped to a non-deleted tenant.

## Auth and policy dictionary

### Authentication
- JWT bearer auth is configured as default authenticate/challenge scheme.
- JWT token must satisfy issuer, audience, signing key, and lifetime checks.
- Token must contain a valid `tenant_id` claim that exists in `Tenants` and is not soft-deleted.

### Access token claim content (TokenService)
- `sub`, `nameidentifier`, `name`, `email`, `tenant_id`, `tenant_role`, `tenant_permissions`, `jti`
- Role claims (`ClaimTypes.Role`) for all assigned Identity roles.
- Permission claims (`permission`) expanded from role claims.

### Authorization policies
- `Admin`: requires role `Admin` OR `TenantAdmin`.
- `User`: requires role `User` OR `TenantUser`.
- `ManageUsers`: true if user is role `Admin` OR has permission claim `full_access` OR `roles.manage`.
- **Referenced but not configured in the shown registration**: `Seller`, `CanEditUsers`, `CanDeleteUsers`, `CanExportUsers`.

## Route dictionary

## GET

### `GET /` 
- **Request DTO**: none.
- **Response DTO**: HTTP redirect to `/swagger`.
- **Auth**: none.
- **Rules/validations**: none.

### `GET /health`
- **Request DTO**: none.
- **Response DTO**: health-check response payload from ASP.NET health checks.
- **Auth**: none.
- **Rules/validations**: none.

### `GET /api/hello`
- **Request DTO**: none.
- **Response DTO**: `{ message: string, timestamp: datetime }` (anonymous object: `Message`, `Timestamp`).
- **Auth**: no `[Authorize]`.
- **Rules/validations**: tenant resolution middleware still applies.

### `GET /api/hello/{name}`
- **Request DTO**: route parameter `name: string`.
- **Response DTO**: `{ message: string, timestamp: datetime }` greeting name.
- **Auth**: no `[Authorize]`.
- **Rules/validations**: tenant resolution middleware still applies.

### `GET /api/ecommerce/products/search?query=&page=1&pageSize=20`
- **Request DTO**: query params: `query: string`, `page: int=1`, `pageSize: int=20`.
- **Response DTO**:
  ```json
  {
    "total": number,
    "page": number,
    "pageSize": number,
    "items": [
      {
        "id": "guid",
        "name": "string",
        "description": "string?",
        "category": "string",
        "basePrice": number,
        "discountPercentage": number?,
        "seller": "string",
        "thumbnail": "string?",
        "searchScore": number
      }
    ]
  }
  ```
- **Auth**: `[AllowAnonymous]`.
- **Rules/validations**:
  - `page = max(page,1)`
  - `pageSize = clamp(pageSize,1,100)`
  - only active products with published seller profiles.
  - in-memory scoring: number of matched terms from `name/description/category/storeName`.

### `GET /api/identity-admin/permissions/catalog`
- **Request DTO**: none.
- **Response DTO**:
  ```json
  {
    "permissions": ["..."],
    "groups": [{ "name": "string", "permissions": ["..."] }]
  }
  ```
- **Auth**: `[Authorize(Policy="ManageUsers")]` from controller.
- **Rules/validations**: permission and group lists are sorted.

### `GET /api/identity-admin/users`
- **Request DTO**: none.
- **Response DTO**: `UserDetailsResponse[]`
  - `{ id, email, firstName, lastName, isActive, roles[] }`
- **Auth**: `ManageUsers` policy.
- **Rules/validations**: excludes soft-deleted users.

### `GET /api/identity-admin/users/{userId}`
- **Request DTO**: route `userId: guid`.
- **Response DTO**: `UserDetailsResponse`.
- **Auth**: `ManageUsers` policy.
- **Rules/validations**: `404 "User not found"` when missing or soft-deleted.

### `GET /api/identity-admin/users/export`
- **Request DTO**: none.
- **Response DTO**: CSV file (`text/csv`): `Id,Email,FirstName,LastName,IsActive,Roles`.
- **Auth**: additionally requires `[Authorize(Policy="CanExportUsers")]`.
- **Rules/validations**: soft-deleted users excluded; role list pipe-separated.

### `GET /api/identity-admin/roles`
- **Request DTO**: none.
- **Response DTO**: `RoleSummaryResponse[]`
  - `{ id, name, description, isActive }`
- **Auth**: `ManageUsers` policy.
- **Rules/validations**: soft-deleted roles excluded.

### `GET /api/identity-admin/roles/{roleId}`
- **Request DTO**: route `roleId: guid`.
- **Response DTO**: `RoleDetailsResponse`
  - `{ id, name, description, isActive, permissions[], groups[] }`
- **Auth**: `ManageUsers` policy.
- **Rules/validations**: `404` if role missing or soft-deleted.

### `GET /api/admin/tenants`
- **Request DTO**: none.
- **Response DTO**: `TenantSummaryDto[]`
  - `{ tenantId, name, slug, schema, plan, status }`
- **Auth**: `[Authorize(Policy="Admin")]`.
- **Rules/validations**: direct DB projection with no paging/filtering.

## POST

### `POST /api/auth/register`
- **Request DTO**: `RegisterRequest`
  - `email` (required, email)
  - `password` (required)
  - `firstName` (required)
  - `lastName` (required)
  - `tenantId` (required guid)
  - `roles` (`string[]`, optional; defaults to `User` if empty)
- **Response DTO**:
  - `200`: `{ message: "User registered successfully" }`
  - `400`: invalid tenant or Identity errors.
- **Auth**: anonymous.
- **Rules/validations**:
  - tenant must exist and not be deleted.
  - creates `UserTenantMembership` with first role as `Role` and all roles CSV in `PermissionsCsv`.
  - identity password complexity/lockout rules apply globally.

### `POST /api/auth/login`
- **Request DTO**: `LoginRequest`
  - `email` (required, email)
  - `password` (required)
  - `tenantId` (required guid)
- **Response DTO**:
  - `200`: `{ accessToken, refreshToken, user: { id, email, firstName, lastName } }`
  - `401`: `"Invalid credentials"`
- **Auth**: anonymous.
- **Rules/validations**:
  - user must exist and be active.
  - password check uses sign-in manager with lockout on failure.
  - token generation requires user membership in requested tenant.

### `POST /api/auth/refresh`
- **Request DTO**: `RefreshTokenRequest`
  - `refreshToken` (required)
  - `tenantId` (required guid)
- **Response DTO**:
  - `200`: `{ accessToken, refreshToken }`
  - `401`: `"Invalid or expired refresh token"`
- **Auth**: anonymous.
- **Rules/validations**:
  - refresh token must match active user-stored token and non-expired timestamp.

### `POST /api/auth/revoke`
- **Request DTO**: `RevokeTokenRequest`
  - `refreshToken` (required)
- **Response DTO**: `200 OK` or `404 NotFound`.
- **Auth**: `[Authorize]`.
- **Rules/validations**: removes refresh token/expiry from Identity auth tokens.

### `POST /api/auth/revoke-all`
- **Request DTO**: none.
- **Response DTO**: `200 OK`.
- **Auth**: `[Authorize]`.
- **Rules/validations**: current user id must parse from access token, else `401 "Invalid access token"`.

### `POST /api/ecommerce/seller/profile`
- **Request DTO**: `UpsertSellerProfileRequest`
  - `storeName` (required, max 200)
  - `storeDescription` (max 2000)
  - `slug` (max 300)
  - `isPublished` (bool)
- **Response DTO**: `{ id, storeName, slug, isPublished }`
- **Auth**: `[Authorize(Policy="Seller")]`.
- **Rules/validations**:
  - user ID must be available in JWT.
  - upsert by current user.
  - relies on tenant-owned entity auto-assignment in save interceptor.

### `POST /api/ecommerce/products`
- **Request DTO**: `CreateProductRequest`
  - `name` (required, max 200)
  - `description` (max 5000)
  - `category` (required, max 100)
  - `basePrice` (range `0.01..100000000`)
  - `discountPercentage` (range `0..90`)
  - `images[]` (`CreateProductImageRequest`)
  - `customFields[]` (`CreateProductCustomFieldRequest`)
  - `promoCodes[]` (`CreatePromoCodeRequest`)
- **Response DTO**: `{ id, name, basePrice, images, customFields }`
- **Auth**: `[Authorize(Policy="Seller")]`.
- **Rules/validations**:
  - published seller profile must exist for current user; else 400.
  - promo code stored uppercase.

### `POST /api/ecommerce/checkout`
- **Request DTO**: `CheckoutRequest`
  - `buyerName` (required, max 200)
  - `buyerEmail` (required, email, max 300)
  - `items[]` (`CheckoutItemRequest`)
- **Response DTO**: `{ id, subtotal, discountAmount, total, status }`
- **Auth**: `[AllowAnonymous]`.
- **Rules/validations**:
  - at least one item required.
  - every `productId` must resolve to active product.
  - `quantity` range `1..1000`.
  - discount = `min(90, productDiscount + activePromoDiscount)`.
  - custom field checks per product:
    - submitted keys must all exist in product custom fields.
    - all required custom fields must be submitted.

### `POST /api/identity-admin/users`
- **Request DTO**: `CreateManagedUserRequest`
  - `email` (required, email)
  - `password` (required)
  - `firstName` (required)
  - `lastName` (required)
  - `isActive` (bool)
  - `roles[]`
- **Response DTO**:
  - `201`: `{ message: "User created successfully", id }`
  - errors via `400`.
- **Auth**: `ManageUsers` policy.
- **Rules/validations**:
  - unknown roles rejected.
  - identity password and uniqueness rules apply.

### `POST /api/identity-admin/roles`
- **Request DTO**: `CreateRoleRequest`
  - `name` (required)
  - `description`
- **Response DTO**:
  - `201`: `{ message: "Role created successfully", id }`
  - `409`: role exists.
- **Auth**: `ManageUsers` policy.
- **Rules/validations**: uniqueness checked via `RoleExistsAsync`.

### `POST /api/identity-admin/roles/{roleId}/groups`
- **Request DTO**: `AddGroupToRoleRequest`
  - `groupName` (required)
- **Response DTO**: `{ message: "Permission group added to role successfully" }`
- **Auth**: `ManageUsers` policy.
- **Rules/validations**:
  - role must exist and not be deleted.
  - `groupName` must exist in `PermissionConstants.Groups`.
  - adds both group claim and any missing permission claims in group.

### `POST /api/admin/tenants`
- **Request DTO**: `CreateTenantRequest`
  - `name`, `slug`, `subdomain`, `planId`
- **Response DTO**: `{ tenantId }`
- **Auth**: `Admin` policy.
- **Rules/validations**:
  - selected plan must exist (`FirstAsync` throw if missing).
  - tenant schema auto-generated as `{slug}_schema` lower-case.

### `POST /api/admin/tenants/{tenantId}/onboard`
- **Request DTO**: `OnboardingRequest`
  - `adminEmail`, `adminPassword`
- **Response DTO**: `200 OK`.
- **Auth**: `Admin` policy.
- **Rules/validations**: delegates to `ITenantOnboardingService.RunAsync`.

## PUT

### `PUT /api/identity-admin/users/{userId}`
- **Request DTO**: `UpdateUserRequest`
  - `email` (required, email)
  - `firstName` (required)
  - `lastName` (required)
  - `isActive`
- **Response DTO**: `{ message: "User updated successfully" }`
- **Auth**: `ManageUsers` + `CanEditUsers` policy.
- **Rules/validations**: user must exist and not be deleted.

### `PUT /api/identity-admin/users/{userId}/roles`
- **Request DTO**: `SetUserRolesRequest`
  - `roles[]`
- **Response DTO**: `{ message: "User roles updated successfully" }`
- **Auth**: `ManageUsers` policy.
- **Rules/validations**:
  - all roles must exist.
  - existing roles removed before assigning distinct new roles.

### `PUT /api/identity-admin/roles/{roleId}`
- **Request DTO**: `UpdateRoleRequest`
  - `name` (required)
  - `description`
  - `isActive`
- **Response DTO**: `{ message: "Role updated successfully" }`
- **Auth**: `ManageUsers` policy.
- **Rules/validations**: role must exist and not be deleted.

### `PUT /api/identity-admin/roles/{roleId}/permissions`
- **Request DTO**: `SetRolePermissionsRequest`
  - `permissions[]`
- **Response DTO**: `{ message: "Role permissions updated successfully" }`
- **Auth**: `ManageUsers` policy.
- **Rules/validations**:
  - requested permissions must all belong to `PermissionConstants.All`.
  - existing permission claims are removed then replaced with distinct requested values.

### `PUT /api/admin/tenants/settings`
- **Request DTO**: `UpdateTenantSettingsRequest`
  - `tenantId`, `settingsJson`, `apiRequestLimitPerDay`, `storageLimitMb`
- **Response DTO**: `204 NoContent`.
- **Auth**: `Admin` policy.
- **Rules/validations**: tenant must exist.

### `PUT /api/admin/tenants/assign-plan`
- **Request DTO**: `AssignPlanRequest`
  - `tenantId`, `planId`
- **Response DTO**: `204 NoContent`.
- **Auth**: `Admin` policy.
- **Rules/validations**:
  - tenant and plan must exist.
  - creates active `TenantSubscription` entry with local billing ID format `local_{tenant}_{plan}`.

### `PUT /api/admin/tenants/suspend`
- **Request DTO**: `SuspendTenantRequest`
  - `tenantId`, `reason`
- **Response DTO**: `204 NoContent`.
- **Auth**: `Admin` policy.
- **Rules/validations**:
  - tenant must exist.
  - tenant status set to `Suspended` and reason written to `SettingsJson`.

## DELETE

### `DELETE /api/identity-admin/users/{userId}`
- **Request DTO**: route `userId: guid`.
- **Response DTO**: `{ message: "User deleted successfully" }`
- **Auth**: `ManageUsers` + `CanDeleteUsers` policy.
- **Rules/validations**: soft delete (`IsDeleted=true`, `IsActive=false`).

### `DELETE /api/identity-admin/roles/{roleId}`
- **Request DTO**: route `roleId: guid`.
- **Response DTO**: `{ message: "Role deleted successfully" }`
- **Auth**: `ManageUsers` policy.
- **Rules/validations**: soft delete (`IsDeleted=true`, `IsActive=false`).

### `DELETE /api/identity-admin/roles/{roleId}/permissions/{permission}`
- **Request DTO**: route `roleId: guid`, `permission: string`.
- **Response DTO**: `{ message: "Permission removed successfully" }`.
- **Auth**: `ManageUsers` policy.
- **Rules/validations**:
  - role must exist and not be deleted.
  - permission claim must currently exist on role, else 404.

## Model notes used by API contracts

- **Soft delete model**: most entities inherit `BaseEntity` (`IsDeleted`, audit timestamps/users).
- **Tenant-owned entities** (`SellerProfile`, `Product`, `Order`, etc.) have `TenantId` auto-assigned on insert by `TenantAuditSaveChangesInterceptor`.
- **Global query filters**:
  - users/roles/tenants filter out `IsDeleted`.
  - ecommerce tables filter by `IsDeleted == false` and current `TenantId`.
- **Identity password rules**:
  - require digit, lowercase, uppercase, non-alphanumeric.
  - min length 10, min unique chars 4.
  - max failed attempts 5; lockout 10 minutes.

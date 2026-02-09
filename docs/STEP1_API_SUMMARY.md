# STEP 1 — Backend API Dictionary (Complete)

> Source analyzed from the local backend in this workspace (no GitHub URL was provided in the prompt).
>
> تم التحليل من نسخة الـ backend الموجودة محليًا لأن رابط GitHub غير متوفر في الرسالة.

## 1) Controllers & Endpoints (Full Coverage)

| Controller | Method | Endpoint | Auth/Policy | Tenant Required |
|---|---|---|---|---|
| Root | GET | `/` | Public | No |
| Health | GET | `/health` | Public | No |
| HelloController | GET | `/api/hello` | Public | Yes |
| HelloController | GET | `/api/hello/{name}` | Public | Yes |
| AuthController | POST | `/api/auth/register` | Public | Yes |
| AuthController | POST | `/api/auth/login` | Public | Yes |
| AuthController | POST | `/api/auth/refresh` | Public | Yes |
| AuthController | POST | `/api/auth/revoke` | `[Authorize]` | Yes |
| AuthController | POST | `/api/auth/revoke-all` | `[Authorize]` | Yes |
| EcommerceController | POST | `/api/ecommerce/seller/profile` | `Seller` | Yes |
| EcommerceController | POST | `/api/ecommerce/products` | `Seller` | Yes |
| EcommerceController | GET | `/api/ecommerce/products/search` | Public | Yes |
| EcommerceController | POST | `/api/ecommerce/checkout` | Public | Yes |
| IdentityAdminController | GET | `/api/identity-admin/permissions/catalog` | `ManageUsers` | Yes |
| IdentityAdminController | GET | `/api/identity-admin/users` | `ManageUsers` | Yes |
| IdentityAdminController | GET | `/api/identity-admin/users/{userId}` | `ManageUsers` | Yes |
| IdentityAdminController | POST | `/api/identity-admin/users` | `ManageUsers` | Yes |
| IdentityAdminController | PUT | `/api/identity-admin/users/{userId}` | `ManageUsers` + `CanEditUsers` | Yes |
| IdentityAdminController | DELETE | `/api/identity-admin/users/{userId}` | `ManageUsers` + `CanDeleteUsers` | Yes |
| IdentityAdminController | PUT | `/api/identity-admin/users/{userId}/roles` | `ManageUsers` | Yes |
| IdentityAdminController | GET | `/api/identity-admin/users/export` | `ManageUsers` + `CanExportUsers` | Yes |
| IdentityAdminController | GET | `/api/identity-admin/roles` | `ManageUsers` | Yes |
| IdentityAdminController | GET | `/api/identity-admin/roles/{roleId}` | `ManageUsers` | Yes |
| IdentityAdminController | POST | `/api/identity-admin/roles` | `ManageUsers` | Yes |
| IdentityAdminController | PUT | `/api/identity-admin/roles/{roleId}` | `ManageUsers` | Yes |
| IdentityAdminController | DELETE | `/api/identity-admin/roles/{roleId}` | `ManageUsers` | Yes |
| IdentityAdminController | PUT | `/api/identity-admin/roles/{roleId}/permissions` | `ManageUsers` | Yes |
| IdentityAdminController | POST | `/api/identity-admin/roles/{roleId}/groups` | `ManageUsers` | Yes |
| IdentityAdminController | DELETE | `/api/identity-admin/roles/{roleId}/permissions/{permission}` | `ManageUsers` | Yes |
| TenantAdminController | GET | `/api/admin/tenants` | `Admin` | Yes |
| TenantAdminController | POST | `/api/admin/tenants` | `Admin` | Yes |
| TenantAdminController | PUT | `/api/admin/tenants/settings` | `Admin` | Yes |
| TenantAdminController | PUT | `/api/admin/tenants/assign-plan` | `Admin` | Yes |
| TenantAdminController | PUT | `/api/admin/tenants/suspend` | `Admin` | Yes |
| TenantAdminController | POST | `/api/admin/tenants/{tenantId}/onboard` | `Admin` | Yes |

**Arabic short note:** دي قائمة كاملة بكل الـ endpoints الموجودة في الـ backend.

## 2) DTOs, Params, Validation, AuthZ, Multi-Tenancy, Business Constraints

- Full request/response DTO mapping, route/query params, validation constraints, JWT + refresh flow, role/policy rules, tenant behavior, and business constraints are documented in:
  - `docs/API_DICTIONARY.md` (comprehensive version).

**Arabic short note:** كل تفاصيل الـ DTOs والـ validation والـ auth والـ multi-tenant متوثقة بالكامل في ملف الـ API Dictionary الكامل.

## 3) Authentication & Authorization Summary

- JWT bearer auth enabled globally for protected endpoints.
- Refresh token workflow implemented via `/api/auth/refresh`, revoke, revoke-all.
- Policies used: `Admin`, `User`, `ManageUsers`, `Seller`, plus granular permissions (`CanEditUsers`, `CanDeleteUsers`, `CanExportUsers`).

**Arabic short note:** نظام الدخول JWT + Refresh شغال، ومعاه صلاحيات Roles/Policies بالتفصيل.

## 4) Multi-Tenant Rules Summary

- Tenant resolution is mandatory for API routes (except root/health/swagger) using `X-Tenant-ID` first, then subdomain fallback.
- Invalid/missing/inactive tenant blocks requests before controller logic.
- Plan API request limits are enforced before hitting business handlers.

**Arabic short note:** الـ tenant بيتحدد قبل أي endpoint، ولو فيه مشكلة الطلب بيتوقف فورًا.

---

## Status

STEP 1 complete. Waiting for your confirmation before frontend generation.

**Arabic short note:** خلصت Step 1 ومستني تأكيدك قبل ما أبدأ الـ Frontend.

# FullWebAPIClean

## Multi-tenant folder structure

- `src/API`
  - `Controllers/Admin`: tenant admin portal endpoints (create/update/plan/suspend/onboard)
  - `Middleware`: tenant resolver, plan limits, feature attribute, swagger tenant header operation filter
- `src/Application`
  - `Common/Interfaces/MultiTenancy`: tenant abstractions (context/store/cache/feature/billing/onboarding/storage)
  - `Tenants/Commands`: CQRS commands for tenant lifecycle operations
  - `Tenants/Queries`: CQRS query DTOs for tenant listings
  - `Tenants/DTOs`: request contracts
- `src/Domain`
  - `Entities/Multitenancy`: tenant, plan, feature, subscription, usage, membership entities
  - `MultiTenancy`: enums and tenant-owned contract
- `src/Persistence`
  - `Interceptors`: save changes interceptor for tenant id, soft delete, auditing
  - `MultiTenancy`: EF model cache key factory for dynamic schema
  - `ApplicationDbContext`: tenant-aware DbContext with global filters
- `src/Infrastructure`
  - `MultiTenancy`: tenant context, tenant store with IMemoryCache, storage service, feature evaluation, onboarding, log enricher
  - `Billing`: Stripe billing service interface implementation
- `src/Worker`
  - `Jobs`: recurring per-tenant jobs (email digest, cleanup, billing sync)
  - `Services`: tenant job contract
  - `Worker.cs`: tenant iteration runner with tenant-scoped execution

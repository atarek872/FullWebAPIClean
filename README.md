# FullWebAPIClean

A multi-tenant ASP.NET Core Web API solution built with Clean Architecture principles, SQL Server persistence, JWT authentication, tenant-aware middleware, and a background worker for tenant jobs.

## What this project includes

## Architecture and projects

The solution is organized by layers and runtime hosts:

- `src/Domain`  
  Core business entities, multi-tenancy contracts, and authorization constants.
- `src/Application`  
  Application services, interfaces, CQRS commands/queries, DTOs, validation behavior.
- `src/Persistence`  
  Entity Framework Core DbContext, SQL Server configuration, tenant-aware model cache, migrations, save interceptors.
- `src/Infrastructure`  
  Cross-cutting implementations (JWT/token generation, email/sms abstractions, tenant store/cache/context, billing service).
- `src/API`  
  ASP.NET Core Web API host, middleware pipeline, controllers, Swagger, rate limiting, health checks, authentication/authorization.
- `src/Worker`  
  Background worker host that loops tenants and executes scheduled tenant jobs.
- `tests/Application.UnitTests` and `tests/API.IntegrationTests`  
  Test projects scaffolded in the repository.

## Main capabilities

- Multi-tenant support with tenant resolution middleware and tenant-aware context/services.
- Tenant administration endpoints (create tenant, update settings, assign plans, suspend, onboarding).
- Authentication endpoints (register, login, refresh token, revoke token).
- Identity administration endpoints for users, roles, permissions, and role groups.
- Sample ecommerce endpoints for seller profile, product creation/search, and checkout flow.
- JWT Bearer authentication with role and permission-based authorization policies.
- ASP.NET Identity-backed user/role management.
- SQL Server persistence with EF Core migrations.
- Serilog request and application logging (console + rolling file).
- API versioning, Swagger/OpenAPI docs, CORS policy, rate limiting, response compression, health checks.
- Worker process that executes per-tenant jobs (email digest, cleanup, billing usage sync).

## API surface (high level)

- `AuthController`: `/api/auth/register`, `/api/auth/login`, `/api/auth/refresh`, `/api/auth/revoke`, `/api/auth/revoke-all`
- `TenantAdminController`: `/api/admin/tenants` + lifecycle operations (settings, plan assignment, suspension, onboarding)
- `IdentityAdministrationController`: permission catalog + CRUD for users and roles + permissions assignment
- `EcommerceController`: seller profile, products, product search, checkout
- `HelloController`: quick hello endpoints

When running in development, Swagger is available and the root URL redirects to `/swagger`.

---

## First-time setup (SQL Server to running API and Worker)

This section is written for a fresh machine.

## 1) Prerequisites

Install:

1. **.NET SDK 9.0**
2. **SQL Server** (any of these options):
   - SQL Server Developer/Express (local install), or
   - SQL Server in Docker
3. **EF Core CLI tools**

Install EF tools globally:

```bash
dotnet tool install --global dotnet-ef
```

If already installed:

```bash
dotnet tool update --global dotnet-ef
```

## 2) Clone and enter repository

```bash
git clone <your-repo-url>
cd FullWebAPIClean
```

## 3) Configure SQL Server connection string

The API reads `ConnectionStrings:DefaultConnection` from configuration.

Default sample currently points to local SQL Server with Windows auth:

```text
Server=localhost;Database=FullWebAPI;Encrypt=false;TrustServerCertificate=true;Trusted_Connection=True
```

### Option A: Local SQL Server with Windows authentication

Use this (or adjust server name like `localhost\\SQLEXPRESS`):

```text
Server=localhost;Database=FullWebAPI;Encrypt=false;TrustServerCertificate=true;Trusted_Connection=True
```

### Option B: SQL auth (username/password)

```text
Server=localhost,1433;Database=FullWebAPI;User Id=sa;Password=<YourStrongPassword>;Encrypt=false;TrustServerCertificate=true
```

### Option C: SQL Server in Docker

Start SQL Server container:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Your_strong_password123" \
  -p 1433:1433 --name fullwebapi-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

Use this connection string:

```text
Server=localhost,1433;Database=FullWebAPI;User Id=sa;Password=Your_strong_password123;Encrypt=false;TrustServerCertificate=true
```

## 4) Add configuration values

You should set configuration for **both API and Worker** (both use persistence/infrastructure registrations).

You can do this either in `appsettings.Development.json` files, user-secrets, or environment variables.

### Recommended: environment variables (works for both hosts)

Linux/macOS:

```bash
export ConnectionStrings__DefaultConnection="Server=localhost;Database=FullWebAPI;Encrypt=false;TrustServerCertificate=true;Trusted_Connection=True"
export JwtSettings__Secret="replace-with-a-long-random-secret-at-least-32-characters"
export JwtSettings__Issuer="FullWebAPI"
export JwtSettings__Audience="FullWebAPI"
export JwtSettings__ExpiryMinutes="60"
```

PowerShell:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost;Database=FullWebAPI;Encrypt=false;TrustServerCertificate=true;Trusted_Connection=True"
$env:JwtSettings__Secret="replace-with-a-long-random-secret-at-least-32-characters"
$env:JwtSettings__Issuer="FullWebAPI"
$env:JwtSettings__Audience="FullWebAPI"
$env:JwtSettings__ExpiryMinutes="60"
```

> Important: replace `JwtSettings__Secret` with a strong secret before using this project.

## 5) Restore dependencies

```bash
dotnet restore FullWebAPI.sln
```

## 6) Create/update database schema (run migrations)

Because migrations are stored in `src/Persistence/Migrations`, apply them to SQL Server:

```bash
dotnet ef database update \
  --project src/Persistence/Persistence.csproj \
  --startup-project src/API/API.csproj
```

## 7) Build solution

```bash
dotnet build FullWebAPI.sln
```

## 8) Run API

```bash
dotnet run --project src/API/API.csproj
```

Expected behavior:

- App starts and seeds default roles (`Admin`, `User`, `TenantAdmin`, `TenantUser`) on startup.
- Browse to:
  - `https://localhost:xxxx/swagger` (or http profile port from launch settings)
  - `/health` endpoint for health check

## 9) Run Worker (separate terminal)

```bash
dotnet run --project src/Worker/Worker.csproj
```

The worker loops through tenants and runs jobs every 5 minutes.

## 10) First operational flow to verify end-to-end

1. Start API.
2. Open Swagger.
3. Use tenant admin endpoints to create a tenant.
4. Register a user for that tenant via `/api/auth/register`.
5. Login via `/api/auth/login` to get JWT + refresh token.
6. Call secured endpoints with `Authorization: Bearer <token>`.
7. Start Worker and check logs for tenant job execution.

---

## Build and run commands (quick reference)

```bash
dotnet restore FullWebAPI.sln
dotnet ef database update --project src/Persistence/Persistence.csproj --startup-project src/API/API.csproj
dotnet build FullWebAPI.sln
dotnet run --project src/API/API.csproj
dotnet run --project src/Worker/Worker.csproj
```

## Troubleshooting

- **`JWT secret is not configured`**  
  Set `JwtSettings:Secret` (or `JwtSettings__Secret`) to a non-empty value.
- **Cannot connect to SQL Server**  
  Verify SQL Server is running, server/port are correct, and auth mode matches your connection string.
- **Migration command fails**  
  Ensure `dotnet-ef` is installed and you are using the API startup project as shown above.
- **Worker crashes on startup**  
  Ensure `ConnectionStrings:DefaultConnection` is available for the Worker process too.

## Notes

- Default `appsettings.json` includes sample values for local development; prefer secure secrets and environment-specific config in real deployments.
- Logs are configured via Serilog with console + rolling file sink (`logs/log-.txt`).

# FullWebAPI

A complete, production-ready ASP.NET Core 9 Web API project implemented using Clean Architecture.

## Features

- Clean Architecture (Domain, Application, Infrastructure, Persistence, API)
- CQRS with MediatR
- JWT Authentication with Refresh Tokens
- Role-based Authorization
- SQL Server with EF Core 9
- Serilog Logging
- Swagger with JWT Auth
- Health Checks
- Rate Limiting
- Response Compression
- API Versioning
- FluentValidation
- Mapster for Mapping
- Background Jobs (Worker Service)
- Email and SMS Services (interfaces)
- Soft Delete and Auditing

## Prerequisites

- .NET 9.0
- SQL Server

## Configuration

Update `appsettings.json` with:

- `ConnectionStrings:DefaultConnection`
- `JwtSettings:Secret`, `Issuer`, `Audience`, `ExpiryMinutes`
- Serilog configuration

## Running the Project

1. `dotnet restore`
2. `dotnet ef database update` (from Persistence project)
3. `dotnet run` (from API project)

## API Endpoints

- POST /api/v1/auth/login
- POST /api/v1/auth/register
- etc.

## Testing

Run tests with `dotnet test`
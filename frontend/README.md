# Frontend (Vue 3 + Vite + Pinia + Axios)

Production-ready starter frontend for this repository's ASP.NET Core API.

## Quick start

```bash
cd frontend
cp .env.example .env
npm install
npm run dev
```

## API integration defaults

- `VITE_API_BASE_URL=https://localhost:7246`
- Automatic `Authorization: Bearer <token>` from persisted auth session.
- Automatic `X-Tenant-ID` header for tenant-aware API routes.
- Automatic refresh token flow on `401` responses.

## Suggested backend startup

Run the API project and trust dev certificates if needed:

```bash
dotnet run --project src/API/API.csproj
```

Then start frontend:

```bash
npm run dev
```

## Folder structure

- `src/api`: Axios client + endpoint modules.
- `src/stores`: Pinia stores (`auth`, `tenant`, `products`).
- `src/router`: route definitions and auth guards.
- `src/views`: feature pages (auth, dashboard, ecommerce).
- `src/layouts`, `src/components`: reusable UI and shells.
- `src/services`, `src/utils`, `src/config`, `src/constants`, `src/types`: app foundations.

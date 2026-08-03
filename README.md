# KitchenDeck

A kitchen order management system. Restaurants manage staff, menus, tables, and
live orders; a big-screen **Kitchen Window** shows active tickets per table.

- **Frontend:** React + TypeScript (Vite) — browser client now, Android app later.
- **Backend:** ASP.NET Core 8 Web API (client–server, REST + JWT).
- **Storage:** Azure Blob Storage holding JSON documents (one blob per entity) — no SQL database.

## Architecture

```
frontend/kitchen-deck-ui   React SPA (auth, restaurants, staff, orders, kitchen window)
backend/KitchenDeck.API     REST API
  Controllers/              HTTP endpoints
  Services/                 Auth (JWT + PBKDF2), restaurant/user logic
  Storage/                  IJsonBlobStore + Azure Blob implementation
  Models/  DTOs/            Domain entities and request/response shapes
```

Data is stored as JSON blobs in these logical containers: `users`, `restaurants`,
`members`, `menu`, `tables`, `orders`. The storage layer sits behind
`IJsonBlobStore`, so the persistence technology can change without touching
controllers or services.

## Prerequisites

- .NET 8 SDK
- Node.js 20+ and npm
- An Azure Storage account **or** [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite)
  for local development.

## Configuration

Backend settings live in `backend/KitchenDeck.API/appsettings.json`. Secrets should
**not** be committed — use user-secrets or environment variables in real use.

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings:AzureBlobStorage` | Azure Storage connection string (or `UseDevelopmentStorage=true` for Azurite). |
| `Jwt:Secret` | Signing key for JWT tokens (min. 32 chars). |
| `Cors:AllowedOrigins` | Origins allowed to call the API (defaults to the Vite dev server). |

For local development, `appsettings.Development.json` already points at Azurite
(`UseDevelopmentStorage=true`) with a throwaway JWT secret. To use a real Azure
account, set the connection string via user-secrets:

```pwsh
cd backend/KitchenDeck.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:AzureBlobStorage" "<your-connection-string>"
dotnet user-secrets set "Jwt:Secret" "<a-strong-32+-char-secret>"
```

## Running locally

**Backend** (http://localhost:5139, Swagger at `/swagger`):

```pwsh
cd backend/KitchenDeck.API
dotnet run --launch-profile http
```

**Frontend** (http://localhost:5173):

```pwsh
cd frontend/kitchen-deck-ui
npm install
npm run dev
```

## Implemented so far

- User registration & login (JWT bearer auth, PBKDF2 password hashing).
- Create a restaurant (creator becomes Admin/owner).
- Add existing users as staff by email; tag them Cook / Waiter / Guard / CleaningStaff / Admin.
- Manage staff roles and remove members.
- Set a 6-digit Kitchen Window passcode.
- Azure Blob JSON storage layer.

## Roadmap

- Menu CRUD (admin).
- Table CRUD (admin).
- Order taking per table (waiter): create, edit, mark Preparing / Served.
- Kitchen Window: passcode-protected live view of orders as per-table tiles.
- Real-time updates (SignAlR/WebSockets) for the kitchen screen.
- Android client.

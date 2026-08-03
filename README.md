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

## CI/CD deployment flow

This repository now includes a GitHub Actions workflow at [.github/workflows/deploy.yml](.github/workflows/deploy.yml) that:

- builds and tests the backend and frontend on every push to main
- builds and pushes the backend container image to Google Container Registry (GCR)
- deploys the backend to Azure Container Instances (ACI)
- deploys the frontend to Azure Static Web Apps

### Required GitHub secrets

Create these secrets in GitHub under Settings → Secrets and variables → Actions:

- `AZURE_CREDENTIALS`
- `AZURE_STATIC_WEB_APPS_API_TOKEN`
- `JWT_SECRET`
- `AZURE_BLOB_CONNECTION_STRING`

### Azure prerequisites

1. Create a resource group:

```bash
az group create --name kitchendeck-rg --location eastus
```

2. Create or reuse an Azure Storage account and obtain its connection string:

```bash
az storage account create --name <unique-storage-account> --resource-group kitchendeck-rg --location eastus --sku Standard_LRS
az storage account show-connection-string --name <unique-storage-account> --resource-group kitchendeck-rg -o tsv
```

3. Create an Azure Static Web App in the Azure portal or CLI. The GitHub Action will deploy to it using the API token.

### GitHub Container Registry prerequisites

1. Ensure the repository package permissions allow the workflow to publish to GHCR.
2. In the repository settings, enable package writes for the workflow.
3. The workflow uses `GITHUB_TOKEN` for authentication, so no extra registry secret is required.

### Backend runtime environment

The container image expects these env vars at runtime:

- `ASPNETCORE_URLS=http://+:8080`
- `Jwt__Secret=<strong secret>`
- `ConnectionStrings__AzureBlobStorage=<azure storage connection string>`

### Frontend runtime configuration

The Vite frontend currently uses the API URL from its build-time environment. Update the deploy config or environment variables so the browser calls your Azure Container Instance public URL.

### Manual deployment checklist

1. Push the repo to GitHub.
2. Add the GitHub secrets listed above.
3. Ensure the Azure service principal credentials in `AZURE_CREDENTIALS` have permission to create resources and deploy to the resource group.
4. Create the Azure Static Web App and copy its deployment token into `AZURE_STATIC_WEB_APPS_API_TOKEN`.
5. Commit to the `main` branch and let GitHub Actions run the workflow.
6. After the run completes, open the Azure Static Web App URL for the frontend and the ACI FQDN for the backend.

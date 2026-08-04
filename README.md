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
to check secrets locally
dotnet user-secrets list

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

Pushing to `main` runs the GitHub Actions workflow at [.github/workflows/deploy.yml](.github/workflows/deploy.yml), which:

- builds the backend image and pushes it to **GitHub Container Registry (GHCR)** as `ghcr.io/<owner>/kitchendeck-api:latest` and `:<sha>`
- updates the **Azure Container App** to the new image (image-only — see below)
- builds the frontend and deploys it to **Azure Static Web Apps**

### Deployment topology

- **Backend:** Azure Container Apps, external HTTP ingress on target port **8080**, scale 0–1 (scales to zero when idle, so the first request after idle has a cold start).
- **Frontend:** Azure Static Web Apps, built by Oryx with `VITE_API_URL` injected at build time.
- **Registry:** GHCR (private package). The Container App stores its own GHCR pull PAT (`read:packages`) in Azure.

### The pipeline is image-only

Backend runtime configuration lives **on the Container App in Azure**, not in the workflow. The workflow only runs `az containerapp update --image …`, so it never touches ingress, target port, container name, environment variables, scale settings, or the registry pull credential. This keeps repeat deployments from regressing the app's configuration.

### Required GitHub secrets

Create these under Settings → Secrets and variables → Actions:

| Secret | Purpose |
| --- | --- |
| `AZURE_CREDENTIALS` | Service principal JSON for `azure/login` (contributor on the resource group). |
| `AZURE_RESOURCE_GROUP` | Resource group holding the Container App (e.g. `kitchen-deck-rg`). |
| `AZURE_CONTAINER_APP_NAME` | Container App name (e.g. `kitchendeck-api`). |
| `BACKEND_URL` | Public backend API base URL **including `/api`**, used as the frontend's `VITE_API_URL`. |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Deployment token from the Static Web App resource. |

The image build/push authenticates with the built-in `GITHUB_TOKEN` (no extra registry secret needed). The GHCR pull PAT used at runtime is stored on the Container App itself, not in GitHub.

### One-time backend configuration

Set these on the Container App once (via the Azure Portal or CLI). The pipeline preserves them on every deploy:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:8080`
- `Jwt__Secret=<strong 32+ char secret>`
- `ConnectionStrings__AzureBlobStorage=<azure storage connection string>`
- `Cors__AllowedOrigins__0=https://<your-swa>.azurestaticapps.net` (exact origin, no trailing slash)

```bash
az containerapp update \
  --name <app> --resource-group <rg> \
  --set-env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    Jwt__Secret="<32+ char secret>" \
    ConnectionStrings__AzureBlobStorage="<blob connection string>" \
    Cors__AllowedOrigins__0="https://<your-swa>.azurestaticapps.net"
```

Also confirm the Container App's ingress target port is **8080** and that a GHCR pull credential (`read:packages` PAT) is configured, since the image is a private package.

### Deployment checklist

1. Push the repo to GitHub and enable Actions.
2. Add the GitHub secrets listed above.
3. Create the Azure Container App (external ingress, target port 8080, GHCR private image, scale 0–1) and the Azure Static Web App.
4. Apply the one-time backend environment configuration.
5. Set `BACKEND_URL` to your Container App URL **plus `/api`**.
6. Commit to `main`; the workflow builds, pushes, and deploys automatically.
7. Open the Static Web App URL for the frontend; it calls the Container App backend.

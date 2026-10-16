# Cloud computing — exercise 4

Tire catalogue migrated from Render/Cloudflare to Northflank/Supabase, keeping the same functionality and data. Exercise 3 code and documentation remain on `main`; this branch is `migration-for-ex4`. Keep the original cloud resources until grading is complete.

- `frontend/`: HTML/CSS/JavaScript, served by a Node container.
- `api/`: .NET 10 REST API, EF Core and PostgreSQL; image uploads go to Supabase Storage.
- `worker/`: standalone Node job that updates simulated view counts hourly.

See [deployment settings](docs/deployment.md) and [architecture](docs/architecture.md).

## Run locally

Requires .NET 10, Node 24.2+ and Docker. From the repository root:

```powershell
docker compose up -d
dotnet run --project api --launch-profile http
```

Open `frontend/index.html`; it uses the API at `http://localhost:5016`. PostgreSQL runs on port 5433. The API applies migrations on startup. Uploads need the Supabase settings in the deployment guide; JSON image URLs work without storage credentials.

## API

`GET /tires`, `GET /tires/{id}`, `POST /tires`, `PUT /tires/{id}`, `DELETE /tires/{id}`.

Create/update accepts JSON or multipart fields: `brand`, `type`, `rimDiameter`, `price`, plus an optional `imageUrl` (JSON) or `image` file (multipart). Uploads allow JPEG, PNG and WebP up to 5 MiB. Omit the image on update to retain it. Examples: `api/api.http`; local API docs: `/scalar`.

Views are simulated integers from 0 to 50, not visitor tracking. Refresh the page after a job run to see them.

## Checks

```powershell
dotnet run --project tests/ConnectionChecks
dotnet run --project tests/StorageChecks
dotnet run --project tests/UploadChecks
node tests/UploadChecks/frontend-checks.cjs
node tests/UploadChecks/frontend-load-checks.cjs
pnpm --dir worker install --frozen-lockfile
pnpm --dir worker test
```

This is a public demo without authentication. Writes have an in-memory rate limit; image cleanup is best effort. Before submission, verify CRUD and image replacement/deletion, data preservation, and one actual scheduled job run.

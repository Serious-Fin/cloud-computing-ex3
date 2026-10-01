# cloud-computing-ex3

## Exercise

Make an application with CRUD (Create, Read, Update Delete + list data) functionality of any kind of entities on any PaaS. Create and update functions should check input of at least 4 different data types. Application should contain web application, public API, database for persistence, file storage and some background operation. Make a small architectural drawing.

## Tech Stack

| Area | Technology |
| --- | --- |
| Backend language | C# + ASP.NET Core Web API |
| ORM | Entity Framework Core |
| Database | PostgreSQL |
| Frontend | HTML/CSS/JavaScript |

## Running locally

1. Start PostgreSQL (from the repository root):

   ```
   docker compose up -d
   ```

   PostgreSQL 17 listens on `localhost:5433`. Host port 5433 is used so a PostgreSQL
   already installed on 5432 is left untouched. Data lives in the `tires-db-data` volume
   and survives `docker compose down` (`docker compose down -v` wipes it).

2. Start the API (from the `api` folder):

   ```
   dotnet run --launch-profile http
   ```

   It applies pending migrations on start-up and listens on `http://localhost:5016`.

3. Open `frontend/index.html` in a browser, or serve the `frontend` folder with a static file server.

   The page selects the local API on localhost or when opened as a file. For other
   hosts, it uses the deployed URL configured in `frontend/app.js`.

## API

| Action | HTTP call |
| --- | --- |
| List tires | `GET /tires` |
| Get a tire | `GET /tires/{id}` |
| Create tire | `POST /tires` |
| Update tire | `PUT /tires/{id}` |
| Delete tire | `DELETE /tires/{id}` |

Create and update validate brand, tire type, rim diameter, price, and image URL in
`api/TireValidator.cs`. The frontend displays validation errors under the form.
Example requests are in `api/api.http`; interactive API documentation is available
at `/scalar` when running in Development.

## Database

PostgreSQL through EF Core (`Npgsql.EntityFrameworkCore.PostgreSQL`). The API reads the
connection string from the `ConnectionStrings:Default` configuration key, so the same
build works locally and on Render — only the value differs:

| Environment | Where the value comes from |
| --- | --- |
| Local | `api/appsettings.Development.json` (`Host=localhost;Port=5433;Database=tires;...`) |
| Render | Environment variable `ConnectionStrings__Default` |

`__` is how .NET represents `:` in environment variable names, so
`ConnectionStrings__Default` maps to `ConnectionStrings:Default`.

The API accepts both Npgsql keyword connection strings and `postgres://` or
`postgresql://` URLs. `api/DatabaseConnection.cs` converts URLs before passing them
to Npgsql, including decoding credentials, defaulting the port to 5432, and honoring
an optional `sslmode` query parameter. Other URL query parameters are rejected.

Run the connection-format checks with `dotnet run --project tests/ConnectionChecks`.

### Migrations

Migrations live in `api/Migrations` and are applied automatically at start-up
(`Database.MigrateAsync()` in `api/Program.cs`), so a fresh database needs no manual
step. After changing `Tire` or `ApiDb`:

```
cd api
dotnet ef migrations add <Name>
```

The EF tools build the project and use the `Development` environment by default, so
`appsettings.Development.json` is picked up without extra configuration.

## Deploying to Render

`render.yaml` is a Render Blueprint that defines the whole stack, so a single push is all
the setup needed:

| Resource | Type | Plan |
| --- | --- | --- |
| `cloud-computing-ex3-db` | PostgreSQL | free |
| `cloud-computing-ex3-api` | Web service (Docker) | free |
| `cloud-computing-ex3-web` | Static site | free |

1. Push the repository to GitHub.
2. In the Render Dashboard choose **New → Blueprint**, select the repository, and apply.
3. Render creates all three resources and wires the API's `ConnectionStrings__Default`
   to the database's internal URL. The API applies migrations on its first start.

Worth knowing:

- The API and the database are both in `frankfurt`. An internal URL only resolves inside
  one region, so keep those two regions equal.
- Render has no native .NET runtime, so the API is built from `api/Dockerfile`, which
  listens on `0.0.0.0:$PORT` as Render requires.
- The page calls whatever URL is in `API_BASE` at the top of `frontend/app.js`. Service
  names are unique per workspace: if `cloud-computing-ex3-api` is already taken, Render
  appends a suffix and the URL changes — update `API_BASE` to match.
- Free plan limits: the database is **deleted 30 days after creation**, a free web
  service **spins down after 15 minutes** without traffic (~1 minute to wake), and
  Background Workers and Cron Jobs have no free tier.

### Wiring it by hand instead

1. Create the database with **New → Postgres**, then pick a region.
2. Render exposes two URLs:

   | | Reachable from | Use for |
   | --- | --- | --- |
   | Internal URL | Render services in the **same region** | The deployed API |
   | External URL | Anything on the internet, over TLS | A local API pointed at the cloud DB |

3. Set `ConnectionStrings__Default` on the API service (**Environment** page) to the
   **internal** URL.

Paste Render's PostgreSQL URL directly into the environment variable; the API
converts it to Npgsql's keyword format. For an external connection, use
`?sslmode=require` to require TLS. You can also supply the keyword form:

```
Host=<host>;Port=5432;Database=<database>;Username=<user>;Password=<password>;SSL Mode=Require
```

A Free Postgres instance holds 1 GB and has no backups. `fromDatabase` with
`property: connectionString` (as used in `render.yaml`) yields the internal URL.

## To-Do
- [x] Create API
- [x] Create frontend
- [x] Save data in DB
- [ ] Save files in file storage
- [ ] Add a background process
- [x] Make data validation in create and put operations (5 types: string, enum, int, decimal, URL)
- [ ] Host app on PaaS
- [ ] Make architectural drawing

# Tire application — cloud architecture draft

This drawing follows the repository code, `render.yaml`, and `worker/wrangler.jsonc`; cloud deployment and credentials require the setup steps in `worker/README.md`.

```mermaid
flowchart LR
    Browser["User's browser<br/>Runs the web application"]

    subgraph Render["Render · PaaS"]
        Web["Static site<br/>HTML · CSS · JavaScript"]
        subgraph Frankfurt["Frankfurt region"]
            subgraph Service["Docker web service · ASP.NET Core API"]
                API["Public REST API · /tires<br/>Create · Read · Update · Delete · List<br/>Input and image validation"]
            end
            DB[("PostgreSQL<br/>Tire records · image URL/key<br/>View counts · update timestamps")]
        end
    end

    subgraph Cloudflare["Cloudflare"]
        Cron["Cron Trigger<br/>Hourly UTC schedule"]
        Worker["Separately hosted Worker<br/>Generates simulated views"]
        Hyperdrive["Hyperdrive<br/>PostgreSQL connection pool"]
        R2[("R2 object storage<br/>tire-images bucket<br/>JPEG · PNG · WebP files")]
    end

    Web -->|"HTTPS: serves page and assets"| Browser
    Browser -->|"HTTPS: CRUD requests; multipart uploads"| API
    API -->|"JSON: tire data, image URLs, errors"| Browser
    API <-->|"Internal DB connection · EF Core / Npgsql"| DB
    Cron --> Worker
    Worker -->|"SQL: persists simulated views"| Hyperdrive
    Hyperdrive -->|"External PostgreSQL connection · TLS"| DB
    API -->|"Authenticated S3 API: upload / delete"| R2
    R2 -->|"Public HTTPS image delivery"| Browser

    classDef client fill:#f1f5f9,stroke:#64748b,color:#0f172a;
    classDef app fill:#dbeafe,stroke:#2563eb,color:#172554;
    classDef data fill:#dcfce7,stroke:#16a34a,color:#14532d;
    classDef job fill:#fef3c7,stroke:#d97706,color:#78350f;
    class Browser client;
    class Web,API app;
    class DB,R2 data;
    class Cron,Worker,Hyperdrive job;
```

## How to explain it

1. Render serves the frontend files; the web application runs in the user's browser.
2. The browser calls the public API to list, read, create, update, or delete tires. Create/update validation covers a string (brand), enum (type), integer (rim diameter), decimal (price), and the image file.
3. The API stores tire records in PostgreSQL over Render's internal database connection. Image bytes are stored in R2; PostgreSQL holds their public URLs and internal cleanup keys.
4. Images are uploaded through the API using credentials configured on the API service. The browser displays them directly from R2's public URL.
5. A separate Cloudflare Worker runs hourly through a Cron Trigger and writes simulated view counts to PostgreSQL through Hyperdrive. It continues running on schedule while the API sleeps. The frontend fetches the latest values when refreshed. Change the cron to every minute for a demo.

## Repository map

| Location | Responsibility |
| --- | --- |
| `frontend/` | Browser interface and API calls |
| `api/Program.cs` | API endpoints, service registration, startup migrations |
| `api/TireForm.cs`, `api/TireValidator.cs`, `api/ImageUpload.cs` | Form parsing and validation |
| `api/TireUploadEndpoints.cs`, `api/R2Storage.cs` | Image uploads, replacement, and cleanup |
| `api/ApiDb.cs`, `api/Tire.cs`, `api/Migrations/` | Database model and schema |
| `worker/src/`, `worker/wrangler.jsonc` | Separate Cloudflare background operation and hourly schedule |
| `worker/README.md` | Cloudflare setup, deployment, and verification |
| `render.yaml`, `api/Dockerfile` | Cloud resource definitions and API container |
| `docker-compose.yml` | Local PostgreSQL development setup |
| `tests/` | Connection, upload, and frontend checks |

The Render Blueprint defines the frontend, API, and database. The R2 bucket and Hyperdrive configuration are created separately in Cloudflare; the Worker is deployed with Wrangler. The background operation has an independent lifecycle and no public HTTP endpoint.

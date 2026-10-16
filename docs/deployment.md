# Deployment reference

Northflank hosts the frontend, API, PostgreSQL and scheduled job. Supabase hosts public images. Use the repository root as Docker build context.

| Component | Dockerfile | Port | Runtime settings |
| --- | --- | --- | --- |
| API | `api/Dockerfile` | `10000` | `ConnectionStrings__Default`, Supabase settings below |
| Frontend | `frontend/Dockerfile` | `8080` | `API_BASE_URL`: public HTTPS API URL, without `/tires` |
| Job | `worker/Dockerfile` | None | `DATABASE_URL`: PostgreSQL URL |

The API accepts a PostgreSQL URL or Npgsql connection string. The frontend generates its configuration at startup; restart it after changing the API URL. `/healthz` checks the frontend server only. Direct/static hosting still has the exercise 3 Render fallback in `frontend/app.js`; use the container for this deployment.

## Supabase settings (API only)

| Variable | Value |
| --- | --- |
| `Supabase__Endpoint` | S3 endpoint from the Supabase dashboard |
| `Supabase__Region` | Region from the S3 configuration |
| `Supabase__BucketName` | `tire-images` |
| `Supabase__PublicBaseUrl` | `https://<project-ref>.supabase.co/storage/v1/object/public/tire-images` |
| `Supabase__AccessKeyId` | S3 access key ID |
| `Supabase__SecretAccessKey` | S3 secret access key |

Use a public bucket and keep S3 credentials in API secrets. The browser receives public image URLs only. Missing storage configuration leaves reads available but uploads return 503.

## Scheduled job

Run hourly with `0 * * * *` (UTC), disallow overlapping runs and set a 60-second deadline. The job updates statistics once and exits; it does not run migrations. See [local job commands](../worker/README.md).

## Migration context

Exercise 3 used Render PostgreSQL/API/frontend, Cloudflare R2 and a Worker with Hyperdrive. Its code and setup remain on `main`. Keep those deployments on their original branch until grading; removing files here does not remove cloud resources.

If repeating the transfer: stop source writes during the final copy, restore the database including IDs, sequences and `__EFMigrationsHistory`, and copy image objects with their exact keys and content types. Restore before starting the API because it applies migrations on startup. Verify referenced files and their contents, then change only destination `ImageUrl` values to the Supabase base URL plus `ImageKey`; leave external URLs (`ImageKey IS NULL`) alone. Preserve source data and backups.

Each job must target its own database. Do not run both schedules against the destination. If returning to the original deployment after new writes, reconcile the newer records and files first.

The owner reports the migrated app is working. Live deployment state is not recorded or independently verified by this guide. Final checks: preserved records/images, create/edit/delete with image cleanup, and an actual hourly run with an advancing `viewsUpdatedAt` timestamp.

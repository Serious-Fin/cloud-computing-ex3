# Tire views job

`src/run-job.js` connects using `DATABASE_URL`, calls `src/update-views.js`, closes the connection and exits (0 on success, 1 on failure). It replaces every tire's simulated views with an integer from 0 to 50 and updates its timestamp. Northflank supplies the schedule; the API owns schema migrations.

From this folder, with Node 24.2+ and pnpm 11.19.0:

```powershell
pnpm install --frozen-lockfile
pnpm test
# Set DATABASE_URL to a disposable database with the API schema applied.
pnpm start
```

`pnpm start` changes statistics for every tire in the selected database. Build from the repository root with `docker build -f worker/Dockerfile -t tire-views-job .`.

Deployment settings are in [docs/deployment.md](../docs/deployment.md).

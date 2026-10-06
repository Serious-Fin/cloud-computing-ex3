# Cloudflare tire views worker

This is the separately hosted background operation. Cloudflare invokes its
`scheduled` handler hourly (at minute 0, UTC); it updates PostgreSQL directly through
Hyperdrive, then exits. It has no HTTP handler or public URL. The API can sleep
without stopping the schedule. Counts remain simulated integers from 0 through 50.

## One-time setup and deployment

1. In Render, deploy the updated API first. It applies the existing database
   migrations, and this version no longer runs the old background service.
   Remove the obsolete `TireViews__IntervalMinutes` API environment variable if
   it is still present. No new migration is required.
2. Open your Render database's connection settings and copy the **External
   Database URL**, not its internal URL. Cloudflare connects from outside Render.
   The database must allow external connections from Cloudflare Hyperdrive;
   check Render's networking/access controls if the connection test fails.
3. In Cloudflare, open **Hyperdrive** and create a configuration named
   `tire-views-db`. Supply the external host, port, database, username, and password
   from Render (or its external connection URL if the form accepts it). Require
   TLS for the origin connection. Disable query caching; this job only writes.
   Credentials live in Hyperdrive, never in the repository or frontend.
4. Copy the resulting Hyperdrive configuration ID. Replace the all-zero `id`
   in `worker/wrangler.jsonc` with it. The ID itself is safe to commit. The
   binding name must stay `HYPERDRIVE`.
5. Install Node.js 22 or later with npm, then open a terminal in this folder.
   Use pnpm to install the exact dependency versions in the committed lockfile:

   ```powershell
   npm install -g pnpm@11.19.0
   pnpm install --frozen-lockfile
   pnpm exec wrangler login
   pnpm test
   pnpm run build
   pnpm run deploy
   ```

   Login opens the browser for your Cloudflare account. Choose the Workers Free
   plan. The deployment bundles the code, adds the Hyperdrive binding, and
   registers the cron trigger; you do not need to paste source into the dashboard.
6. In **Workers & Pages**, open `cloud-computing-ex3-tire-views`. Confirm its
   Hyperdrive binding and hourly Cron Trigger. Trigger changes can take up to
   15 minutes to propagate. Use the worker's logs to see successful runs or failures.
7. Refresh the frontend after a run. Check `viewsUpdatedAt` in `GET /tires`:
   it should advance even if the API was asleep. A random count may equal its
   previous value, so use the timestamp to verify success. New tires remain
   pending until the next scheduled run.

For a demo, change `triggers.crons` to `["* * * * *"]` in `wrangler.jsonc`
and redeploy. Return it to `["0 * * * *"]` afterward. Keep the schedule in the
file: redeploying can overwrite schedule changes made only in the dashboard.
One SQL update per minute is 1,440 queries/day, within Hyperdrive's currently
documented free 100,000 queries/day allowance. Other Workers and database limits
still apply; this is a small demo workload.

The deployed worker does not run database migrations. If Render's free database
expires or you replace it, update Hyperdrive's connection details and ensure the
API has applied its migrations before expecting successful runs. Failed runs are
reported to Cloudflare and tried again at the next cron invocation; no custom
automatic retry or queue is configured.

## Local verification

Start PostgreSQL with `docker compose up -d` in the repository root, then run the
API once so migrations are applied. The committed `localConnectionString` points
to that disposable local database on port 5433. It does not point to production.

From `worker/`, run:

```powershell
pnpm run dev
```

In another terminal, invoke the local scheduled handler:

```powershell
Invoke-WebRequest 'http://localhost:8787/__scheduled?cron=0+*+*+*+*'
```

Then fetch `http://localhost:5016/tires` and inspect timestamps. Use an ignored
`.dev.vars` file with `CLOUDFLARE_HYPERDRIVE_LOCAL_CONNECTION_STRING_HYPERDRIVE`
to override the local connection string if necessary. Do not use `--remote`
for a disposable local test: it can connect to the real database.

## Reference documentation

- [Cloudflare Cron Triggers](https://developers.cloudflare.com/workers/configuration/cron-triggers/)
- [Hyperdrive with node-postgres](https://developers.cloudflare.com/hyperdrive/examples/connect-to-postgres/postgres-drivers-and-libraries/node-postgres/)
- [Hyperdrive pricing](https://developers.cloudflare.com/hyperdrive/platform/pricing/)
- [Render database connections](https://render.com/docs/postgresql-creating-connecting)

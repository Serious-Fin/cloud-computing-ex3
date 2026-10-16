import { Client } from "pg";
import { runUpdate } from "./update-views.js";

// Return an exit status without printing credential-bearing driver errors.
export async function runJob({ env = process.env, ClientClass = Client, logger = console } = {}) {
  if (!env.DATABASE_URL?.trim()) {
    logger.error("DATABASE_URL is required for the tire views job.");
    return 1;
  }

  try {
    const client = new ClientClass({
      connectionString: env.DATABASE_URL,
      connectionTimeoutMillis: 10000,
      query_timeout: 20000,
      statement_timeout: 15000,
    });
    await runUpdate(client, logger);
    return 0;
  } catch {
    logger.error("Tire views job failed.");
    return 1;
  }
}

if (import.meta.main) {
  process.exitCode = await runJob();
}

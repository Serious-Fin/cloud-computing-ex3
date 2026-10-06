import { Client } from "pg";
import { runUpdate } from "./update-views.js";

export default {
  async scheduled(controller, env, ctx) {
    // Each invocation has its own client; Hyperdrive manages the origin pool.
    const client = new Client({
      connectionString: env.HYPERDRIVE.connectionString,
      connectionTimeoutMillis: 10000,
      query_timeout: 20000,
      statement_timeout: 15000,
    });
    await runUpdate(client);
  },
};

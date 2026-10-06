// Update only statistics, preserving concurrent CRUD edits and image fields.
export async function updateViews(client) {
  const result = await client.query(`
    UPDATE "Tires"
    SET "ViewsLastHour" = floor(random() * 51)::integer,
        "ViewsUpdatedAt" = CURRENT_TIMESTAMP
  `);
  return result.rowCount;
}

export async function runUpdate(client, logger = console) {
  try {
    await client.connect();
    const count = await updateViews(client);
    logger.log(`Updated simulated view counts for ${count} tires.`);
    return count;
  } catch (error) {
    // Do not log connection strings or driver errors that might contain credentials.
    logger.error("Could not update simulated tire views; the scheduled run failed.");
    throw error;
  } finally {
    await client.end();
  }
}

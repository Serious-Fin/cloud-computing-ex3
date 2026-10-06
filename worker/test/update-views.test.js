import { test } from "node:test";
import assert from "node:assert/strict";
import { runUpdate } from "../src/update-views.js";

test("scheduled database operation connects, updates, logs count, and closes", async () => {
  const events = [];
  const client = {
    async connect() { events.push("connect"); },
    async query(sql) {
      assert.match(sql, /UPDATE "Tires"/);
      assert.match(sql, /floor\(random\(\) \* 51\)/);
      assert.match(sql, /"ViewsUpdatedAt" = CURRENT_TIMESTAMP/);
      assert.doesNotMatch(sql, /"Brand"|"Price"|"ImageKey"/);
      events.push("query");
      return { rowCount: 3 };
    },
    async end() { events.push("end"); },
  };
  const logger = { log(message) { events.push(message); } };
  assert.equal(await runUpdate(client, logger), 3);
  assert.deepEqual(events, ["connect", "query", "Updated simulated view counts for 3 tires.", "end"]);
});

for (const stage of ["connect", "query"]) {
  test(`${stage} failure rejects the run and closes the client`, async () => {
    let closed = false;
    const failure = new Error("private connection details");
    const client = {
      async connect() { if (stage === "connect") throw failure; },
      async query() { throw failure; },
      async end() { closed = true; },
    };
    const messages = [];
    await assert.rejects(runUpdate(client, { error(message) { messages.push(message); } }), failure);
    assert.equal(closed, true);
    assert.equal(messages.length, 1);
    assert.doesNotMatch(messages[0], /private connection details/);
  });
}

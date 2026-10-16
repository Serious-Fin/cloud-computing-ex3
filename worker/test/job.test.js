import { test } from "node:test";
import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { runJob } from "../src/run-job.js";

for (const failureStage of [null, "construct", "connect", "query", "end"]) {
  test(`standalone job: ${failureStage ?? "success"}`, async () => {
    const events = [];
    const messages = [];
    const secret = "postgres://user:private-password@example.invalid/tires";
    const failAt = (stage) => {
      events.push(stage);
      if (failureStage === stage) throw new Error(secret);
    };
    class FakeClient {
      constructor(options) {
        assert.equal(options.connectionString, secret);
        assert.equal(options.connectionTimeoutMillis, 10000);
        assert.equal(options.query_timeout, 20000);
        assert.equal(options.statement_timeout, 15000);
        failAt("construct");
      }
      async connect() { failAt("connect"); }
      async query() { failAt("query"); return { rowCount: 4 }; }
      async end() { failAt("end"); }
    }
    const status = await runJob({
      env: { DATABASE_URL: secret }, ClientClass: FakeClient,
      logger: { log: (m) => messages.push(m), error: (m) => messages.push(m) },
    });
    assert.equal(status, failureStage ? 1 : 0);
    if (failureStage !== "construct") assert.equal(events.at(-1), "end");
    if (!failureStage) {
      assert.deepEqual(events, ["construct", "connect", "query", "end"]);
      assert.deepEqual(messages, ["Updated simulated view counts for 4 tires."]);
    }
    assert.doesNotMatch(messages.join("\n"), /private-password|example\.invalid/);
  });
}

test("entry point exits with failure when configuration is missing", () => {
  const result = spawnSync(process.execPath, ["src/run-job.js"], {
    cwd: new URL("..", import.meta.url),
    env: { ...process.env, DATABASE_URL: "" }, encoding: "utf8", timeout: 5000,
  });
  assert.equal(result.status, 1);
  assert.match(result.stderr, /DATABASE_URL is required/);
});

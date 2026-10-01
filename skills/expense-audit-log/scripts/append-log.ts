import { appendFileSync } from "node:fs";

// Expects exactly two arguments: the log file path and a single-line JSON entry.
const args = process.argv.slice(2);
if (args.length !== 2) {
  console.error("Usage: node scripts/append-log.ts <log-file-path> <json-entry>");
  process.exit(1);
}

const [logPath, entryText] = args;

// Validate the entry is actually valid JSON before writing it - a malformed
// line would silently corrupt the audit trail.
let entry: unknown;
try {
  entry = JSON.parse(entryText);
} catch {
  console.error(`Invalid JSON entry: ${entryText}`);
  process.exit(1);
}

// Stamp the entry with a server-side timestamp rather than trusting one
// supplied by the caller, so the audit trail can't be backdated.
const stamped = { ...(entry as Record<string, unknown>), logged_at: new Date().toISOString() };

appendFileSync(logPath, JSON.stringify(stamped) + "\n", "utf-8");
console.log(`Logged to ${logPath}`);

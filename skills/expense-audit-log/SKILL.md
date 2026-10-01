---
name: expense-audit-log
description: Appends a completed expense check to a persistent, append-only audit log, so every compliance decision stays traceable. Use this right after a verdict has been produced and reported to the user.
---

# Expense Audit Log

Appends one line per completed expense check to a durable, append-only audit
log, so every decision made during a conversation or a session stays
traceable afterwards. This skill only logs - it never computes or changes a
verdict, see compliance-check for that.

For worked examples, see references/examples.md.

## Steps

1. Identify the check's details: category, amount (EUR), cap, verdict, and
   the requester if known.
2. Determine the log location:
   - If a memory store is mounted (a directory under `/mnt/memory/`), log to
     `<mount>/expense-audit-log.jsonl` so the entry survives beyond this
     session.
   - Otherwise, log to `./expense-audit-log.jsonl` in the current working
     directory.
3. Build a single-line JSON object with the check's details (category,
   amount, cap, verdict, requester if known), then run:
   node scripts/append-log.ts <log-file-path> '<json-entry>'
   This appends one line and stamps it with a server-side timestamp - it
   never rewrites or deletes prior entries.
4. Confirm to the user in one short sentence that the check was logged - do
   not print the whole log file back unless asked.

## Notes

- Never edit or remove an existing log entry - this is an append-only audit
  trail, not an editable record.
- If asked to review history, read the log file directly rather than
  recomputing or recalling past verdicts from the conversation.

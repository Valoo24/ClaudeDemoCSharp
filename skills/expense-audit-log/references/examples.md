# Worked Examples

## Example 1 - Logging to a mounted memory store

A compliance-check just produced: category = equipment, amount = 184.00,
cap = 150, verdict = NEEDS_MANAGER_APPROVAL. A memory store is mounted at
`/mnt/memory/expense-log/`.

1. Log path = `/mnt/memory/expense-log/expense-audit-log.jsonl`.
2. Run:
   `node scripts/append-log.ts /mnt/memory/expense-log/expense-audit-log.jsonl '{"category":"equipment","amount":184.00,"cap":150,"verdict":"NEEDS_MANAGER_APPROVAL"}'`
3. Response: "Logged to the audit trail."

## Example 2 - No memory store mounted

Same check, but no `/mnt/memory/` directory is present in this session.

1. Log path = `./expense-audit-log.jsonl`.
2. Run:
   `node scripts/append-log.ts ./expense-audit-log.jsonl '{"category":"meals","amount":18,"cap":25,"verdict":"APPROVED"}'`
3. Response: "Logged to the audit trail for this session."

## Notes

- Always let the script add the timestamp - never include one in the JSON
  entry yourself.
- Keep the confirmation to one short sentence; don't dump the log file
  unless the user asks to see it.

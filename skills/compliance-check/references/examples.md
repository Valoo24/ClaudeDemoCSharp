# Worked Examples

These are few-shot examples of the expected workflow: which script call to
make and how to format the final response. Follow the same structure for
every request, regardless of how it is phrased.

## Example 1 - Simple approval

User: "Check this expense: 18 EUR for meals."

1. category = meals, amount = 18.
2. Run: `node scripts/check-compliance.ts meals 18`
   Output: `{"category":"meals","cap":"25","amount":18,"verdict":"APPROVED"}`
3. Response:
   ```
   Category: meals
   Cap: 25 EUR
   Amount: 18 EUR
   Verdict: APPROVED
   ```

## Example 2 - Manager approval required

User: "Check this expense: 184.00 EUR for equipment (a monitor)."

1. category = equipment, amount = 184.00.
2. Run: `node scripts/check-compliance.ts equipment 184.00`
   Output: `{"category":"equipment","cap":"150","amount":184.00,"verdict":"NEEDS_MANAGER_APPROVAL"}`
3. Response:
   ```
   Category: equipment
   Cap: 150 EUR
   Amount: 184.00 EUR
   Verdict: NEEDS_MANAGER_APPROVAL
   ```

## Example 3 - Category with no cap

User: "Check this expense: 340 EUR train ticket, transport category."

1. category = transport, amount = 340.
2. Run: `node scripts/check-compliance.ts transport 340`
   Output: `{"category":"transport","cap":"none","amount":340,"verdict":"APPROVED"}`
3. Response:
   ```
   Category: transport
   Cap: none (must be itemized)
   Amount: 340 EUR
   Verdict: APPROVED
   ```

## Example 4 - Unrecognized category falls back to "other", over cap

User: "Check this: 75 EUR, miscellaneous purchase."

1. "miscellaneous" is not one of the five listed categories -> category = other.
2. Run: `node scripts/check-compliance.ts other 75`
   Output: `{"category":"other","cap":"50","amount":75,"verdict":"OVER_CAP"}`
3. Response:
   ```
   Category: other
   Cap: 50 EUR
   Amount: 75 EUR
   Verdict: OVER_CAP
   ```

## Notes

- Always run the script - never estimate the verdict.
- Keep the final response to exactly those four lines, no extra commentary,
  no restating of the policy table.

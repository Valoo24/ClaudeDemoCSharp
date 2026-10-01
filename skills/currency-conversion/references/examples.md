# Worked Examples

These are few-shot examples of the expected workflow. Follow the same
structure for every request, regardless of how it is phrased.

## Example 1 - Simple conversion

User: "How much is $200 in EUR?"

1. amount = 200, currency = USD.
2. Run: `node scripts/convert-currency.ts 200 USD`
   Output: `184.00`
3. Response:
   ```
   200 USD = 184.00 EUR
   ```

## Example 2 - Already in EUR

User: "Convert 45 EUR to EUR."

1. currency is already EUR -> skip the script entirely.
2. Response:
   ```
   45 EUR = 45 EUR (already in EUR, no conversion needed)
   ```

## Example 3 - Unsupported currency

User: "Convert 500 JPY to EUR."

1. JPY is not in the supported table.
2. Response:
   ```
   JPY isn't a currency I have a fixed rate for (supported: USD, GBP, CHF).
   Can you give me the amount in one of those, or directly in EUR?
   ```

## Notes

- Always run the script for a real conversion - never estimate.
- Keep the response to the single-line result, no extra commentary, unless
  the currency is unsupported (example 3).

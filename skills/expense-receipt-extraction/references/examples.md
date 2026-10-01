# Worked Examples

These describe the expected extraction behavior for a few kinds of attached
receipts.

## Example 1 - Clear, legible receipt

User attaches a photo of a restaurant receipt showing a total of "42.50 EUR"
at the bottom, from "Le Bistrot".

1. Amount and currency are both clearly legible: 42.50 EUR.
2. Description: "Le Bistrot restaurant".
3. Response:
   ```
   Amount: 42.50 EUR
   Description: Le Bistrot restaurant
   ```

## Example 2 - Foreign currency receipt

User attaches a PDF hotel invoice showing "Total: $220.00" for one night.

1. Amount and currency: 220.00 USD.
2. Description: "hotel, one night".
3. Response:
   ```
   Amount: 220.00 USD
   Description: hotel, one night
   ```
   (Conversion to EUR is not done here - that's the currency-conversion
   skill's job, run separately if needed.)

## Example 3 - Illegible total

User attaches a blurry photo where the total is not readable.

1. The total cannot be reliably read.
2. Response: "I can't make out the total on this receipt clearly enough to
   extract an amount - could you confirm it, or attach a clearer photo?"

## Notes

- Never guess an amount that isn't actually legible.
- Category is intentionally left to the expense-categorization skill.

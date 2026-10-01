---
name: expense-receipt-extraction
description: Reads an attached receipt (photo or PDF) and extracts the expense amount, currency, and a short description from it. Use this when the user attaches a receipt instead of describing the expense in text.
---

# Expense Receipt Extraction

Reads an attached receipt directly - photo or PDF - and extracts the fields a
downstream check needs: amount, currency, and a short description of what
was purchased. This skill only extracts. It does not judge compliance,
convert currency, or assign a category - pass its output to
expense-categorization (if the category isn't obvious), currency-conversion,
and compliance-check as needed.

For worked examples, see references/examples.md.

## Steps

1. Read the attached receipt directly - never ask the user to retype its
   contents.
2. Extract:
   - the total amount and its currency, exactly as printed (do not convert
     it here);
   - a short description of what was purchased (merchant name and/or the
     item(s), in a few words).
3. If the category isn't obvious from the receipt alone, leave it unset and
   say that the expense-categorization skill should be run on the extracted
   description - do not guess a category from the receipt's layout or logo
   alone.
4. If the amount or currency can't be read reliably (blurry photo, missing
   or cropped total), say so plainly instead of guessing a number.
5. Report the extracted fields in this exact form:
   ```
   Amount: <amount> <currency>
   Description: <description>
   ```

## Notes

- Never invent a value that isn't legible on the receipt.
- This skill never performs currency conversion or a compliance check
  itself - it only reads and reports what's on the receipt.

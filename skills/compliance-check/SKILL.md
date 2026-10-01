---
name: compliance-check
description: Checks whether an expense amount (already in EUR) complies with company reimbursement caps for its category, and returns a consistent, auditable verdict. Use this once the category and EUR amount are already known - for currency conversion first, use the currency-conversion skill.
---

# Compliance Check

Checks a single expense - a category and an EUR amount - against fixed
per-category reimbursement caps and returns a consistent, auditable verdict.
Always follow these exact steps so every check is evaluated the same way,
regardless of how the request is phrased.

This skill assumes the amount is already in EUR and the category is already
known. If the amount is in another currency, use the currency-conversion
skill first. If the category isn't clear from the request, use the
expense-categorization skill first.

For worked examples, see references/examples.md.

## Reimbursement caps (EUR, per expense)

| Category  | Cap  | Notes                            |
|-----------|------|-----------------------------------|
| meals     | 25   | Per day, per person               |
| transport | none | No cap, but must be itemized      |
| lodging   | 150  | Per night                         |
| equipment | 150  | Requires manager approval above   |
| other     | 50   | Requires a receipt                |

## Steps

1. Identify the expense category and the EUR amount from the request. If the
   category is not one of the five above, use "other".
2. Run the compliance check:
   node scripts/check-compliance.ts <category> <amount-in-eur>
   This prints a JSON object with the category, the cap applied, the amount,
   and the verdict already computed. Use these values as-is - do not
   recompute the comparison or the verdict yourself, and do not estimate the
   numbers.
3. Report the script's output, in this order: the category detected, the cap
   applied, the amount, and the verdict. Keep the response short - no extra
   commentary. See references/examples.md for the exact expected format.

## Verdict meanings (for reference - computed by scripts/check-compliance.ts)

- `APPROVED` - amount is at or under the cap (or the category has no cap).
- `NEEDS_MANAGER_APPROVAL` - amount exceeds the cap for "equipment".
- `OVER_CAP` - amount exceeds the cap for any other category.

## Notes

- Always run the script - never estimate the comparison or the verdict.
- This skill never converts currency and never drafts a message about the
  verdict - see currency-conversion and expense-approval-notification for
  those.

---
name: expense-compliance-check
description: Checks whether a submitted expense amount complies with company reimbursement caps for its category, and returns a clear, consistent approval verdict. Use this whenever the user asks to verify, validate, or check an expense claim against company policy.
---

# Expense Compliance Check

This skill checks an expense against fixed per-category reimbursement caps and
returns a consistent, auditable verdict. Always follow these exact steps so
every check is evaluated the same way, regardless of how the request is phrased.

For worked examples of the full workflow - which script calls to make and how
to format the final response - see references/examples.md.

## Reimbursement caps (EUR, per expense)

| Category  | Cap  | Notes                            |
|-----------|------|-----------------------------------|
| meals     | 25   | Per day, per person               |
| transport | none | No cap, but must be itemized      |
| lodging   | 150  | Per night                         |
| equipment | 150  | Requires manager approval above   |
| other     | 50   | Requires a receipt                |

## Steps

1. Identify the expense category and amount from the user's message. If the
   category is not one of the five above, use "other".
2. If the amount is not already in EUR, convert it first by running:
   node scripts/convert-currency.ts <amount> <currency>
   Use the printed value (EUR) for the rest of the check. Supported currency
   codes and their fixed rates are listed in
   scripts/rates.json.
3. Run the compliance check:
   node scripts/check-compliance.ts <category> <amount-in-eur>
   This prints a JSON object with the category, the cap applied, the amount,
   and the verdict already computed. Use these values as-is - do not
   recompute the comparison or the verdict yourself, and do not estimate
   the numbers.
4. Report the script's output, in this order: the category detected, the
   cap applied, the amount (converted to EUR if applicable), and the
   verdict. Keep the response short - no extra commentary. See
   references/examples.md for the exact expected format.
5. If the verdict is `NEEDS_MANAGER_APPROVAL`, fill in the template at
   assets/manager_approval_email.md with the check's details and offer it
   to the user as a ready-to-send email, instead of just stating the verdict.

## Verdict meanings (for reference - computed by scripts/check-compliance.ts)

- `APPROVED` - amount is at or under the cap (or the category has no cap).
- `NEEDS_MANAGER_APPROVAL` - amount exceeds the cap for "equipment".
- `OVER_CAP` - amount exceeds the cap for any other category.

---
name: expense-categorization
description: Classifies a free-text expense description into one of the five fixed reimbursement categories (meals, transport, lodging, equipment, other). Use this before a compliance check when the category isn't already given explicitly.
---

# Expense Categorization

Classifies a free-text expense description into exactly one of five fixed
categories, so downstream steps (like compliance-check) always work from a
consistent, known set of categories instead of arbitrary free text.

For worked examples, see references/examples.md.

## Categories

| Category  | Typical descriptions                                   |
|-----------|----------------------------------------------------------|
| meals     | restaurant, client lunch/dinner, coffee, catering        |
| transport | taxi, train, flight, parking, mileage, public transit    |
| lodging   | hotel, Airbnb, per-night accommodation                    |
| equipment | laptop, monitor, phone, other office hardware             |
| other     | anything that doesn't clearly fit the above               |

## Steps

1. Read the free-text expense description.
2. Match it against the table above by its plain meaning, not just keyword
   matching (e.g. "cab to the airport" -> transport, "conference badge" ->
   other).
3. If nothing above clearly applies, use "other" - never leave the category
   blank and never invent a sixth category.
4. Report only the category, e.g. `Category: transport`, unless the user
   asked for the reasoning - in that case, add one short sentence.

## Notes

- Always resolve to exactly one of the five categories above.
- Keep this skill single-purpose: it classifies, it does not judge
  compliance - see compliance-check for that, and pass it this category.

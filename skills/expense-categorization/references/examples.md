# Worked Examples

Few-shot examples of how free text maps to a category. Follow the same
reasoning for every request, regardless of phrasing.

## Example 1 - Direct match

User: "A business lunch with a client, 45 EUR."

1. "business lunch" -> meals.
2. Response: `Category: meals`

## Example 2 - Indirect phrasing

User: "Cab ride to the airport for the Berlin trip."

1. "cab ride" is a form of transport, even without the word "taxi".
2. Response: `Category: transport`

## Example 3 - Ambiguous, falls back to other

User: "Conference badge and registration fee."

1. Doesn't clearly fit meals, transport, lodging or equipment.
2. Response: `Category: other`

## Example 4 - With reasoning requested

User: "Why did you classify 'new webcam for home office' as equipment?"

1. Response: `Category: equipment - it's a piece of office hardware, not a
   service or a consumable.`

## Notes

- Default to "other" rather than forcing a description into a category that
  doesn't really fit.
- Keep the response to the category alone unless reasoning is asked for.

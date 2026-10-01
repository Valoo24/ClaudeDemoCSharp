# Worked Examples

These are few-shot examples of the expected workflow: which template to
pick, and how placeholders are filled from the verdict and the surrounding
conversation.

## Example 1 - Manager approval required

Input (from compliance-check): category = equipment, cap = 150, amount =
184.00, verdict = NEEDS_MANAGER_APPROVAL. Context known from the
conversation: requester = Benjamin, manager = Patrick, description = "a
monitor for the home office".

1. Verdict is NEEDS_MANAGER_APPROVAL -> use assets/manager_approval_email.md.
2. Fill placeholders: {{category}} = equipment, {{amount}} = 184.00,
   {{cap}} = 150, {{manager_name}} = Patrick,
   {{description}} = "a monitor for the home office",
   {{requester_name}} = Benjamin.
3. Present the filled email, subject and body, as ready to send.

## Example 2 - Over the cap, rejected

Input: category = other, cap = 50, amount = 75, verdict = OVER_CAP.
Requester = Benjamin, description = "miscellaneous purchase".

1. Verdict is OVER_CAP -> use assets/rejection_notice_email.md.
2. Fill in the known placeholders; {{manager_name}} is not needed for this
   template.
3. Present the filled email as ready to send.

## Example 3 - Missing information

Input: category = equipment, cap = 150, amount = 184.00, verdict =
NEEDS_MANAGER_APPROVAL. No manager name given anywhere in the conversation.

1. Verdict is NEEDS_MANAGER_APPROVAL -> use assets/manager_approval_email.md.
2. {{manager_name}} is unknown - ask the user for it instead of guessing or
   leaving the placeholder unfilled.

## Notes

- Always ask for a missing placeholder value rather than inventing one.
- Do not add extra commentary beyond the filled template.

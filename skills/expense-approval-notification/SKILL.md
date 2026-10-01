---
name: expense-approval-notification
description: Drafts the right outbound message for an already-computed expense verdict - a manager-approval request for NEEDS_MANAGER_APPROVAL, a confirmation for APPROVED, or a rejection notice for OVER_CAP. Use this once a verdict has already been produced, typically by the compliance-check skill.
---

# Expense Approval Notification

Turns an already-computed expense verdict into the right outbound
communication. This skill does not compute verdicts itself - it expects
category, cap, amount and verdict as input (typically the output of the
compliance-check skill), and only handles how that verdict is communicated.

For worked examples, see references/examples.md.

## Steps

1. Identify the category, cap, amount and verdict from the input.
2. Pick the template based on the verdict:
   - `NEEDS_MANAGER_APPROVAL` -> assets/manager_approval_email.md
   - `OVER_CAP` -> assets/rejection_notice_email.md
   - `APPROVED` -> assets/approval_confirmation_email.md
3. Fill in the template's placeholders with the check's details. If a
   placeholder's value wasn't given (manager name, requester name,
   description), ask the user for it rather than inventing one.
4. Present the filled message as ready to send, keeping the template's own
   format (subject line + body for an email).

## Notes

- Never invent a name, description, or detail that wasn't provided.
- Keep the message in the template's own tone - do not add commentary beyond
  what the template already asks for.
- This skill never recomputes or second-guesses the verdict it was given -
  see compliance-check for that.

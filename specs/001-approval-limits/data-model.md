# Data model: Approval limits by role

## Invoice

Existing fields stay: id, customer name, amount, status, created time, decided by, decided at, decision note.

Status values stay draft, pending approval, approved, and rejected. No new status.

Added for story 2:

- **Approvals**: ordered list of approval records. Empty for a single-approver decision. One entry after the first approval above 500,000. Two entries when a different director completes it.

## Approval record

- **Username**: the signed-in name, stored as given. Compared with exact ordinal match.
- **Role**: `Manager` or `Director` at the time of the approval.
- **At**: the time that approval was recorded.

## Validation

- Amount must already be greater than zero. That rule exists on create.
- Approve only while status is pending approval.
- Manager single approval: amount <= 50,000.
- Director single approval: amount <= 500,000.
- Above 500,000: first record may be Manager or Director. Second record must be a different username and role Director.
- A manager on an amount from 50,000.01 through 500,000 adds nothing to Approvals.

## State

1. Draft, submitted, becomes pending with an empty approval list.
2. Pending and within the caller's single-approver ceiling: status becomes approved, decided by is that person. The approval list stays empty.
3. Pending and above 500,000 with an empty list: append one record, status stays pending, decided by stays empty.
4. Pending with one record: a valid second director appends the second record, status becomes approved, decided by is the second person.
5. Any refusal leaves every field, including the approval list, as it was.

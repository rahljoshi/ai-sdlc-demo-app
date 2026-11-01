# Contract: Approve invoice

## Request

`POST /api/approvals/{invoiceId}/approve`

No body. The signed-in session supplies the username and the role.

Roles allowed through the gate: Approver, Admin, Manager, Director. Amount rules are not decided at the gate.

## Success

`200` with the invoice.

- Within a single-approver ceiling: status `Approved`, `decidedBy` set to the username, `decidedAt` set, `approvals` empty.
- First approval above 500,000: status `PendingApproval`, `decidedBy` empty, `approvals` has one record (`username`, `role`, `at`).
- Second, different director above 500,000: status `Approved`, `decidedBy` is the second username, `approvals` has two records.

## Refusal

`400` with `{ "error": "<reason>" }`.

The reason states what was wrong and that nothing was changed. The stored invoice matches the invoice from before the call.

Reasons:

- The role cannot approve invoices.
- A manager is above 50,000 on an amount that is not in the two-approver band, or, in story 1, any caller is above their own ceiling.
- The same user tried to approve twice.
- The second approver is not a director.

## Unchanged conflicts

- Unknown invoice: `404`.
- Invoice is not pending: `409`.
- Rejecting an invoice on purpose stays `POST /api/approvals/{invoiceId}/reject` with a reason. That path is not a refused approval.

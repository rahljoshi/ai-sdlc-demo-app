---
name: approval-limits
description: Applies invoice approval ceilings and the two-approver rule in InvoiceApp.Domain. Use when editing ApprovalService, approval tests, the approvals API, or any invoice amount check.
---

# Approval limits

Read `AGENTS.md` before editing. Implement only the step the user named.

## Where the rule lives

- Put the decision in `backend/src/InvoiceApp.Domain/ApprovalService.cs`.
- The controller passes the signed-in username and role. It does not compare amounts.
- Keep `Approver` and `Admin` able to call the endpoint. The domain denies them.

## Rules

- Manager: approve when amount is at most 50,000.
- Director: approve alone when amount is at most 500,000.
- Above 500,000: record the first Manager or Director, leave status `PendingApproval`, and approve only when a different Director approves second.
- From 50,000.01 through 500,000, a Manager is denied. Do not record that attempt as a first approver.
- Same username, ordinal match, cannot be both approvers.
- On every denial: throw `ArgumentException` with a reason that says nothing was changed. Do not assign status, decision fields, or a new approver, and do not call `Update`.

## Tests

Run `dotnet test backend/InvoiceApp.sln` from the repository root. A denial test must assert the invoice fields from before the call, not only the exception type.

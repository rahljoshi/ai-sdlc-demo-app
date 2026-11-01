---
name: test-review
description: Reviews generated invoice-approval unit tests against the real failure cases. Use after tests are written for ApprovalService and before they are committed.
---

# Test review

Read each new test and reject it when any of these are missing.

- A denial test stores status, decision fields, and recorded approvers before the call, then asserts those same values afterwards.
- Manager 50,000 succeeds and 50,000.01 fails with an empty approver list.
- The same user as first and second approver leaves exactly one recorded approver and status `PendingApproval`.
- A second approver who is not a Director leaves the first approver in place.
- Assertions name the reason text, not only the exception type.
- Tests call `ApprovalService` directly with `FakeInvoiceRepository`. They do not move the rule into the controller to make a test pass.

Report one line per test: keep, or rewrite, with the rule it fails to protect.

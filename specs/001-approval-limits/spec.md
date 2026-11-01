# Feature Specification: Approval limits by role

**Feature Branch**: `feature/approval-limits-by-role`

**Created**: 2026-11-02

**Status**: Draft

**Input**: User description: "Add approval limits by role. A Manager may approve invoices up to 50,000. A Director may approve invoices up to 500,000. Anything above 500,000 needs two distinct approvers, and the second approver must be a director. The same user may never count as both approvers on one invoice. A rejected attempt returns a clear reason, and nothing about the invoice changes."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Approve within a role ceiling (Priority: P1)

A manager can approve a pending invoice of 50,000 or less. A director can approve a pending invoice of 500,000 or less on their own. An attempt past that person's ceiling is refused, the person is told why, and the invoice is still pending with no decision recorded.

**Why this priority**: Most invoices fall under a single ceiling. Shipping this alone already stops a manager from approving a large invoice.

**Independent Test**: Sign in as a manager, submit a pending invoice of 50,000, and approve it. Repeat with 50,000.01 and confirm the invoice is unchanged. Repeat the ceiling check for a director at 500,000 and 500,000.01.

**Acceptance Scenarios**:

1. **Given** a pending invoice of 50,000, **When** a manager approves it, **Then** the invoice is approved and records that manager.
2. **Given** a pending invoice of 50,000.01, **When** a manager approves it, **Then** the attempt is refused with a reason, and the status, decision, and approver list are unchanged.
3. **Given** a pending invoice of 500,000, **When** a director approves it, **Then** the invoice is approved by that director alone.
4. **Given** a pending invoice of any amount, **When** an approver or an admin tries to approve it, **Then** the attempt is refused with a reason and the invoice is unchanged.
5. **Given** a manager is signed in, **When** they approve an invoice of 50,001 through the approval screen, **Then** the screen shows the reason and the invoice is still pending.

---

### User Story 2 - Two approvers above 500,000 (Priority: P2)

An invoice above 500,000 is not approved by one person. The first manager or director is recorded and the invoice stays pending. A second, different director completes the approval. The same person cannot supply both approvals. A second person who is not a director is refused, and the first approval remains.

**Why this priority**: This is the control for the largest invoices. It depends on the ceilings from story 1, and it is the case an automated change is most likely to get wrong.

**Independent Test**: Record one approver on an invoice of 500,000.01 and show it is still pending. Repeat the attempt as the same person and as a different manager, and show the first record is untouched. Then complete it with a different director.

**Acceptance Scenarios**:

1. **Given** a pending invoice above 500,000 with no approver yet, **When** a manager or a director approves it, **Then** that person is recorded and the invoice stays pending.
2. **Given** one approver is already recorded, **When** a different director approves it, **Then** the invoice is approved and both people are recorded.
3. **Given** one approver is already recorded, **When** that same person approves again, **Then** the attempt is refused, the invoice stays pending, and exactly one approver remains.
4. **Given** one approver is already recorded, **When** a different manager approves, **Then** the attempt is refused and the first approver is still the only one recorded.
5. **Given** a pending invoice from 50,000.01 through 500,000, **When** a manager approves it, **Then** this is a refusal, not the start of a two-person approval.

---

### Edge Cases

- 50,000 and 500,000 are allowed for the role that owns that ceiling. One cent over is not.
- A second attempt by the same person must not add a second record, clear the first record, or set a decision time.
- A refusal must not change customer, amount, status, decision note, decision time, or the approver already stored.
- An invoice that is still a draft, already approved, or already rejected cannot be approved.
- The first approver on an invoice above 500,000 may be a manager. Only the second must be a director.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A manager must be able to approve a pending invoice when the amount is at most 50,000.
- **FR-002**: A director must be able to approve a pending invoice alone when the amount is at most 500,000.
- **FR-003**: An invoice above 500,000 must stay pending after the first approval and become approved only after a second, different director approves.
- **FR-004**: The same person must never count as both approvers on one invoice.
- **FR-005**: A refused attempt must return a clear reason and leave the invoice unchanged.
- **FR-006**: A manager must be refused for amounts from 50,000.01 through 500,000, and that refusal must not be stored as a first approval.
- **FR-007**: People whose role is approver or admin must be refused if they try to approve, and the invoice must stay unchanged.
- **FR-008**: The approval screen must show the refusal reason for an over-limit attempt, and the invoice must remain pending.

### Key Entities

- **Invoice**: A customer, an amount, and a status of draft, pending, approved, or rejected. It may also hold a decision and the people who approved it.
- **Approver**: A signed-in person with one role: manager, director, approver, or admin.
- **Approval record**: One person and role attached to an invoice that needs two approvers. Order matters: the second record must be a different director.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In a trial of the boundary amounts, 50,000 and 500,000 are approved by the matching role, and 50,000.01 and 500,000.01 are not approved by one person.
- **SC-002**: In every refused attempt, zero invoice fields change and the person can read the reason without leaving the approval screen.
- **SC-003**: For an invoice above 500,000, two different people are required, the second is a director, and a repeated attempt by the first person still leaves exactly one approval on the invoice.
- **SC-004**: A person can see the result of an over-limit approval on the approval screen in one attempt, without a second tool or a refresh by hand.

## Assumptions

- Amounts are in a single currency. There is no conversion.
- "Up to" includes the stated amount.
- Usernames are compared exactly, including letter case.
- Existing sign-in is reused. Manager and director are additional demo people, not a new sign-in system.
- Approver and admin keep their current sign-in. They do not gain an approval ceiling.
- A rejected business attempt is a refused approval, not the existing "reject invoice with a reason" action. That action still rejects a pending invoice on purpose.
- Bulk approval and partial-amount approval are out of scope.
- The first of the two approvers on an invoice above 500,000 may be a manager or a director.

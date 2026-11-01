---

description: "Task list for approval limits by role"
---

# Tasks: Approval limits by role

**Input**: Design documents from `/specs/001-approval-limits/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required. The spec's acceptance scenarios are the tests, including both negative rules.

**Organization**: Story 1 is the single-approver MVP. Story 2 is the two-approver rule and must be a later change.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2)
- Include exact file paths in descriptions

## Path Conventions

- Domain and API: `backend/src/`
- Unit tests: `backend/tests/InvoiceApp.Tests/`
- End-to-end: `e2e/`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the existing app is the base. Do not create a new project.

- [ ] T001 Confirm `dotnet test backend/InvoiceApp.sln` passes on the unchanged app
- [ ] T002 Record the Stryker 4.13.0 score from `backend` with `dotnet stryker --solution InvoiceApp.sln --project src/InvoiceApp.Domain/InvoiceApp.Domain.csproj` in `docs/mutation/before.txt`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Let manager and director reach the existing approval endpoint. No amount rule yet.

- [ ] T003 Add demo users `demo.manager` (Manager), `demo.director` (Director), and `demo.director2` (Director) in `backend/src/InvoiceApp.Api/appsettings.json`, password `ChangeMe123!`
- [ ] T004 Allow roles Approver, Admin, Manager, and Director on the approvals gate in `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`
- [ ] T005 Pass the session role into `ApprovalService.Approve` from `backend/src/InvoiceApp.Api/Controllers/ApprovalsController.cs`

**Checkpoint**: Existing lifecycle tests still pass once they send a role. No ceiling behaviour yet.

---

## Phase 3: User Story 1 - Approve within a role ceiling (Priority: P1) MVP

**Goal**: A manager approves up to 50,000. A director approves alone up to 500,000. Anything above the caller's ceiling is refused and the invoice is unchanged. Above 500,000 is still a refusal in this story.

**Independent Test**: `dotnet test backend/InvoiceApp.sln` covers 50,000, 50,000.01, 500,000, 500,000.01, and a non-approval role. Each refusal asserts status, decided-by, decided-at, and decision note.

### Tests for User Story 1

- [ ] T006 [US1] Add ceiling and unchanged-invoice tests in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` for manager 50,000 and 50,000.01, director 500,000 and 500,000.01, and roles Approver and Admin
- [ ] T007 [US1] Review those tests against `.cursor/skills/test-review/SKILL.md` before implementing the ceiling

### Implementation for User Story 1

- [ ] T008 [US1] Enforce the inclusive ceilings in `backend/src/InvoiceApp.Domain/ApprovalService.cs`. Throw `ArgumentException` with a reason that names the ceiling and says nothing was changed. Do not assign fields and do not call update on that path
- [ ] T009 [US1] Update the existing approve calls in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` to pass role Manager for the 100 amount invoices
- [ ] T010 [P] [US1] Document the new demo users in `README.md`

**Checkpoint**: Story 1 is one commit. Story 2 is not in that commit. `dotnet test backend/InvoiceApp.sln` passes.

---

## Phase 4: User Story 2 - Two approvers above 500,000 (Priority: P2)

**Goal**: An amount above 500,000 records the first manager or director and stays pending. A different director completes it. The same user, or a second manager, is refused and the first record stays.

**Independent Test**: Unit tests for one stamp, the same user, a second manager, and two different directors. The manager 50,000.01 test still shows an empty approval list.

### Tests for User Story 2

- [ ] T011 [US2] Add two-approver tests in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs` for the first stamp, the same username, a non-director second approver, and two different directors. Assert the approval list length and that decision fields stay empty on refusal
- [ ] T012 [US2] Review those tests against `.cursor/skills/test-review/SKILL.md` before changing the above-500,000 path

### Implementation for User Story 2

- [ ] T013 [US2] Add the ordered approval records on `backend/src/InvoiceApp.Domain/Invoice.cs` with username, role, and time. The list starts empty. Quote: username exact ordinal match; second role must be Director
- [ ] T014 [US2] Change only the above-500,000 path in `backend/src/InvoiceApp.Domain/ApprovalService.cs` so the first approval stays pending and the second different director approves
- [ ] T015 [US2] Show "1 of 2 approvals recorded" on a pending row in `backend/src/InvoiceApp.Api/wwwroot/app.js`, and refresh the list after a refusal so the screen matches the stored invoice
- [ ] T016 [US2] Add a page object in `e2e/pages/invoice-app.page.ts` and one Playwright test in `e2e/tests/over-limit-approval.spec.ts` where `demo.manager` approves 50,001, the alert contains the reason, and the invoice stays pending

**Checkpoint**: Story 2 is its own commit. Both stories pass together.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Evidence for the hand-in. No new business rules.

- [ ] T017 Run Stryker 4.13.0 again from `backend` and save the console summary in `docs/mutation/after.txt`
- [ ] T018 Review the branch diff with `.cursor/skills/pr-review-verdict/SKILL.md` and record every finding as fixed, dismissed, or deferred
- [ ] T019 Write `ASSIGNMENT.md` with the before and after mutation text, the pull request link, and a place for the video link
- [ ] T020 Confirm `ai-interaction-log.jsonl` uses the timestamp, event, actor, and summary keys from `.cursor/skills/interaction-log/SKILL.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup is already true for this repository.
- Foundational role passing blocks both stories.
- User Story 2 starts only after User Story 1 is committed. Do not implement both in one change.
- Polish starts after both stories pass.

### User Story Dependencies

- **User Story 1 (P1)**: After T003-T005. No dependency on story 2. A director above 500,000 is a refusal in this story.
- **User Story 2 (P2)**: After story 1. Replaces only the above-500,000 refusal with the two-approver path.

### Within Each User Story

- Write the tests, review them, then implement.
- A refusal test must compare the invoice with its values from before the call.

### Parallel Opportunities

- T010 can be written beside T008 because it only edits `README.md`.
- T006 and T011 edit the same test file, so they are not parallel with each other.

---

## Parallel Example: User Story 1

```text
Task: "T008 Enforce ceilings in backend/src/InvoiceApp.Domain/ApprovalService.cs"
Task: "T010 Document demo users in README.md"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Finish T003-T005.
2. Finish T006-T010.
3. Stop. Run the unit tests. Commit story 1 alone.

### Incremental Delivery

1. Story 1 commit: ceilings and unchanged refusals.
2. Story 2 commit: two distinct approvers, then the Playwright over-limit test.
3. Polish: mutation after score, review verdicts, and `ASSIGNMENT.md`.

---

## Notes

- [P] tasks touch different files.
- Story 2 must not be folded into the story 1 commit.
- Amount 50,000.01 for a manager stays a hard refusal with an empty approval list.
- Same username means ordinal equality.

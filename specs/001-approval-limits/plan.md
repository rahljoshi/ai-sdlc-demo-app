# Implementation Plan: Approval limits by role

**Branch**: `feature/approval-limits-by-role` | **Date**: 2026-11-02 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-approval-limits/spec.md`

## Summary

Managers can approve a pending invoice up to 50,000. Directors can approve alone up to 500,000. Above 500,000, the first manager or director is recorded and the invoice stays pending until a different director approves. A refused attempt returns a reason and writes nothing. The decision stays in the existing approval service. Story 1 ships before story 2.

## Technical Context

**Language/Version**: C# on .NET 8

**Primary Dependencies**: ASP.NET Core 8, xUnit, Playwright

**Storage**: In-memory invoice repository already in the API project

**Testing**: `dotnet test backend/InvoiceApp.sln`, one Playwright test under `e2e/`, Stryker.NET 4.13.0 for mutation score

**Target Platform**: Local web app at http://localhost:5000

**Project Type**: Web service plus a static web UI

**Performance Goals**: A single approval decision on one invoice. No throughput target.

**Constraints**: Business rules stay in the domain project. A refusal must not change the invoice. Demo accounts only. No database.

**Scale/Scope**: One approval service, the approvals endpoint, the pending-approval screen, and the tests named in the spec.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Rules live in the domain. Pass. The plan puts the decision in `ApprovalService` and passes role from the controller.
- A denied attempt changes nothing. Pass. Denial throws before any field is assigned and before save.
- Tests protect the negative rules. Pass. Each story has unit tests that compare the invoice before and after a refusal. One end-to-end test covers the over-limit screen.
- One step at a time. Pass. Tasks for story 1 finish and are committed before story 2 starts.
- Small surface. Pass. No new status, no database, no bulk or partial approval.

Post-design re-check: the data model adds approval records on the invoice. That is the smallest way to remember the first approver without a new status. Gate still passes.

## Project Structure

### Documentation (this feature)

```text
specs/001-approval-limits/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
└── tasks.md
```

### Source Code (repository root)

```text
backend/
├── src/InvoiceApp.Domain/
│   ├── ApprovalService.cs
│   └── Invoice.cs
├── src/InvoiceApp.Api/
│   ├── Controllers/ApprovalsController.cs
│   ├── appsettings.json
│   └── wwwroot/
└── tests/InvoiceApp.Tests/ApprovalServiceTests.cs
e2e/
├── pages/
└── tests/
```

**Structure Decision**: Keep the existing three projects. Domain decides, the API passes the signed-in person, the static UI shows the reason, and xUnit plus Playwright prove it.

## Complexity Tracking

No constitution violations.

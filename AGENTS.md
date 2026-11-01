# Project conventions for AI agents

Invoice approval demo app: ASP.NET Core 8 Web API with a small static web UI.

## Layout

- `backend/src/InvoiceApp.Domain`: business rules, no web or storage code. `ApprovalService` owns the invoice lifecycle (Draft, PendingApproval, Approved, Rejected).
- `backend/src/InvoiceApp.Api`: controllers, sign-in (`Auth/`), in-memory repository, and the web UI under `wwwroot/`.
- `backend/tests/InvoiceApp.Tests`: xUnit tests, one test class per class under test, named `<ClassUnderTest>Tests`.
- `e2e`: Playwright end-to-end tests (`tests/`) and page objects (`pages/`).
- `docs/use-cases`: short briefs for features we may build.

## Conventions

- Controllers live in `Controllers/`, one per resource, route `api/[controller]`. Request bodies are `record` types declared inside the controller.
- Controllers inherit `ApiControllerBase` and use `Handle(...)` so service errors map to 404, 409 and 400.
- Every endpoint is behind sign-in. Restrict actions with `[RequireRole("...")]`: destructive actions are Admin only. The approvals endpoint allows Approver, Admin, Manager, and Director through the gate. Amount rules are not enforced by that attribute.
- Business rules go in the Domain project, never in controllers. `ApprovalService.Approve` owns approval limits.
- Register services in `Program.cs`. Repositories are interfaces in Domain with an in-memory implementation in Api.
- Every new rule or endpoint needs tests. Use `FakeInvoiceRepository` in tests.
- Use clear names; no one-letter variables or `flag` parameters.
- End-to-end tests use page objects and role-based locators (`getByRole`, `getByLabel`), never CSS or XPath selectors.

## Commands

- Run the app: `dotnet run --project backend/src/InvoiceApp.Api` then open http://localhost:5000
- Run the tests: `dotnet test backend/InvoiceApp.sln`
- Run end-to-end tests: `cd e2e && npm install && npx playwright install chromium && npm test`
- Mutation testing, from `backend`: `dotnet stryker --solution InvoiceApp.sln --project src/InvoiceApp.Domain/InvoiceApp.Domain.csproj`
- Install the mutation tool with the .NET 8 pin: `dotnet tool install -g dotnet-stryker --version 4.13.0`. A newer Stryker release targets a later runtime and will fail to install.

## Approval limits

These rules are the whole feature. Thresholds are inclusive. Compare `Invoice.Amount` as `decimal`.

- A Manager may approve a pending invoice up to 50,000.
- A Director may approve a pending invoice alone up to 500,000.
- An amount above 500,000 needs two approvers. The first may be a Manager or a Director. The invoice stays `PendingApproval` after the first approval, with that approver recorded. The second approver must be a different user and a Director. Only then does the invoice become `Approved`.
- A Manager faced with an amount from 50,000.01 through 500,000 is denied. That denial does not start the two-approver flow.
- Approver and Admin are not approval roles. An attempt by either returns a reason and does not approve the invoice.
- The same username must never count as both approvers on one invoice. Compare usernames with ordinal equality.
- A denied attempt returns a clear reason and changes nothing: status, `DecidedBy`, `DecidedAt`, `DecisionNote`, and any approver already recorded stay as they were. Do not call `Update` on that path.
- Do not land the single-approver ceilings and the two-approver rule in one change. Ceilings first, with tests. Two-approver rule second, with tests.

The tests that must exist before either change is called done:

- Manager at 50,000 approves. Manager at 50,000.01 does not, and the invoice is unchanged.
- Director at 500,000 approves alone.
- Above 500,000, one approver does not approve the invoice.
- The same user cannot be both approvers, and the first record is still there after the denial.
- A second approver who is not a Director is denied, and the first record is still there.
- One Playwright test: a Manager approves an invoice over 50,000 through the UI, sees the reason, and the invoice is still pending.

## Review expectations

Review the diff against the rules above, not against how complete the code looks. For every finding, record one verdict: fixed, dismissed with a reason, or deferred with a reason. A test that does not fail when the invoice is mutated on a denied attempt is a finding, not a passing suite.

## Demo accounts (not real credentials)

- `demo.approver` / `ChangeMe123!` (role Approver)
- `demo.admin` / `ChangeMe123!` (role Admin)
- `demo.manager` / `ChangeMe123!` (role Manager)
- `demo.director` / `ChangeMe123!` (role Director)
- `demo.director2` / `ChangeMe123!` (role Director)

## Never

- Commit real credentials, tokens or customer data.
- Add business logic to `Program.cs` or controllers.

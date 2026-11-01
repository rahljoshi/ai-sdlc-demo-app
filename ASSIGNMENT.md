# Assignment write-up

Feature: approval limits by role, on a fork of `NITIN1670/ai-sdlc-demo-app`.

Pull request: see the open pull request on `rahljoshi/ai-sdlc-demo-app` for branch `feature/approval-limits-by-role`. The link is filled in after the pull request is opened.

Video: record this, upload it, and replace this sentence with the link. The recording is 8 to 10 minutes and is the only hand-in item left.

## What was built

- A manager can approve a pending invoice up to 50,000, inclusive.
- A director can approve a pending invoice alone up to 500,000, inclusive.
- Above 500,000, the first manager or director is recorded and the invoice stays pending. A different director must approve second.
- The same username cannot be both approvers. The check still holds when another approver is already on the invoice.
- A manager between 50,000.01 and 500,000 is refused. That refusal does not start the two-approver flow.
- Approver and Admin are refused. They do not gain a ceiling.
- Every refusal returns a reason and leaves status, decision fields, amount, customer, and recorded approvers as they were.

## Why the context mattered

`AGENTS.md` is the cold-start copy of those rules, the layering, the test command, the Stryker 4.13.0 pin, and the review bar. The numbers are not only in a chat. Story 1 and story 2 are separate commits on purpose: the first refuses a director above 500,000, and the second changes only that path. The tests that would still pass if a refusal wrote the invoice, or if one person counted twice, are called out in the test-review skill.

## Specification

GitHub Spec Kit produced the feature directory `specs/001-approval-limits/`. The specification, plan, and task list are there. Story 1 is the single-approver ceiling. Story 2 is the two-approver rule. The constitution in `.specify/memory/constitution.md` matches the same rules.

## Commits

1. `docs: record approval rules and the agent review bar`
2. `docs: add the Spec Kit specification for approval limits`
3. `feat: refuse an approval that is past the caller ceiling`
4. `feat: require two distinct approvers above 500000`
5. `test: reject the same user even when another approver is recorded`

Each of 3, 4, and 5 stands on its own. 3 does not contain the two-approver rule. 4 adds it, with the Playwright test. 5 is the review fix for a same-user hole that a one-person list did not catch.

## Tests

Unit tests are in `backend/tests/InvoiceApp.Tests/ApprovalServiceTests.cs`. They cover both ceilings, one cent over each ceiling, Approver and Admin, the first stamp above 500,000, the same user, a second manager, two directors, a manager then a director, and the same user when another approver is already recorded. Refusal tests assert the invoice fields, not only the exception type.

The Playwright test is `e2e/tests/over-limit-approval.spec.ts`. A manager creates an invoice for 50,001, submits it, approves it, sees the reason, and the invoice is still pending. Locators are role and label based, through `e2e/pages/invoice-app.page.ts`.

Run them with:

```text
dotnet test backend/InvoiceApp.sln
cd e2e
npm install
npx playwright install chromium
npm test
```

## Mutation scores

Tool: `dotnet tool install -g dotnet-stryker --version 4.13.0`

Same command both times, from `backend`:

```text
dotnet stryker --solution InvoiceApp.sln --project src/InvoiceApp.Domain/InvoiceApp.Domain.csproj
```

### Before

```text
Stryker.NET 4.13.0
Command (from backend): dotnet stryker --solution InvoiceApp.sln --project src/InvoiceApp.Domain/InvoiceApp.Domain.csproj
When: before approval-limit changes. Upstream suite only, 7 tests passed.
Final mutation score: 30.77%

ApprovalService.cs: Killed=12, Survived=8, Ignored=7
DiscountCalculator.cs: NoCoverage=17, Ignored=5
Invoice.cs: Survived=2

Score is killed divided by killed + survived + no coverage. Ignored mutants are excluded. DiscountCalculator has no tests, which holds the project score down before the feature exists.
```

### After

```text
Stryker.NET 4.13.0
Command (from backend): dotnet stryker --solution InvoiceApp.sln --project src/InvoiceApp.Domain/InvoiceApp.Domain.csproj
When: after the approval-limit rules and tests, including the same-user case with another approver already recorded. 18 unit tests.
Final mutation score: 57.53%

ApprovalService.cs: Killed=42, Survived=10, Ignored=14, CompileError=2
DiscountCalculator.cs: NoCoverage=17, Ignored=5
Invoice.cs: Survived=4

Score is killed divided by killed + survived + no coverage. Ignored mutants and compile errors are excluded. DiscountCalculator is unchanged and still has no tests, so it keeps the project score below the ApprovalService result. ApprovalService killed mutants rose from 12 before the feature to 42 after it.
```

The copies also live in `docs/mutation/before.txt` and `docs/mutation/after.txt`.

## Review

The workshop did not include a separate review prompt. The review used `.cursor/skills/pr-review-verdict/SKILL.md` on the branch diff, then checked the Stryker survivors. Verdicts are also in the pull request description.

- Finding: the same-user check was only proven when the invoice had one recorded approver. Changing `Any` to `All` still passed. Evidence: Stryker survivor on `ApprovalService` before the extra test. Verdict: fixed. Test `Approve_SameUserIsBlockedEvenWhenAnotherApproverIsRecorded`.
- Finding: the successful two-director path has no end-to-end test. Verdict: deferred. The assignment asks for one UI test, the rejected over-limit approval. Unit tests cover two directors.
- Finding: Approver and Admin get a 400 from the domain instead of a 403 at the gate. Verdict: dismissed. The reason has to be readable. The role attribute is only the gate, which `AGENTS.md` already says.
- Finding: deleting the repository `Update` call still passes the tests. Verdict: dismissed. The service edits the instance the repository already stores, so the field assertions are the behaviour that matters. A refusal still assigns nothing before it throws.
- Finding: usernames are compared with exact case. Verdict: dismissed. The specification says ordinal equality.
- Finding: `DiscountCalculator` is still uncovered, so the project score is 57.53% rather than a score for `ApprovalService` alone. Verdict: deferred. It is outside this feature, and both scores use the same command.

## Agents, skills, and the log

Project skills are under `.cursor/skills/`: approval limits, test review, pull-request verdicts, the interaction log, and the Spec Kit skills. Subagent briefs are `.cursor/agents/approval-implementer.md`, `test-reviewer.md`, and `pr-reviewer.md`. The hook `.cursor/hooks/log-interaction.js` appends one JSON line, with the keys `timestamp`, `event`, `actor`, and `summary`, to `ai-interaction-log.jsonl`.

## What is left for you

Record the demo. Something must run on screen. A file tour is not enough.

1. Show the two feature commits and the ceilings in `AGENTS.md` (about two minutes).
2. Run `dotnet test backend/InvoiceApp.sln` and point at the same-user test and an unchanged-invoice test (about two minutes).
3. From `backend`, run `dotnet stryker --solution InvoiceApp.sln --project src/InvoiceApp.Domain/InvoiceApp.Domain.csproj` (about three minutes).
4. From `e2e`, run `npm run test:headed` and show the manager, the 50,001 invoice, the reason, and the invoice still pending (about two minutes).

Use Teams, meeting only yourself, and press Record. Or use any recorder that writes an MP4. Upload it, replace the video sentence at the top of this file, and push.

# Research: Approval limits by role

## Decision: Keep the decision in ApprovalService

**Rationale**: The domain project already owns submit, approve, and reject. The controller already maps `ArgumentException` to a 400 response whose `error` text the page shows.

**Alternatives considered**: Checking the amount in the controller. Rejected because `AGENTS.md` forbids business rules there, and a controller check is easy to bypass with a direct service call.

## Decision: Refusal is an ArgumentException before any write

**Rationale**: The existing error mapper returns 400 for `ArgumentException` and 409 for a status conflict. A policy refusal is a bad request, not a missing invoice. Throwing before assigning fields means the in-memory object cannot change by accident.

**Alternatives considered**: Returning a result object. Rejected because every current decision uses exceptions, and a second style would split the tests.

## Decision: Amounts above 500,000 stay PendingApproval

**Rationale**: The spec does not ask for a new status. A list of approval records on the invoice remembers the first person while the status stays pending.

**Alternatives considered**: A PartiallyApproved status. Rejected because the UI, filters, and existing tests would all grow for no stated requirement.

## Decision: Inclusive ceilings and exact username match

**Rationale**: "Up to 50,000" and "up to 500,000" include those amounts. Usernames are compared exactly so `demo.director` and `demo.Director` are different people.

**Alternatives considered**: Treating a higher role as able to ignore the two-person rule above 500,000. Rejected because the spec says anything above 500,000 needs two distinct approvers.

## Decision: Approver and Admin are not given a ceiling

**Rationale**: The assignment names only manager and director. Letting admin approve any amount would invent a third rule. They can still sign in, and admin can still delete.

**Alternatives considered**: Mapping admin to director. Rejected because that is not in the assignment.

## Decision: Story 1 does not record a first approver above 500,000

**Rationale**: The assignment asks for two commits. Story 1 refuses every amount above the caller's ceiling, including a director above 500,000. Story 2 changes only that path.

**Alternatives considered**: Building both rules together. Rejected because the assignment forbids one prompt for the whole feature.

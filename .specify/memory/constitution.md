# Invoice Approval Constitution

## Core Principles

### I. Rules live in the domain

Invoice decisions belong in the approval service. Controllers pass the signed-in person and role. They do not compare amounts.

### II. A denied attempt changes nothing

If an approval is refused, the invoice status, decision fields, and any approver already recorded stay as they were. The person receives a clear reason.

### III. Tests protect the negative rules

Every ceiling and every refusal has a test. A test that only checks the exception type, and not the unchanged invoice, is not enough.

### IV. One step at a time

The single-approver ceilings land first, with tests. The two-approver rule lands second, with tests. They are not one change.

### V. Small surface

Use the existing sign-in, in-memory store, and web UI. Do not add a database, a new invoice status, or the other use cases.

## Review

Every review finding is fixed, dismissed with a reason, or deferred with a reason.

## Governance

This constitution guides the approval-limits feature. `AGENTS.md` is the agent-facing copy of the same rules.

**Version**: 1.0.0 | **Ratified**: 2026-11-02 | **Last Amended**: 2026-11-02

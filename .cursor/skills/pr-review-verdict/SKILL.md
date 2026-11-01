---
name: pr-review-verdict
description: Reviews the approval-limit diff and records a verdict for every finding. Use when preparing the pull request description or judging review comments.
---

# Pull request review

The workshop prompt was not attached to the assignment. Use this prompt on the branch diff.

Review the diff against `AGENTS.md`. Look for rules that appear to pass but do not: a denied approval that still writes the invoice, the same user counted twice, a non-director accepted as the second approver, a Manager between 50,000.01 and 500,000 starting a two-approver flow, amount checks in the controller, and tests that never assert unchanged fields.

Return findings in this shape only, one finding per block:

```
Finding: <what is wrong or risky>
Evidence: <file and behaviour>
Verdict: fixed | dismissed | deferred
Reason: <required for dismissed and deferred; the change for fixed>
```

Do not skip a finding because the suite is green.

---
name: interaction-log
description: Appends one JSON line per AI interaction to ai-interaction-log.jsonl. Use when an agent or subagent starts, finishes, or records a review verdict.
---

# Interaction log

Append one line to `ai-interaction-log.jsonl` at the repository root. Same keys every time, in this order:

```json
{"timestamp":"2026-01-01T00:00:00.000Z","event":"afterAgentResponse","actor":"approval-implementer","summary":"one line"}
```

- `timestamp` is UTC ISO-8601.
- `event` is `afterAgentResponse`, `subagentStop`, `stop`, or `session`.
- `actor` is the agent name.
- `summary` is one line, at most 500 characters, with no secrets.

The project hook `.cursor/hooks/log-interaction.js` writes this same line. Do not invent a second format.

# Cosmonauts — return report template

Use this after executing one numbered tranche. Report evidence, not confidence adjectives or planned checks.

```text
Request ID:
Mode performed: IMPLEMENT
Actual Git branch/HEAD before:
Actual Git branch/HEAD after:
Working-tree changes before/after:
Scoped source snapshot method/digest, if measured:

Implemented behavior:
Changed files and purpose:
Actual new/changed APIs and persisted fields:
Feature/ruleset versions:
Compatibility behavior for legacy and previous campaign saves:

Acceptance checks:
- Test ID:
- Exact command:
- Outcome / assertion evidence:
- Log path:

Baseline failures not introduced by this tranche:
New failures or regressions:
Known unverified behavior:

Replay/checkpoint/archive/practice-branch result:
People/resource ownership and accounting result:
UI mock result:
Isolated real-window result, or explicitly NOT RUN:
Performance numbers with units, workload, runtime, and method:

Isolated manual reproduction/play steps:
Deviations from the prompt and precise reason:
Unresolved source contradictions or blockers:
Handoff file path:

Commit/push status: none, unless separately authorized by the owner
Next tranche executed: no
```

Do not report an old context-packet check as a new executed check. A mocked GUI pass is not a native-window pass. A CPU timing is not FPS, wall-clock latency, RSS, or peak memory. A zero resource residual covers only the categories and assertions actually measured.

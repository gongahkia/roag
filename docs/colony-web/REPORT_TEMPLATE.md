# Phase report template

Write a report under `docs/colony-web/reports/PXX-report.md`. For a repair attempt,
use `PXX-repair-01-report.md` and retain earlier evidence. Update STATUS.md only to
the level established by actual checks. A report is required even for partial work.

## Identity and outcome

- Phase and exact requested goal:
- Result: COMPLETE / PARTIAL / BLOCKED (report labels, not a request to change tool lifecycle rules).
- Starting commit and pre-existing local changes:
- Ending commit, or HEAD plus the actual uncommitted changed-file list/diff artifact:
- What a player can now do:
- What remains unavailable:

## Acceptance evidence

| Plan gate | Result | Actual command/scenario | Evidence path and observed outcome |
| --- | --- | --- | --- |
| Each gate from this phase | PASS / FAIL / NOT RUN | Include cwd, command, exit code, seed when relevant | Logs, state hashes, screenshots, traces or browser URL |

Distinguish commands executed from suggested commands. Include failed attempts and
their resolution if they change confidence. A test count alone is not enough: state
which player behavior and edge cases it covers. Screenshots must be opened/inspected;
capturing a file alone does not establish that the UI is readable or correct.

## Port and design decisions

| Legacy source | Retained invariant | Intentional adaptation | New test/player scenario |
| --- | --- | --- | --- |

List new assumptions, tuning, schema/content changes, and whether DECISIONS.md changed.
Call out any departure from the plan and the reason. Report numerical balance as
observed evidence, not proof of universal difficulty or fun.

## Preservation and scope

- Result of `python3 -B tools/verify_preservation.py`:
- Any added artwork/bindings and their original IDs/provenance:
- Original protected files changed: must be none; explain any unexpected failure.
- Legacy source/archive status if deletion was in scope:
- Changes outside the requested phase and why they were necessary:

## Tests, browser, saves, and performance

- Exact install/typecheck/test/build/browser commands and exits:
- Scenario seeds and command logs:
- Save/reload checkpoint, compared fields/hash and any unsupported save versions:
- Browser/platform/viewport tested; console/network failures:
- Screenshots/traces and what was visually reviewed:
- Performance workload, machine/browser, warmup/sample count, p95/max where measured:
- Download/save size, actor/job/event counts and long-run growth where relevant:
- Untested environments and remaining limitations:

Do not claim active-AI throughput from query-only fixtures or browser playability
from headless rules. Do not claim a deployed URL from local preview. If no performance
work is required in this phase, say not measured instead of inventing numbers.

## Resume and recommendation

- Exact local start command and what to click to reproduce the new behavior:
- Known defects and severity, with reproduction steps:
- Required repairs before proceeding:
- Smallest next useful goal and its acceptance checks:
- Questions requiring user decisions (omit if none):
- Files a fresh implementation session should read first:

## Paste-back block

End the final response with a self-contained 300–600 word block the user can paste
into this conversation or a planning turn of the same implementation instance:

```text
ROAG MIGRATION REPORT
Phase: PXX
Status: COMPLETE | PARTIAL | BLOCKED
Source: <commit or HEAD plus uncommitted diff location>
Report: docs/colony-web/reports/<file>.md
Player-visible result: ...
Acceptance: <each gate and evidence; distinguish NOT RUN>
Checks executed: <commands/exit codes; logs and screenshots>
Preservation: <hash check result>
Ported/changed rules: ...
Known gaps: ...
Run/reproduce: ...
Recommendation: repair PXX | proceed to PYY, with reason
Questions: none | specific decision needed
END ROAG MIGRATION REPORT
```

Do not automatically begin another phase after emitting this report. If a goal
cannot yet finish, continue authorized repairs where feasible; do not mark completion
merely to produce a tidy report or because a budget is nearly exhausted.

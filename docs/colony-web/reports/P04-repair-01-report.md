# P04 repair 01: atomic care claims on storage removal

Status: COMPLETE. Reviewed 2026-11-06. Repair commit:
`5c9609a581ab21742ca1f52665f20813a070eeff`.

Two independent read-only reviews reproduced the same problem. Dismantling a
stockpile could leave a meal or medicine reservation referring to the removed
source until the next tick. Saving or exporting in that tick failed strict save
validation. An ordinary three-day colony reproduced it at tick 10854 after
ordering stockpile 26 dismantled; no injected stock was required.

Removal now clears those supply claims and releases bed/infirmary claims in the
same transaction. Physical supplies remain in recoverable bundles; the consumer
must still collect them. Save validation was not weakened.

The isolated repair branch passed all 59 local tests, strict typecheck and
preservation of all 1,163 originals. Both supply regressions failed before the
repair. Four new regressions cover meal/medicine supply removal, claimed bed and
infirmary removal, every-tick validation, conservation, immediate JSON reload,
and exact continuation for 500 ticks. Medicine recovery consumes the two real
doses required for field rescue and clinic treatment.

Evidence was retained from the isolated worktree under
`artifacts/colony-web/P04-repair-01/` in the main workspace. The original P04 report,
53 tested source hashes and earlier evidence were checked and retained. Browser
checks were not repeated for this simulation-only repair; the integrated P05
suite must retain the existing browser journeys.

P04 is accepted with this repair. The user authorized direct multi-agent
implementation of subsequent phases, integration, and the eventual tested push
to local and remote main. This supersedes the earlier manual report/prompt
handoff between phases. Hosting selection remains pending separately.

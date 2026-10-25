# Living world implementation

Approved scope: the complete performance and simulation proposal in the conversation, tracked by `devtools/tasks/living-world.json`. Preserve the existing enemy combat design. Each implemented feature gets its own rollback-friendly commit and actual verification evidence. No test may control the user's desktop; native tests use Xvfb/private displays and dummy audio.

## Implementation order and completion evidence

1. Spatial actor index: implemented ordered tile/region/identity queries and mutation notifications. Focused tests (35), full Core (667) and seed-42 final-state comparison passed; world/scaling acceptance remains part of final verification.
2. Streaming: implemented bounded independent background generation, demand-ordered adoption, visible/travel/stair prediction, lifecycle/failure tests and a reproducible `benchmark-streaming` command. Core (676), world audits and private native checks (59 mouse / 42 rendered) passed. Later native streaming-journey stress evidence remains required for the full expansion.
3. Presentation/profile infrastructure: private-display runtime stacks/GC/counters and actual OpenGL capture/replay tooling implemented and exercised (52 workflow tests). Settled terrain reuse passed 676 Core tests, 66 mouse checks, 42 rendered assertions and complete-frame pixel equality in two scenes; moving views bypass the target. Panel/control reuse remains next; intended-hardware profiling remains distinct from local software captures.
4. Scheduled simulation: event deadlines and explicit near/far transitions, body/accounting equivalence, production/recovery continuity, deterministic saved deadlines.
5. Purposeful routines: danger, needs, work, social priorities; food/rest/work/friend targets; visible activities and interruption/resumption.
6. Production/logistics: real node extraction, carrying, delivery, storage and supply collection for construction, avoiding duplicate regional production.
7. Ecology: saved habitat pressure/food, migration and bounded recovery.
8. Consequences: food, help, interruption and livelihood memories influence trust, routines and contextual dialogue.
9. Weather/seasons: deterministic regional conditions with gameplay effects and readable UI.
10. Reusable stress/audit suite: long runs at 7/30/100 days, chunk/floor journeys, crowded 64-order work, near/far and exact interruption comparisons; bounded state/history/cache/save growth.
11. Fresh full scoped verification, isolated visual review and requirement-by-requirement completion audit.

The implementation is in progress. This file is not a completion claim. New persisted mechanics require explicit validation and documented content/save compatibility; player saves are never used as test fixtures.

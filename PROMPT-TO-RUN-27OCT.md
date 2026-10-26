/goal Complete the Roag C# migration and gameplay integration in /home/gongahkia/coding/projects/roag, satisfying every approved requirement and completion criterion in docs/MIGRATION_DECISIONS.md.

This explicitly authorizes resuming implementation and supersedes the earlier instruction to keep gameplay work paused.

First read AGENTS.md and:
1. docs/MIGRATION_DECISIONS.md — authoritative decisions, scope, delegated tuning and completion criteria.
2. docs/MIGRATION_BOSS_PROPOSALS.md — all four boss concepts are approved.
3. docs/MIGRATION.md — source identities and migration status.
4. docs/MIGRATION_OVERLAPS.md — historical comparison; later approved decisions override its unresolved alternatives.

Inspect the actual repository and existing changes before continuing. Preserve existing work and planning documents. Use the original Roag revision and sibling Vagabonds repository as references; do not modify or delete the sibling repository.

Implement the complete game in handwritten, idiomatic C# using the Vagabonds/MonoGame foundation. Preserve Roag’s systems and content subject to our explicitly approved adaptations. Replace the generated translation runtime and preliminary adapters with proper native implementations. Retire Lua/LÖVE only after verified replacements exist.

Use only Vagabonds sprites, selected through its generic universal atlas and existing mappings, with Roag’s combat readability and polish. Implement the approved world simulation, party systems, combat, survival, dungeon behavior and four additional bosses. Keep deferred expansions out of scope.

Proceed autonomously through implementation, testing, rendered gameplay verification, fixes and cleanup. Maintain a durable feature-preservation checklist, progress record and evidence paths. Continue beyond individual milestones until the complete acceptance criteria are satisfied.

You may choose, document and adjust configurable numerical tuning within the agreed mechanics. Do not reopen settled decisions or ask permission for routine engineering. If a genuinely new gameplay ambiguity or conflicting requirement appears, explain it and ask a concise question before implementing the dependent behavior. Do not silently change scope or omit difficult features.

Completion requires all agreed functionality, checked preservation coverage, passing relevant automated tests, actual rendered end-to-end gameplay verification, and a playable Linux build with reproducible launch instructions. Add Windows support if feasible and accurately distinguish built from runtime-tested support. No mandatory user-playtest approval checkpoint is required.

Do not declare completion based only on compilation, selected passing tests, screenshots, stubs or partial integration. Missing agreed features and known gameplay blockers remain unfinished work. If blocked, report the specific blocker and evidence without marking the goal complete.

Keep progress updates concise. The final report must include what was delivered, how to run it, verification evidence, documented tuning and any remaining limitations.

Read docs/MIGRATION_PROGRESS.md, docs/MIGRATION_PRESERVATION.md and docs/MIGRATION_TUNING.md alongside the decision documents. Inspect the actual code and tests, then resume unfinished work while preserving existing changes.

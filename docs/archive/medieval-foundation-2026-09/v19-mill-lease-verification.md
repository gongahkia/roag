# v19 Mill Lease verification record

## Completed behaviour

The physical browser route is public tally → cargo-hold delivery → seed-derived Mill Lease worksite. The terminal offers exactly two time-bearing resolutions after the required delivered ironwork:

- **Fit delivered ironwork:** the cargo is consumed, the Mill Lease becomes relieved, local ironwork pressure drops, and world time advances 20 minutes.
- **Take Mill Lease credit:** the cargo remains in the hold, the lease becomes owed, local pressure remains, and world time advances 35 minutes.

The implementation also repairs two slice-local correctness defects found in browser verification: the terminal validator now accepts worksite choices, and replay reconstructs an unresolved seed-derived worksite while replaying commands before a later resolution. Legacy persistence loading no longer repeatedly applies upgrade bridges that already return current state.

## Browser evidence

Two Chromium keyboard sessions completed the physical route. Fit showed `MILL LEASE RELIEVED // IRONWORK FITTED // +20 ACTION MINUTES` at world minute 49 and was reopened from saved local state. Credit showed `MILL LEASE CREDIT RECORDED // CARGO RETAINED // +35 ACTION MINUTES` at world minute 65; a reload/reopen retained minute 65, the cargo action, and no worksite prompt. Screenshots were inspected during the run, including the Credit-selection prompt and both outcome messages.

## Automated evidence

- `npx vitest run src/medieval/hearthford-worksite.test.ts` — passed, 4 tests (37.33 s). The added browser-facing route test validates both resolutions through full state validation and causal replay.
- `npx tsc --noEmit` — passed.
- `git diff --check` — passed before the external market commit; the worktree was clean immediately afterward.
- `npm run build` — TypeScript and Vite production build passed; the existing 500 kB bundle guard failed because `main-TZrYoezR.js` was 530.0 kB. No bundle-limit change was made in this slice.
- Serial focused medieval command covering worksite, world state, market, settlement trading, storage, world, and terminal presentation — 96 passed / 3 failed. Every failure was an existing `storage.test.ts` timeout (5 or 20 seconds); no assertion or contract error was reported. A representative cargo persistence test then passed in its normal budget (3.06 s). The recurring social-refusal compaction test still timed out in isolation at 22.2 s under its 20-second budget. These timing limits were not changed because they are outside the Mill Lease behaviour.

The original parallel focused run was also timeout-only under a busier desktop; it is not used as positive evidence.

# Route Reckoning implementation contract

## Scope

M2 replaces the prototype's offline wall-clock reconciliation with a persisted,
deterministic active-session clock. It deliberately does not build the future
Route Board, new institutions, global danger scaling, real-time combat, or
additional package content.

`LORE.md` defines Route Reckoning as the Jomon's sequential custody and
scheduling record rather than a universal calendar. This document makes that
fictional rule concrete without treating a real-world date, timezone, or locale
as simulation input.

## Existing behaviour being replaced

`src/engine/galaxy.ts` currently stores `createdAt`, `lastSimulatedAt`, and
`lastClockAt` as wall-clock millisecond values. `reconcileGalaxy` compares them
with `Date.now()`, increases `sectorDay`, expires generic cargo contracts, and
applies site/courier simulation ticks. `src/main.ts` calls that function while
loading a campaign, so reopening a save can change the world.

Those fields remain readable by migration code only. New canonical outcomes
must never use them. Presentation clocks, loading animations, diagnostics, and
run-record dates may continue to use runtime or wall-clock time because they do
not decide canonical game state.

## Canonical representation

`GalaxyState` version 2 stores these integer fields:

| Field | Meaning |
| --- | --- |
| `routeReckoning` | Total processed Route Reckoning marks since the campaign began. |
| `lastWorldTick` | Last completed coarse world-simulation tick. |
| `sectorDay` | Deprecated compatibility mirror: `routeReckoning / 1440`; it is not a decision input. |

One Route Reckoning cycle contains 1,440 marks. The player-facing form is
`ROUTE <cycle> · WATCH <watch> · MARK <mark>` where a watch is 60 marks and a
cycle starts at 1. It is a Jomon operational convention, not an Earth calendar.

At runtime, one 100 ms fixed active-session step requests one mark. Thus a
four-cycle Kestrel deadline lasts 5,760 active marks (9.6 active minutes). A
coarse world tick is 360 marks, so site work is not performed every rendered
frame. These rates keep an idle deadline demonstration practical without
making the terminal a real-time combat or universal escalation system.

## Fixed-step scheduler and pause rules

The browser loop uses `performance.now()` only to request fixed 100 ms steps.
It keeps a fractional runtime remainder in memory, caps a single visible-frame
catch-up at 20 steps, and discards a gap larger than that cap rather than
simulating a suspended browser. The engine receives only an integer requested
step count.

The same active elapsed duration must result in the same engine request:

```text
advance(10 × 100 ms) == advance(1 × 1000 ms)
```

The loop resets its runtime baseline and discards unprocessed elapsed time when
visibility or focus is lost. It advances only when all of the following are
true:

- a campaign and active courier are loaded;
- the document is visible and focused;
- the route is an interactive Jomon hub, sector chart, or playable level;
- no loading, restoration, migration, death transition, or transit handoff is
  in progress; and
- no explicit pause, settings, accessibility, help, contract/custody terminal,
  General Manifest, or other blocking modal is open.

Standing still in an otherwise playable scene is explicitly active. Closing the
tab, hiding it, blurring it, opening a blocking terminal, or reopening a saved
campaign does not add marks. Saves store canonical marks directly; there is no
persisted animation remainder and no reload reconciliation.

## Deterministic simulation ordering

`advanceGalaxyRouteReckoning(galaxy, marks)` processes marks in ascending
integer order. At each mark it first evaluates contract warning/expiry
boundaries, then, on a multiple of 360, runs one world tick. Site and courier
work use existing seeded generators keyed by campaign seed, tick number, and
stable entity ID. Sites are iterated by sorted ID; courier routines are
iterated by sorted courier ID. This makes a save/reload immediately before a
boundary identical to uninterrupted advancement and makes render-frame chunks
irrelevant.

The existing numeric site fields still change through their current bounded
rules. A state transition is recorded only when it crosses a meaningful
threshold: supply crisis/recovery, integrity degradation/repair, ecological
shift, construction completion/loss, or control change. Random flavour alone
does not become a Manifest entry. World events are bounded by their coarse
tick; the General Manifest retains its newest 160 entries and the existing
chronicle retains its newest 240 entries.

## Contract and package timing

Generic cargo contracts gain `deadlineReckoning`. Existing `deadlineDay`
remains a derived compatibility field during M2 and is never used to decide
expiry. M1 package terms gain the same canonical deadline. M1 lifecycle
records add Route Reckoning timestamps while retaining their day-based fields
for old saves and existing readers.

At the warning boundary (240 marks before an open or accepted sealed-package
deadline), exactly one `contractNearingExpiry` event is written. At the first
mark strictly after the deadline, `expireSealedPackageContracts` makes the one
legal terminal transition and writes its existing `contractExpired` plus
`deliveryFailed` records. Repeated steps, reloads, explicit late settlement,
and cache recovery cannot create another expiry or reward. Package seal and
custody state are not altered merely by expiry, including route-cache packages.

Generic cargo preserves its existing status and rewards. Migration supplies a
canonical deadline from the saved day value; it does not accept, deliver, fail,
or expire cargo during loading.

## General Manifest and player surface

The General Manifest becomes version 2 while reading version-1 M1 entries.
Every new entry has a stable sequence-derived ID, kind, integer
`routeReckoning`, affected entity IDs, source, structured payload, and a short
player-facing summary. M1 references and detail text remain supported.

The hub gains a small `M` General Manifest surface. It shows the current Route
Reckoning and several newest material entries, including the destination,
contract, package, or site reference where one is known. The custody terminal
continues to present its existing package flow. Both the Hub/sector status and
the canvas accessible label show current Route Reckoning. Opening this history
surface pauses the clock so reading it never consumes a deadline.

## Migration contract

`migrateGalaxy` accepts version-1 and version-2 states. Version-1 `sectorDay`
is converted once with `round(sectorDay * 1440)`; legacy `last*At` values are
retained only as deprecated compatibility data. Migration never compares them
with the current date and never calls simulation advancement.

It normalizes missing Manifest, package, cache, and deadline fields
additively. It derives each legacy deadline from its prior day field, preserves
package seal/custody/lifecycle state and site snapshots, normalizes Manifest
sequence state, and fills structured version-2 fields deterministically.
Reloading the same untouched migrated save is byte-stable after its first
normalization.

## Verification contract

Focused engine and scheduler tests cover fixed-step chunking, no offline
reconciliation, deterministic event order/IDs, migration, package warning and
expiry paths, bounded history, and a long deterministic fingerprinted soak.
Browser coverage uses Playwright's controlled clock to observe active idle
advancement, an event, the Manifest, a reload, and no hidden/offline elapsed
advance. Completion additionally requires `npm run test:autoplay:tasks`,
`npm run test:e2e`, `npm run build`, and `git diff --check`.

The cosmetic loading/text-wrap work is intentionally separate in commit
`e79b6a2`; it is not part of this simulation contract.

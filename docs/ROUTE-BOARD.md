# Route Board implementation contract

## Scope

M3 adds Jomon's first ship-scale route-selection loop. It does not replace the
existing on-foot connector resident window, generic cargo compatibility path,
or later Route Board institution/economy work. A carrier movement is an
explicit, persistent operation; the animated transit screen is presentation
only.

## Authored starter network

The `helios-intake-v1` network maps stable board destination IDs onto existing
stable galaxy-site IDs. Its five initial destinations are Kestrel Landing,
Orison Relay, Halcyon Dock, Nerida Pressure Chain, and Borealis Glassworks.
Kestrel is the migration and new-campaign starting location.

Kestrel has two open initial connections: Orison is longer and carries a lower
reported danger; Halcyon is shorter, has an elevated warning, and can produce
a seeded navigation delay. Later connections make both a return to Kestrel and
onward choices legible. A closed connection remains visible with its material
reason. Network topology and display positions are authored data; runtime state
stores only its stable network ID and campaign-specific discovery/history.

## State and migration

`GalaxyState` version 3 owns `routeBoard`. It records the network identity,
current destination, known destination IDs, unavailable route IDs, an optional
selected route, optional committed transit, resolved route history, and a
monotonic transit sequence.

`currentDestinationId` is the canonical carrier location. `activeSiteId`
remains the existing expedition-site compatibility field and is updated only
with the matching destination during initial creation, migration, and route
arrival. The invariant prevents two competing location models.

Migration accepts versions 1–3. A v1/v2 save maps its active site to a starter
destination when possible, otherwise to Kestrel; it updates the compatibility
site at the same boundary. It retains Route Reckoning, package/custody state,
caches, couriers, Manifest records, and unresolved contract state exactly. It
does not advance simulation or inspect wall-clock values. Normalization is
deterministic and idempotent.

## Commitment and resolution

Selecting a reachable connection writes a preview selection. Confirmation
creates one transit record with a stable `route-transit:<seed>:<sequence>` ID,
the base duration, and any seeded consequence already resolved and stored.
It writes `routeCommitted` and `routeDeparted` Manifest entries before the
operation is persisted. Cancel clears the preview and creates no route history
or clock change.

The presentation may pause/reload while that record exists. Resolution never
uses its display duration or wall clock. It advances canonical state by the
stored base marks, writes a meaningful stored consequence, then advances its
stored delay marks if any. Existing per-mark M2 deadline processing therefore
handles expiry at its exact mark and only once. Arrival atomically updates the
board location and expedition site, reveals that site, clears transit/preview,
appends bounded route history, and writes `routeArrived`. Repeating resolution
after arrival is a no-op.

## Information boundary and UI

The Route Board shows current location, stable topology, open and unavailable
connections, duration, reported risk, opportunity, warning, confidence, and
the active package's destination/deadline impact without exposing contents or
hidden rolls. It is a compact keyboard panel in the existing sector screen:
arrows inspect, `E`/`Enter` selects, `Enter` confirms, and `C`/`Esc` cancels or
returns. Selecting the current location instead requires an explicit landing
confirmation. Board lines encode status in words as well as colour.

The existing `M` General Manifest surface remains the sole durable-history
reader. It displays the new route entries alongside custody and site records.

## Verification

Focused tests prove deterministic topology, selection/cancellation, unreachable
rejection, chunk-invariant travel resolution, a seeded delay, deadline expiry
during transit, one-time Manifest events, migration/idempotence, and save-like
resolution continuity. Autoplay covers board commit, arrival, and deadline
consequences. Playwright travels away from Kestrel and back through the board,
checks canonical time and Manifest history, physically lands, and explicitly
settles the existing sealed package.

# Conversations, civic choices and moving buildings

The settlement remains a construction-led colony. Residents choose their own
work and visits. This tranche adds optional conversations and three open promises,
a one-time Market-day decision, free building relocation, and real sprite previews.
There are no individual orders, quests screen, dialogue parser, external AI,
new resource types or fantasy-species mechanics.

## Local play

From the repository root, if a local server is not already running:

```sh
python3 -B apps/settlement/tool/serve.py --port 8765
```

Open or hard-refresh <http://127.0.0.1:8765/settlement/>. Port 8765 already hosts
the app on the verification machine; a second server is unnecessary. The rebuilt
local archive is `artifacts/settlement/roag-settlement-web.zip`. Nothing in this
tranche is deployed or published. Test profiles use a separate loopback origin
and fresh browser storage; the player's private town is not opened or reset.

## Controls and construction art

`B` opens the existing grouped catalogue. Every buildable entry has a real runtime
composition: Cottage, Farm, Park, Workshop, Market, Community Hall, Dirt Path,
Stone Road and Garden Path. Names, prices, short purposes and the connected-path
requirement remain readable. The selection has a check and accent; the nine
choices live in a scrolling 360-pixel popup, not a permanent sidebar.

The original `1/C`, `2/P`, `3/R`, `4/G`, `5/V`, and new `6` through `0` shortcuts
retain their meanings. Drag painting, stroke budgets, Escape cancellation,
Ctrl/⌘ Z and Shift Z, pan/zoom, H and speed controls remain available. Footer
tools now use the same construction art, with the original clear icon for Remove.

`BuildArt` is shared by the world, placement ghosts and `BuildThumbnail`. Existing
frame drawing, semantic tint, Farm beds, Park benches, Market awning and road
paving were extracted without creating a second crop implementation. Thumbnails
use the already-loaded runtime sprites, nearest filtering and square native
proportions; 36-pixel catalogue images are 3× the original 12-pixel miniatures.
Cottage 0 represents the two unchanged roof variants. Roads show a connected
horizontal lane with the same mask/variant selection and surface treatment as
world roads. Thumbnails do not show fictional working residents or animation.
The existing previews now draw all current road and building kinds.

The treasury uses the audited **`godot-white-coin`** source,
`assets/godot-node-icons/white/icon_coin.png`, full rectangle **[0,0,16,16]**,
SHA-256 `9801557ba43d134a4d7ce75288ceff44b05263dd536cf533d02085b8759d0226`.
It is copied by the existing exporter, tinted gold and drawn with nearest filtering.
The source sheet/image is unchanged. Pixel-boy's CC0 credit is retained and the
app attribution now names coin alongside hammer/grid/clear. Existing Urizen crops,
palettes and compositions retain the exact provenance in the manifest and
[Tranche 05](TRANCHE_05.md). No new asset pack or species recolour was introduced.

The numerical amount, fractional accounting and grant line are unchanged. An
accessible treasury label still names coins. The icon adds no resource. The money
tooltip also identifies cumulative civic spending when there has been any.

## Free relocation

Use `I` or Alt-click to inspect a building, then **Move building · Free (M)**.
`M` also starts a move from building inspection. A short right-click on a building
offers Inspect and Move; a right-drag beyond five pixels continues to pan. Roads
stay under the normal painting tools. Right-click is a shortcut, not the only route.

Point at a destination to see the actual ghost, footprint, changed road spurs and
validity. Click to commit. Escape, tool/menu changes and focus loss cancel.
Panning/zooming can retain the selected move. After commitment, ordinary building
controls work immediately with the previous tool and a fresh pointer gesture.

Cottages, Farms, Parks, Workshops, Markets and Halls all move for **zero coins**.
The final layout uses normal placement validation after vacating the original
one-cell footprint. Occupied plots, roads, trees, out-of-bounds positions and
missing connected access are rejected. Replaceable plants remain replaceable.
Preview changes neither world, IDs, money, clock nor RNG. Final execution checks
the current world again and rejects a stale plan.

The building ID, type and cottage variant are retained; moving never allocates a
new resident or building ID. Destination ground comes from that plot. The old
site retains its ordinary removal pad. The complete before/after worlds form one
zero-charge history entry: Undo restores the original site and the destination's
former grass/scenery/trace exactly; Redo reapplies the move. Both preserve forward
clock, earnings, requests, civic choices and the ID high-water mark. No placement
refund or construction reward is issued by relocation.

A person keeps their home ID, name, body, palette, trait and past experiences.
Legal underway edges can finish with their recorded duration. The new layout
invalidates navigation once and uses the existing bounded replan queue. Moving
an occupied workplace/social venue stops invalid productive presence immediately,
before the post-edit checkpoint; visitors remain at their actual positions and
reroute, return or wait. Travel is never counted as productive work.

That immediate validation is a narrow shared correction: a moved/removed
occupied destination cannot be saved as a worker still working at its old access.
Removing the assignment reports missing work rather than an imaginary unreachable
assignment. All previous valid progress/earnings remain; a later arrival itself
still pays nothing. A moved active gathering venue ends attendance safely while
retaining event identity, awards and remaining legitimate benefits. Undo does
not restart that gathering. Global Rain/Harvest/Market-day timing is not rerolled.

## Short contextual conversations

Inspect a visible resident with `I` or Alt-click, then choose **Talk**. Repeat
clicks still cycle overlapping people. The compact scrolling panel shows the
same sprite/name/trait, current activity, one short authored line and relevant
choices. Close/Maybe later returns to inspection; **Back to building** leaves
inspection. Escape dismisses the interaction. Choosing a construction tool also
closes conversation; the canvas outside the panel and existing camera controls
remain usable. The speech director avoids the panel's screen region.

The normal world clock continues while a modeless conversation is open, unless
the player paused it. Viewing/closing lines has no action-time charge and never
calls a simulation step, assigns work, improves mood or pays money. Consequential
choices recheck the current situation at one authoritative boundary. A situation
or event that changes while the panel is open can make a stale choice unavailable.

The small catalogue has stable nodes (`hello`, `mood`, `recent`, `offer`,
`promised`, `declined`, `withdrawn`, `civic`, `civic-done`) and stable choice IDs,
condition predicates, authored lines and explicit effects. It is in `dialogue.dart`,
separate from widgets. Typical paths are greeting → mood/recent experience → need
→ promise/decline → close. Choices are evaluated only for the selected conversation.
No population-wide dialogue-tree search runs each frame.

Lines use actual records: a failed job search, cut assigned workplace, actual
productive work, completed social visit, credited gathering attendance, current
activity and happiness. Sociable wording can emphasise its real recent social
visit. No invented profession, family, belongings or relationship is assigned.
A resolved request gives a truthful later acknowledgment without issuing another
benefit merely because the player reads it.

## Optional resident promises

The player may accept a request only when the selected resident genuinely has:

| Concern | Acceptance condition | Real completion |
| --- | --- | --- |
| Suitable work | Arrived resident with an actual failed search, no assignment and no reachable available job | Their own validated productive worker tick |
| Work connection | An existing assignment with missing access or an unreachable route | Their own validated productive worker tick after regaining suitable work |
| Social space | An actual missing/cut social concern and no reachable suitable venue | Their own completed qualifying social visit |

There are at most **three open promises**. Nothing generates a request automatically.
Declining changes no authoritative state and has no penalty. An accepted promise
has a stable `request-N` ID, resident ID, need, acceptance time and status.
Inspection shows promised/helped/withdrawn state; **Locate request** centres the
relevant home or assigned workplace. The player can withdraw a promise through
conversation. There is no deadline or expiry pressure. Removing its home withdraws
an open promise; Undo restores the person but does not rewind that conversation.

A construction operation alone never completes a promise. Placing a distant or
unrelated Farm does not solve inaccessible work; an actual resident must reach
valid work. Visiting travel/arrival does not satisfy social space; service/visiting
time must qualify under the existing model.

Completion gives **+5 gratitude for 75 simulation seconds**, once per resident/need
for this run. Multiple gratitude records do not stack beyond +5. A later distinct
fulfilled need can refresh the active source. Status and exact reward expiry
persist. Resolved/withdrawn concerns are not repeatedly offered as new promises;
reloading, moving/replacing a destination, repeated conversations and construction
Undo/Redo cannot reissue that reward. An acknowledgment is available in Talk,
and the existing speech system can show the authoritative thanks cue.

The request book indexes the three need types by resident and keeps an open map
bounded to three. Only real experience hooks resolve open records. Inspection
and mood query bounded per-person records; historical requests are not searched
as dialogue trees every render frame.

## One civic choice: Market day

During an actual eligible Market day, inspect its persistent event label and
choose **Discuss Market day**. A real present vendor hosts the conversation.
The same topic can appear in a deliberately opened resident conversation. There
is no automatic popup, mandatory pause or repeated demand for attention.

- **Sponsor · 3.0 coins:** pay exactly 3 coins once, from the existing treasury.
  The next **six distinct qualifying served visitors** during that event receive
  **+5 Market hospitality for 75 simulation seconds**. This is a mood benefit;
  ordinary base trade, the normal Market-day trade bonus and vendor income stay
  unchanged. Completed eligible service is required. Already-credited daily visits
  cannot claim hospitality through an uncredited repeat transaction.
- **Let it happen normally · Free:** record the ordinary choice; all existing
  service and trade rules continue, with no hospitality benefit or charge.
- **Maybe later:** close without choosing or spending.

The panel states cost, cap, duration and the need to finish service before the
Market day ends. Insufficient funds disable sponsorship with a reason and leave
the ordinary option available. Late sponsorship can benefit fewer than six or
nobody; it does not manufacture customers. A missing vendor/destination can lapse
service; it cannot reroll the selected event or replay a fee/award.

`chooseCivic` checks the event identity, active Market day, real host, previous
choice and funds. Payment is separate from construction history; Unlimited
building does not waive it. Current-event choice/credited guest IDs and each
recipient's remaining benefit persist. A compact event-ID decision ledger retains
prior choices when later special events replace the current one. Cumulative
`civicSpent` is an accounting total, not another resource. Reopening, import,
reload and Undo cannot spend again for the same event or rewind a prior decision.

## Saves, ownership and future peoples

The existing schema-3 storage key and legacy backup/error protections remain.
All additions are optional: old towns get no fabricated requests, past choices,
rewards, charges or dialogue sessions. Building/resident IDs, existing body/name/
trait fields, money, existing participation and landscape traces retain their
meanings. Conversation node selection is transient; explicit consequences are
saved synchronously. Import still validates before replacing the current town
and monotonically rebases navigation. Existing construction history remains
session-local and clears on reload/import, as it did before this tranche.

New persisted fields are the request book/counter/status/times, civic decision
ledger/spending, current-event choice/guest set and per-resident hospitality expiry.
Their owners validate IDs, duplicate concerns/guests, active limit, times, bounded
rewards, event decisions and required spending. They are not part of world Undo.
No migration replays old days or decisions.

| Owner | Responsibility |
| --- | --- |
| `lib/model.dart` | Pure final-layout move validation; free atomic history transaction |
| `lib/colony.dart` | Actual need predicates, experience-based resolution, civic fees/participation, immediate occupancy validation, full checkpoint validation |
| `lib/resident_request.dart`, `lib/dialogue.dart` | Small request records/index and data-driven authored conversations/explicit commands |
| `lib/residents.dart`, `lib/special_event.dart`, `lib/rules.dart`, `lib/reasons.dart` | Existing actors/events plus bounded benefit state, rules and truthful thanks cue |
| `lib/build_art.dart`, `lib/build_thumbnail.dart` | Shared runtime compositions and sprite catalogue widget |
| `lib/main.dart`, `lib/settlement_game.dart` | Compact interactive panels, Move/input, ghosts, camera locating and existing speech avoidance |
| `tool/assets.py`, `assets/manifest.json`, `assets/ATTRIBUTION.txt` | Existing verified exporter plus audited coin copy/credit |
| `test/tranche06_test.dart`, `tool/tranche06_*` | Focused verification, safe fixtures, real browser/native journeys and no-choice replay |

Paths above are relative to `apps/settlement/`. Shared changes are limited to the
needed renderer extraction, move transaction, valid post-edit occupancy and new
explicit consequences. The coordinated clock, queue budget, weighted navigation,
resident atlas and off-screen simulation remain in place.

No species was implemented. Existing identity is independent of `bodyId`, palette
and trait, so it can retain stable IDs through a future people identifier. The
current appearance registry has four explicit versioned body IDs, fixed palettes
and a plain-body fallback for unknown IDs; the atlas layout assumes those four
human silhouettes. A future people requires a validated separate sprite registry,
asset/codec versioning and actual rules before behaviour changes. One-resident-per-
cottage ownership is another present simplification. Recolouring these humans
would not establish a new species. No speculative refactor was made for that work.

# Vote on items and interactive rooms

Open [the focused review](items-rooms.html). This is a **design/artwork vote**,
not an implementation or publisher. The original 89-proposal review, its
downloaded feedback, source PNGs and existing game remain unchanged.

## Open it locally

From the ROAG repository root:

```sh
python3 -m http.server 8080 --bind 127.0.0.1
```

Visit **http://localhost:8080/docs/sprite-audit/items-rooms.html** in your
browser. If a server already runs there, use that URL directly. Alternatively
open `docs/sprite-audit/items-rooms.html` as a local file: its artwork and
scripts are embedded, so the page also works offline. No LÖVE launch is needed.

Keep the same browser/origin for autosaved drafts. Download/import moves a
draft between browsers, file/HTTP origins or computers.

## What is up for review?

These **six items and eight room interactions** were broadly approved in the
earlier feedback. This page proposes the concrete behavior now that all five
requested classes have been implemented. Earlier approvals/comments are shown
on the cards; new detail votes start blank and do not erase the old decisions.

| Item / interaction | Proposed choice |
| --- | --- |
| HP offering font | Nonlethal 20% max-HP payment for one fixed boon offer |
| Ration cache | +4 HP versus a short one-hit ward |
| Cleansing flask | One carried charge to remove Burning |
| Escape scroll | One carried charge for a clear, previewed move up to three tiles |
| Return token | Paid plant + one paid, short same-room return; finite alternative to Wayfinder |
| Charm well | Temporary +2 direct attack damage versus a one-hit ward |
| Attunement forge | One exact stack + 3 gold for a same-rarity alternative |
| Supply trader | One purchase from two fixed, visible services |
| Memorial | One exact stack for immediate 35% max-HP recovery |
| Bell room | Visible three-symbol sequence; one optional reward |
| Falling fixture | Visible switch, fixed delayed cross, friendly fire |
| Route signs | Truthful coarse branch hints, preserving fog and secrets |
| Camp | Finite 25% max-HP rest versus cleanse + short ward |
| Archivist contract | Optional fixed targets and a movement constraint; abilities remain usable |

**All numbers are proposals**, not applied tuning. Each card explains the
ordinary action cost, cancel/invalid-use behavior, save state, an example and
risks. Expand the shared-proposal section for the proposed single utility slot,
interaction menu and bounded floor frequency. Comment on these shared rules
in any relevant card; they are not additional automatic approvals.

No Oracle room, Cantor class, new terrain or later boon/environment batch is
introduced. The Pilgrim stays a future boon redirect. Existing chests, drops,
classes, scheduler and player saves are untouched.

## Vote and send feedback

1. Choose a card. Read the player flow and example.
2. **Approve / Reject / Discuss** its specific design; leave requested changes.
   Optionally mark Next or Later. Clicking the same vote clears it.
3. Inspect Mixed Kenney and Standalone 1-Bit independently. Click optional
   preferred sprites, then vote on each look. A preference change clears that
   look's approval. Suggested sprites are recommendations, never default votes.
4. The combined button approves only this card and its two displayed art pools.
   It does not settle ambiguity or override a comment. You can approve a mechanic
   while rejecting its artwork.
5. Click **Download feedback**. Send **`roag-items-rooms-feedback.json`**, or
   tell me it is in Downloads. Partial reviews are valid.

Comments accept up to 4,000 characters each and remain literal text. Keyboard:
Tab/Enter operates controls; left/right arrows change cards outside form
controls. Search supports idea names and exact cells, such as `onebit:574`.

**Import this review** accepts its own downloaded files. Old idea/room/enemy
exports, stale fingerprints, invalid IDs/votes, excluded sprite preferences and
files over 2 MiB are rejected before replacing the current draft. Browser
storage is a convenience; download a backup before clearing it or changing origin.

## Artwork findings

The review checks the current **399-binding** mapping and original PNG bytes.
It contains **111 unique candidate cells** across four embedded original sheets:
108 have no current direct binding; three have an existing enemy-cue use:

- `onebit:574`: Oath Twins' link cue; also the previously approved token motif.
- `onebit:539`: revival corpse cue; also a memorial alternative.
- `onebit:915`: Alarm Keeper cue; also a bell alternative.

These uses are labelled on thumbnails and spelled out on the card. They are
not unused reservations. Your earlier exclusions remain enforced in both
looks: scroll 525, charm RPG 855, memorial 24, the six rejected RPG signs and
the six rejected RPG tent parts. The camp uses accepted Micro 117 / 1-Bit 508–509.

A fresh screenshot inspection also removed **Tiny 117**, a hammer incorrectly
included in the old potion pool. It is recorded as a visual omission, not
retroactively as your rejection. Bell alternatives have ambiguous silhouettes;
1-Bit fixture alternatives look like a chair/headstone. The cards flag these
issues and suggest no primary 1-Bit assignment for those two interactions.
Request different art or an interpretation in comments rather than assuming
the mechanic approval proves its appearance. Final assemblies and in-game
visual acceptance remain future implementation work.

## Rebuild / validate / read a returned export

```sh
make item-room-review
make item-room-review-test
node tools/item_room_feedback.js /path/to/roag-items-rooms-feedback.json
```

The builder checks the original frozen review before generating this separate
page. `--check` is read-only. Data/HTML are deterministic for the same plans,
source pixels and live mappings. Once feedback is returned, preserve this exact
proposal fingerprint alongside it; do not regenerate and reinterpret old votes
against later gameplay/source changes. The read-only importer prints votes,
comments, pending artwork and selected existing bindings; it implements nothing.

Verification: seven Node data/state checks; all 14 cards/28 art panels exercised
in isolated headless Chromium, including actual download/import, draft reload,
rejected incompatible files, preference invalidation, filters and 390px mobile
layout. Screenshots were inspected as **HTML review renders**, not game screenshots.
The old review/archive and catalogue checks pass. No LÖVE window, physical
mouse/keyboard/Wacom test or new gameplay test is claimed for this page-only change.

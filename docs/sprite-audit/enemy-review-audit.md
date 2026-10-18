# Enemy combinations: sprite audit and approval handoff

**Status: user accepted all 30 decisions on 2026-10-18.** The original review
and the source audit below are archived unchanged in meaning. The live
implementation is described in [ROAG_ENEMY_SQUADS.md](../ROAG_ENEMY_SQUADS.md);
[the verbatim approval export](enemy-combo-feedback.json) records the selected alternatives
and the guard-leash / 2–5 follower conditions. The remaining text describes
the pre-implementation review stage, not the current live binding count.
The five combinations are concepts the user asked to pursue; the assignments
and detailed interaction rules below are proposals.

## Open the review

From the repository root:

```sh
python3 -m http.server 8000 --directory docs/sprite-audit
```

Open **http://localhost:8000/enemy-review.html**. The existing catalogue and
earlier review page link to it. The new HTML also opens directly from disk;
all five source sheets, proposal data and client code are embedded. No npm
installation, LÖVE process or network service is needed to use it.

1. Choose one of the five combination tabs.
2. Read its proposed interaction and counterplay; approve/reject/discuss the
   behaviour and add conditions in the comment box.
3. For each role, compare **Mixed Kenney** and **Standalone 1-Bit**, including
   40px untrimmed previews and enlarged native art. Select a candidate, then
   vote independently on each look. A selected default is not approval.
4. Review the shared effect/identification cue and comment on readability.
5. Repeat for all groups, then **Download feedback JSON**. Send
   `roag-enemy-combo-feedback.json` back in chat with your go-ahead or changes.

There are **30 decisions**: five group behaviours, twenty actor assignments,
and five effect cues. Nothing starts approved. Discuss/reject are valid
outcomes. Comments can request another sprite or reuse of an existing enemy.
Changing a selected cell clears that assignment's earlier vote, so an approval
cannot silently move to different art. Shared selections across role proposals
produce a visible warning rather than silently treating them as unique.

Drafts survive a browser reopen at the same origin. Local files, localhost,
127.0.0.1 and different server ports can have separate browser storage;
download/import the JSON to carry feedback between them. The storage key is
separate from the earlier four sprite-review rounds and room reviews. It also
includes the proposal fingerprint. Import checks schema, source/proposal
fingerprints, IDs, duplicates, allowed choices, decisions and comment bounds
before replacing the current draft. Rejected imports leave it intact.

## What was inspected

The current audit resolves **289/289** bindings in each Kenney look. This
review adds **zero** to that live registry. The five existing source sheets
were scanned for source dimensions, correct packed/gutter stride, visible
pixels and current binding usage, then inspected as numbered contact sheets.

| Sheet | Grid / source cell | Nonempty | Referenced by either look | Unbound | Suitability |
| --- | --- | ---: | ---: | ---: | --- |
| Micro Roguelike | 16×10, 8px packed | 159 | 52 | 107 | Its complete actor silhouettes are already assigned; free cells largely contain environment, equipment and props. Do not pretend these are 107 unused characters. |
| Caves & Dungeons | 29×18, 16px, 1px gutters | 522 | 68 | 454 | Strong environment/prop source; no suitable unused complete character sheet for these ten roles. |
| 1-Bit | 49×22, 16px packed | 1,077 | 132 | 945 | Independent actor and symbol candidates for the standalone look. |
| Roguelike/RPG | 57×31, 16px, 1px gutters | 1,704 | 7 | 1,697 | Mainly environments/fixtures; no character shortlist taken from door pieces, UI or furniture. |
| Tiny Dungeon | 12×11, 16px, 1px gutters | 132 | 5 | 127 | Complete unused characters appropriate for the mixed look, already present as a checked runtime source. |

These are native cell counts, not unique mechanics. No additional packs were
downloaded. PNGs and existing licenses remain unchanged. Source provenance is
in [the earlier source record](review-art/README.md) and the
`assets/kenney/*/README.md` files. The full live usage remains in
[the catalogue](index.html), [CSV](tiles.csv), and [JSON](inventory.json).

There are **49 candidate options referencing 45 unique source cells**:
36 actor cells and nine cue cells. Every candidate has visible pixels, is
in bounds at its actual stride, and is currently unbound in **both** looks.
All ten primary actor choices are distinct within each look. Some alternatives
overlap other proposals deliberately, and the page labels that risk.

### Existing art concerns found during inspection

Resolver coverage is not semantic approval of the existing imagery. The mixed
look has 26 body silhouettes for 34 actors: eight pairs still share art,
including Thorn Warden/Hollow Regent and several player/support or regular/elite
pairs. Its Bastion Bulwark uses Micro **#30**, which visually reads as a stone
post/column rather than a complete humanoid body. That is an existing motif
assignment to flag for a separate replacement decision; this proposal does
not silently change it. The new Tiny candidates were chosen as complete
figures rather than recycling such object cells. The standalone look has
34 distinct actor cells, but those remain representative motifs, not bespoke
art that proves each enemy's gameplay role at a glance.

## Proposed actor assignments

All indices are **zero-based, row-major** source cells. Descriptions are visual
readings, not official character names. Alternatives remain proposals.

| Group | Proposed role | Mixed primary / alternatives | 1-Bit primary / alternatives |
| --- | --- | --- | --- |
| Wizard / brutes | Ward Weaver | Tiny **100** / 84 | **318** / 319 |
| Wizard / brutes | Bodyguard Brute | Tiny **110** / 122 | **270** / 125 |
| Healer / attackers | Mend Acolyte | Tiny **86** / 85 | **123** / 124 |
| Healer / attackers | Rushwing | Tiny **120** | **415** / 414 |
| Trapper / artillery | Snare Scribe | Tiny **88** / 124 | **222** / 221 |
| Trapper / artillery | Siege Wisp | Tiny **108** / 121 | **321** / 417 |
| Shield / shooter | Shield Bearer | Tiny **96** / 87 | **126** / 173 |
| Shield / shooter | Pin Archer | Tiny **87** / 124 | **78** / 224 |
| Commander / followers | Banner Captain | Tiny **98** / 99 | **172** / 225 |
| Commander / followers | Banner Follower | Tiny **97** / 124 | **174** / 223 |

Mixed characters use the existing Tiny source at its native 16px cell. This
matches the source scale already used by Hookrunner/Trapper, while other mixed
characters remain 8px Micro. Both occupy the same fixed 40px game footprint.
Padding and body size are preserved, not trimmed away to disguise differences.
At 40px, 16px art scales 2.5× with nearest sampling, so screen pixel widths
alternate. The page also shows enlarged art to inspect the original cells.
The 1-Bit previews use the current monochrome ink; Micro background keying is
applied as in the runtime. These are art/formation diagrams, not game captures.

Known identity limitations:

- Tiny #122 is a crab-like brute alternative, not a humanoid guard. Selecting
  it should also change the creature identity/readability discussion.
- The selected archer bodies have no explicit bow painted into them. A shared
  existing weapon cue is proposed; reject the body assignment if that does not
  communicate a ranged enemy clearly enough.
- Bat sprites are assigned to a **Rushwing**, not labelled as a hound.
- Figures are static native art. No attack/hurt animation, rotation set or
  newly authored frames are implied by approving a body.
- Native sheet imagery cannot by itself prove a shield blocks attacks or a
  caster is supporting another enemy. Links, facing arcs and locked attack
  outlines still need clear presentation in the later implementation.

Shared cue candidates (both looks already use 1-Bit effect symbols):

| Cue | Primary / alternative | Future presentation distinction |
| --- | --- | --- |
| Forcefield bubble | **636** / 637 | Protects brutes, not its wizard; visible link collapses on caster death |
| Healing mark | **535** / 536 | Marks one committed heal, not invulnerability |
| Snare ground mark | **715** / 677 | Visible ground trap, distinct from attack-outline geometry |
| Directional shield badge | **239** / 637 | Local frontal protection, distinct from a shared all-around forcefield |
| Rally banner | **527** / 528 | Identifies leader-dependent support |

## Existing enemy overlap and intended new behaviour

This is not a request to clone current stat blocks with a new name:

- **Cinder Mender** already heals. The review explicitly asks whether to extend
  that identity or introduce a healer with formation/retreat behaviour.
- **Crystal Bombardier** already marks area attacks. Artillery in a trap group
  needs a distinct footprint and deliberate coordination, not extra range.
- **Mire Binder** already controls space; the playable **Trapper** already
  places a snare. Enemy snares need their own visible, bounded rules.
- **Pack Chorister** already gains damage from nearby allies. Commander support
  would instead make followers depend on a living, vulnerable leader.
- **Mossbound Guard**, **Bastion Bulwark** and **Ruin Skitter** overlap guard,
  defender and fast-attacker space. Escorting, directional protection and
  group flanking are the proposed distinctions.

Encounter rules remain pending: a reachable/vulnerable wizard, bounded squad
and trap counts, readable support links, no friendly-fire exemptions, and no
replacement of the strict scheduler. No guaranteed reaction action is implied
by a placement check. The room must offer tactical routes; it cannot seal the
only approach behind invulnerable brutes. Exact numbers and archetype reuse
will follow the user's comments rather than being silently decided now.

## Build, checks and evidence

```sh
make enemy-review       # build from proposals, original sheets and live usage
make enemy-review-check # read-only freshness and candidate validation
make review-test        # five data/import/state checks, no LÖVE
make sprite-audit-check
node tests/room_review_page_test.js
```

`tools/build_enemy_review.py` checks current usage and every candidate against
the original PNGs, then embeds data/client/template into one HTML. Definitions
are in `enemy-combos.json`; the client never writes game content. Regeneration
does not overwrite browser feedback or the older review records.

Verification for this review stage:

- Five Node data/state checks passed: candidate bounds/usage, exact embedded
  source bytes, no default approvals, roundtrip comments/choices, and rejected
  malformed/incompatible imports.
- Actual **headless Chromium** checks passed using isolated browser profiles:
  all five tabs and sprite rendering, independent votes, comments, changed-art
  vote invalidation, page reload, real JSON download/import, incompatible
  import preservation, blocked-storage fallback and a 390px mobile viewport.
  Selected captures were inspected visually. No LÖVE process was launched.
- Final browser artifacts: `/var/tmp/roag-enemy-review-browser-final/`;
  earlier failures and logs remain under `/var/tmp/roag-enemy-review-browser-1*`.
  The first browser test failed when its automation tried to serialize the
  entire `window` after a storage-blocking probe; the test now returns a scalar.
  This was an automation error, not a game or page crash. A later screenshot
  review caught heading overlap behind the sticky toolbar; scroll offsets were
  corrected and the final browser run passed.
- Catalogue freshness and the existing room-review test passed. No complete
  gameplay regression or GUI game suite was rerun for this documentation-only
  stage. Runtime source/asset files and the player's saves remain unchanged.
- Automated example votes/files are synthetic test artifacts, **not user
  approval**. Physical mouse/keyboard/tablet operation remains untested.

To repeat the optional browser check, use an installed Chromium executable:

```sh
TMPDIR=/var/tmp node tests/enemy_review_page_test.js --browser /path/to/chrome /var/tmp/roag-enemy-review-check
```

**Resumption:** read the user's exported feedback and chat go-ahead, resolve
any rejected/discussed assignments or comments, then implement the accepted
combinations with meaningful scheduler, visibility, encounter-generation,
save/restore and in-game rendering checks. Do not treat elapsed time, a selected
thumbnail or synthetic browser-test choices as approval.

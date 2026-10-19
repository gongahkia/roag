# Finite items and interactive rooms

This batch implements the six items and eight interactions approved in the
[detail review](sprite-audit/items-rooms.html). Approval came from the user's
explicit message, **“i aporoveall jsut go ahead”**; the
[approval record](sprite-audit/items-rooms-approval.json) preserves that authority
and the reviewed file hashes. No browser feedback export was invented.

## Play and controls

Start a new run with `make game`. Existing saved floors are preserved: reopening
an older save does not insert extra rewards into that floor.

- Walk onto or cardinally beside a visible object, then press **E**, or click
  that object. If several objects are nearby, choose one in the free inspection
  list. The HUD offers an E prompt when urgent combat feedback is absent.
- **Up/Down** selects; **Left/Right** changes pages; **1–6** selects a row on
  the current page. Inspect a forge stack before choosing its replacement.
- **Enter** first requests confirmation; **Enter again** commits. Mouse users
  can select a row and use the visible Confirm button twice. A double-click
  starts confirmation. **Esc** cancels confirmation, then closes/backtracks.
- Inspection, selection and cancellation are free. A confirmed service uses
  one normal **100-base-time action**, scaled by actual speed. Enemies and
  previously committed attacks continue under the existing scheduler rules.
  Confirmation is refused during presentation playback.
- **U** opens the carried utility, also available from **I → Utility**.
  There is one slot and no item stacking. Replacing an occupied slot is an
  explicit confirmed exchange. Using a flask/scroll costs a normal action;
  planting and returning a token each cost one action.
- An escape scroll enters the existing directional aiming interface:
  choose a cardinal direction, **1/2/3** for distance, **Enter** to commit,
  **Esc** to cancel. Blocked or invalid use retains the charge and costs nothing.
- Gamepad: left-stick click opens nearby objects; right-stick click opens the
  utility. D-pad selects, A inspects/confirms and B cancels.
- **F1** includes these contextual controls. There is no extra inventory grid.

## Exact rules

| Feature | Implemented behavior |
| --- | --- |
| Offering font | Choose one of three saved class-eligible boon offers. Pay `ceil(20% max HP)`, minimum 1, while retaining at least 1 HP. One use; no rerolls. |
| Ration cache | Choose capped +4 HP, or a one-hit supply ward for the next three committed actions. Full-health healing is disabled. |
| Cleansing flask | One carried charge removes actual Burning and its pending tick. It does not heal. If not Burning, use is refused without consuming time/charge. |
| Escape scroll | One paid move through exactly 1–3 visible, walkable, unoccupied cardinal cells. No stairs/closed doors/creature bypass; each entered tile still triggers ordinary terrain and pickup handling. |
| Return token | Plant inside a room, then return once to its visible empty marker over a known clear same-room path of at most four steps. Leaving the room/floor forfeits a planted token. An unplanted charge can travel between stages. Independent of Wayfinder's beacon. |
| Charm well | Choose +2 damage for the next direct primary attack batch, or a one-hit ward. Conditions expire after three committed actions and replace the supply condition. Focus does not amplify a delayed echo or a generated sentry attack. |
| Attunement forge | Retire exactly one owned rarity stack and pay 3 gold for one of three fixed, different, same-rarity alternatives. Class eligibility and the run's frozen boon pool apply; incompatible choices are disabled. Other stacks and committed delayed effects remain. |
| Supply trader | Buy capped +4 HP for 2 gold, or its fixed flask (2) / escape-scroll (3) stock. One purchase per stall; no shop reroll. |
| Memorial | Retire one exact rarity stack for capped `ceil(35% max HP)` recovery. Full-health use is disabled. Automatic growth stats and pending committed effects remain; derived boon abilities update. |
| Bell sequence | Read the central plaque for a fixed three-bell order, also shown as numbered labels. Each ring is paid; a wrong ring resets progress without damage. Complete once for 5 gold. No hearing/color requirement. |
| Falling fixture | A switch commits a locked five-cell cross at +100 world time, base 4 physical damage with ordinary defenses/wards and friendly fire. See the whole cross before confirming. Impact may resolve before your next decision; no guaranteed reaction action and no new rubble collision. |
| Route sign | A free coarse direction along an existing walkable route to another room, labelled treasure, supplies or rooms/danger. It does not reveal fog, secrets or a new map route. |
| Camp | Choose capped `ceil(25% max HP)` recovery, or Burning cleanse plus a three-action one-hit ward. One use. Rest is not falsely recorded as a food pickup achievement. |
| Archivist contract | Accept a room's fixed original ordinary targets. Walk and fight inside the room for an extra 8 gold once. Actual Dash/scroll/token movement, leaving the room, or an enemy-faction kill fails the contract; normal controls and ordinary kill rewards remain. Explicit abandon is a paid action. Revived targets must be settled too. |

Contract target book markers use the existing visibility/camera path. A
nonmoving class deployment remains permitted; an actual movement ability does
not. In particular, Stormcaller's current Grounding Dash **moves**, so it fails
the contract. The review's hypothetical stationary-ground example is not an
exception to the actual class behavior.

## Generation and presentation

Each newly generated floor has at most **two distinct services**, in separate
eligible rooms; at most **one** bell/fixture assembly; and at most **one** sign.
Small/occupied rooms can omit them. Start, elite and exit rooms are excluded.
Placement is nonblocking and does not rewrite terrain, routes or existing
enemy/chest placement. It uses a derived local seed, never the combat RNG.

Both Kenney choices have 16 stable image bindings (14 concepts plus plaque
and token marker), bringing live required coverage to **415/415** in each.
Original PNG sheets remain byte-for-byte intact. Mixed Kenney uses the reviewed
RPG/Tiny/Micro motifs; standalone 1-Bit uses its own motifs. Bells are labelled
gongs; the 1-Bit fixture is an explicitly identified **falling stone slab**,
not a hanging chair. The fixture art is drawn at its impact center, separate
from its switch. Spent objects dim. Classic retains code-drawn fallbacks.

The old 399-binding review is frozen, not regenerated against new mappings.
The live [catalogue](sprite-audit/index.html) and its JSON/CSV show current use.
No new boon catalogue entries, terrains, enemies or classes belong to this batch.

## State and safe continuation

Authoritative state is inert plain data: optional `idea_rooms` version 1 nodes,
bell groups, fixed offers/trades/targets, spent flags and fixture commitments;
the player's optional one-slot `utility` and short focus counter. Existing
`roag-run/11`, content 8 and boon 3 contracts remain unchanged. Old saves lacking
this optional store remain loadable without repopulating rewards. Concrete
saved generated maps remain authoritative.

Fixture work pauses with its floor and rebases on return. Offers, bell progress,
contract state, utilities, planted tokens and expiry counters survive Continue.
Invalid versions, references, offers, marker rooms or locked fixture schedules
are rejected before activation by the existing checked-store loader. No profile
write is performed by browsing/previewing these menus.

## Verification

Run without opening LÖVE windows:

```sh
TMPDIR=/var/tmp luajit tests/run.lua
TMPDIR=/var/tmp ROAG_TEST_RANGE=555-589 luajit tests/run.lua
sh tests/idea_interaction_disk_resume.sh
make item-room-review-test sprite-ideas-test enemy-review-check surplus-audit-check
python3 tools/sprite_audit.py --check
git diff --check
```

The unmodified baseline is **554/554**. The new focused set is **35/35**;
seven write → fresh-process Continue scenarios compare complete future events,
snapshots, RNG and scheduler state through two checked save generations. A
70-floor corpus (seven biomes, five seeds, both floors) finds all 14 features,
enforces placement budgets and compares exact regeneration/restoration.
The full final gate and legal-action probe results are recorded in the
[progress log](goal/ROAG_IDEAS_IMPLEMENTATION_PROGRESS.md).

Actual `main.lua` keyboard/mouse callbacks and bounded menu draw calls are
tested using injected headless Lua graphics tables. **These are not LÖVE GUI,
rasterized screenshots, physical input or native GPU evidence.** No LÖVE
window was opened, following the user's standing request. Visual readability
and human play acceptance remain pending.

Manual follow-up when convenient: E/click inspection and two-step confirmation;
U scroll aiming/cancellation; I item button; a visible fixture's locked outline;
contract marker visibility; forge/memorial before/after abilities; save/quit
and Continue partway through bells and a planted token, in Classic and both
Kenney choices. Report seed, floor, action and selected pack on any issue.

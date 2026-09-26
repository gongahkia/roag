# Frontier working claims

Low-cover correction: the shutter and collapse `%` tiles now provide partial
cover to a body on or beside them, through the shared player/enemy projectile
query. Downward shots pass over this loose cover; walls and trees still use
their existing geometry. F inspection explains the cover rule. Previously
the shutter glyph changed without affecting this query.

Fresh frontier generation chooses one of two finite situations per region.
These are installed in the actual map, dormant until critical pressure wakes
them. Existing generated maps are not repopulated. Each has three working
charges, an initial observation turn, a marked preparation turn and a release
at the old mark. Leaving, interrupting or blocking that mark matters. Marks
use reverse/bold `!`, including reverse treatment on the courier; hidden
actors do not reveal marks. `O` reports the observed mechanism and charges.

| Region | Situation | Spatial rule and counters |
|---|---|---|
| Dunmire | Veyra Reedlock | Floods a three-cell lane; height, leaving the mark or the drying spill counters it. Water uses the shared opening/load rules. |
| Dunmire | Smouldering peat crown | Smoke and dry fuel weaken a rack before collapse. Quench, brace its linkage or use the spill. |
| Rillscar | Darrin Splitspan | Cuts a visible support twice before collapse; public connectors and stores are protected. Brace, interrupt, change levels or work the tailrace. Two recovery actions permit stair travel. |
| Rillscar | Counterweight convoy foreman | Two existing escorts can intercept harm with their actual health. Separate or defeat helpers, or work the tailrace; finite rally signals do not spawn more bodies. |
| Marlbank | Elsa Kilnmark | Ignites three marked dry cells. Water, crosswind movement, interruption or the kiln release counters the firing. |
| Marlbank | Counterweighted kiln shutters | Moves passable low cover and warns before a sweep. Reposition, guard, brace/cut the linkage or operate the release. |
| Frostmere | Tova Frostwake | Brine breaks existing ice into current, or salts unfrozen footing. Move off the sheet, prepare for wet load, interrupt or release the boom. Saltwater does not immediately refreeze. |
| Frostmere | Loaded net-haul drum | Pulls an unmoved bearer toward its wet cut; heavy loads suffer longer net drag. Leave the mark, guard or physically cut/brace its linkage. |

Shared fire, water and structural reactions can affect other actors and physical
items. Machines have material linkages at their original positions; `F` exposes
their condition and the existing finite tool actions. Critical stairs and
objective stores cannot be collapsed by the cordmaster.

Veyra, Darrin, Elsa and Tova are named claimants. One later return is possible
after withdrawal, only after a returned expedition, with an unresolved control
and one real local market lot to provision it. Their finite second appearance
has two charges. Death is permanent. The secondary contact offers `S` to settle
a living claim after witnessed material progress, costing two credits and
recording the contact's memory. Defeat, disablement or settlement leaves one
physical reward on the ground; its ID is recorded and loss does not reissue it.
Any carried stolen property is released through the common recovery reducer.
These are bounded claimants, not a general rival or succession framework.

## Evidence and limits

Tests exercise every mark/release/counter, helper damage, actual attack and
contact dispatch, cross-level water, stair movement, hidden-state suppression,
finite charges, return stock, death, property recovery and mid-warning save
validation. Sixteen seeds per frontier cover all eight definitions and valid
positions. Supported-size dialogue rendering is checked at 80×24 and 100×32.

Real 80×24 PTY: seed `elite smoke`, forced arrival near Darrin with the normal
region's other actors retained. A marked two-cut support was inspected and
braced with `F` before its due collapse. Repositioning left a later mark; three
charges exhausted and Darrin withdrew. His accompanying caller and escort
continued fighting, causing arm exposure and bruised ribs when their attacks
were crossed. The courier physically reached and worked the tailrace and quit
with terminal restoration. This was a tactical fixture, not a full expedition,
revisit or permanent-rival-death playtest. The earlier executable had one-turn
recovery and a misleading initial bolt notice; both were corrected afterward
and are covered by focused tests, not this PTY claim. The other seven situations
and the named return loop still require manual campaign coverage.

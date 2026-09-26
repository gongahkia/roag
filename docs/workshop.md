# Counted workshop fittings

This is optional work at the physical lower-deck bench, not a departure
requirement. Existing basic equipment remains sufficient to start exploring.
Stock is one or two kits of each kind, derived from the seed, preserved on
load and never replenished merely by revisiting the bench.

## Controls and ownership

Stand on `W` at (39,5,-1), press `E`, then `1`–`8` for an equipment slot or
`P` for loose kits. Direct letters or arrows/Enter select a preview. `F`
confirms; Escape returns without charging. Mouse selection and double-click
use the same choices when reported by the terminal.

A new kit costs three credit; fitting adds one credit of work. A kit already
in the courier pack or Jomon locker costs only that one credit to fit.
Every confirmed purchase, fit, removal or repair takes two action-clock steps.
An unfittable purchase or removal leaves layout, item identity, stock, credit
and clock unchanged. Removing a fitting needs one credit and pack space;
repairing the parent needs two credit and restores 35 condition, not fitting
wear. No workshop action equips or supplies a replacement parent weapon.

Unmounted kits occupy rotatable pack/locker cells. A mounted kit instead has
one physical parent, one socket and no independent owner. Its weight counts
while that parent is carried or equipped. Dropping, stealing, destroying or
losing the parent carries the attachment with it. Saved parent identities and
socket uniqueness are validated. A worn-out part remains physical but stops
providing its effect; it still occupies the socket until removed.

## Effects exercised by production actions

| Fitting | Socket / shape / weight | Effect | Cost or counterweight |
|---|---|---|---|
| Iron heel | structure / 1×2 / 2 | Staff, spear, pike, boar spear or cudgel can brace, lever and break through `F` | Successful work wears five condition and adds two sound |
| Quiet binding | structure / 1×2 / 1 | Committed attack sound falls by two | One condition per attack; excludes another structural fitting |
| Retrieval cord | structure / 1×3 / 2 | Javelin or net casts leave physical ammunition; casts within four cells reel back when line of sight and pack space allow | Two less range; ten condition per cast; no ammunition fabrication before a successful throw |
| Resin seal | treatment / 2×1 / 1 | Rain does not spoil a prepared bow/crossbow shot | One less range; five condition per wet shot; parent takes accelerated fire wear |
| Ash wrap | treatment / 1×2 / 1 | Removes the inhalation range penalty for ranged preparation | Does not shoot through obstructing smoke or walls; firing from smoke wears five condition |
| Wool lining | lining / 2×2 / 2 | Warm insulation resists winter-water chilling | Four additional wet weight |
| Reed lining | lining / 2×2 / 1 | Foot/leg equipment resists bogging and deep-current stance penalties | One less pierce protection at that location |
| Iron scales | lining / 2×2 / 3 | Adds local pierce protection and coverage | Extra armour noise and mobility burden |

Effects are explicit in `workshop.py`, ranged/material action reducers and
the existing armour/terrain queries. They are not a generic item script.
Part wear also follows armour damage and fire exposure. Inventory detail
names attached parts; bench inspection lists each condition and socket.

Format-6 migration adds counted stock without issuing items or altering old
layouts. Format-7 saves preserve mounted parts and depleted stock. A damaged
or legitimately lost unique parent is not recreated.

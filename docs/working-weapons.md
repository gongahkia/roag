# Working weapons

Historical implementation note: this page describes the earlier six-weapon slice. The current main-Jomon arsenal has 72 weapon definitions; see [courier progression and production](progression-production.md) for the present systems.

At the time of this slice, the arsenal had 24 definitions. Six additional
actions are direct reducers in `work_weapons.py`, called by ordinary A attacks.
Their physical weapons appear in the first three containers of newly generated
frontiers and in bounded visiting merchant stock. Existing opened containers
are not replenished; earlier saves can buy from later merchant visits.

| Weapon | Decision and consequence | Physical source |
|---|---|---|
| Cooper's pot sling | A opens targeting; P cycles carried pitch/lime/brine pots; Enter casts at a visible cell, minimum range three. Pitch burns, lime obscures and reacts with water, brine quenches and flows. Hazards affect every nearby body. | Dunmire or Marlbank quay chest |
| Balanced throwing axe | Throw the actual readied weapon up to five paces; it weakens timber and lands at the impact. The slot is then empty until physical recovery or replacement. | Rillscar or Frostmere quay chest |
| Forked ward pike | Range two–three pins a pair across the forward line for one enemy turn; cannot attack an adjacent opponent. | Rillscar market or Marlbank market coffer |
| Jointed threshing flail | Exposed first action winds up; second sweeps all nearby bodies. Moving cancels even with a roof nail. Area harm can hit the convoy escort. | Dunmire market or Marlbank old store |
| Bank cutter's spade | F cuts/digs; dry-soil strikes throw obscuring dust, wet/stone strikes do not. It does not also grant structural bracing. | Dunmire or Frostmere old store |
| Boarding shield and hanger | Closes two clear dry same-level paces under guard before a weak cut. Water, bad footing, leg/foot injury or an encumbered load denies the charge. | Rillscar old store or Frostmere market coffer |

Pots each occupy 2×2, weigh three and stack to two. Each of the first three
frontier containers has two pots of one material. A merchant brings one finite
additional pot lot. P selection, cursor movement and cancellation are zero-time;
only a valid confirmed throw consumes one physical pot and one action. The
firing-place quiet binding does not silence the pot breaking at its landing.

All six have compact ASCII previews. Target rows show physical pots or the one
readied axe instead of a fictitious ammunition counter. Pin intent is now
consumed by the ordinary enemy turn; this also activates the existing boar
spear's advertised crossbar pin.

Verification: 14 focused working-weapon tests passed in 1.650 seconds; six
target-visibility tests in 0.479 seconds. Compilation and whitespace checks
passed. The first weapon run had one assertion error: the opponent moved out
of spade dust after the attack; the regression now checks the actual impact
cell, not its later position.

The first full suite ran 356 tests in 182.472 seconds, with two failures:
the previous arsenal-count assertion still expected 18, and the clothing
test expected its merchant lot last. The count now expects 24 and merchant
stock retains the clothing lot's ordering after the new implement/pot lots.
Frontier stock is bounded at six lots; older regions retain four. The affected
15 clothing tests then passed in 4.522 seconds and three tactical-reward tests
in 2.067 seconds. Full-suite confirmation remains required after integration.

Real 80×24 PTY, seed `working pottery`, used a saved fixture placed at the
production Dunmire quay chest, not a full chart expedition. E opened its normal
contents; the pack acquired the pot sling and two pitch pots. Keyboard inventory
equipped it while stowing the billhook. A selected an empty landing four paces
east; Enter consumed one pot, ignited the timber path and spread fire into reeds.
Reopening targeting showed one remaining pot. Escape and physical travel west
returned to Jomon while smoke and warned support damage remained behind.
Quit restored the terminal. This is one environmental weapon exercise, not six
manual build demonstrations. Carrying the entire chest made this courier
encumbered; leaving supplies behind remains an actual packing decision.

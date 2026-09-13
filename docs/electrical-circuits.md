# Working circuits

Couriers can lay finite, persistent electrical fittings on Jomon's working decks
and in regional maps. The tavern and the water approach are not construction
sites. Use `W` at a physical workshop or forge to fabricate parts from counted
goods; the cell recipe also needs gathered Greywash brine. Finished parts take
pack space. Nothing is conjured by placing a trace.

Press `\` to survey the local circuit. Arrows or `HJKL` move the cursor; work
must be on the courier's level and within two squares. `Tab` switches between
surface and buried traces. Press `1`–`7` to fit a trace, via, rack, switch, lamp,
gate, or drain; `E` operates a switch or loads a carried galvanic cell into a
rack; `R` reclaims a fitting into a pack cell. Fitting, operating, and reclaiming
cost one world action. Inspection and changing layers cost no time.

Surface traces and devices have distinct map glyphs. A buried trace is drawn
only in circuit view, so an existing floor or wall can cover it without erasing
the connection. An insulated via joins the surface and buried trace at the same
square. Paired vias also join levels at an existing regional vertical link.
Adjacent traces connect across the eight neighboring squares within a layer;
corners are real connections, so leave a gap where two lines must not touch.

The pulse phase follows [Wireworld's four-state automaton](https://mathworld.wolfram.com/WireWorld.html):
an electron head becomes a tail, a tail becomes a conductor, and a conductor
becomes a head only when one or two neighboring heads are present. Jomon adds
the layer-via rule and powered devices. A loaded rack injects one pulse every
six world actions and spends one of its cell's 24 pulses; an empty rack cannot
inject more. Opening a switch breaks that circuit path. A receiving lamp
brightens nearby sight for five actions; a latch gate opens for five actions
and otherwise blocks movement; a sump pump clears temporary water in its
surrounding nine squares when a pulse reaches it. The game's pulses are a
deliberate discrete electrical abstraction, not an analog voltage simulator.

Circuits persist in save format 12. Existing format-11 saves load with an empty
circuit register. Reclaimed racks do not refund spent galvanic cells.

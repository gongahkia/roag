# Working circuits

Couriers can lay finite, persistent electrical fittings on Jomon's working decks
and in regional maps. The tavern and the water approach are not construction
sites. Use `W` at a physical workshop or forge to fabricate parts from counted
goods; the cell recipe also needs gathered Greywash brine. Finished parts take
pack space. Nothing is conjured by placing a trace.

Press `\` to survey the local circuit. Arrows or `HJKL` move the cursor; work
must be on the courier's level and within two squares. `Tab` switches between
surface and buried traces. `1`–`7` fit a trace, via, rack, switch, lamp, gate,
or pump; `8`, `9`, and `0` fit a sensor, one-way relay, or counted relay; `P`
fits a piston and `B` fits a movable freight crate. The panel shows each part's
carried count. `E` operates or configures the selected fitting, `T` changes its
alternate setting, `R` reclaims it into a pack cell, and `.` lets one world
action pass so you can watch a pulse. Fitting, operating, reclaiming, and
stepping cost world time. Cursor movement, inspection, and changing layers do
not.

Surface traces and devices have distinct map glyphs. A buried trace is drawn
only in circuit view, so an existing floor or wall can cover it without erasing
the connection. An insulated via joins the surface and buried trace at the same
square. Paired vias also join levels at an existing regional vertical link.
Adjacent traces connect across the eight neighboring squares within a layer;
corners are real connections, so leave a gap where two lines must not touch.
A freight crate can cover a buried trace or mass sensor, and blocks walking.
The visible head of an extended piston blocks its front square until it
retracts. Pistons push at most three crates, never terrain or living actors;
the optional sticky setting pulls one crate back if the return square is clear.
Loose physical goods dropped on a square before a crate is fitted there ride
with that crate, up to 12 kg per crate. Reclaim the crate to access the goods
at its destination. If blocked or overloaded, the piston remains retracted
and records the reason.

The pulse phase follows [Wireworld's four-state automaton](https://mathworld.wolfram.com/WireWorld.html):
an electron head becomes a tail, a tail becomes a conductor, and a conductor
becomes a head only when one or two neighboring heads are present. Jomon adds
the layer-via rule and powered devices. This is one unified system: the visible
pulse is both the control signal and the energy that works a device. There is
no second power wire, wattage budget, or per-device charge cost. A loaded rack
sends one pulse every six world actions only while an open path reaches a
device or counted relay. Each sent pulse spends one of the cell's 24 charges;
an idle or blocked rack conserves its charge. A pulse can cross at most 64
fitting links. Branches can work several devices from one pulse, but a loop
cannot keep working indefinitely after its source is removed or emptied.
Opening a switch breaks that circuit path. A receiving lamp
brightens nearby sight for seven actions; a latch gate opens for seven actions
and otherwise blocks movement; a sump pump clears temporary water and reduces
standing material water in its surrounding nine squares. Sensors pass pulses
only while detecting mass on their square, local water, or a nearby threat;
their threshold is configurable. One-way relays receive from behind and emit
only ahead. Counted relays pass every second, third, or fourth accepted pulse.
All fitted settings, counts, phase, charge, remaining pulse span, last-pulse
time, and actuator fault are saved. The circuit panel reports the next phase,
live input heads, physical links, connected racks, source readiness, and the
first blocked switch, sensor, or relay on a route. A physical connection alone
does not mean that an open switch or clear sensor passes a pulse. Even an open
route may lose a pulse at a Wireworld junction with too many simultaneous
inputs; `.` steps time so you can watch the exact point of failure.

In a new world, Hearthford's mill has an authored relief circuit: its finite
rack feeds a mass sensor, sump pump, and lamp. Standing on the sensor while a
pulse arrives runs the pump and lights the mill. It can be inspected, altered,
reclaimed, and recharged like a player-built circuit. Older saves keep their
existing circuits without receiving a new installation mid-expedition.

Jomon's lower-deck bilge has a second authored circuit near the `U` station at
8,15,-1. Its rack at 5,15,-1 feeds a knife switch, a paired via, a trace buried
beneath the work station, and a water sensor at 9,15,-1. When that sensor sees
one measure of water, pulses reach the sump pump and lamp. The rack saves its
cell while the hold is dry or the switch is open. A flooded-hold voyage can be
cleared by the resulting *local* pumping under the same material and crisis
rules as manual work; the circuit does not repair an open seam, and water beyond
the pump's nine-square reach still needs another pump or manual bilge work.
Use `\` near the station and `Tab` to inspect the buried run. The switch can
be opened to reserve charge, and the rack can be reloaded with a crafted cell.

Useful builds available now include a mill or bilge pump fed through a water
sensor, a threat-triggered warning lamp, a pressure-operated latch gate, and a
freight lane where pistons shift crates over buried mass sensors. The counted
relay can require repeated events before a gate opens; the one-way relay keeps
its output from feeding back into the input. Vehicle control, automated trade
orders, and terrain-block movement are not connected to circuits yet.

The design borrows the physical push and obstruction limits of
[Minecraft pistons](https://www.minecraft.net/nb-no/article/block-week-piston),
the single source-to-device language of [Minecraft redstone](https://www.minecraft.net/fr-ca/article/redstone-dust),
the observable state of [Factorio's circuit interface](https://www.factorio.com/blog/post/fff-384),
and the tangible sensor-to-machine response shown in [Infinifactory](https://www.zachtronics.com/infiniupdate-2/).
Jomon remains a discrete pulse game, not an analog voltage simulator or a full
numeric factory network. Pistons do not move world terrain, vehicles, or actors;
circuits do not yet automate regional production orders or ship navigation.

Circuits persist in save format 14. Format-13 in-flight pulses gain one final
link during migration; format-12 circuits gain their earlier device settings,
and format-11 saves still load with an empty circuit register. Existing saves
do not receive new authored fixtures mid-expedition. Reclaimed racks do not
refund spent galvanic cells.

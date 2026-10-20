# Ithica architecture

[PROJECT_STATE.md](PROJECT_STATE.md) is the implemented reference. Ithica uses one graphics-free C# campaign and one MonoGame window. Home, expedition, preparation and arrival screens operate on the same population, inventory and timeline.

## Ownership

- `Ithica.Core/Campaign` owns the campaign, clock, commands, phase transitions, site registry and carried supplies/cargo.
- `Colony` owns home cell arrays, machines, work, routing, contact survival and authoritative residents.
- `Expeditions` owns site/encounter mechanics and the retained fantasy compatibility path. A field session's heroes and combat units are controlled projections of resident IDs, not another population.
- `Generation` recasts the existing CA/WFC algorithms into lunar terrain, objects and validated access.
- `Persistence` owns unified snapshots and validation; `Diagnostics` replays ordinary commands for tools and native inspection.
- `Ithica.Client` handles input and presentation. Its `_sim` property references `Campaign.Home`. Gameplay callbacks collected in immediate-mode UI drawing are queued for Update.
- `Ithica.Graphics` owns sprite libraries, masks, text and cameras. `Ithica.Tools` and Core.Tests run without MonoGame or SDL initialization.

The solution has five projects. The explicit `Preview/` client path retains the old fantasy gameplay and save readers; it is not the normal expedition phase.

## One clock

`CampaignTick` is the home simulation tick. In colony mode an elapsed-time adapter requests fixed ticks at eight per second, with 1×/3×/8× speed. The client limits how long it starts further ticks to roughly 12 ms and retains bounded backlog. Each tick is synchronous; expensive individual ticks may exceed the frame budget.

In field mode there is no wall-time simulation. Accepted commands advance their declared costs through the campaign scheduler, updating home, active site fields, actor needs and known distress conditions together. Hostile actions resolve deterministically to the next player turn. Thinking, inspection, targeting, menus, camera changes and invalid actions spend no time or RNG.

The clock's accumulator resets at phase transitions. A return charges travel once and adds no fixed expedition shift. Save snapshots preserve the colony remainder but contain no uncommitted click-route or animation timer. Every public action settles before saving; there is no partially executed enemy action to repeat on load.

## People and stock

Residents keep stable IDs, location and field capabilities independently of their work/profession. Departure validates all people/supplies first, releases their home jobs and commits the phase once. Away residents receive needs updates once and never home work, local treatment or farm bonuses. Empty home staffing does not imply defeat.

At each field command, encounter health/stress deltas commit to residents; fractional values remain intact. Post-tick projections refresh from that authority. Charge, level and experience persist across phases. New rescues recruit only in breathable shelter and keep a resident-ID link in historical contact data; old saves may omit that link.

Packed food/water, manufactured medical kits and filters are deducted once; field use consumes carried supplies. Cargo and unused supplies return once. Construction reserves component stock through the same `Stock` arithmetic as metal/crystal, including cancel refunds. Failed returns leave undeposited salvage recoverable at its persistent site, retaining cleared encounters.

## Station pressure and crew community

`Colony/Maintenance.cs` resolves condition, physical faults and reserved `Maintain` jobs through existing machines/workers. `Community.cs` owns deterministic resident profiles, bounded expiring memories and directional trust; local conversation/rest never applies to away residents. `Story.cs` selects eligible targets with an independent warmed PCG stream and bounded budget, cooldowns, streak caps and recovery windows. Authored `Story/events.json` is embedded in Core and supplies 12 definitions/24 responses; typed handlers own eligibility and physical/social effects. Core has no UI/runtime file dependency for event loading.

The normal Campaign enables story mode, replacing old fixed disasters but retaining distress and safety notices. The bare legacy Simulation retains its old behavior. Every effect follows the shared logical clock, including during accepted field actions. Campaign commands gate station responses/service to the home phase. The director and its forecast/budget/recovery metrics are internal diagnostics only. `StationReports` describes observable conditions and translates historical handler IDs/notices without mutating saves. Player station-report and personal-file panels hold time; response callbacks are queued for Update. Outstanding deadlines/follow-ups, RNG, service reservations, resident memories/trust and pacing state are in the nested home snapshot with validation and deterministic defaults for older saves.

## World representation and performance

Cells use row-major contiguous arrays. Atmosphere/heat/fire use old-state/new-state buffers; hull blocks exchange and exterior vacuum drains breached rooms. Only active floors participate. Fire requires oxygen. Deck/airlock topology defines local electrical islands; topology and machine changes invalidate caches. Connected batteries and prioritized consumers avoid one global power pool.

Work is indexed spatially. BFS reuses buffers and path cursors; at most 64 new routes start per tick, with rotating crew processing. People can overlap. The home and current site are active; distant site fields freeze while explicitly known contact deadlines continue on campaign time.

Keep the synchronous foundation until profiling justifies another thread. Moving simulation off the UI thread requires a command queue and immutable/double-buffered render snapshots; exposing mutable arrays to both threads would be unsafe. Hierarchical routing, region caches, collision reservations and more detailed hauling remain future work, not present scaling guarantees.

## Persistence and compatibility

The root format is `ithica`, version 1, at `~/.ithica/campaign.json`. It contains home state, clock, phase, registry/site arrays, roster locations, pack/cargo, encounters, completion and independent RNG. Temp-file writes flush before atomic replacement. Load validates map, phase, roster and projection consistency without regenerating visited sites.

The nested home retains the `regolith-csharp` identity and old reader. Field sessions use `ithica-field`; the legacy fantasy path retains `lantern-rpg` versions 1/2 and `LegacyWorldBuilder`. Explicit colony import leaves its source untouched and rejects in-flight automated legacy rescues. Two unrelated campaigns are never implicitly merged.

## Assets

Original source images/IDs/crops remain stable. Home and field aliases use separate contexts; combining their dictionaries by overwrite would corrupt mappings. Source pixels are not edited; black removal and palette masks happen in texture copies. Native atlas textures load lazily.

Schema 5 distinguishes current Ithica phase use from preview-only bindings and proposals. Edit the documented authoring inputs and regenerate derived manifests/catalogs/tables. See the [catalog guide](../assets/reference/CATALOG_V2.md).

## Verification boundaries

Core coverage preserves all original colony/RPG cases and exercises phase clocks, identity, supplies, failure recovery, persistent access and exact save continuation. Native smoke replays the integrated loop through the same client and command paths. Headless tools verify useful work and procedural seeds. [VERIFICATION.md](VERIFICATION.md) records results; [PERFORMANCE.md](PERFORMANCE.md) records hardware and workload limits.

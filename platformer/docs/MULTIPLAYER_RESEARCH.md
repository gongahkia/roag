# Shared worlds, collaborative building and speedrun races

Research and implementation checked on 17 October 2026. The user accepted desktop rooms reached by direct IP; the selected movement remains the earlier Mario Maker-led controller.

## Everybody Edits as a design reference

MegaLamb’s developer listing describes a multiplayer level editor where people create challenges together. Its published controls include mouse placement, a quick tile panel and movement in the same world. The design takeaway for this project is immediate shared building and playtesting. We keep a player in the world while other players edit, with a personal build toggle and a visible palette. [Developer listing and controls](https://www.kongregate.com/en/games/megalamb/everybody-edits).

The building guide hosted by PlayEE shows small start/obstacle/finish challenges, ladders, checkpoints, shared key/door puzzles, coins, arrow paths and decorative backgrounds. Those are useful building ingredients for short courses and alternate routes. This is a primary artifact about building, not an authoritative specification of Everybody Edits physics or networking. Our 35 tile types are an original subset with original procedural artwork and explicit behavior. We did not import its sprites, worlds or source code. [Building guide, especially pages 7–14, 19–20 and 29–36](https://playee.github.io/building-guide.pdf).

## Transport and engine decision

Godot already runs the controller and provides ENet client/server peers through its high-level multiplayer API. ENet uses UDP and supports reliable and unreliable delivery. Keeping the existing engine avoids a second movement migration. Direct-IP guests need a reachable host address and UDP port; local hosting behind a router may require forwarding. Our default is UDP 24567. The room cap is an application choice: 16 total players, including a playing host, or 16 clients with a dedicated host. [ENetMultiplayerPeer reference](https://docs.godotengine.org/en/stable/classes/class_enetmultiplayerpeer.html).

| Approach | Fit | Decision |
| --- | --- | --- |
| Native Godot + ENet | Supports the accepted desktop scope and independent reliable/unreliable traffic | Implemented |
| WebSocket multiplayer | Offers another Godot multiplayer peer; a browser version would need a different transport and export validation | Deferred |
| Browser WebRTC | A possible future browser transport; signaling and connectivity infrastructure add scope | Deferred |
| Public matchmaking/relay | Would simplify finding and reaching rooms; requires a hosted service and lifecycle work | Outside this implementation |

Godot documents the differences in platform networking support. The existing desktop ENet implementation should not be described as browser compatible. [High-level multiplayer guide](https://docs.godotengine.org/en/stable/tutorials/networking/high_level_multiplayer.html), [WebSocket peer](https://docs.godotengine.org/en/stable/classes/class_websocketmultiplayerpeer.html).

The dedicated launcher uses Godot’s headless mode and the same game project. This requires no renderer or separate server engine. It is a runnable local server, rather than a deployed service or an optimized dedicated-server export. [Official dedicated-server guide](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_dedicated_servers.html).

## Movement authority and responsiveness

The host simulates collisions, movement, hazards, checkpoints, pickups and finish detection at 120 Hz. Clients submit bounded input commands, never accepted positions or claimed finish times. A local client predicts its own movement immediately; snapshots provide complete controller state and the last processed input sequence. The client restores that state and replays unacknowledged commands. Other players use interpolated, noncolliding visual avatars.

Sequence numbers and redundant recent inputs follow established networked-physics practice. Full controller state includes velocity, jump/coyote buffers, wall contact/lock state and spring-launch state. This is correction-based synchronization, with no claim of bit-identical cross-platform replay. Unlike the extrapolation described in Fiedler’s article, this server freezes a remote runner when its next input is missing and waits for retransmission. That conservative choice can penalize poor connections; it needs WAN playtesting. [Glenn Fiedler, State Synchronization](https://gafferongames.com/post/state_synchronization/).

The client retains at most 240 unacknowledged inputs and sends batches of up to 64 commands at about 60 Hz. The host consumes at most one command per player per simulation tick and broadcasts snapshots at 30 Hz. Respawn, editor transitions and new rounds advance an input generation so delayed pre-transition packets cannot block a new attempt. Reconnecting clears snapshot and prediction history.

Traffic has independent ENet channels: reliable room lifecycle on 0, reliable document edits on 1, ordered unreliable inputs on 2, and ordered unreliable snapshots on 3. Separate channels avoid a reliable edit waiting for unrelated movement acknowledgements. Cross-channel order is not guaranteed, so round transitions carry a complete frozen document and an epoch; old-epoch edit packets are ignored. [Godot’s explanation of channels and ordering](https://godotengine.org/article/multiplayer-changes-godot-4-0-report-2/).

## Concurrent editing and world storage

The server owns a two-layer grid and a monotonically increasing document revision. Each cell has its own version. Requests include the version the builder saw: edits to different cells can both succeed, while stale writes to the same cell are rejected. Moving a required start/finish marker clears its former location in one transaction. Each player has conditional undo/redo; another builder’s later write is preserved.

The host validates permission, bounds, tile IDs, layers, cell versions, edit rate and solid placement against player bodies. World loads validate dimensions, tile arrays, versions and required markers. JSON loads are capped at 2 MB. Clients can request a save/load only as owner, and cannot choose arbitrary server file paths. Changed worlds autosave every five seconds using a temporary file and rename; explicit Save writes immediately.

The renderer is a custom grid with visible-cell drawing and individual static collision bodies. It does not use Godot TileMapLayer or a borrowed tile atlas. The current map-size limit is 128×64; much larger worlds would benefit from chunking, collider merging and measured bandwidth budgets. This implementation does not include copy/paste, flood fill, a world browser or accounts.

## Usernames, colors and visual effects

The server assigns the first free slot from a 16-color palette when admitting a player. It releases that slot on departure, avoiding the former count-based allocation that could collide after disconnects or repeat after eight players. Clients receive the assigned slot in the reliable roster; existing players keep their colors through respawns, edits and round changes.

Usernames are sanitized, capped at 20 characters and made unique within the room with case-insensitive suffixes. A client can rename only its own peer identity. Accepted changes update the nameplates, roster and any current race result. The UI remembers the accepted name in a local profile; it is a display identity rather than account authentication.

Both simulated players and remote avatars share one `canvas_item` tint shader and the original sprite texture. The shader replaces saturated body colors while preserving neutral pixels, brightness differences, transparency and CanvasItem modulation. Each avatar has its own material parameter. [Godot CanvasItem shader reference](https://docs.godotengine.org/en/stable/tutorials/shaders/shader_reference/canvas_item_shader.html).

Particles are cosmetic CPU-simulated pixel shapes drawn in world space. Dust, sprint trails, takeoff/landing bursts, braking dust and wall-slide sparks derive from movement once per rendered frame. Sampling in the render path avoids duplicating effects during input reconciliation. Remote avatars derive effects from the state already supplied by snapshots; no particle events are transmitted. Teleports reset tracking, stationary/editing/finished players stop emitting, lifetimes expire and each avatar has a hard cap of 80 particles. F7 disables effects locally and the preference persists. Headless servers skip simulation and rendering of effects. Godot also provides dedicated CPU/GPU particle nodes; a small custom system was chosen here for bounded allocation and simple pixel artwork. [Godot 2D particle systems](https://docs.godotengine.org/en/stable/tutorials/2d/particle_systems_2d.html).

## Race rules

The owner switches between collaborative Build mode and Race mode. Starting a race freezes editing and resets players together, then schedules a three-second countdown on the server’s 120 Hz clock. The large HUD clock displays elapsed round time; finish freezes an individual racer’s display. Results include elapsed ticks, deaths, coins and the frozen map revision. Deaths and manual respawns retain elapsed race time. Checkpoints are personal; colored doors are shared. Late joiners spectate until the next round or Build mode.

These are room results, not authenticated global records. Server authority prevents ordinary position/finish RPC claims; it does not establish competitive anti-cheat, fair latency compensation or protection from a modified host. The earlier offline training courses retain their separate local PB/ghost system.

## Evidence and remaining limits

`test-speedrunner.sh` passes 18 controller checks, three actual clean course completions with ghost reload verification, 28 workshop checks, and 27 identity/effect checks. Workshop checks exercise serialization, conflicts, markers, saves and actual collision behavior for ice, conveyors, springs, ladders, water, one-way platforms, wind, keys/doors, coins, checkpoints and hazards.

`test-multiplayer.py` launches actual independent Godot processes over loopback ENet. It verifies simultaneous and conflicting edits, foreground/background convergence, conditional undo/redo, permission enforcement, world saves/reloads, late join, editor/respawn transitions, reconnecting, disconnect removal, shared countdowns, locked race geometry and server-validated finishes. A dedicated host admitted and synchronized 16 simultaneous clients. A separate playing-host test completed a race with both host and guest. Network checks also verify live renames, duplicate-name suffixes, distinct colors after reconnecting, and all 16 nameplates and shader tints. Native X11 inspection also verified the rendered palette, mouse painting, saved edits, room controls and large running timer.

Not verified here: external WAN/NAT reachability, high latency, sustained packet loss, large-world performance, exported binaries on other operating systems, or long-running public hosting. There is no host migration, relay, matchmaking, password/account system or public deployment. When a playing host leaves, its clients return to local copies; dedicated-room ownership can pass between connected players without replacing the server process.

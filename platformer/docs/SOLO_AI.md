# Offline races and AI rivals

The single-player screen is available with F4 from shared worlds or `./run.sh -- --solo`. It supports three purpose-built courses, four difficulty profiles, mixed fields, up to four opponents and a zero-opponent time trial. The earlier training courses and their local PB/ghost system remain available through F5.

## Actual controls and prediction

`race_bot.gd` is a local receding-horizon controller. At a decision point it captures the racer's current position, velocity and controller state. It tests running and several jump durations/launch delays using a hidden `Runner` with the same movement resource and the actual static collision world. Predictions run at the same 120 Hz step as gameplay, looking approximately 0.67–1 second ahead. A candidate stops at its first meaningful landing, rather than rejecting a safe jump merely because a second obstacle would require another decision.

The probe's collision layer is zero. It queries course geometry but cannot block another racer. It does not call race interaction logic, collect coins, activate checkpoints, edit tiles or claim finishes. Its particles and automatic processing are disabled. Only the visible racer's ordinary input commands are executed by the room; hazards, checkpoints, deaths and finish checks still use the usual authoritative simulation. No external AI service, training data or recorded route is involved.

Rollouts reject lethal tiles, falls and predicted contact with moving saws. Surviving trajectories are scored for progress, stable landings, unnecessary jumps and fall exposure. A clear grounded run at chosen pace skips the more expensive jump search. The bot executes a short control prefix, observes the real body again and replans when grounded. Airborne controls retain the chosen jump height; reactive wall kicks handle descending wall contact. Finish approach uses braking and alignment so a bot does not sail over the flag and run into the map boundary.

This uses Godot's character controller for movement rather than treating a navigation path as movement instructions. Godot's navigation documentation distinguishes pathfinding from the agent code needed to traverse links; a platformer must explicitly handle jumps and falls. Here, actual trajectory forecasts are the chosen mechanism. [2D navigation overview](https://docs.godotengine.org/en/stable/tutorials/navigation/navigation_introduction_2d.html), [CharacterBody2D movement](https://docs.godotengine.org/en/stable/tutorials/physics/using_character_body_2d.html).

## Difficulty

| Profile | Ground decision interval | Forecast | Jump hold options (120 Hz ticks) | Other differences |
| --- | --- | --- | --- | --- |
| Easy | 22 ticks | 0.67 s | 2, 10, 60 | Runs rather than sprints, hesitates at the start and on landings, larger stability penalty |
| Normal | 16 ticks | 0.8 s | 2, 14, 60 | Sprints, shorter hesitation, moderate stability penalty |
| Hard | 12 ticks | 0.9 s | 2, 8, 14, 22, 60 | More jump choices, continuous landings, smaller stability penalty |
| Expert | 8 ticks | 1.0 s | 2, 6, 14, 22, 60 | Frequent replanning, fast start, smallest stability penalty |

All use the same acceleration, gravity, jump impulse, sprint ceiling and collision shape as the player. They choose different inputs rather than receiving different physical abilities. Difficulty is a behavior profile, not a forced finishing order; some layouts suit a lower-tier line. There is no rubber banding based on the human's position.

Bots monitor whether they are making progress. A prolonged stall requests a normal checkpoint respawn, counts a death and retains race time. Hazard deaths use the same path. Rival names, original base sprite, assigned colors and movement particles use the existing avatar system.

## Race lifecycle

`solo_race.gd` owns an offline `WorkshopRoom`; there is no network peer or server to start. It creates negative-ID local racers through `add_local_bot()`. Existing multiplayer peers and their input queues keep their original behavior. Leaving the mode or creating another field disposes of both bot controllers and their trajectory probes.

Courses use the same two-layer tile document and renderer as shared worlds. Split Second combines a low ceiling, spikes, gaps and raised one-way platforms; Skyline Steps adds ascending and descending islands; Foundry Rush combines ice, conveyors, raised blocks and moving saws. Course geometry and hazard motion are shared between the human and AI.

Race start resets everyone and schedules the common countdown. R respawns the human only; Enter creates a fresh round. Each finish freezes that racer's time, while the others keep racing. Rankings put finished racers first by time and rank unfinished racers by forward progress. The progress strip keeps off-camera opponents visible. Pausing or losing focus freezes the offline simulation, including bots and saws. Solo race results do not write training PBs or ghosts.

## Validation and limits

The regression suite includes 23 solo lifecycle checks and 42 bot integration checks. It completes all four tiers on all three tracks using their real controller, terrain and hazards, verifies multiple live decisions/jumps, checks the live document remains unchanged by prediction, and confirms Expert outpaces Easy on each course. Existing controller, course/ghost, workshop and identity/effect checks also pass. A separate real-process multiplayer regression still admits 16 clients and validates network play/editing.

Observed automated finishes span roughly 6–12 seconds for Normal/Hard/Expert and 10–14 seconds for Easy on these layouts. These are regression results, not optimal times or a human-tested difficulty calibration. Native graphical checks cover the lobby, countdown, colored opponents, particles, timer, standings and progress display.

The controller is designed and tested for these forward-running solo courses. It does not perform general global pathfinding through arbitrary player-built worlds, learn from human races or support network-room bots. Deep mazes, required backward routes, switches/keys, ladders and arbitrary custom physics need additional planning work before bot support can be claimed. Forecasting several candidate character trajectories costs CPU; a four-opponent cap bounds that work, but sustained performance on other hardware remains unverified.

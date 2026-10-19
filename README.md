# REGOLITH

**A small life on the Moon.** A sprite-based colony simulator built with **C# / .NET 10 and MonoGame DesktopGL**. Ada starts alone at Tranquility. Click stranded people in the wasteland, send a rescue, escort them into shelter, and build a colony from the people you save.

![Tranquility outpost in the MonoGame client](docs/images/outpost.png)

## Play

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), then run:

```sh
./play.sh
./play.sh --load
./play.sh --seed 8675309
./play.sh --demo --skip-intro
```

The launcher works from any directory. On macOS/Linux, `./tools/setup-dotnet.sh` can install SDK 10.0.402 into `~/.local/share/regolith/dotnet` without changing your PATH. The SDK is already installed there on the development machine. First launch restores the pinned NuGet packages and builds Release.

On Windows, or with the SDK on your PATH:

```sh
dotnet run --project src/Regolith.Client --configuration Release
```

The native window defaults to **1440×900** and supports resizing down to **1024×720**. Use `--window-size 1280x800` to set its initial size. MonoGame DesktopGL targets Windows, macOS, and Linux; native rendering has been exercised on macOS Intel. Other desktop platforms still need a graphical smoke run. Linux needs a working desktop/OpenGL environment; headless tools need only .NET.

The six-section tutorial opens before time starts. Enter advances; Left goes back; Esc closes it. Reopen it with **T**. Tutorial and construction menus hold simulation time.

## First colony

1. **Save Mika.** Click the red distress contact on the map, then press Space to run time. A rescue costs **2 food + 2 water**. Ada travels out, shares oxygen, and escorts Mika home at half speed. Recruitment happens only inside pressure. **R** locates contacts; it does not open a separate expedition screen.
2. **Supply the outpost.** Press **D**, then click cyan ice for water, amber ore for metal, or purple crystal for equipment. Jobs reserve workers automatically. **P** surveys nearby open terrain.
3. **Expand carefully.** **B** opens construction. Machines need deck and connected power. Enclose new deck with hull and an airlock, and add generation/storage before new consumers. **O** cycles oxygen, heat, and power overlays. Repair meteor breaches with pressure hull.
4. **Bring more people home.** Incoming distress signals have real oxygen and health countdowns. New arrivals need food, sleep, and recovery before becoming reliable workers.
5. **Earn the founding charter.** Rescue three people, reach cycle 3 with four living crew and 20 food, and power a pressurized Earth uplink. The uplink costs 60 metal, 20 crystal, and 5 kW. Keep building afterward.

## Controls

| Input | Action |
| --- | --- |
| Left click / Enter | Inspect, rescue a stranded person, or place the active order |
| Mouse wheel over map | Zoom around the pointer, from 25% to 300% |
| Right / middle mouse drag | Pan the map |
| Arrows / H J K L | Move the map cursor; Shift moves five cells, except Shift+L loads |
| + / − or Page Up / Down | Zoom around the cursor |
| 0 / map zoom percentage | Reset zoom |
| Space / 1 2 3 / period | Pause; select 1× / 3× / 8×; pause and advance one tick |
| D / B / P / X | Mine or salvage / construction / survey / cancel with refund |
| V | Start/cancel a rectangular designation; click or Enter confirms |
| Esc | Close menu, cancel selection, or return to inspection |
| R / G / F | Locate distress contact / return home / follow selected crew |
| C / E | Crew manifest / expanded log; mouse wheel scrolls the panel |
| O | Cycle terrain, oxygen, temperature, and power views |
| T / ? | Six-section tutorial / control reference |
| S or F5 / Shift+L or F9 | Save / load (loading pauses) |
| F11 / Q | Toggle fullscreen / save and quit |

Closing the window also saves a living colony. After colony loss, quitting preserves the previous save; **N** starts again. The default C# save is `~/.regolith/colony-csharp.json`; `--save PATH` selects another slot. Writes are atomic and include random state and in-flight expeditions. Python saves are a separate format and are retained at `~/.regolith/colony.json`.

## Simulation and structure

Geology uses five passes of a Moore-neighborhood cellular automaton. Atmosphere, heat, and fire use local four-neighbor, double-buffered updates. Hull blocks gas; exterior vacuum drains breached rooms; airlocks pass people and electrical connectivity while sealing the air. Connected decks form separate power islands with local battery banks and prioritized life support. Crew track health, food, rest, morale, and suit oxygen while finding routes to work and rescues.

The implementation separates the game from its presentation:

| Project | Responsibility |
| --- | --- |
| [Regolith.Core](src/Regolith.Core/) | Graphics-free simulation, contiguous world arrays, jobs, routing, rescue, atomic saves |
| [Regolith.Client](src/Regolith.Client/) | MonoGame rendering, sprite loading, camera, mouse/keyboard input, HUD/tutorial |
| [Regolith.Tools](src/Regolith.Tools/) | Deterministic headless runs and simulation benchmarks |
| [Regolith.Core.Tests](tests/Regolith.Core.Tests/) | Environment, work, power, rescue, progression, and saved-continuation checks |
| [legacy/python](legacy/python/) | Archived terminal prototype, runnable independently |

This is a playable single-layer foundation. Resources go directly into shared inventory; crew may share a cell. Hauling, multi-level excavation, combat, modding, rich social behavior, and background simulation threads are future work. See [architecture](docs/ARCHITECTURE.md) for design decisions and [performance](docs/PERFORMANCE.md) for measured limits and reproduction commands.

## Assets

All original spritesheets, stable sprite IDs, crop rectangles, and proposed mappings are preserved. The client uses **Urizen tiles and bitmap font**, **Godot node icons**, and **keyboard/mouse prompts** from the three imported packs. Palette tinting and black-background removal happen in texture copies during loading.

[assets/runtime-sprites.json](assets/runtime-sprites.json) selects **154 existing IDs** for 60 semantic bindings and 94 glyphs. Regenerate this small runtime selection with `python3 tools/build_runtime_assets.py` (standard library only). Python is not a game dependency, and no content-pipeline editor is required.

The full [sprite guide](assets/SPRITE_GUIDE.md), [visual browser](assets/sprites.html), and [sprite catalog](assets/sprites.json) retain every occupied region and future-use proposal. Their unused/proposed flags describe the original import audit; use the runtime manifest for current bindings. [catalog.json](assets/catalog.json) retains the original image hashes and archive inventory. Publishers and CC0 declarations are recorded in [CREDITS.txt](assets/CREDITS.txt).

## Build and verify

Use `./tools/dotnet.sh` in place of `dotnet` if you used the per-user SDK installer.

```sh
dotnet restore Regolith.sln --locked-mode
dotnet build Regolith.sln --configuration Release --no-restore
dotnet test Regolith.sln --configuration Release --no-build
./play.sh --headless 700 --demo --seed 42
./play.sh --smoke-test --screenshot /tmp/regolith-smoke.png
./play.sh --benchmark --width 256 --height 256 --agents 1000 --ticks 120
```

Headless runs print JSON and never write saves. The graphical smoke run opens a real window, exercises tutorial navigation, direct rescue clicks, zoom, recruitment, build cancellation, and save/load, then exits without changing the player's save. `--screenshot PATH --frames 12` captures a normal view and exits; omit `--frames` for an interactive session.

NuGet versions are pinned with lockfiles. CI builds and runs core/headless checks on Linux, Windows, and macOS; it does not certify native graphics on those platforms. To create a local distributable for the current machine:

```sh
dotnet publish src/Regolith.Client --configuration Release --self-contained false --output artifacts/game
```

The output includes runtime artwork and credits. This framework-dependent build requires .NET 10 on the destination machine; native installers, signing, and store publishing are not configured.

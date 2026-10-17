Archived notes for the earlier gravity-flip prototype. The current game is REDLINE in Godot; use these instructions only to run the old version.

# FLIP / SPRINT

A playable speedrunning modification of [Terry Cavanagh’s VVVVVV](https://github.com/TerryCavanagh/VVVVVV), built in its native C++/SDL engine. This is an altered version, not an official VVVVVV release. The original source code is copyright Terry Cavanagh © 2010–2020; original music is by Magnus Pålsson. Upstream credits are preserved below.

Run five original rooms back to back, flip gravity between floor and ceiling, and race your own personal best. The sprint uses a separate deterministic movement simulation; its physics are **not identical** to the original campaign.

## Play

Requires Git, CMake, a C/C++ compiler, curl, and **SDL 3.2 or newer** with development headers. Current upstream source uses SDL3; its older desktop README still mentions SDL2.

```sh
./run-legacy.sh
```

The launcher builds the Make and Play edition, downloads the official free data pack if missing, and opens the sprint directly. The assets are already downloaded in this workspace. No Steam installation is required. On macOS with Homebrew: `brew install cmake sdl3`. On Fedora: `sudo dnf install cmake gcc-c++ SDL3-devel`. If CMake cannot locate SDL3, set `CMAKE_PREFIX_PATH` to your SDL3 installation before running the launcher.

| Control | Action |
| --- | --- |
| A/D or left/right | Move; first movement starts the clock |
| Space, Z, or up | Flip gravity when touching the floor or ceiling |
| R | Restart the entire sprint immediately; restart the room in practice |
| P | Toggle practice for the current room / return to a fresh full sprint |
| [ / ] | Select a room in practice |
| G | Toggle the personal-best ghost |
| Escape | Pause / resume |
| Q while paused | Quit |
| Enter on results | Retry |
| M / N | Mute all audio / music |
| F6 / F11 | Screenshot / fullscreen |

Gamepad movement, flip, restart, and pause use VVVVVV’s existing bindings.

## Race rules

- The clock advances in fixed **34 ms** simulation ticks. It includes deaths and the short respawn delay, and stops at the final room’s exit.
- Dying returns you to the current room’s start. R starts a new attempt with zero time and deaths.
- Splits are cumulative room exit times, compared with the same splits in your personal best.
- A full sprint is eligible for a local record only if it was never paused and never lost window focus after starting. Practice cannot set records.
- A faster valid finish saves the time, splits, and ghost. Ghosts retain up to 18,000 ticks (10 minutes 12 seconds); longer finishes can save a time without a ghost. These are local practice records, not verified competitive leaderboard submissions.
- The launcher stores settings and records separately from your normal VVVVVV profile, under `.race-data/player/`. The record is `saves/sprint-v1.vvv`. Remove that file to clear your PB. Record files are versioned against the course and replaced through a temporary file.

Use `./run-legacy.sh -original-menu` to open the upstream Make and Play menu and editor. Native binaries live at `build/VVVVVV` (or `build/Release/VVVVVV.exe` for Visual Studio); pass absolute `-assets`, `-langdir`, `-fontsdir`, and an existing `-basedir` when launching directly. The Bash launcher targets Linux/macOS. On Windows, configure `desktop_version` with CMake and `-DMAKEANDPLAY=ON`, then build with your compiler.

## Build and verify

```sh
./scripts/build.sh
ctest --test-dir build --output-on-failure
```

The SDL-independent race tests complete every room, replay a complete race against every recorded ghost position, and check checkpoint deaths, retries, timer stopping, surface-only flips, split order, and record eligibility. The game has also been built and exercised under X11 with the original data pack.

The race simulation is in `desktop_version/src/SpeedrunCore.cpp`; the native input, presentation, audio, and record storage are in `desktop_version/src/Speedrun.cpp`. Change the five room definitions in the core to design another course, and increment `COURSE_VERSION` plus the save filename when changing race rules or geometry.

## Assets and license

The source follows the original [custom noncommercial license](../LICENSE.md). Sprites and audio are proprietary assets, downloaded separately from the [official Make and Play page](https://thelettervsixtim.es/makeandplay/) and excluded from Git. The [Make and Play distribution exception](../License%20exceptions.md) permits asset distribution with builds compiled with `MAKEANDPLAY` and without the original campaign. This fork defaults to that configuration. Retain the original license, credits, and altered-version notice in any distribution; see the linked terms for details.

# Station Echo

Station Echo is a complete real-time ASCII platformer for the Linux terminal. You control a survey robot inside a derelict orbital station, restore three relays, recover movement modules, cross the damaged hull, defeat the sentinel, and reach the escape pod.

The game is inspired by the compact exploration and ability-gated progression of *Environmental Station Alpha*. Its world, name, code, and ASCII presentation are original.

## Build and run

Station Echo uses Rust for a deterministic, testable game simulation; ncurses (through `pancurses`) for terminal rendering, keyboard, and mouse events; and `gilrs` for hot-pluggable game controllers.

On Fedora 43:

```sh
sudo dnf install cargo rust ncurses-devel systemd-devel pkgconf-pkg-config
cargo run --release
```

The terminal must be at least 64 columns by 20 rows. A terminal around 100x30 gives a wider view. Colors and mouse reporting depend on the terminal emulator. If a controller is connected after launch, it is detected during play even if the HUD initially says `PAD:--`.

## Controls

| Action | Keyboard | Controller | Mouse |
| --- | --- | --- | --- |
| Move | `A`/`D` or arrows | D-pad or left stick | Click left/right control, or either side of the playfield |
| Jump / double jump | `W`, up, or space | South face button (`A`/Cross) | Click `JUMP`, or the upper playfield |
| Fire | `X` | West face button (`X`/Square) or right trigger | Click `FIRE`, or near the player |
| Dash (after upgrade) | `Z` | East face button (`B`/Circle) or left trigger | Click `DASH` |
| Pause | `P` or Escape | Start | Click `PAUSE` |
| Restart run | `R` | Select/Back | Click `RESTART` after winning |
| Quit | `Q` | — | — |

Keyboard movement uses the terminal's key-repeat stream, then decays smoothly when input stops. Controller movement is held-state input. Mouse controls are deliberately click-based because ncurses mouse-motion reporting varies among terminal emulators.

## Symbols

- `@` survey robot
- `#` station hull, `^` exposed circuitry, `|` locked core gate
- `*` relay, `>` dash module, `+` double-jump module, `h` repair
- `m` crawler, `v` drone, `S` sentinel, `E` escape pod
- `-` player pulse, `o` hostile pulse

The latest restored relay acts as a checkpoint. Upgrades and restored relays survive reconstruction, while enemies and the sentinel reset.

## Development

```sh
cargo fmt --check
cargo clippy --all-targets -- -D warnings
cargo test
cargo build --release
```

The core simulation lives in `src/lib.rs` and does not depend on a terminal, so tests run headlessly. The ncurses adapter, renderer, and keyboard/mouse/controller mappings live in `src/main.rs`.

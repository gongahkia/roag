# NIGHTSHIFT: Sable Wake

NIGHTSHIFT is a wholly original ASCII first-person stealth-horror game that runs inside a terminal. You are a maintenance specialist trapped aboard the derelict research ship *Sable Wake*. Recover three power cells, restart the generator, and reach the airlock while an adaptive creature—the Warden—hunts through the deck.

The renderer is a real-time raycaster built on ncursesw. It uses ANSI terminal colors and Unicode half-block cells to pack two vertical color samples into every row, giving the first-person view twice the vertical detail of plain ASCII. Depth shading, wall seams, floor panels, flashlight falloff, and local flare light make the ship readable without leaving the terminal. The simulation, AI, collision, sound propagation, pathfinding, controller reader, and renderer are implemented in Go without third-party Go dependencies.

## Requirements and build

- Go 1.24 or newer
- a C compiler and ncursesw development headers (`ncurses-devel` on Fedora)
- a UTF-8 terminal with color support, at least 72 columns by 24 rows; 256-color terminals give the best result
- an xterm-compatible terminal enables mouse motion

```sh
make build
./bin/nightshift
```

Or run directly with `go run ./cmd/nightshift`. Use `make check` for formatting, vetting, tests, and a full build.

The renderer automatically falls back to monochrome Unicode shading when the terminal exposes too few color pairs. Plain remote consoles therefore remain playable, though a modern terminal such as Kitty, WezTerm, Alacritty, or GNOME Console produces the intended image.

## Controls

| Action | Keyboard | Mouse | Controller |
| --- | --- | --- | --- |
| Move / strafe | W/S, A/D | — | left stick |
| Turn / look | arrow keys | move | right stick |
| Interact, open door, hide | E | middle button | A / Cross |
| Crouch | C | — | B / Circle |
| Toggle sprint | R | — | left bumper |
| Fire stunner | Space | left button | right bumper |
| Throw noisemaker | G | right button | X / Square |
| Ignite flare | B | — | Y / Triangle |
| Flashlight | F | — | — |
| Motion tracker | T | — | Back / Select |
| Pause / controls / quit | Esc / H / Q | — | Start / — / — |

Terminal keyboard APIs do not expose key-up events, so movement follows normal terminal key-repeat. Holding a movement key is smooth in standard terminal configurations.

## Outsmarting the Warden

- Sound is positional. Crouching is slow and quiet; sprinting is fast and extremely loud. Opening doors, firing, and powering the generator can be heard across several rooms.
- Darkness matters. The flashlight helps you navigate but greatly extends the Warden's sight range.
- Doors are temporary barriers. Closing one breaks sight and forces the Warden to spend time opening it, but the noise can reveal its approach.
- Noisemakers create a remote sound source. Repeated fake signals make the Warden increasingly skeptical, so vary your tactics.
- Lockers and maintenance recesses are marked `[`. The Warden searches likely hiding spots and remembers repeated use; a reliable locker eventually becomes a trap.
- Flares repel it at close range but advertise your position. The stunner rewards a centered shot, has scarce charges, and becomes less effective each time it lands.
- The Warden does not know your live position. It combines line of sight, recent sound, last-seen movement, local searches, and likely objective locations. Breaking sight and moving away from the obvious route is often stronger than simply hiding.

The motion tracker reveals direction and approximate range, but using it does not pause the game.

## Controller support

On Linux, readable evdev devices with names containing `gamepad`, `controller`, `xbox`, or `joystick` are detected automatically. Select a specific device with:

```sh
./bin/nightshift --controller /dev/input/event12
```

Use `--no-controller` to skip discovery. Many Linux distributions restrict `/dev/input/event*`; add your user to the appropriate input-access group or configure a udev rule if desired. If no readable controller is found, the game continues normally with keyboard and mouse. Controller support is intentionally reported as unavailable on non-Linux systems rather than preventing the game from launching.

## Legend

- `F`: power cell
- `n`: noisemaker
- `*`: flare
- `b`: battery
- `+`: stunner charges (when rendered as an item)
- `G`: generator
- `X`: airlock
- `[`: hiding place
- `W`: the Warden—if you can see it, it can probably see you

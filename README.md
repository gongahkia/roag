# Pocket Edition

A native voxel creative sandbox built with Odin, GLFW, and OpenGL.

## Start playing

Install Python 3.12 or newer and the platform's native build tools, then run:

```sh
make run
```

Every build, check, test, and run command reuses a compiler matching `toolchain.json`,
or installs that exact Odin release into the ignored `.tools/` directory. Existing
Odin installations on PATH are reused when their commit matches. Other globally
installed versions are left in place. Downloads are checked against the pinned
SHA-256 hashes before extraction; subsequent runs need no toolchain download.

Without Make:

```sh
# macOS / Linux
./scripts/run.sh

# Windows (Command Prompt or PowerShell)
python scripts/build.py run
```

Windows also has `scripts\run.bat`. Both launch scripts accept game options.

```sh
make run ARGS="--profile low --fps-limit 60"
./scripts/run.sh --profile low --fps-limit 60
```

### Platform prerequisites

- **macOS:** Xcode command-line tools (`xcode-select --install`). Apple Silicon
  uses the pinned prebuilt compiler. Intel Macs automatically build the pinned
  Odin commit from source, reusing LLVM 17–22 or installing `llvm@22` through an
  existing Homebrew installation. Without Homebrew, install a supported LLVM and
  set `LLVM_CONFIG` to its `llvm-config` executable. The first source build takes
  a few minutes. Git LFS is not required for this project's dependencies.
- **Windows x64:** Visual Studio Build Tools with **Desktop development with C++**
  and a Windows SDK. Use an x64 Native Tools command prompt if the linker is not
  found. Make is optional; the Python command above works directly.
- **Linux x64 / ARM64:** Clang (or a system C compiler), GLFW, and an OpenGL driver.
  On Ubuntu: `sudo apt install clang libglfw3-dev libgl1-mesa-dri`.
  Linux x64 can alternatively install the pinned local GLFW library with
  `python3 scripts/bootstrap.py --local-glfw`.

To explicitly select a compiler, set `ODIN_BIN` to its executable; it must match
the pinned commit. Set `PYTHON` to override the interpreter used by Make or the
Unix launch script, e.g. `make run PYTHON=python3.12`.

## Commands

| Command | Action |
| --- | --- |
| `make` / `make build` | Compile `build/voxel` (`build/voxel.exe` on Windows) |
| `make run` | Build and launch the game |
| `make setup` | Install or reuse the pinned compiler without building the game |
| `make check` | Check source and unused imports/variables |
| `make test` | Run installer tests and the native Odin test suite |
| `make clean` | Remove generated build output |

Without Make, use `python3 scripts/build.py build`, `check`, `test`, or `run`
(`python` on Windows). Add `--debug` for debug symbols and faster compilation.

## Controls

| Input | Action |
| --- | --- |
| WASD / mouse | Move / look |
| Space / Shift | Jump or swim up / sprint |
| **F** | Toggle walking and spectator flight with noclip |
| Space / Ctrl in spectator mode | Fly up / down |
| Left / right mouse | Break / place a block |
| 1–9 / Tab | Select a block / change palette page |
| Esc | Open or close settings |
| F3 / F5 | Toggle diagnostics / save |
| Q | Quit |

Worlds and settings are saved under `saves/`. More detail is in
[settings](docs/settings.md), [terrain](docs/terrain.md), and
[architecture](docs/architecture.md).

Flowing water and rivers carry you downstream, and waterfalls pull downward.
WASD and Space/Ctrl let you swim against the flow. Land jumps reach about 1.25
blocks with brisker gravity; spectator flight ignores water forces and gravity.

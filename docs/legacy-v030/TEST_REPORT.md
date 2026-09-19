# Verification report — DEEPWARD 0.2.0

Executed 19 September 2026 against the delivered source.

## Scope and environment

The actual game core ran in native **Lua 5.4.7** on Linux x86_64. The container had
`liblua5.4.so.0` but no Lua CLI, so a small C launcher invoked that installed runtime.
No Lua algorithms were replaced by Python versions. The launcher is an authoring
tool and is not bundled or needed by a player with LÖVE/Lua installed.

**Real LÖVE, LuaJIT, SDL input, GPU rendering, audio and interactive frame rate were
not verified.** The project targets LÖVE 11.5 and Lua 5.1-compatible syntax. A
successful Lua 5.4 test is not proof of LuaJIT compatibility; F10 in the actual game
is the next runtime check. No claim of production readiness or complete game
balance follows from these tests.

## Executed checks

| Check | Actual result |
|---|---|
| Syntax compilation under native Lua 5.4 | All **32 Lua source files** compile |
| Core regression suite | **31 groups, 331 assertions passed** |
| Application adapter | **420 gameplay frames**, with construction input, layers, resizing, timeline navigation, save/restore, exports and modal input |
| Benchmark adapter | Two cases; six cooperative yields; valid output metadata |
| Full recorded benchmark | **18 cases, 5,400 timed ticks**, plus warm-ups |
| Default-size end-to-end run | **192 × 112**, seed 12345, cistern, **2,000 ticks**; three survivors, completed farm/bed, food production and consumption |
| Accounting in all recorded benchmark cases and default-size run | Water, mineral, and food residuals **all zero** |

The UI adapter implements only the interfaces used by the application. It invokes
the real Lua drawing/input/gameplay code, but it is still a **mock**, not a substitute
for LÖVE's API implementation. A software rendering of captured drawing calls was
visually inspected; it was not an actual LÖVE screenshot and is not represented as
one in the deliverables.

Full test names and output are retained in `docs/core-test-output.txt`.
The default run is recorded in `docs/default-headless-output.txt`. Its diagnostic
checksum is not cryptographic and should not be treated as portable across runtimes.

## Regression coverage

- Reproducible seeds and three material-world generators, including extreme seeds.
- Cell update stamps, closed boundaries, powder/liquid movement and implemented
  lava/water/ice reactions.
- Body-aware navigation, ladders, blocked routes, and the directed-return-path
  regression: supply collection refuses a known one-way pit until a ladder exists.
- Mining output, actual hauling and delivery, exclusive reservations, blueprint
  escrow, cancellation, material-blocking construction and half-refund demolition.
- Physical pump transfers, blocked outlets, crop water use, worker irrigation and
  the complete arrival build/irrigate/harvest loop.
- Lethal hunger, drowning, and falls; death inventory release; no replacement crew.
- Read-only challenge history, eviction/replay, separate practice branching,
  full-state save/restore including pending commands, invalid commands and versions.
- Data-only codec rejection of malformed or executable-looking input.

These are targeted regressions and end-to-end checks, not exhaustive state-space,
statistical fairness, long-campaign balance, fuzzing, cross-platform or security
audits. Replay is promised only for the same source and supported runtime/build.

## Recorded CPU benchmark

Source: `docs/benchmark-native-lua54.csv` and its `.txt` manifest.

Three presets × three seeds × two scenarios (arrival and construction), each
**128 × 80 fine cells**, with **300 measured simulation ticks** after **20 discarded
warm-up ticks per case**. Clock: `os.clock`, CPU seconds. Automatic collection stays
on. Rendering, history snapshots, cooperative yields and final evaluation are
outside tick timers. Measurements vary with this shared container's workload.

| Measurement across the 18 cases | Observed range |
|---|---:|
| Generation CPU time, ms | 27.898–35.193 |
| Mean CPU time per tick, ms | 4.304–6.153 |
| Nearest-rank p95 CPU tick time, ms | 5.389–9.887 |
| Retained Lua heap delta, KiB | 431.336–543.570 |
| Encoded checkpoint size, bytes | 195233–196432 |

**These numbers are not game FPS, LÖVE/LuaJIT performance, wall-clock latency, or
predictions for the user's machine.** Tick observations come from an evolving world,
not repeated identical stationary trials. The retained heap delta includes case
bookkeeping and timing arrays; it is not a pure world footprint, peak memory, process
RSS or GPU memory. Encoded checkpoint bytes are not retained in-memory snapshot size.

## Reproduce locally

From the project root:

```sh
lua tests/syntax.lua
lua tests/run.lua
lua tests/benchmark_smoke.lua
lua tests/gui_smoke.lua
lua tools/headless.lua 12345 cistern 2000
lua tools/benchmark.lua benchmark-local
```

`luajit` can replace `lua` when installed. The mock GUI test uses POSIX temporary
storage and shell directory creation; it is a developer check, not the actual game.
For the real application, run `love .`, press **F10** for the core suite and **F8**
for an isolated benchmark. Tests pause/block the UI while they execute.

**F6** exports the current metrics, chronicle, live replay data and runtime/inspection
context. **F12** captures the real LÖVE window. Include the source snapshot with local
changes when reporting a discrepancy.

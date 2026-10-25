# Game profiling

`./dev profile` collects developer-owned diagnostics with isolated saves and private Xvfb/software displays. It does not use the host desktop, start a browser or invoke a model. Each invocation requires a new output directory and retains source identity, process identity, collection logs and success/failure evidence.

```sh
./dev profile --kind runtime --live --seconds 15 --install-tools --output artifacts/profile-world
./dev profile --kind graphics --output artifacts/profile-graphics
./dev profile --kind all --scene world-battle --seconds 10 --output artifacts/profile-battle
```

Builds run by default; use `--no-build` only after building the current source. `--scene` accepts the existing playtest scenes. `--live` advances exploration during runtime collection; without it, the explicit preview remains paused. Graphics capture retains a short stationary workload. Never interpret a prepared battle or paused scene as a long-session exploration benchmark.

## Runtime stacks and GC

`--install-tools` installs pinned Microsoft `dotnet-trace` and `dotnet-counters` version `10.0.750501` under ignored `artifacts/diagnostic-tools`, without modifying the global tool path. The repository's SDK launcher runs their DLLs, so a separately registered system runtime is unnecessary. Without that flag, missing tools fail with an explanation.

The tool identifies the actual .NET client inside its owned private-display process tree. A shell wrapper containing the client DLL in its arguments does not qualify as the client. It collects an EventPipe trace with managed stack sampling and GC/allocation events, a Speedscope export, a top-method report and `System.Runtime` counters for process CPU time, allocation rate, collections, pause time, working set and other runtime measures. Captures must contain sampled profiles and counter events to pass.

Stack sampling measures managed thread wall time, including waiting. A high waiting/finalizer percentage in `top-methods.txt` is not proof of CPU consumption. OS-native CPU stacks require a suitable kernel profiler; the runtime tool deliberately does not request machine-wide privileged collection. See [Microsoft's tracing documentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace) and [counter documentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters).

## OpenGL capture and replay

Install `apitrace` with the platform package manager, or pass `--apitrace PATH`. A locally extracted build at `artifacts/diagnostic-tools/apitrace-portable/usr/bin/apitrace` is also recognized; keep its wrapper libraries and `glretrace` in the same installation layout. Local acceptance used Fedora's apitrace `13.0-6.fc44`, extracted under artifacts without a system install. Graphics capture and replay each run inside their own private Xvfb display.

Artifacts include the compressed API trace, JSON frame metadata, a filtered draw/present call list, per-call CPU/GPU-query replay profile and replay errors. Passing requires clean client closure, a nonempty trace, recorded frames/draws and positive CPU/GPU-query draw samples; a profiling header alone cannot pass.

The [MonoGame 3.8.6 SDL backend](https://github.com/MonoGame/MonoGame/blob/v3.8.6/MonoGame.Framework/Platform/GraphicsDeviceManager.SDL.cs) requests an OpenGL 2.1 context, while [RenderDoc documents modern Core OpenGL support](https://github.com/baldurk/renderdoc#api-support). Local RenderDoc trials loaded the injected library but produced no valid capture. The supported workflow uses [apitrace](https://github.com/apitrace/apitrace/blob/master/docs/USAGE.markdown) and preserves the game's graphics configuration.

These are actual API captures and replay queries, but the default driver is software rendering. Query durations include that driver's work and profiling overhead; they are not measurements of the intended hardware GPU or guaranteed gameplay FPS. Use the retained trace for graphics analysis and repeat profiling on the intended hardware when that environment is available. Collection itself changes timing; compare separate uninstrumented `benchmark` / `benchmark-render` runs for performance acceptance.

# Controls and display settings

Press **Escape** or **F1** to open settings. Use the mouse buttons or Up/Down to select a row and Left/Right to adjust it. **Enter**, Escape, F1, or **Resume** returns to the game. **R** or **Reset** restores defaults. Movement, camera look, and block editing stop while the menu is open; terrain streaming and fluid simulation continue.

During play the mouse is captured and the system pointer is hidden, leaving only the center crosshair. Settings and loss of window focus release the pointer; returning to play recaptures it without a camera jump. Linux X11 builds also request native cursor hiding through XFixes when available, alongside GLFW's disabled cursor mode.

| Setting | Behavior |
|---|---|
| Keyboard movement | Shows both walk and flight speeds. Default walking is 4.5 blocks/second (7.2 while sprinting); spectator flight is 14 (56 with Shift). Adjustments scale both speeds, and +/- also works during play. |
| Mouse look | Sensitivity from 0.1× to 5×; default 1×. |
| VSync | Requests synchronized buffer swaps and limits the software cap to the active display refresh rate when available. |
| FPS limit | Display (default), Unlimited, or 30/60/90/120/144/165/240/360 FPS. Unlimited requires VSync off to remove the refresh cap. |

Changes apply immediately and save on resume and exit to `saves/settings.cfg`, separately from world data. `--settings PATH` selects another file. `--no-save` skips settings and world reads/writes. Invalid settings fall back to defaults. Keyboard movement is already independent of frame rate; this setting adjusts travel speed, not key repeat timing.

Normal play starts with **walking, gravity, and block collisions**. **WASD** moves, **Space** jumps, and **Shift** sprints. In deep water or lava, Space swims upward and Ctrl swims downward. Press **F4** to toggle **spectator/noclip**, using the previous free-flight controls: WASD, Space up, Ctrl down, and Shift for faster flight. Start with `--spectator` to select that mode on launch. The hotbar shows the active mode. Benchmarks retain free flight for reproducible camera paths. Opening settings freezes the player's motion; streaming and fluid simulation continue.

The renderer checks the current video mode of the monitor covering the largest part of the window once per second. Moving to another monitor or changing its mode updates the detected refresh rate and cap. If the display reports no usable rate, the panel says **DISPLAY UNKNOWN** and does not invent a 60 Hz limit. A software limiter supplements VSync when drivers or compositors ignore the swap request. It sleeps rather than spinning, using a high-resolution waitable timer on supported Windows systems.

**FPS is measured from completed frames**, including CPU work, buffer swapping, and cap waits. GPU query time is a separate measurement and is never used to calculate FPS. The HUD labels the measured rate, display Hz, active cap, and software renderers such as llvmpipe. The frame-time graph's reference line follows the active cap (or detected refresh when uncapped).

Refresh Hz is the display's configured scanout limit, not a prediction of GPU throughput. FPS counts completed application frames; it cannot verify which frames a desktop compositor actually scans out. Uncapped rendering can exceed display Hz. The program does not clamp or fabricate measurements to hide this distinction. Unknown display rates and driver overrides remain observable.

For repeatable benchmarks, `--benchmark` skips saved settings and defaults to uncapped rendering with VSync off. **Benchmark cameras are scripted: mouse look and WASD are disabled.** Launch without `--benchmark` for interactive play, for example `python3 scripts/build.py run -- --fps-limit 60`. Use `--fps-limit 30` to exercise pacing, or `--fps-limit display` for the detected display cap. Normal play also accepts these overrides and `--fps-limit unlimited`. `--no-vsync` disables VSync and defaults to uncapped rendering unless an explicit FPS limit is supplied.

Benchmark CSVs include `display_hz`, `frame_cap` (zero if no software cap is known), `swap_ms`, `pacing_wait_ms`, and `software_renderer`. `frame_ms` covers the completed frame interval. The panel keeps a fixed 240-frame rolling history and refreshes statistical summaries four times per second.

The behavior follows GLFW's [current video mode](https://www.glfw.org/docs/3.3/monitor_guide.html#monitor_modes) and [buffer swap](https://www.glfw.org/docs/3.3/window_guide.html#buffer_swap) APIs. Driver settings can override the requested swap interval.

Run `python3 scripts/build.py test` and `python3 scripts/smoke.py` to check settings parsing, cap selection, measured timing, actual menu input, persistence, and gameplay input isolation. Linux graphical checks save menu previews to `build/settings-menu.png` and `build/settings-small.png`.

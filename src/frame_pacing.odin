package sandbox

import "core:fmt"
import "core:strings"
import glfw "vendor:glfw"

Frame_Pacing :: struct {
	sleeper: Frame_Sleeper,
	display_hz:                                         int,
	last_display_check, completed_at, swap_ms, wait_ms: f64,
	software_renderer:                                  bool,
}

// The monitor overlapping the largest part of the window determines its refresh
// limit. Re-query the current mode so moving windows and mode changes are handled.
display_refresh_rate :: proc(window: glfw.WindowHandle) -> int {
	wx, wy := glfw.GetWindowPos(window)
	ww, wh := glfw.GetWindowSize(window)
	best_area := -1
	hz := 0
	for monitor in glfw.GetMonitors() {
		mode := glfw.GetVideoMode(monitor)
		if mode == nil {continue}
		mx, my := glfw.GetMonitorPos(monitor)
		area :=
			int(max(0, min(wx + ww, mx + mode.width) - max(wx, mx))) *
			int(max(0, min(wy + wh, my + mode.height) - max(wy, my)))
		if area > best_area {best_area = area; hz = max(0, int(mode.refresh_rate))}
	}
	// In particular, virtual displays may report zero. Never invent a refresh rate.
	return hz
}

frame_target :: proc(s: Settings, display_hz: int) -> int {
	target := s.fps_limit
	if target == 0 {target = display_hz}
	if s.vsync && display_hz > 0 {
		target = target > 0 ? min(target, display_hz) : display_hz
	}
	return max(0, target)
}

frame_limit_label :: proc(s: Settings, p: ^Frame_Pacing) -> string {
	target := frame_target(s, p.display_hz)
	if target > 0 {return fmt.tprintf("CAP %d", target)}
	if s.fps_limit == 0 || s.vsync {return "CAP UNKNOWN"}
	return "UNCAPPED"
}

renderer_is_software :: proc(name: string) -> bool {
	lower := strings.to_lower(name)
	defer delete(lower)
	markers := []string {
		"llvmpipe",
		"softpipe",
		"software",
		"swiftshader",
		"gdi generic",
		"basic render",
	}
	for marker in markers {
		if strings.contains(lower, marker) {return true}
	}
	return false
}

// Run after SwapBuffers: a blocking swap already consumed some or all of the
// interval. The fallback also works when a compositor/driver ignores VSync.
// Sleeping (no busy spin) keeps CPU use low; oversleep remains visible in FPS.
frame_pace :: proc(p: ^Frame_Pacing, s: Settings) -> f64 {
	before := glfw.GetTime()
	target := frame_target(s, p.display_hz)
	if target > 0 {
		remaining := p.completed_at + 1 / f64(target) - before
		if remaining > 0 {frame_sleep(&p.sleeper, remaining)}
	}
	after := glfw.GetTime()
	p.wait_ms = (after - before) * 1000
	ms := (after - p.completed_at) * 1000
	p.completed_at = after
	return ms
}

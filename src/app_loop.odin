package sandbox

import "core:fmt"
import "core:os"
import "core:time"
import gl "vendor:OpenGL"
import glfw "vendor:glfw"

Frame_Status :: enum {
	Ready,
	Skip,
	Quit,
}

app_loop :: proc(a: ^App) {
	for !glfw.WindowShouldClose(a.window) {
		app_clock(a)
		if !app_controls(a) {break}
		app_update_gameplay(a)
		session_update_save(a)
		status := app_prepare_frame(a)
		if status == .Quit {break}
		if status == .Skip {continue}
		a.immersion = renderer_world(
			&a.renderer,
			&a.stream,
			&a.draw_items,
			&a.world,
			{
				position = a.position,
				forward = a.forward,
				hit = a.hit,
				width = a.width,
				height = a.height,
				frame = a.frame,
				elapsed = a.elapsed,
				draw_outline = !a.menu.open,
			},
		)
		if !a.config.no_hud {hud_draw(a)}
		if app_present(a) {break}
		free_all(context.temp_allocator)
	}
}

app_clock :: proc(a: ^App) {
	glfw.PollEvents()
	a.now = glfw.GetTime()
	a.elapsed = a.now - a.start
	a.dt = clamp(a.now - a.last, 0, 0.1)
	a.last = a.now
	telemetry_update(&a.telemetry, &a.stream, a.now)
	if a.now - a.pacing.last_display_check >= 1 {
		a.pacing.display_hz = display_refresh_rate(a.window)
		a.pacing.last_display_check = a.now
	}
}

app_prepare_frame :: proc(a: ^App) -> Frame_Status {
	if a.width <= 0 || a.height <= 0 {
		time.sleep(16 * time.Millisecond)
		return .Skip
	}
	// Reserve double-buffered color/depth storage conservatively on resize.
	available := a.config.gpu_budget - graphics_reserved_bytes(0, int(a.width), int(a.height))
	if available < MiB {
		ww, wh := glfw.GetWindowSize(a.window)
		if ww <= 320 && wh <= 240 {
			fmt.eprintln("Budget cannot accommodate the framebuffer")
			return .Quit
		}
		glfw.SetWindowSize(a.window, max(320, ww / 2), max(240, wh / 2))
		return .Skip
	}
	old_budget := a.stream.mesh_budget
	a.stream.mesh_budget = min(a.config.mesh_budget, available, a.stream.device_mesh_limit)
	if a.stream.mesh_budget > old_budget {
		for _, c in a.stream.chunks {
			if c.budget_deferred || c.oversized {
				c.budget_deferred = false
				c.oversized = false
				c.complete = false
			}
		}
		a.stream.radius = a.stream.configured_radius
		stream_reconcile(&a.stream, &a.renderer, a.position, true)
	}
	if a.renderer.mesh_bytes > a.stream.mesh_budget {
		stream_pressure(&a.stream, &a.renderer, a.position)
	}
	stream_settings(&a.stream, &a.renderer, a.position, a.settings)
	stream_reconcile(&a.stream, &a.renderer, a.position)
	stream_collect(&a.stream, &a.renderer, a.position)
	stream_schedule(&a.stream, &a.world, a.position)
	if a.config.benchmark_ready && a.benchmark_ready_at == 0 {
		requested, ready := stream_readiness(&a.stream)
		if requested > 0 && ready == requested {a.benchmark_ready_at = a.elapsed}
	}

	return .Ready
}

app_present :: proc(a: ^App) -> bool {
	renderer_end(&a.renderer)
	a.telemetry.main_work_ms = (glfw.GetTime() - a.now) * 1000
	error := gl.GetError()
	if error != gl.NO_ERROR {
		fmt.eprintf("OpenGL error: 0x%x\n", error)
		os.exit(1)
	}
	ending :=
		(a.config.frames > 0 && a.frame == a.config.frames - 1) ||
		(a.config.seconds > 0 && (
			(a.config.benchmark_ready && a.benchmark_ready_at == 0 && a.elapsed >= 60) ||
			((!a.config.benchmark_ready || a.benchmark_ready_at > 0) && a.elapsed >= a.benchmark_ready_at + f64(a.config.seconds))))
	if a.config.screenshot != "" && ending {
		if !screenshot(a.width, a.height, a.config.screenshot) {
			fmt.eprintln("Screenshot failed")
		}
	}
	swap_start := glfw.GetTime()
	glfw.SwapBuffers(a.window)
	a.pacing.swap_ms = (glfw.GetTime() - swap_start) * 1000
	ms := frame_pace(&a.pacing, a.settings)
	telemetry_record_frame(&a.telemetry, &a.renderer, ms)

	benchmark_record(a, ms)
	a.frame += 1
	return ending
}

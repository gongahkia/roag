package sandbox

import "core:fmt"
import "core:math"
import gl "vendor:OpenGL"

FRAME_COLOR :: F4{0.40, 0.77, 0.96, 1}
GPU_COLOR :: F4{0.75, 0.55, 0.98, 1}
CPU_COLOR :: F4{1.0, 0.69, 0.29, 1}
RAM_COLOR :: F4{0.38, 0.85, 0.62, 1}

chart_segment :: proc(r: ^Renderer, x1, y1, x2, y2: f32, tint: F4) {
	dx, dy := x2 - x1, y2 - y1
	length := math.sqrt(dx * dx + dy * dy)
	if length < 0.001 {return}
	nx, ny := -dy / length * 0.7, dx / length * 0.7
	if len(r.flats) + 6 > cap(r.flats) {return}
	a := Flat_Vertex{{x1 + nx, y1 + ny, 0}, tint}
	b := Flat_Vertex{{x2 + nx, y2 + ny, 0}, tint}
	c := Flat_Vertex{{x2 - nx, y2 - ny, 0}, tint}
	d := Flat_Vertex{{x1 - nx, y1 - ny, 0}, tint}
	append(&r.flats, a, b, c, a, c, d)
}

chart_plot :: proc(
	r: ^Renderer,
	h: ^History,
	x, y, w, height: f32,
	ceiling: f64,
	tint: F4,
	fill: bool = true,
) {
	if h.count == 0 || ceiling <= 0 {return}
	step := w / f32(max(h.count - 1, 1))
	px, py: f32
	for i in 0 ..< h.count {
		value := clamp(history_at(h, i) / ceiling, 0, 1)
		xx := x + f32(i) * step
		yy := y + height - f32(value) * height
		if fill {shade := tint; shade.a = 0.20; rect(r, xx, yy, min(max(step, 1), max(0, x + w - xx)), y + height - yy, shade)}
		if i > 0 {chart_segment(r, px, py, xx, yy, tint)}
		px, py = xx, yy
	}
}

chart_grid :: proc(r: ^Renderer, x, y, w, height: f32) {
	rect(r, x, y, w, height, {0.025, 0.04, 0.055, 0.92})
	for i in 1 ..< 4 {rect(r, x, y + height * f32(i) / 4, w, 1, {0.17, 0.23, 0.28, 0.5})}
}

hud_line :: proc(r: ^Renderer, x, y, width: f32, s: string, color: F4 = {0.83, 0.88, 0.91, 1}) {
	max_chars := max(0, int(width / 6))
	text_draw(r, x, y, s[:min(len(s), max_chars)], 1, color)
}

benchmark_panel :: proc(a: ^App, ortho: Mat4) {
	r := &a.renderer
	s := &a.stream
	c := a.config
	t := &a.telemetry
	pacing := &a.pacing
	settings := a.settings
	fluids := &a.fluids
	player := &a.player
	pos := a.position
	width, height := a.width, a.height
	elapsed, speed := a.elapsed, a.settings.move_speed

	full := width >= 900 && height >= 520
	panel_w := full ? 424.0 : min(344.0, max(144.0, f32(width) * 0.46))
	panel_h := full ? 412.0 : min(244.0, f32(height) - 96)
	x, y: f32 = 12, 12
	rect(r, x, y, panel_w, panel_h, {0.035, 0.055, 0.075, 0.94})
	rect(r, x, y, 2, panel_h, {0.31, 0.67, 0.75, 0.95})
	inner := panel_w - 20
	font_scale: f32 = full ? 2.3 : 1.5
	text_draw(r, x + 10, y + 10, fmt.tprintf("FPS %.0F", t.frames.fps), font_scale, FRAME_COLOR)
	if panel_w >=
	   250 {text_draw(r, x + panel_w - 115, y + 12, fmt.tprintf("%.2F MS", t.frames.mean_ms), 1.3)}
	hud_line(
		r,
		x + 10,
		y + 32,
		inner,
		full ? fmt.tprintf("MEASURED / 1%% LOW %.0F / FRAME %d", t.frames.low_1pct_fps, t.frame_count) : fmt.tprintf("%s / %s", pacing.display_hz > 0 ? fmt.tprintf("%dHZ", pacing.display_hz) : "HZ N/A", frame_limit_label(settings, pacing)),
	)
	frame_top := y + (full ? 61 : 49)
	frame_h: f32 = full ? 40 : 24
	frame_ceiling := max(1000.0 / 30, t.frames.max_ms)
	if full {
		display :=
			pacing.display_hz > 0 ? fmt.tprintf("DISPLAY %d HZ", pacing.display_hz) : "DISPLAY UNKNOWN"
		hud_line(
			r,
			x + 10,
			y + 49,
			inner,
			fmt.tprintf(
				"%s / %s / GPU %.2F MS",
				display,
				frame_limit_label(settings, pacing),
				r.gpu_ms,
			),
			FRAME_COLOR,
		)
	}
	chart_grid(r, x + 10, frame_top, inner, frame_h)
	goal := frame_target(settings, pacing.display_hz)
	if goal <= 0 {goal = pacing.display_hz}
	if goal > 0 {
		frame_ceiling = max(frame_ceiling, 1000 / f64(goal))
		reference := frame_top + frame_h - f32((1000 / f64(goal)) / frame_ceiling) * frame_h
		rect(r, x + 10, reference, inner, 1, {0.95, 0.73, 0.36, 0.65})
	}
	chart_plot(r, &t.frame_history, x + 10, frame_top, inner, frame_h, frame_ceiling, FRAME_COLOR)
	chart_plot(
		r,
		&t.gpu_history,
		x + 10,
		frame_top,
		inner,
		frame_h,
		frame_ceiling,
		GPU_COLOR,
		false,
	)
	flat_flush(r, ortho, gl.TRIANGLES)
	sys_top := full ? y + 114 : y + 81
	half := (inner - 10) / 2
	cpu_label :=
		!t.process.cpu_valid ? "CPU N/A" : (t.cpu_interval_valid ? fmt.tprintf("CPU %.1F%%", t.cpu_percent) : "CPU SAMPLING")
	ram_label :=
		t.process.ram_valid ? fmt.tprintf("RAM %.1F MIB", f64(t.process.rss_bytes) / MiB) : "RAM N/A"
	hud_line(r, x + 10, sys_top, half, cpu_label, CPU_COLOR)
	hud_line(r, x + 20 + half, sys_top, half, ram_label, RAM_COLOR)
	graph_h: f32 = full ? 38 : 24
	chart_grid(r, x + 10, sys_top + 13, half, graph_h)
	chart_grid(r, x + 20 + half, sys_top + 13, half, graph_h)
	ram_ceiling: f64 = 64
	for i in 0 ..< t.ram_history.count {ram_ceiling = max(ram_ceiling, math.ceil(history_at(&t.ram_history, i) * 1.10 / 64) * 64)}
	chart_plot(r, &t.cpu_history, x + 10, sys_top + 13, half, graph_h, 100, CPU_COLOR)
	chart_plot(
		r,
		&t.ram_history,
		x + 20 + half,
		sys_top + 13,
		half,
		graph_h,
		ram_ceiling,
		RAM_COLOR,
	)
	if full {
		hud_line(
			r,
			x + 10,
			sys_top + 55,
			half,
			fmt.tprintf(
				"%.0F S / %d LOGICAL CPUS",
				f64(max(0, t.cpu_history.count - 1)) * SAMPLE_INTERVAL,
				t.logical_cores,
			),
		)
		hud_line(
			r,
			x + 20 + half,
			sys_top + 55,
			half,
			fmt.tprintf(
				"%.0F S / %.0F MIB SCALE",
				f64(max(0, t.ram_history.count - 1)) * SAMPLE_INTERVAL,
				ram_ceiling,
			),
		)
	}
	flat_flush(r, ortho, gl.TRIANGLES)
	graphics := graphics_reserved_bytes(r.mesh_bytes, int(width), int(height))
	cull := r.cull_tested > 0 ? 100 * (1 - f64(r.visible_chunks) / f64(r.cull_tested)) : 0
	profile := "TINY"; if c.profile == "low" {profile = "LOW"}; if c.profile == "standard" {profile = "STANDARD"}
	if full {
		lines := []string {
			fmt.tprintf(
				"FRAME MIN %.2F / MAX %.2F / MAIN %.2F MS",
				t.frames.min_ms,
				t.frames.max_ms,
				t.main_work_ms,
			),
			fmt.tprintf(
				"P50 %.2F / P95 %.2F / P99 %.2F MS",
				t.frames.p50_ms,
				t.frames.p95_ms,
				t.frames.p99_ms,
			),
			fmt.tprintf("CPU/F %.2F MS / %.2F ACTIVE CORES", t.cpu_ms_per_frame, t.cpu_cores),
			fmt.tprintf(
				"RSS PEAK %.1F MIB / PAGE FAULTS %d",
				f64(t.process.peak_rss_bytes) / MiB,
				t.process.page_faults,
			),
			fmt.tprintf(
				"HEAP %.2F / PEAK %.2F / LIMIT %d MIB",
				f64(t.heap_bytes) / MiB,
				f64(t.heap_peak_bytes) / MiB,
				c.cpu_budget / MiB,
			),
			fmt.tprintf(
				"GFX RESV %.1F/%d / MESH %.1F/%d MIB",
				f64(graphics) / MiB,
				c.gpu_budget / MiB,
				f64(r.mesh_bytes) / MiB,
				s.mesh_budget / MiB,
			),
			fmt.tprintf(
				"DRAWS %d / CULLED %.1F%% / UI %d",
				r.draw_calls,
				cull,
				r.last_overlay_draws,
			),
			fmt.tprintf(
				"TRI %d / FLQ %d / FL %.2F / PHYS %.2F MS",
				r.triangles,
				fluids.count,
				fluids.last_ms,
				player.last_ms,
			),
			fmt.tprintf("CHUNKS %d READY / %d RES / RADIUS %d", t.ready, t.resident, s.radius),
			fmt.tprintf(
				"JOBS %d/%d BUSY / %d QUEUED / %.0F CH/S",
				t.busy,
				s.worker_count,
				t.queued,
				t.chunks_per_second,
			),
			fmt.tprintf(
				"GEN %.2F / MESH %.2F MS / UPLOAD %d KB",
				s.last_generation_ms,
				s.last_meshing_ms,
				s.last_upload_bytes / 1024,
			),
			fmt.tprintf(
				"CACHE %d / %.2F MIB / REUSED %d",
				r.pool_count,
				f64(r.pool_bytes) / MiB,
				r.reused_buffers,
			),
			fmt.tprintf(
				"PRESS %d / STALE %d / OVER %d / FLUID LIMIT %d",
				s.pressure,
				s.stale,
				s.oversized,
				fluids.refused,
			),
			fmt.tprintf(
				"SLOW 33MS %d / 50MS %d / WORST %.1F MS",
				t.slow_33ms,
				t.slow_50ms,
				t.worst_frame_ms,
			),
			fmt.tprintf(
				"%s / %dX%d / VSYNC %s / %s",
				profile,
				width,
				height,
				c.vsync ? "ON" : "OFF",
				pacing.software_renderer ? "SOFTWARE" : "GPU",
			),
			fmt.tprintf(
				"XYZ %.0F %.0F %.0F / %s %.1F / SEED %d",
				pos.x,
				pos.y,
				pos.z,
				player.spectator ? "FLY" : "WALK",
				player.spectator ? speed : speed * WALK_SPEED_SCALE,
				c.seed,
			),
		}
		for line, i in lines {
			hud_line(r, x + 10, y + 187 + f32(i) * 13, inner, line)
			if i % 4 == 3 {flat_flush(r, ortho, gl.TRIANGLES)}
		}
	} else {
		lines := []string {
			fmt.tprintf(
				"P95 %.1F / P99 %.1F / GPU %.1F MS",
				t.frames.p95_ms,
				t.frames.p99_ms,
				r.gpu_ms,
			),
			fmt.tprintf("HEAP %.1F / GFX %.1F MIB", f64(t.heap_bytes) / MiB, f64(graphics) / MiB),
			fmt.tprintf("%d DRAWS / %d TRI", r.draw_calls, r.triangles),
			fmt.tprintf("JOBS %d/%d / QUEUED %d", t.busy, s.worker_count, t.queued),
			fmt.tprintf("GEN %.1F / MESH %.1F MS", s.last_generation_ms, s.last_meshing_ms),
			fmt.tprintf("UPLOAD %d KB / REUSE %d", s.last_upload_bytes / 1024, r.reused_buffers),
			fmt.tprintf("PRESSURE %d / STALE %d", s.pressure, s.stale),
			fmt.tprintf("FRAME %d / %.1F S", t.frame_count, elapsed),
		}
		available := max(0, int((panel_h - 127) / 12))
		for i in 0 ..< min(len(lines), available) {hud_line(r, x + 10, y + 127 + f32(i) * 12, inner, lines[i]); if i % 4 == 3 {flat_flush(r, ortho, gl.TRIANGLES)}}
	}
	flat_flush(r, ortho, gl.TRIANGLES)
}

package sandbox

import "core:fmt"
import "core:math"
import "core:mem"
import "core:os"
import "core:slice"
import "core:strconv"
import "core:sync"
import "core:time"
import gl "vendor:OpenGL"
import glfw "vendor:glfw"

Config :: struct {
	profile:                            string,
	width, height:                      int,
	radius, workers:                    int,
	gpu_budget, mesh_budget:            int,
	cpu_budget, upload_budget:          int,
	seed:                               u64,
	frames, seconds:                    int,
	benchmark, save_path, screenshot:   string,
	hidden, no_save, vsync, naive_mesh: bool,
}

usage :: proc() {
	fmt.println(
		`VOXEL / creative sandbox
Usage: voxel [--profile tiny|low|standard] [--seed N] [--radius N]
  --gpu-mb N --cpu-mb N --workers N --upload-kb N --width N --height N
  --save PATH --no-save --no-vsync --hidden --frames N --seconds N --screenshot PATH
  --benchmark normal|flight|edit|dense --naive-mesh (benchmark baseline)
WASD fly / SPACE up / CTRL down / SHIFT fast / +/- speed
Mouse look / ESC release mouse / click recapture / Q quit
LMB break / RMB place / 1-6 select / F3 stats / F5 save`,
	)
}

config_parse :: proc() -> (Config, bool) {
	c := Config {
		profile       = "tiny",
		seed          = 42,
		workers       = 2,
		upload_budget = 2 * MiB,
		save_path     = "saves/creative.vox",
		vsync         = true,
	}
	for i := 1; i < len(os.args); i += 1 {
		if os.args[i] == "--profile" {
			if i + 1 >= len(os.args) {
				return c, false
			}
			c.profile = os.args[i + 1]
		}
	}
	switch c.profile {
	case "tiny":
		c.width = 960
		c.height = 540
		c.radius = 4
		c.gpu_budget = 64 * MiB
		c.mesh_budget = 40 * MiB
		c.cpu_budget = 256 * MiB
	case "low":
		c.width = 1280
		c.height = 720
		c.radius = 8
		c.gpu_budget = 128 * MiB
		c.mesh_budget = 88 * MiB
		c.cpu_budget = 512 * MiB
	case "standard":
		c.width = 1920
		c.height = 1080
		c.radius = 12
		c.gpu_budget = 256 * MiB
		c.mesh_budget = 184 * MiB
		c.cpu_budget = 1024 * MiB
	case:
		return c, false
	}
	for i := 1; i < len(os.args); i += 1 {
		arg := os.args[i]
		switch arg {
		case "--help", "-h":
			usage()
			os.exit(0)
		case "--no-save":
			c.no_save = true
		case "--hidden":
			c.hidden = true
		case "--no-vsync":
			c.vsync = false
		case "--naive-mesh":
			c.naive_mesh = true
		case:
			if i + 1 >= len(os.args) {
				return c, false
			}
			i += 1
			v := os.args[i]
			if arg == "--profile" {
				continue
			}
			if arg == "--save" {
				c.save_path = v
				continue
			}
			if arg == "--screenshot" {
				c.screenshot = v
				continue
			}
			if arg == "--benchmark" {
				c.benchmark = v
				continue
			}
			if arg == "--seed" {
				n, err := strconv.parse_u64(v)
				if !err {
					return c, false
				}
				c.seed = n
				continue
			}
			n, err := strconv.parse_int(v)
			if !err || n < 0 || n > 1000000 {
				return c, false
			}
			switch arg {
			case "--width":
				c.width = n
			case "--height":
				c.height = n
			case "--radius":
				c.radius = n
			case "--workers":
				c.workers = n
			case "--gpu-mb":
				c.gpu_budget = n * MiB
				c.mesh_budget = c.gpu_budget * 5 / 8
			case "--cpu-mb":
				c.cpu_budget = n * MiB
			case "--upload-kb":
				c.upload_budget = n * 1024
			case "--frames":
				c.frames = n
			case "--seconds":
				c.seconds = n
			case:
				return c, false
			}
		}
	}
	if c.benchmark != "" &&
	   c.benchmark != "normal" &&
	   c.benchmark != "flight" &&
	   c.benchmark != "edit" &&
	   c.benchmark != "dense" {
		return c, false
	}
	if c.width < 320 ||
	   c.width > 3840 ||
	   c.height < 240 ||
	   c.height > 2160 ||
	   c.radius < 1 ||
	   c.radius > 32 ||
	   c.workers < 1 ||
	   c.workers > 8 ||
	   c.gpu_budget < 16 * MiB ||
	   c.gpu_budget > 4096 * MiB ||
	   c.cpu_budget < 32 * MiB ||
	   c.cpu_budget > 8192 * MiB ||
	   c.upload_budget < 16 * 1024 ||
	   c.upload_budget > 64 * MiB ||
	   c.frames < 0 {
		return c, false
	}
	c.workers = min(c.workers, max(1, (c.cpu_budget - 12 * MiB) / (16 * MiB)))
	c.mesh_budget = min(c.mesh_budget, c.gpu_budget - 2 * MiB - c.width * c.height * 16)
	if c.mesh_budget < MiB {
		return c, false
	}
	if c.benchmark != "" {
		c.no_save = true
		c.vsync = false
		if c.frames == 0 && c.seconds == 0 {
			c.seconds = 30
		}
	}
	return c, true
}

BLOCK_NAMES := [7]string{"AIR", "GRASS", "DIRT", "STONE", "SAND", "WOOD", "LEAVES"}
hud :: proc(
	r: ^Renderer,
	s: ^Stream,
	c: Config,
	pos: V3,
	selected: int,
	show: bool,
	width, height: i32,
	telemetry: ^Telemetry,
	elapsed: f64,
	speed: f64,
) {
	gl.Disable(gl.DEPTH_TEST)
	gl.Disable(gl.CULL_FACE)
	gl.Enable(gl.BLEND)
	gl.BlendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA)
	ortho := identity()
	ortho[0] = 2 / f32(width)
	ortho[5] = -2 / f32(height)
	ortho[12] = -1
	ortho[13] = 1
	if show {
		benchmark_panel(r, s, c, telemetry, pos, width, height, elapsed, speed, ortho)
	}
	flat_flush(r, ortho, gl.TRIANGLES)
	cx, cy := f32(width) / 2, f32(height) / 2
	rect(r, cx - 5, cy - 1, 10, 2, {1, 1, 1, 0.9})
	rect(r, cx - 1, cy - 5, 2, 10, {1, 1, 1, 0.9})
	start := cx - 156
	for i in 1 ..= 6 {
		x := start + f32(i - 1) * 52
		tint: F4 = {0.08, 0.12, 0.14, 0.9}
		if i == selected {
			tint = {0.28, 0.44, 0.35, 0.95}
		}
		rect(r, x, f32(height) - 64, 48, 43, tint)
		text_draw(r, x + 5, f32(height) - 59, fmt.tprintf("%d", i), 1)
		colors := [6]F4 {
			{0.38, 0.6, 0.25, 1},
			{0.5, 0.35, 0.23, 1},
			{0.5, 0.53, 0.56, 1},
			{0.86, 0.79, 0.59, 1},
			{0.45, 0.31, 0.19, 1},
			{0.25, 0.47, 0.23, 1},
		}
		rect(r, x + 17, f32(height) - 52, 18, 18, colors[i - 1])
	}
	text_draw(
		r,
		cx - f32(len(BLOCK_NAMES[selected])) * 4.5,
		f32(height) - 82,
		BLOCK_NAMES[selected],
		1.5,
	)
	flat_flush(r, ortho, gl.TRIANGLES)
	gl.Disable(gl.BLEND)
}

// Read and write one scanline at a time so captures respect small CPU budgets.
screenshot :: proc(width, height: i32, path: string) -> bool {
	file, err := os.create(path)
	if err != nil {return false}
	defer os.close(file)
	header := fmt.tprintf("P6\n%d %d\n255\n", width, height)
	_, header_err := os.write(file, transmute([]u8)header)
	if header_err != nil {return false}
	row := make([]u8, int(width) * 3)
	defer delete(row)
	gl.PixelStorei(gl.PACK_ALIGNMENT, 1)
	gl.ReadBuffer(gl.BACK)
	for y := height - 1; y >= 0; y -= 1 {
		gl.ReadPixels(0, y, width, 1, gl.RGB, gl.UNSIGNED_BYTE, raw_data(row))
		_, write_err := os.write(file, row)
		if write_err != nil {return false}
	}
	return gl.GetError() == gl.NO_ERROR
}

main :: proc() {
	config, valid := config_parse()
	if !valid {
		usage()
		fmt.eprintln("Invalid arguments or insufficient budget for resolution")
		os.exit(2)
	}
	tracker: mem.Tracking_Allocator
	mem.tracking_allocator_init(&tracker, context.allocator)
	context.allocator = mem.tracking_allocator(&tracker)
	defer mem.tracking_allocator_destroy(&tracker)
	w: World
	world_init(&w, config.seed)
	w.tracker = &tracker
	w.allocation_limit = config.cpu_budget - (config.workers * 16 + 8) * MiB
	defer world_destroy(&w)
	pos := V3{8, f64(terrain_height(w.seed, 8, 8)) + 12, 8}
	if !config.no_save && os.exists(config.save_path) {
		if !world_load(&w, &pos, config.save_path) {
			fmt.eprintln("Cannot load world; existing save was preserved:", config.save_path)
			os.exit(1)
		}
		fmt.println("Loaded", config.save_path)
	}
	config.seed = w.seed
	if !glfw.Init() {
		description, _ := glfw.GetError()
		fmt.eprintln("GLFW:", description)
		os.exit(1)
	}
	defer glfw.Terminate()
	glfw.WindowHint(glfw.CONTEXT_VERSION_MAJOR, 3)
	glfw.WindowHint(glfw.CONTEXT_VERSION_MINOR, 3)
	glfw.WindowHint(glfw.OPENGL_PROFILE, glfw.OPENGL_CORE_PROFILE)
	glfw.WindowHint(glfw.OPENGL_FORWARD_COMPAT, true)
	glfw.WindowHint(glfw.SAMPLES, 0)
	glfw.WindowHint(glfw.DEPTH_BITS, 24)
	glfw.WindowHint(glfw.STENCIL_BITS, 0)
	glfw.WindowHint(glfw.VISIBLE, !config.hidden)
	window := glfw.CreateWindow(
		i32(config.width),
		i32(config.height),
		"VOXEL / Creative",
		nil,
		nil,
	)
	if window == nil {
		description, _ := glfw.GetError()
		fmt.eprintln("OpenGL 3.3 window:", description)
		os.exit(1)
	}
	defer glfw.DestroyWindow(window)
	glfw.MakeContextCurrent(window)
	glfw.SwapInterval(i32(int(config.vsync)))
	r: Renderer
	renderer_init(&r)
	defer renderer_destroy(&r)
	fmt.println("GL:", string(gl.GetString(gl.VERSION)), "/", string(gl.GetString(gl.RENDERER)))
	fmt.printf(
		"Profile %s: GPU budget %d MiB, meshes %d MiB, CPU %d MiB\n",
		config.profile,
		config.gpu_budget / MiB,
		config.mesh_budget / MiB,
		config.cpu_budget / MiB,
	)
	stream: Stream
	stream_init(
		&stream,
		config.workers,
		config.radius,
		config.mesh_budget,
		config.cpu_budget,
		config.upload_budget,
		&tracker,
	)
	draw_items := make(
		[dynamic]Draw_Item,
		0,
		(config.radius * 2 + 3) * (config.radius * 2 + 3) * 8,
	)
	defer delete(draw_items)
	stream.naive_mesh = config.naive_mesh
	defer if stream.chunks != nil {stream_destroy(&stream, &r)}
	captured := !config.hidden
	if captured {
		glfw.SetInputMode(window, glfw.CURSOR, glfw.CURSOR_DISABLED)
	}
	yaw, pitch: f64 = -0.8, -0.27
	previous_x, previous_y := glfw.GetCursorPos(window)
	last := glfw.GetTime()
	start := last
	speed: f64 = 14
	selected := 1
	stats := true
	old_keys: [512]bool
	old_mouse: [2]bool
	telemetry: Telemetry
	telemetry_init(&telemetry, last)
	csv: ^os.File
	if config.benchmark != "" {
		os.make_directory("benchmarks")
		path := fmt.tprintf(
			"benchmarks/%s%s.csv",
			config.benchmark,
			config.naive_mesh ? "-naive" : "",
		)
		handle, err := os.create(path)
		if err != nil {
			fmt.eprintln("Cannot write benchmark:", err)
			os.exit(1)
		}
		csv = handle
		fmt.fprintln(
			csv,
			"frame,elapsed_s,frame_ms,p95_ms,p99_ms,gpu_ms,cpu_bytes,mesh_bytes,graphics_reserved_bytes,chunks,radius,draws,triangles,generation_ms,meshing_ms,pressure,stale,upload_bytes,gpu_budget_bytes,cpu_budget_bytes,upload_budget_bytes,fps,low_1pct_fps,process_cpu_percent,rss_bytes,rss_peak_bytes,cpu_sample_valid,ram_sample_valid,heap_peak_bytes,main_work_ms,chunks_per_second,queued_jobs,busy_workers",
		)
		fmt.println("Benchmark:", path)
	}
	defer if csv != nil {
		os.close(csv)
	}
	frame := 0
	for !glfw.WindowShouldClose(window) {
		glfw.PollEvents()
		now := glfw.GetTime()
		elapsed := now - start
		dt := clamp(now - last, 0, 0.1)
		ms := (now - last) * 1000
		last = now
		telemetry_update(&telemetry, &stream, &r, now, ms)
		down: [512]bool
		for k in 32 ..< 349 {
			down[k] = glfw.GetKey(window, i32(k)) == glfw.PRESS
		}
		if down[glfw.KEY_Q] {
			break
		}
		if down[glfw.KEY_ESCAPE] && !old_keys[glfw.KEY_ESCAPE] {
			captured = !captured
			mode := glfw.CURSOR_NORMAL
			if captured {
				mode = glfw.CURSOR_DISABLED
			}
			glfw.SetInputMode(window, glfw.CURSOR, i32(mode))
		}
		if down[glfw.KEY_F3] && !old_keys[glfw.KEY_F3] {
			stats = !stats
		}
		if down[glfw.KEY_EQUAL] && !old_keys[glfw.KEY_EQUAL] {
			speed = min(speed * 1.5, 512)
		}
		if down[glfw.KEY_MINUS] && !old_keys[glfw.KEY_MINUS] {
			speed = max(speed / 1.5, 1)
		}
		for i in 1 ..= 6 {
			if down[glfw.KEY_0 + i] {
				selected = i
			}
		}
		mx, my := glfw.GetCursorPos(window)
		if captured && config.benchmark == "" {
			yaw += (mx - previous_x) * 0.0025
			pitch = clamp(pitch - (my - previous_y) * 0.0025, -1.55, 1.55)
		}
		previous_x, previous_y = mx, my
		forward := direction(yaw, pitch)
		if captured && config.benchmark == "" {
			move: V3
			horizontal := V3{math.cos(yaw), 0, math.sin(yaw)}
			right := V3{-math.sin(yaw), 0, math.cos(yaw)}
			if down[glfw.KEY_W] {
				move += horizontal
			}
			if down[glfw.KEY_S] {
				move -= horizontal
			}
			if down[glfw.KEY_D] {
				move += right
			}
			if down[glfw.KEY_A] {
				move -= right
			}
			if down[glfw.KEY_SPACE] {
				move.y += 1
			}
			if down[glfw.KEY_LEFT_CONTROL] {
				move.y -= 1
			}
			multiplier: f64 = 1
			if down[glfw.KEY_LEFT_SHIFT] {
				multiplier = 4
			}
			pos += normalize(move) * dt * speed * multiplier
		}
		if config.benchmark != "" {
			// Fixed-time path gives equivalent routes even at different frame rates.
			t := max(elapsed - 2, 0)
			if config.benchmark == "flight" {
				cycle := math.mod(t, 24)
				travel := cycle
				if cycle > 12 {
					travel = 24 - cycle
				}
				pos.x = 8 + travel * 38
				pos.z = 8 + math.sin(t * 0.3) * 80
				pos.y = f64(terrain_height(w.seed, i32(pos.x), i32(pos.z))) + 24
				yaw = 0.4
				pitch = -0.35
			} else {
				pos = {8, 100, 8}
				yaw = -0.8
				pitch = -0.5
			}
			forward = direction(yaw, pitch)
			if config.benchmark == "edit" && frame % 8 == 0 {
				p := I3{12 + i32((frame / 8) % 16), 84, 12 + i32((frame / 128) % 16)}
				b: Block = .Stone
				if (frame / 2048) % 2 == 1 {
					b = .Air
				}
				if world_apply(&w, {w.sequence + 1, p, b}) {
					stream_invalidate(&stream, p)
				}
			}
			if config.benchmark == "dense" && frame == 0 {
				for z in 0 ..< 32 {
					for y in 0 ..< 32 {
						for x in 0 ..< 32 {
							p := I3{i32(x), i32(y + 64), i32(z)}
							b: Block = .Air
							if (x + y + z) % 2 == 0 {
								b = .Stone
							}
							world_apply(&w, {w.sequence + 1, p, b})
						}
					}
				}
			}
		}
		pos.y = clamp(pos.y, -16, 1024)
		pos.x = clamp(pos.x, -100000000, 100000000)
		pos.z = clamp(pos.z, -100000000, 100000000)
		mouse := [2]bool {
			glfw.GetMouseButton(window, glfw.MOUSE_BUTTON_LEFT) == glfw.PRESS,
			glfw.GetMouseButton(window, glfw.MOUSE_BUTTON_RIGHT) == glfw.PRESS,
		}
		hit := raycast(&w, pos, forward, 8)
		for pressed, i in mouse {
			if pressed && !old_mouse[i] {
				if !captured {
					captured = true
					glfw.SetInputMode(window, glfw.CURSOR, glfw.CURSOR_DISABLED)
				} else if hit.hit {
					p := hit.block
					b: Block = .Air
					if i == 1 {
						p = hit.adjacent
						b = Block(selected)
					}
					if world_apply(&w, {w.sequence + 1, p, b}) {
						stream_invalidate(&stream, p)
					}
				}
			}
		}
		old_mouse = mouse
		if down[glfw.KEY_F5] && !old_keys[glfw.KEY_F5] && !config.no_save {
			os.make_directory_all(os.dir(config.save_path))
			if !world_save(&w, pos, config.save_path) {
				fmt.eprintln("Save failed")
			}
		}
		old_keys = down
		width, height := glfw.GetFramebufferSize(window)
		if width <= 0 || height <= 0 {
			time.sleep(16 * time.Millisecond)
			continue
		}
		// Reserve double-buffered color/depth storage conservatively on resize.
		available := config.gpu_budget - 2 * MiB - int(width) * int(height) * 16
		if available < MiB {
			ww, wh := glfw.GetWindowSize(window)
			if ww <= 320 && wh <= 240 {
				fmt.eprintln("Budget cannot accommodate the framebuffer")
				break
			}
			glfw.SetWindowSize(window, max(320, ww / 2), max(240, wh / 2))
			continue
		}
		old_budget := stream.mesh_budget
		stream.mesh_budget = min(config.mesh_budget, available, stream.device_mesh_limit)
		if stream.mesh_budget > old_budget {
			for _, c in stream.chunks {
				if c.budget_deferred || c.oversized {
					c.budget_deferred = false
					c.oversized = false
					c.complete = false
				}
			}
			stream.radius = stream.configured_radius
			stream_reconcile(&stream, &r, pos, true)
		}
		if r.mesh_bytes > stream.mesh_budget {
			stream_pressure(&stream, &r, pos)
		}
		stream_reconcile(&stream, &r, pos)
		stream_collect(&stream, &r, pos)
		stream_schedule(&stream, &w, pos)
		renderer_begin(&r, width, height, frame)
		vp := camera_matrix(forward, f32(width) / f32(height), f32((stream.radius + 2) * 32))
		gl.UniformMatrix4fv(r.vp_loc, 1, false, &vp[0])
		gl.Uniform1f(r.fog_loc, f32(stream.radius * 32))
		clear(&draw_items)
		for k, c in stream.chunks {
			if c.gpu.vao == 0 {
				continue
			}
			r.cull_tested += 1
			offset := F3 {
				f32(f64(k.x) * 32 - pos.x),
				f32(f64(k.y) * 32 - pos.y),
				f32(f64(k.z) * 32 - pos.z),
			}
			if !visible(vp, offset) {
				continue
			}
			center := offset + F3{16, 16, 16}
			append(
				&draw_items,
				Draw_Item {
					&c.gpu,
					offset,
					center.x * center.x + center.y * center.y + center.z * center.z,
				},
			)
		}
		slice.sort_by(draw_items[:], draw_nearer)
		for item in draw_items {
			gl.Uniform3f(r.offset_loc, item.offset.x, item.offset.y, item.offset.z)
			gl.BindVertexArray(item.gpu.vao)
			gl.DrawElements(gl.TRIANGLES, item.gpu.count, gl.UNSIGNED_INT, nil)
			r.draw_calls += 1
			r.triangles += int(item.gpu.count) / 3
		}
		if hit.hit {
			outline(&r, hit.block, pos, vp)
		}
		hud(&r, &stream, config, pos, selected, stats, width, height, &telemetry, elapsed, speed)
		renderer_end(&r)
		telemetry.main_work_ms = (glfw.GetTime() - now) * 1000
		error := gl.GetError()
		if error != gl.NO_ERROR {
			fmt.eprintf("OpenGL error: 0x%x\n", error)
			os.exit(1)
		}
		ending :=
			(config.frames > 0 && frame == config.frames - 1) ||
			(config.seconds > 0 && elapsed >= f64(config.seconds))
		if config.screenshot != "" && ending {
			if !screenshot(width, height, config.screenshot) {
				fmt.eprintln("Screenshot failed")
			}
		}
		if csv != nil {
			fmt.fprintf(
				csv,
				"%d,%.4f,%.4f,%.4f,%.4f,%.4f,%d,%d,%d,%d,%d,%d,%d,%.4f,%.4f,%d,%d,%d,%d,%d,%d,%.4f,%.4f,%.4f,%d,%d,%d,%d,%d,%.4f,%.4f,%d,%d\n",
				frame,
				elapsed,
				ms,
				telemetry.frames.p95_ms,
				telemetry.frames.p99_ms,
				r.gpu_ms,
				cpu_bytes(&stream),
				r.mesh_bytes,
				r.mesh_bytes + 2 * MiB + int(width) * int(height) * 16,
				len(stream.chunks),
				stream.radius,
				r.draw_calls,
				r.triangles,
				stream.last_generation_ms,
				stream.last_meshing_ms,
				stream.pressure,
				stream.stale,
				stream.last_upload_bytes,
				config.gpu_budget,
				config.cpu_budget,
				stream.upload_budget,
				telemetry.frames.fps,
				telemetry.frames.low_1pct_fps,
				telemetry.cpu_percent,
				telemetry.process.rss_bytes,
				telemetry.process.peak_rss_bytes,
				int(telemetry.cpu_interval_valid && telemetry.process.cpu_valid),
				int(telemetry.process.ram_valid),
				telemetry.heap_peak_bytes,
				telemetry.main_work_ms,
				telemetry.chunks_per_second,
				telemetry.queued,
				telemetry.busy,
			)
		}
		glfw.SwapBuffers(window)
		frame += 1
		if ending {
			break
		}
		free_all(context.temp_allocator)
	}
	if !config.no_save {
		os.make_directory_all(os.dir(config.save_path))
		if !world_save(&w, pos, config.save_path) {
			fmt.eprintln("Save failed:", config.save_path)
		}
	}
	stream_destroy(&stream, &r)
	sync.mutex_lock(&tracker.mutex)
	peak_cpu := tracker.peak_memory_allocated
	sync.mutex_unlock(&tracker.mutex)
	fmt.printf(
		"Frames %d / CPU peak %.2f MiB / mesh peak %.2f MiB / generated %d / stale %d / oversized %d / reused buffers %d\n",
		frame,
		f64(peak_cpu) / MiB,
		f64(r.peak_bytes) / MiB,
		stream.generated,
		stream.stale,
		stream.oversized,
		r.reused_buffers,
	)
}

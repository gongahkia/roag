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
	profile:                                         string,
	width, height:                                   int,
	radius, workers:                                 int,
	gpu_budget, mesh_budget:                         int,
	cpu_budget, upload_budget:                       int,
	seed:                                            u64,
	frames, seconds:                                 int,
	benchmark, save_path, screenshot, settings_path: string,
	frame_limit:                                     int,
	hidden, no_save, vsync, naive_mesh:              bool,
}

usage :: proc() {
	fmt.println(
		`VOXEL / creative sandbox
Usage: voxel [--profile tiny|low|standard] [--seed N] [--radius N]
  --gpu-mb N --cpu-mb N --workers N --upload-kb N --width N --height N
  --save PATH --no-save --no-vsync --hidden --frames N --seconds N --screenshot PATH
  --settings PATH --fps-limit display|unlimited|30|60|90|120|144|165|240|360
  --benchmark normal|flight|edit|dense|fluids --naive-mesh (benchmark baseline)
WASD fly / SPACE up / CTRL down / SHIFT fast / +/- speed
Mouse look / ESC or F1 settings / click recapture / Q quit
LMB break / RMB place / 1-9 select / TAB palette page / F3 stats / F5 save`,
	)
}

config_parse :: proc() -> (Config, bool) {
	c := Config {
		profile       = "tiny",
		seed          = 42,
		workers       = 2,
		upload_budget = 2 * MiB,
		save_path     = "saves/creative.vox",
		settings_path = "saves/settings.cfg",
		frame_limit   = -2,
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
			if arg == "--settings" {c.settings_path = v; continue}
			if arg == "--fps-limit" {
				if v == "display" {c.frame_limit = 0; continue}
				if v == "unlimited" {c.frame_limit = -1; continue}
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
			case "--fps-limit":
				c.frame_limit = n
			case:
				return c, false
			}
		}
	}
	if c.frame_limit != -2 {
		test_settings := settings_defaults(); test_settings.fps_limit = c.frame_limit
		if !settings_valid(test_settings) {return c, false}
	}
	if c.benchmark != "" &&
	   c.benchmark != "normal" &&
	   c.benchmark != "flight" &&
	   c.benchmark != "edit" &&
	   c.benchmark != "fluids" &&
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

hud :: proc(
	r: ^Renderer,
	s: ^Stream,
	c: Config,
	pos: V3,
	selected: int,
	show: bool,
	width, height: i32,
	telemetry: ^Telemetry,
	menu: ^Settings_Menu,
	settings: Settings,
	pacing: ^Frame_Pacing,
	fluids: ^Fluid_Sim,
	immersion: Block,
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
	if immersion != .Air {
		tint: F4 = immersion == .Water ? F4{0.08, 0.27, 0.54, 0.23} : F4{0.9, 0.23, 0.02, 0.48}
		rect(r, 0, 0, f32(width), f32(height), tint)
		flat_flush(r, ortho, gl.TRIANGLES)
	}
	if menu.open {
		settings_panel(r, menu, settings, pacing, width, height, ortho)
		gl.Disable(gl.BLEND)
		return
	}
	if show {
		benchmark_panel(
			r,
			s,
			c,
			telemetry,
			pacing,
			settings,
			fluids,
			pos,
			width,
			height,
			elapsed,
			speed,
			ortho,
		)
	}
	flat_flush(r, ortho, gl.TRIANGLES)
	cx, cy := f32(width) / 2, f32(height) / 2
	rect(r, cx - 5, cy - 1, 10, 2, {1, 1, 1, 0.9})
	rect(r, cx - 1, cy - 5, 2, 10, {1, 1, 1, 0.9})
	page := selected / 9
	pitch := min(44, (f32(width) - 16) / 9)
	start := cx - pitch * 4.5
	for i in 0 ..< 9 {
		index := page * 9 + i
		x := start + f32(i) * pitch
		tint: F4 = {0.08, 0.12, 0.14, 0.9}
		if index == selected {
			tint = {0.28, 0.44, 0.35, 0.95}
		}
		rect(r, x, f32(height) - 64, pitch - 3, 43, tint)
		if index >= len(CREATIVE_BLOCKS) {continue}
		text_draw(r, x + 4, f32(height) - 59, fmt.tprintf("%d", i + 1), 1)
		b := CREATIVE_BLOCKS[index]
		rect(r, x + pitch / 2 - 7, f32(height) - 49, 15, 16, BLOCK_COLORS[int(b)])
		if b >= .Coal_Ore && b <= .Redstone_Ore {
			rect(r, x + pitch / 2 - 5, f32(height) - 47, 4, 5, BLOCK_COLORS[int(Block.Stone)])
		}
	}
	name := BLOCK_NAMES[int(CREATIVE_BLOCKS[selected])]
	text_draw(r, cx - f32(len(name)) * 4.5, f32(height) - 82, name, 1.5)
	text_draw(r, cx - 42, f32(height) - 12, fmt.tprintf("TAB: PAGE %d/2", page + 1), 1)
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
	if w.generator ==
	   1 {fmt.println("Legacy terrain preserved; a new save generates ores and natural fluids.")}
	settings := settings_defaults()
	if !config.no_save && os.exists(config.settings_path) {
		loaded, ok := settings_load(config.settings_path)
		if ok {settings = loaded} else {fmt.eprintln("Invalid settings; using defaults:", config.settings_path)}
	}
	if !config.vsync {settings.vsync = false; settings.fps_limit = -1}
	if config.frame_limit != -2 {settings.fps_limit = config.frame_limit}
	config.vsync = settings.vsync
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
	pacing := Frame_Pacing {
		sleeper           = frame_sleep_init(),
		display_hz        = display_refresh_rate(window),
		software_renderer = renderer_is_software(string(gl.GetString(gl.RENDERER))),
	}
	defer frame_sleep_destroy(&pacing.sleeper)
	fmt.printf(
		"Display: %d Hz / %s / VSync %s / software renderer %t\n",
		pacing.display_hz,
		frame_limit_label(settings, &pacing),
		settings.vsync ? "on" : "off",
		pacing.software_renderer,
	)
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
	pacing.completed_at = last
	pacing.last_display_check = last
	menu: Settings_Menu
	selected := 0
	fluids: Fluid_Sim
	fluids_init(&fluids)
	defer fluids_destroy(&fluids)
	if config.benchmark == "fluids" {fluid_demo(&w)}
	fluids_reseed(&fluids, &w, pos, last)
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
			"frame,elapsed_s,frame_ms,p95_ms,p99_ms,gpu_ms,cpu_bytes,mesh_bytes,graphics_reserved_bytes,chunks,radius,draws,triangles,generation_ms,meshing_ms,pressure,stale,upload_bytes,gpu_budget_bytes,cpu_budget_bytes,upload_budget_bytes,fps,low_1pct_fps,process_cpu_percent,rss_bytes,rss_peak_bytes,cpu_sample_valid,ram_sample_valid,heap_peak_bytes,main_work_ms,chunks_per_second,queued_jobs,busy_workers,display_hz,frame_cap,swap_ms,pacing_wait_ms,software_renderer",
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
		last = now
		telemetry_update(&telemetry, &stream, now)
		if now - pacing.last_display_check >= 1 {
			pacing.display_hz = display_refresh_rate(window)
			pacing.last_display_check = now
		}
		down: [512]bool
		for k in 32 ..< 349 {
			down[k] = glfw.GetKey(window, i32(k)) == glfw.PRESS
		}
		if down[glfw.KEY_Q] && !menu.open {
			break
		}
		mouse := [2]bool {
			glfw.GetMouseButton(window, glfw.MOUSE_BUTTON_LEFT) == glfw.PRESS,
			glfw.GetMouseButton(window, glfw.MOUSE_BUTTON_RIGHT) == glfw.PRESS,
		}
		width, height := glfw.GetFramebufferSize(window)
		mx, my := glfw.GetCursorPos(window)
		menu_was_open := menu.open
		if (down[glfw.KEY_ESCAPE] && !old_keys[glfw.KEY_ESCAPE]) ||
		   (down[glfw.KEY_F1] && !old_keys[glfw.KEY_F1]) {menu.open = !menu.open}
		if menu.open {
			ww, wh := glfw.GetWindowSize(window)
			ui_x := mx * f64(width) / f64(max(ww, 1))
			ui_y := my * f64(height) / f64(max(wh, 1))
			if settings_menu_input(
				&menu,
				&settings,
				&down,
				&old_keys,
				mouse[0] && !old_mouse[0] && menu_was_open,
				ui_x,
				ui_y,
				width,
				height,
			) {menu.open = false}
		}
		capture_changed := menu.open != menu_was_open
		if capture_changed {
			captured = !menu.open
			glfw.SetInputMode(
				window,
				glfw.CURSOR,
				captured ? glfw.CURSOR_DISABLED : glfw.CURSOR_NORMAL,
			)
			mx, my = glfw.GetCursorPos(window)
			previous_x, previous_y = mx, my
			if !menu.open && !config.no_save {
				menu.save_failed = !settings_save(settings, config.settings_path)
				if menu.save_failed {fmt.eprintln("Settings save failed:", config.settings_path)}
			}
		}
		if settings.vsync != config.vsync {
			config.vsync = settings.vsync
			glfw.SwapInterval(i32(int(settings.vsync)))
		}
		if down[glfw.KEY_F3] && !old_keys[glfw.KEY_F3] {
			stats = !stats
		}
		if !menu.open && down[glfw.KEY_EQUAL] && !old_keys[glfw.KEY_EQUAL] {
			settings.move_speed = min(settings.move_speed * 1.5, 512)
		}
		if !menu.open && down[glfw.KEY_MINUS] && !old_keys[glfw.KEY_MINUS] {
			settings.move_speed = max(settings.move_speed / 1.5, 1)
		}
		if !menu.open && down[glfw.KEY_TAB] && !old_keys[glfw.KEY_TAB] {
			page := (selected / 9 + 1) % 2
			selected = min(page * 9 + selected % 9, len(CREATIVE_BLOCKS) - 1)
		}
		for i in 1 ..= 9 {
			if !menu.open && down[glfw.KEY_0 + i] {
				index := selected / 9 * 9 + i - 1
				if index < len(CREATIVE_BLOCKS) {selected = index}
			}
		}
		if captured && !capture_changed && config.benchmark == "" {
			yaw += (mx - previous_x) * 0.0025 * settings.mouse_sensitivity
			pitch = clamp(
				pitch - (my - previous_y) * 0.0025 * settings.mouse_sensitivity,
				-1.55,
				1.55,
			)
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
			pos += normalize(move) * dt * settings.move_speed * multiplier
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
			if config.benchmark == "fluids" {pos = {26, 97, 26}; yaw = -2.25; pitch = -0.55}
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
		hit := raycast(&w, pos, forward, 8)
		for pressed, i in mouse {
			if pressed && !old_mouse[i] && !menu.open && !menu_was_open && !capture_changed {
				if !captured {
					captured = true
					glfw.SetInputMode(window, glfw.CURSOR, glfw.CURSOR_DISABLED)
					previous_x, previous_y = glfw.GetCursorPos(window)
				} else if hit.hit {
					p := hit.block
					b: Block = .Air
					if i == 1 {
						p = hit.adjacent
						b = CREATIVE_BLOCKS[selected]
						if is_fluid(world_get(&w, hit.block)) {p = hit.block}
					}
					previous := world_get(&w, p)
					if world_apply(&w, {w.sequence + 1, p, b}) {
						stream_invalidate(&stream, p)
						fluids_notify(&fluids, &w, p, previous)
					}
				}
			}
		}
		old_mouse = mouse
		fluids_reseed(&fluids, &w, pos, now)
		fluids_step(&fluids, &w, u64(now * 10))
		for i in 0 ..< fluids.changed_count {stream_invalidate(&stream, fluids.changed[i])}
		if down[glfw.KEY_F5] && !old_keys[glfw.KEY_F5] && !config.no_save {
			os.make_directory_all(os.dir(config.save_path))
			if !world_save(&w, pos, config.save_path) {
				fmt.eprintln("Save failed")
			}
		}
		old_keys = down
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
		gl.Uniform1f(r.time_loc, f32(math.mod(elapsed, 4096)))
		camera_block := world_get(
			&w,
			{i32(math.floor(pos.x)), i32(math.floor(pos.y)), i32(math.floor(pos.z))},
		)
		immersion := fluid_kind(camera_block)
		gl.Uniform1i(r.immersion_loc, immersion == .Water ? 1 : (immersion == .Lava ? 2 : 0))
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
			r.visible_chunks += 1
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
			count := item.gpu.count - item.gpu.water_count
			if count == 0 {continue}
			gl.Uniform3f(r.offset_loc, item.offset.x, item.offset.y, item.offset.z)
			gl.BindVertexArray(item.gpu.vao)
			gl.DrawElements(gl.TRIANGLES, count, gl.UNSIGNED_INT, nil)
			r.draw_calls += 1
			r.triangles += int(count) / 3
		}
		// Opaque terrain/lava establish depth. Water blends back to front without
		// writing depth, using the same compact buffers and no extra framebuffer.
		gl.Enable(gl.BLEND); gl.BlendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA)
		gl.DepthMask(false); gl.Disable(gl.CULL_FACE)
		for i := len(draw_items) - 1; i >= 0; i -= 1 {
			item := draw_items[i]
			if item.gpu.water_count == 0 {continue}
			gl.Uniform3f(r.offset_loc, item.offset.x, item.offset.y, item.offset.z)
			gl.BindVertexArray(item.gpu.vao)
			offset := uintptr(item.gpu.count - item.gpu.water_count) * 4
			gl.DrawElements(gl.TRIANGLES, item.gpu.water_count, gl.UNSIGNED_INT, rawptr(offset))
			r.draw_calls += 1; r.triangles += int(item.gpu.water_count) / 3
		}
		gl.DepthMask(true); gl.Disable(gl.BLEND)
		if hit.hit && !menu.open {
			outline(&r, hit.block, pos, vp)
		}
		hud(
			&r,
			&stream,
			config,
			pos,
			selected,
			stats,
			width,
			height,
			&telemetry,
			&menu,
			settings,
			&pacing,
			&fluids,
			immersion,
			elapsed,
			settings.move_speed,
		)
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
		swap_start := glfw.GetTime()
		glfw.SwapBuffers(window)
		pacing.swap_ms = (glfw.GetTime() - swap_start) * 1000
		ms := frame_pace(&pacing, settings)
		telemetry_record_frame(&telemetry, &r, ms)
		if csv != nil {
			fmt.fprintf(
				csv,
				"%d,%.4f,%.4f,%.4f,%.4f,%.4f,%d,%d,%d,%d,%d,%d,%d,%.4f,%.4f,%d,%d,%d,%d,%d,%d,%.4f,%.4f,%.4f,%d,%d,%d,%d,%d,%.4f,%.4f,%d,%d,%d,%d,%.4f,%.4f,%d\n",
				frame,
				pacing.completed_at - start,
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
				pacing.display_hz,
				frame_target(settings, pacing.display_hz),
				pacing.swap_ms,
				pacing.wait_ms,
				int(pacing.software_renderer),
			)
		}
		frame += 1
		if ending {
			break
		}
		free_all(context.temp_allocator)
	}
	if !config.no_save {
		if !settings_save(
			settings,
			config.settings_path,
		) {fmt.eprintln("Settings save failed:", config.settings_path)}
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

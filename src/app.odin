package sandbox

import "core:fmt"
import "core:mem"
import "core:os"
import "core:sync"
import gl "vendor:OpenGL"
import glfw "vendor:glfw"

// App owns main-thread state. Core world, player, meshing, and rendering code
// operate on explicit subsystem arguments rather than reaching into this owner.
App :: struct {
	config:                        Config,
	window:                        glfw.WindowHandle,
	cursor_visibility:             Cursor_Visibility,
	world:                         World,
	renderer:                      Renderer,
	stream:                        Stream,
	player:                        Player,
	fluids:                        Fluid_Sim,
	settings:                      Settings,
	menu:                          Settings_Menu,
	pacing:                        Frame_Pacing,
	telemetry:                     Telemetry,
	input:                         Input_State,
	draw_items:                    [dynamic]Draw_Item,
	benchmark_file:                ^os.File,
	position, forward:             V3,
	benchmark_camera, benchmark_target: V3,
	yaw, pitch:                    f64,
	hit:                           Ray_Hit,
	immersion:                     Block,
	selected, frame:               int,
	width, height:                 i32,
	captured, stats:               bool,
	start, last, now, elapsed, dt: f64,
}

app_run :: proc(config: Config) {
	tracker: mem.Tracking_Allocator
	mem.tracking_allocator_init(&tracker, context.allocator)
	context.allocator = mem.tracking_allocator(&tracker)
	defer mem.tracking_allocator_destroy(&tracker)
	a := new(App)
	defer free(a)
	a.config = config
	world_init(&a.world, a.config.seed)
	a.world.tracker = &tracker
	a.world.allocation_limit = a.config.cpu_budget - cpu_reservation(a.config.workers)
	defer world_destroy(&a.world)
	session_load(a)
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
	glfw.WindowHint(glfw.VISIBLE, !a.config.hidden)
	a.window = glfw.CreateWindow(
		i32(a.config.width),
		i32(a.config.height),
		"VOXEL / Creative",
		nil,
		nil,
	)
	if a.window == nil {
		description, _ := glfw.GetError()
		fmt.eprintln("OpenGL 3.3 window:", description)
		os.exit(1)
	}
	defer glfw.DestroyWindow(a.window)
	glfw.SetWindowUserPointer(a.window, a)
	glfw.SetWindowFocusCallback(a.window, app_window_focus)
	a.input.focused = glfw.GetWindowAttrib(a.window, glfw.FOCUSED) != 0
	a.cursor_visibility = cursor_visibility_init(a.window)
	defer cursor_visibility_destroy(&a.cursor_visibility)
	glfw.MakeContextCurrent(a.window)
	glfw.SwapInterval(i32(int(a.config.vsync)))
	renderer_init(&a.renderer)
	defer renderer_destroy(&a.renderer)
	fmt.println("GL:", string(gl.GetString(gl.VERSION)), "/", string(gl.GetString(gl.RENDERER)))
	a.pacing = Frame_Pacing {
		sleeper           = frame_sleep_init(),
		display_hz        = display_refresh_rate(a.window),
		software_renderer = renderer_is_software(string(gl.GetString(gl.RENDERER))),
	}
	defer frame_sleep_destroy(&a.pacing.sleeper)
	fmt.printf(
		"Display: %d Hz / %s / VSync %s / software renderer %t\n",
		a.pacing.display_hz,
		frame_limit_label(a.settings, &a.pacing),
		a.settings.vsync ? "on" : "off",
		a.pacing.software_renderer,
	)
	fmt.printf(
		"Profile %s: GPU budget %d MiB, meshes %d MiB, CPU %d MiB\n",
		a.config.profile,
		a.config.gpu_budget / MiB,
		a.config.mesh_budget / MiB,
		a.config.cpu_budget / MiB,
	)
	stream_init(
		&a.stream,
		a.config.workers,
		a.config.radius,
		a.config.mesh_budget,
		a.config.cpu_budget,
		a.config.upload_budget,
		&tracker,
	)
	a.draw_items = make(
		[dynamic]Draw_Item,
		0,
		(a.config.radius * 2 + 3) * (a.config.radius * 2 + 3) * 8,
	)
	defer delete(a.draw_items)
	a.stream.naive_mesh = a.config.naive_mesh
	defer if a.stream.chunks != nil {stream_destroy(&a.stream, &a.renderer)}
	app_sync_mouse_capture(a)
	a.yaw, a.pitch = -0.8, -0.27
	a.input.previous_x, a.input.previous_y = glfw.GetCursorPos(a.window)
	a.last = glfw.GetTime()
	a.start = a.last
	a.pacing.completed_at = a.last
	a.pacing.last_display_check = a.last
	a.selected = 0
	fluids_init(&a.fluids)
	defer fluids_destroy(&a.fluids)
	if a.config.benchmark == "fluids" {fluid_demo(&a.world)}
	benchmark_prepare(a)
	fluids_reseed(&a.fluids, &a.world, a.position, a.last)
	if a.config.benchmark == "fluids" {
		fluids_notify(&a.fluids, &a.world, {6, 90, 8})
		fluids_notify(&a.fluids, &a.world, {15, 90, 8})
	}
	a.stats = true
	telemetry_init(&a.telemetry, a.last)
	benchmark_start(a)
	defer if a.benchmark_file != nil {
		os.close(a.benchmark_file)
	}

	app_loop(a)
	session_save_settings(a)
	session_save_world(a)
	stream_destroy(&a.stream, &a.renderer)
	sync.mutex_lock(&tracker.mutex)
	peak_cpu := tracker.peak_memory_allocated
	sync.mutex_unlock(&tracker.mutex)
	fmt.printf(
		"Frames %d / CPU peak %.2f MiB / mesh peak %.2f MiB / generated %d / stale %d / oversized %d / reused buffers %d\n",
		a.frame,
		f64(peak_cpu) / MiB,
		f64(a.renderer.peak_bytes) / MiB,
		a.stream.generated,
		a.stream.stale,
		a.stream.oversized,
		a.renderer.reused_buffers,
	)
}

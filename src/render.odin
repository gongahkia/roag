package sandbox

import gl "vendor:OpenGL"
import glfw "vendor:glfw"

Renderer :: struct {
	program, flat_program, texture, flat_vao, flat_vbo:          u32,
	vp_loc, offset_loc, fog_loc, flat_vp_loc:                    i32,
	time_loc, immersion_loc:                                     i32,
	queries:                                                     [4]u32,
	query_pending:                                               [4]bool,
	query_active:                                                int,
	gpu_ms:                                                      f64,
	gpu_samples, overlay_draws, last_overlay_draws, cull_tested: int,
	visible_chunks:                                              int,
	flats:                                                       [dynamic]Flat_Vertex,
	mesh_bytes, peak_bytes, draw_calls, triangles:               int,
	pool:                                                        [32]GPU_Mesh,
	pool_count, pool_bytes, reused_buffers:                      int,
}

renderer_init :: proc(r: ^Renderer) {
	gl.load_up_to(3, 3, glfw.gl_set_proc_address)
	r.query_active = -1
	r.program = program(
		#load("../shaders/world.vert", string),
		#load("../shaders/world.frag", string),
	)
	r.flat_program = program(
		#load("../shaders/flat.vert", string),
		#load("../shaders/flat.frag", string),
	)
	r.vp_loc = gl.GetUniformLocation(r.program, "vp")
	r.offset_loc = gl.GetUniformLocation(r.program, "offset")
	r.fog_loc = gl.GetUniformLocation(r.program, "fogDistance")
	r.time_loc = gl.GetUniformLocation(r.program, "worldTime")
	r.immersion_loc = gl.GetUniformLocation(r.program, "immersion")
	r.flat_vp_loc = gl.GetUniformLocation(r.flat_program, "vp")
	gl.UseProgram(r.program)
	gl.Uniform1i(gl.GetUniformLocation(r.program, "atlas"), 0)
	renderer_texture_init(r)
	ui_renderer_init(r)
	gl.GenQueries(4, &r.queries[0])
	gl.Enable(gl.DEPTH_TEST)
	gl.Enable(gl.CULL_FACE)
}

renderer_destroy :: proc(r: ^Renderer) {
	renderer_trim_pool(r)
	gl.DeleteProgram(r.program)
	gl.DeleteProgram(r.flat_program)
	gl.DeleteTextures(1, &r.texture)
	gl.DeleteBuffers(1, &r.flat_vbo)
	gl.DeleteVertexArrays(1, &r.flat_vao)
	gl.DeleteQueries(4, &r.queries[0])
	delete(r.flats)
}

renderer_begin :: proc(r: ^Renderer, width, height: i32, frame: int) {
	r.draw_calls = 0
	r.last_overlay_draws = r.overlay_draws
	r.overlay_draws = 0
	r.cull_tested = 0
	r.visible_chunks = 0
	r.triangles = 0
	slot := frame % 4
	if r.query_pending[slot] {
		available: u32
		gl.GetQueryObjectuiv(r.queries[slot], gl.QUERY_RESULT_AVAILABLE, &available)
		if available != 0 {
			ns: u64
			gl.GetQueryObjectui64v(r.queries[slot], gl.QUERY_RESULT, &ns)
			r.gpu_ms = f64(ns) / 1e6
			r.gpu_samples += 1
			r.query_pending[slot] = false
		}
	}
	if !r.query_pending[slot] {
		gl.BeginQuery(gl.TIME_ELAPSED, r.queries[slot])
		r.query_active = slot
	}
	gl.Viewport(0, 0, width, height)
	gl.ClearColor(0.56, 0.72, 0.81, 1)
	gl.Clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT)
	gl.Enable(gl.DEPTH_TEST)
	gl.Enable(gl.CULL_FACE)
	gl.UseProgram(r.program)
	gl.ActiveTexture(gl.TEXTURE0)
	gl.BindTexture(gl.TEXTURE_2D_ARRAY, r.texture)
}

renderer_end :: proc(r: ^Renderer) {
	if r.query_active >= 0 {
		gl.EndQuery(gl.TIME_ELAPSED)
		r.query_pending[r.query_active] = true
		r.query_active = -1
	}
}

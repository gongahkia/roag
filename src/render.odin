package sandbox

import "core:fmt"
import "core:math"
import "core:os"
import gl "vendor:OpenGL"
import glfw "vendor:glfw"

Mat4 :: [16]f32
F3 :: [3]f32
F4 :: [4]f32
GPU_Mesh :: struct {
	vao, vbo, ibo: u32,
	count:         i32,
	bytes:         int,
}

Draw_Item :: struct {
	gpu:      ^GPU_Mesh,
	offset:   F3,
	distance: f32,
}

draw_nearer :: proc(a, b: Draw_Item) -> bool {
	return a.distance < b.distance
}

Flat_Vertex :: struct {
	p:     F3,
	color: F4,
}

Renderer :: struct {
	program, flat_program, texture, flat_vao, flat_vbo:          u32,
	vp_loc, offset_loc, fog_loc, flat_vp_loc:                    i32,
	queries:                                                     [4]u32,
	query_pending:                                               [4]bool,
	query_active:                                                int,
	gpu_ms:                                                      f64,
	gpu_samples, overlay_draws, last_overlay_draws, cull_tested: int,
	flats:                                                       [dynamic]Flat_Vertex,
	mesh_bytes, peak_bytes, draw_calls, triangles:               int,
	pool:                                                        [32]GPU_Mesh,
	pool_count, pool_bytes, reused_buffers:                      int,
}

identity :: proc() -> Mat4 {
	return {1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1}
}

mat_mul :: proc(a, b: Mat4) -> Mat4 {
	r: Mat4
	for c in 0 ..< 4 {
		for row in 0 ..< 4 {
			for k in 0 ..< 4 {
				r[c * 4 + row] += a[k * 4 + row] * b[c * 4 + k]
			}
		}
	}
	return r
}

normalize :: proc(v: V3) -> V3 {
	l := math.sqrt(v.x * v.x + v.y * v.y + v.z * v.z)
	if l == 0 {
		return {}
	}
	return v / l
}

cross :: proc(a, b: V3) -> V3 {
	return {a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x}
}

direction :: proc(yaw, pitch: f64) -> V3 {
	return {math.cos(pitch) * math.cos(yaw), math.sin(pitch), math.cos(pitch) * math.sin(yaw)}
}

camera_matrix :: proc(forward: V3, aspect, far: f32) -> Mat4 {
	f := normalize(forward)
	s := normalize(cross(f, {0, 1, 0}))
	u := cross(s, f)
	view := Mat4 {
		f32(s.x),
		f32(u.x),
		f32(-f.x),
		0,
		f32(s.y),
		f32(u.y),
		f32(-f.y),
		0,
		f32(s.z),
		f32(u.z),
		f32(-f.z),
		0,
		0,
		0,
		0,
		1,
	}
	t := f32(1 / math.tan(75.0 * math.PI / 360))
	near: f32 = 0.08
	projection := Mat4 {
		t / aspect,
		0,
		0,
		0,
		0,
		t,
		0,
		0,
		0,
		0,
		(far + near) / (near - far),
		-1,
		0,
		0,
		2 * far * near / (near - far),
		0,
	}
	return mat_mul(projection, view)
}

visible :: proc(vp: Mat4, offset: F3) -> bool {
	center := offset + F3{16, 16, 16}
	for axis in 0 ..< 3 {
		for sign: f32 = -1; sign <= 1; sign += 2 {
			a := vp[3] + sign * vp[axis]
			b := vp[7] + sign * vp[4 + axis]
			c := vp[11] + sign * vp[8 + axis]
			d := vp[15] + sign * vp[12 + axis]
			if a * center.x + b * center.y + c * center.z + d + 16 * (abs(a) + abs(b) + abs(c)) <
			   0 {
				return false
			}
		}
	}
	return true
}

shader :: proc(kind: u32, source: string) -> u32 {
	id := gl.CreateShader(kind)
	ptr := cstring(raw_data(source))
	length := i32(len(source))
	gl.ShaderSource(id, 1, &ptr, &length)
	gl.CompileShader(id)
	ok: i32
	gl.GetShaderiv(id, gl.COMPILE_STATUS, &ok)
	if ok == 0 {
		buf: [4096]u8
		length: i32
		gl.GetShaderInfoLog(id, 4096, &length, &buf[0])
		fmt.eprintln("Shader:", string(buf[:length]))
		os.exit(1)
	}
	return id
}

program :: proc(vs, fs: string) -> u32 {
	v, f := shader(gl.VERTEX_SHADER, vs), shader(gl.FRAGMENT_SHADER, fs)
	p := gl.CreateProgram()
	gl.AttachShader(p, v)
	gl.AttachShader(p, f)
	gl.LinkProgram(p)
	gl.DeleteShader(v)
	gl.DeleteShader(f)
	ok: i32
	gl.GetProgramiv(p, gl.LINK_STATUS, &ok)
	if ok == 0 {
		fmt.eprintln("Shader link failed")
		os.exit(1)
	}
	return p
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
	r.flat_vp_loc = gl.GetUniformLocation(r.flat_program, "vp")
	gl.UseProgram(r.program)
	gl.Uniform1i(gl.GetUniformLocation(r.program, "atlas"), 0)
	gl.GenTextures(1, &r.texture)
	gl.BindTexture(gl.TEXTURE_2D_ARRAY, r.texture)
	pixels: [16 * 16 * 10 * 4]u8
	colors := [10][3]u8 {
		{0, 0, 0},
		{115, 92, 59},
		{126, 91, 61},
		{129, 136, 143},
		{220, 203, 151},
		{111, 80, 47},
		{66, 119, 58},
		{65, 69, 77},
		{99, 151, 65},
		{157, 124, 76},
	}
	for layer in 0 ..< 10 {
		for y in 0 ..< 16 {
			for x in 0 ..< 16 {
				noise := i32(hash3(19, i32(x), i32(layer), i32(y)) % 31) - 15
				color := colors[layer]
				if layer == 1 && y > 11 {
					color = colors[8]
				}
				if layer == 5 && x % 4 == 0 {
					noise -= 22
				}
				if layer == 9 && (x == 3 || x == 12 || y == 3 || y == 12) {
					noise -= 25
				}
				for c in 0 ..< 3 {
					pixels[((layer * 16 + y) * 16 + x) * 4 + c] = u8(
						clamp(i32(color[c]) + noise, 0, 255),
					)
				}
				pixels[((layer * 16 + y) * 16 + x) * 4 + 3] = 255
			}
		}
	}
	gl.TexImage3D(
		gl.TEXTURE_2D_ARRAY,
		0,
		gl.RGBA8,
		16,
		16,
		10,
		0,
		gl.RGBA,
		gl.UNSIGNED_BYTE,
		&pixels[0],
	)
	gl.TexParameteri(gl.TEXTURE_2D_ARRAY, gl.TEXTURE_MIN_FILTER, gl.NEAREST_MIPMAP_LINEAR)
	gl.TexParameteri(gl.TEXTURE_2D_ARRAY, gl.TEXTURE_MAG_FILTER, gl.NEAREST)
	gl.TexParameteri(gl.TEXTURE_2D_ARRAY, gl.TEXTURE_WRAP_S, gl.REPEAT)
	gl.TexParameteri(gl.TEXTURE_2D_ARRAY, gl.TEXTURE_WRAP_T, gl.REPEAT)
	gl.GenerateMipmap(gl.TEXTURE_2D_ARRAY)
	r.flats = make([dynamic]Flat_Vertex, 0, 32768)
	gl.GenVertexArrays(1, &r.flat_vao)
	gl.GenBuffers(1, &r.flat_vbo)
	gl.BindVertexArray(r.flat_vao)
	gl.BindBuffer(gl.ARRAY_BUFFER, r.flat_vbo)
	gl.BufferData(gl.ARRAY_BUFFER, 32768 * size_of(Flat_Vertex), nil, gl.DYNAMIC_DRAW)
	gl.VertexAttribPointer(0, 3, gl.FLOAT, false, size_of(Flat_Vertex), 0)
	gl.EnableVertexAttribArray(0)
	gl.VertexAttribPointer(1, 4, gl.FLOAT, false, size_of(Flat_Vertex), 12)
	gl.EnableVertexAttribArray(1)
	gl.GenQueries(4, &r.queries[0])
	gl.Enable(gl.DEPTH_TEST)
	gl.Enable(gl.CULL_FACE)
}

mesh_pool_slot :: proc(r: ^Renderer, bytes: int) -> int {
	best := -1
	for i in 0 ..< r.pool_count {
		capacity := r.pool[i].bytes
		if capacity >= bytes &&
		   capacity <= bytes * 2 &&
		   (best < 0 || capacity < r.pool[best].bytes) {
			best = i
		}
	}
	return best
}

mesh_allocation_cost :: proc(r: ^Renderer, m: Mesh) -> int {
	bytes := len(m.vertices) * size_of(Vertex) + len(m.indices) * 4
	if mesh_pool_slot(r, bytes) >= 0 {
		return 0
	}
	return bytes
}

mesh_allocate :: proc(r: ^Renderer, m: Mesh) -> GPU_Mesh {
	if len(m.indices) == 0 {
		return {}
	}
	bytes := len(m.vertices) * size_of(Vertex) + len(m.indices) * 4
	if slot := mesh_pool_slot(r, bytes); slot >= 0 {
		g := r.pool[slot]
		r.pool_count -= 1
		r.pool[slot] = r.pool[r.pool_count]
		r.pool_bytes -= g.bytes
		g.count = i32(len(m.indices))
		r.reused_buffers += 1
		return g
	}
	g: GPU_Mesh
	g.count = i32(len(m.indices))
	g.bytes = bytes
	gl.GenVertexArrays(1, &g.vao)
	gl.GenBuffers(1, &g.vbo)
	gl.GenBuffers(1, &g.ibo)
	gl.BindVertexArray(g.vao)
	gl.BindBuffer(gl.ARRAY_BUFFER, g.vbo)
	gl.BufferData(gl.ARRAY_BUFFER, len(m.vertices) * size_of(Vertex), nil, gl.STATIC_DRAW)
	gl.BindBuffer(gl.ELEMENT_ARRAY_BUFFER, g.ibo)
	gl.BufferData(gl.ELEMENT_ARRAY_BUFFER, len(m.indices) * 4, nil, gl.STATIC_DRAW)
	gl.VertexAttribIPointer(0, 4, gl.UNSIGNED_SHORT, size_of(Vertex), 0)
	gl.EnableVertexAttribArray(0)
	if error := gl.GetError(); error != gl.NO_ERROR {
		fmt.eprintf("GPU mesh allocation failed (0x%x); reducing residency\n", error)
		gl.DeleteVertexArrays(1, &g.vao)
		gl.DeleteBuffers(1, &g.vbo)
		gl.DeleteBuffers(1, &g.ibo)
		return {}
	}
	r.mesh_bytes += g.bytes
	r.peak_bytes = max(r.peak_bytes, r.mesh_bytes)
	return g
}

mesh_upload_part :: proc(g: GPU_Mesh, m: Mesh, offset, limit: int) -> int {
	vertex_bytes := len(m.vertices) * size_of(Vertex)
	remaining := min(limit, vertex_bytes + len(m.indices) * 4 - offset)
	done := 0
	gl.BindVertexArray(g.vao)
	if offset < vertex_bytes {
		count := min(remaining, vertex_bytes - offset)
		gl.BindBuffer(gl.ARRAY_BUFFER, g.vbo)
		gl.BufferSubData(
			gl.ARRAY_BUFFER,
			offset,
			count,
			rawptr(uintptr(raw_data(m.vertices)) + uintptr(offset)),
		)
		done += count
		remaining -= count
	}
	if remaining > 0 {
		at := max(offset + done - vertex_bytes, 0)
		gl.BindBuffer(gl.ELEMENT_ARRAY_BUFFER, g.ibo)
		gl.BufferSubData(
			gl.ELEMENT_ARRAY_BUFFER,
			at,
			remaining,
			rawptr(uintptr(raw_data(m.indices)) + uintptr(at)),
		)
		done += remaining
	}
	return done
}

mesh_release :: proc(r: ^Renderer, g: ^GPU_Mesh) {
	if g.vao != 0 {
		gl.DeleteVertexArrays(1, &g.vao)
		gl.DeleteBuffers(1, &g.vbo)
		gl.DeleteBuffers(1, &g.ibo)
		r.mesh_bytes -= g.bytes
	}
	g^ = {}
}

mesh_recycle :: proc(r: ^Renderer, g: ^GPU_Mesh) {
	if g.vao != 0 && r.pool_count < len(r.pool) && r.pool_bytes + g.bytes <= 2 * MiB {
		r.pool[r.pool_count] = g^
		r.pool_count += 1
		r.pool_bytes += g.bytes
		g^ = {}
	} else {
		mesh_release(r, g)
	}
}

renderer_trim_pool :: proc(r: ^Renderer) {
	for i in 0 ..< r.pool_count {
		mesh_release(r, &r.pool[i])
	}
	r.pool_count = 0
	r.pool_bytes = 0
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

flat_flush :: proc(r: ^Renderer, transform: Mat4, mode: u32) {
	if len(r.flats) == 0 {
		return
	}
	gl.UseProgram(r.flat_program)
	t := transform
	gl.UniformMatrix4fv(r.flat_vp_loc, 1, false, &t[0])
	gl.BindVertexArray(r.flat_vao)
	gl.BindBuffer(gl.ARRAY_BUFFER, r.flat_vbo)
	gl.BufferSubData(gl.ARRAY_BUFFER, 0, len(r.flats) * size_of(Flat_Vertex), raw_data(r.flats))
	gl.DrawArrays(mode, 0, i32(len(r.flats)))
	r.overlay_draws += 1
	clear(&r.flats)
}

rect :: proc(r: ^Renderer, x, y, w, h: f32, color: F4) {
	if len(r.flats) + 6 > cap(r.flats) {
		return
	}
	append(
		&r.flats,
		Flat_Vertex{{x, y, 0}, color},
		{{x + w, y, 0}, color},
		{{x + w, y + h, 0}, color},
		{{x, y, 0}, color},
		{{x + w, y + h, 0}, color},
		{{x, y + h, 0}, color},
	)
}
// Original 5x7 bitmap font, embedded to avoid a font dependency or texture.
glyph :: proc(c: u8) -> [7]u8 {
	switch c {
	case 'A':
		return {14, 17, 17, 31, 17, 17, 17}
	case 'B':
		return {30, 17, 17, 30, 17, 17, 30}
	case 'C':
		return {14, 17, 16, 16, 16, 17, 14}
	case 'D':
		return {30, 17, 17, 17, 17, 17, 30}
	case 'E':
		return {31, 16, 16, 30, 16, 16, 31}
	case 'F':
		return {31, 16, 16, 30, 16, 16, 16}
	case 'G':
		return {14, 17, 16, 23, 17, 17, 15}
	case 'H':
		return {17, 17, 17, 31, 17, 17, 17}
	case 'I':
		return {14, 4, 4, 4, 4, 4, 14}
	case 'J':
		return {7, 2, 2, 2, 18, 18, 12}
	case 'K':
		return {17, 18, 20, 24, 20, 18, 17}
	case 'L':
		return {16, 16, 16, 16, 16, 16, 31}
	case 'M':
		return {17, 27, 21, 21, 17, 17, 17}
	case 'N':
		return {17, 25, 21, 19, 17, 17, 17}
	case 'O':
		return {14, 17, 17, 17, 17, 17, 14}
	case 'P':
		return {30, 17, 17, 30, 16, 16, 16}
	case 'Q':
		return {14, 17, 17, 17, 21, 18, 13}
	case 'R':
		return {30, 17, 17, 30, 20, 18, 17}
	case 'S':
		return {15, 16, 16, 14, 1, 1, 30}
	case 'T':
		return {31, 4, 4, 4, 4, 4, 4}
	case 'U':
		return {17, 17, 17, 17, 17, 17, 14}
	case 'V':
		return {17, 17, 17, 17, 17, 10, 4}
	case 'W':
		return {17, 17, 17, 21, 21, 21, 10}
	case 'X':
		return {17, 17, 10, 4, 10, 17, 17}
	case 'Y':
		return {17, 17, 10, 4, 4, 4, 4}
	case 'Z':
		return {31, 1, 2, 4, 8, 16, 31}
	case '0':
		return {14, 17, 19, 21, 25, 17, 14}
	case '1':
		return {4, 12, 4, 4, 4, 4, 14}
	case '2':
		return {14, 17, 1, 2, 4, 8, 31}
	case '3':
		return {30, 1, 1, 14, 1, 1, 30}
	case '4':
		return {2, 6, 10, 18, 31, 2, 2}
	case '5':
		return {31, 16, 16, 30, 1, 1, 30}
	case '6':
		return {14, 16, 16, 30, 17, 17, 14}
	case '7':
		return {31, 1, 2, 4, 8, 8, 8}
	case '8':
		return {14, 17, 17, 14, 17, 17, 14}
	case '9':
		return {14, 17, 17, 15, 1, 1, 14}
	case '.':
		return {0, 0, 0, 0, 0, 12, 12}
	case ':':
		return {0, 12, 12, 0, 12, 12, 0}
	case '-':
		return {0, 0, 0, 31, 0, 0, 0}
	case '/':
		return {1, 2, 2, 4, 8, 8, 16}
	case '+':
		return {0, 4, 4, 31, 4, 4, 0}
	case '%':
		return {17, 18, 2, 4, 8, 9, 17}
	case '[':
		return {14, 8, 8, 8, 8, 8, 14}
	case ']':
		return {14, 2, 2, 2, 2, 2, 14}
	}
	return {}
}

text_draw :: proc(
	r: ^Renderer,
	x, y: f32,
	s: string,
	scale: f32 = 1.5,
	color: F4 = {0.9, 0.94, 0.96, 1},
) {
	cx := x
	for c in transmute([]u8)s {
		g := glyph(c)
		for row in 0 ..< 7 {
			for col in 0 ..< 5 {
				if g[row] & (1 << u32(4 - col)) != 0 {
					rect(r, cx + f32(col) * scale, y + f32(row) * scale, scale, scale, color)
				}
			}
		}
		cx += 6 * scale
	}
}

outline :: proc(r: ^Renderer, p: I3, camera: V3, vp: Mat4) {
	minp := F3 {
		f32(f64(p.x) - camera.x) - 0.003,
		f32(f64(p.y) - camera.y) - 0.003,
		f32(f64(p.z) - camera.z) - 0.003,
	}
	for a in 0 ..< 3 {
		for u in 0 ..< 2 {
			for v in 0 ..< 2 {
				q := minp
				q[(a + 1) % 3] += f32(u) * 1.006
				q[(a + 2) % 3] += f32(v) * 1.006
				e := q
				e[a] += 1.006
				append(
					&r.flats,
					Flat_Vertex{q, {0.98, 0.96, 0.73, 1}},
					Flat_Vertex{e, {0.98, 0.96, 0.73, 1}},
				)
			}
		}
	}
	gl.Disable(gl.CULL_FACE)
	flat_flush(r, vp, gl.LINES)
}

package sandbox

import gl "vendor:OpenGL"

Flat_Vertex :: struct {
	p:     F3,
	color: F4,
}

ui_renderer_init :: proc(r: ^Renderer) {
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

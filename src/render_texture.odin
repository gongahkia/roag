package sandbox

import gl "vendor:OpenGL"

renderer_texture_init :: proc(r: ^Renderer) {
	gl.GenTextures(1, &r.texture)
	gl.BindTexture(gl.TEXTURE_2D_ARRAY, r.texture)
	pixels: [16 * 16 * 20 * 4]u8
	colors := [20][3]u8 {
		{0, 0, 0},
		{115, 92, 59},
		{126, 91, 61},
		{129, 136, 143},
		{220, 203, 151},
		{111, 80, 47},
		{66, 119, 58},
		{65, 69, 77},
		{36, 38, 42},
		{205, 166, 129},
		{205, 114, 58},
		{243, 199, 45},
		{73, 230, 220},
		{201, 35, 49},
		{44, 29, 63},
		{119, 126, 133},
		{40, 110, 210},
		{243, 92, 15},
		{99, 151, 65},
		{157, 124, 76},
	}
	for layer in 0 ..< 20 {
		for y in 0 ..< 16 {
			for x in 0 ..< 16 {
				noise := i32(hash3(19, i32(x), i32(layer), i32(y)) % 31) - 15
				color := colors[layer]
				if layer == 1 && y > 11 {
					color = colors[18]
				}
				if layer == 5 && x % 4 == 0 {
					noise -= 22
				}
				if layer == 19 && (x == 3 || x == 12 || y == 3 || y == 12) {
					noise -= 25
				}
				if layer >= 8 && layer <= 13 {
					if hash3(711, i32(x / 3), i32(layer), i32(y / 3)) % 5 >= 2 ||
					   (x + y) % 3 == 0 {color = colors[3]}
				}
				if layer == 15 && (x % 8 == 0 || (y + (x / 8) * 3) % 8 == 0) {noise -= 30}
				if layer == 17 &&
				   hash3(921, i32(x / 2), 0, i32(y / 2)) % 3 == 0 {color = {255, 187, 32}}
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
		20,
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
}

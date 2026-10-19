package sandbox

import gl "vendor:OpenGL"

ATLAS_LAYERS :: 75

renderer_texture_init :: proc(r: ^Renderer) {
	gl.GenTextures(1, &r.texture)
	gl.BindTexture(gl.TEXTURE_2D_ARRAY, r.texture)
	pixels: [16 * 16 * ATLAS_LAYERS * 4]u8
	colors := [ATLAS_LAYERS][3]u8 {
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
		{0, 0, 0},
		{0, 0, 0},
		{0, 0, 0},
		{0, 0, 0},
		{0, 0, 0},
		{0, 0, 0},
		{0, 0, 0},
		{0, 0, 0},
		{0, 0, 0},
		{0, 0, 0},
		{234, 243, 248},
		{150, 198, 230},
		{64, 131, 56},
		{173, 102, 68},
		{152, 67, 47},
		{210, 159, 74},
		{215, 189, 161},
		{224, 216, 191},
		{110, 156, 70},
		{83, 61, 43},
		{45, 87, 66},
		{132, 103, 68},
		{139, 151, 68},
		{115, 86, 51},
		{44, 119, 47},
		{108, 65, 65},
		{235, 157, 187},
		{210, 199, 175},
		{190, 48, 48},
		{149, 106, 71},
		{139, 111, 146},
		{81, 73, 61},
		{113, 78, 48},
		{173, 168, 73},
		{65, 164, 60},
		{93, 120, 67},
		{255, 213, 115},
		{0, 0, 0},
		{0, 0, 0},
		{0, 0, 0},
		{0, 0, 0},
		{0, 0, 0},
		{0, 0, 0},
		{0, 0, 0},
		{0, 0, 0},
		{220, 205, 159},
		{137, 108, 74},
		{180, 140, 84},
		{166, 133, 83},
		{171, 118, 111},
		{173, 168, 73},
		{65, 164, 60},
		{93, 120, 67},
		{139, 111, 146},
		{113, 78, 48},
	}
	for layer in 0 ..< ATLAS_LAYERS {
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
				if layer ==
				   56 {noise /= 2; if x == 0 || y == 0 || x == 15 || y == 15 {noise -= 90}}
				if layer == 30 {noise /= 3}
				if layer == 31 {noise /= 2; if (x + y * 2) % 13 == 0 {noise += 24}}
				if layer ==
				   32 {if x % 4 == 0 {noise -= 30}; if x % 7 == 2 && y % 5 == 1 {noise += 55}}
				if layer ==
				   37 {if hash3(88, i32(x / 3), 0, i32(y / 2)) % 6 == 0 {color = {50, 46, 40}}}
				if layer == 39 ||
				   layer == 41 ||
				   layer == 43 ||
				   layer == 45 {if x % 4 == 0 {noise -= 22}}
				if layer >= 65 &&
				   layer <= 69 {if x == 3 || x == 12 || y == 3 || y == 12 {noise -= 25}}
				if layer == 48 &&
				   hash3(39, i32(x / 3), 0, i32(y / 3)) % 5 == 0 {color = {241, 227, 207}}
				if layer == 46 {noise /= 2; if (x + y) % 7 == 0 {color = {248, 201, 215}}}
				if layer == 50 || layer == 52 || (layer >= 53 && layer <= 55) {
					color =
						colors[2]; if y > 11 {color = colors[layer == 50 ? 73 : (layer == 52 ? 74 : 70 + layer - 53)]}
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
		ATLAS_LAYERS,
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

package sandbox

Island_Form :: struct {
	x, z, rx, rz, top, depth: i32,
	active:                   bool,
}
Ground_Form :: struct {
	x, z, height, base, kind, rx, rz, foundation: i32,
	rotated, active:                              bool,
}

island_form :: proc(seed: u64, gx, gz: i32) -> Island_Form {
	h := hash3(seed + 3101, gx, 0, gz)
	f := Island_Form {
		x     = gx * 128 + 64 + i32(h >> 8 & 15) - 8,
		z     = gz * 128 + 64 + i32(h >> 16 & 15) - 8,
		rx    = 28 + i32((h >> 24) % 17),
		rz    = 24 + i32((h >> 32) % 21),
		top   = 176 + i32((h >> 40) % 41),
		depth = 18 + i32((h >> 48) % 15),
	}
	f.active = h % 4 != 0 && sky_weight(seed, f.x, f.z) >= TQ * 3 / 4
	if f.active {
		// Keep the entire footprint over low ground, including biome blends.
		for dz: i32 = -1; dz <= 1; dz += 2 {for dx: i32 = -1; dx <= 1; dx += 2 {
				if sky_weight(seed, f.x + dx * f.rx, f.z + dz * f.rz) <
				   TQ * 3 / 4 {f.active = false}
			}}
	}
	return f
}

island_form_span :: proc(seed: u64, f: Island_Form, x, z: i32) -> (bottom, top: i32) {
	bottom, top = -1, -1
	if !f.active {return}
	ox, oz := i64(x - f.x), i64(z - f.z)
	if abs(ox) > i64(f.rx) || abs(oz) > i64(f.rz) {return}
	r2 := ox * ox * TQ / i64(f.rx * f.rx) + oz * oz * TQ / i64(f.rz * f.rz)
	if r2 >= TQ {return}
	top = f.top - i32(qmul(r2, r2) * 6 / TQ)
	top += i32((noise_scaled(seed + 3102, x, z, 19) - TQ / 2) * 5 / TQ)
	bottom = top - 2 - i32((TQ - r2) * i64(f.depth) / TQ)
	return
}

island_span :: proc(seed: u64, x, z: i32) -> (bottom, top: i32) {
	return island_form_span(seed, island_form(seed, floor_div(x, 128), floor_div(z, 128)), x, z)
}

ground_form :: proc(
	seed: u64,
	gx, gz: i32,
	generator: u32 = 3,
	rivers: ^River_Set = nil,
) -> Ground_Form {
	h := hash3(seed + 3201, gx, 0, gz)
	f := Ground_Form {
		x       = gx * 128 + 64,
		z       = gz * 128 + 64,
		height  = 24 + i32((h >> 16) % 15),
		kind    = i32((h >> 32) % 3),
		rotated = h >> 8 & 1 != 0,
		rx      = 10 + i32((h >> 44) % 5),
		rz      = 10 + i32((h >> 52) % 5),
	}
	if h % 16 != 0 {return f}
	c := terrain_reference_column(seed, f.x, f.z, generator, rivers)
	if generator >= 5 && c.river > 0 {return f}
	if c.height <= SEA_LEVEL + 4 || c.height > 150 || c.sky > TQ / 4 {return f}
	f.base = c.height
	f.foundation = c.height - 12
	if f.kind == 1 {
		ax, az := f.x - 8, f.z
		if f.rotated {ax, az = f.x, f.z - 8}
		f.foundation = min(
			f.foundation,
			terrain_reference_column(seed, ax, az, generator, rivers).height - 1,
		)
	}
	if f.kind == 0 {
		left, right: i32
		if f.rotated {
			left = terrain_reference_column(seed, f.x, f.z - 25, generator, rivers).height
			right = terrain_reference_column(seed, f.x, f.z + 25, generator, rivers).height
		} else {
			left = terrain_reference_column(seed, f.x - 25, f.z, generator, rivers).height
			right = terrain_reference_column(seed, f.x + 25, f.z, generator, rivers).height
		}
		if abs(left - right) > 10 || c.height > min(left, right) + 8 {return f}
		f.base = min(left, right) - 2
	}
	f.active = true
	return f
}

ground_form_interval :: proc(seed: u64, f: Ground_Form, x, z: i32) -> (bottom, top: i32) {
	bottom, top = -1, -1
	if !f.active {return}
	a, b := i64(x - f.x), i64(z - f.z)
	if f.rotated {a, b = b, a}
	if abs(a) > 30 || abs(b) > 16 {return}
	texture := noise_scaled(seed + 3301, x, z, 13) - TQ / 2
	perturb := i32(texture * 5 / TQ)
	if f.kind == 0 {
		width := 6 + noise_scaled(seed + 3302, x, z, 23) * 2 / TQ
		if abs(a) > 26 || abs(b) > width {return}
		top =
			f.base +
			i32(i64(f.height) * integer_sqrt(max(0, TQ * TQ - a * a * TQ * TQ / (26 * 26))) / TQ)
		bottom = f.base
		if abs(a) <
		   20 {bottom = f.base + i32(i64(f.height - 6) * integer_sqrt(max(0, TQ * TQ - a * a * TQ * TQ / (20 * 20))) / TQ) + 1}
		top += perturb - i32(b * b * 3 / (width * width))
		if abs(a) < 20 {bottom += i32(b * b * 3 / (width * width)) - perturb / 2}
	} else if f.kind == 1 {
		if a < -12 ||
		   a > 26 ||
		   (a + 4) * (a + 4) * TQ / (30 * 30) + b * b * TQ / i64(f.rz * f.rz) > TQ {return}
		top = f.base + f.height / 2 + perturb - i32(b * b / 40)
		bottom = top - 4 - i32(noise_scaled(seed + 3303, x, z, 9) * 3 / TQ)
		if a < -5 {bottom = f.foundation}
	} else {
		a += texture * 3 / TQ
		r2 := a * a * TQ / i64(f.rx * f.rx) + b * b * TQ / i64(f.rz * f.rz)
		if r2 > TQ {return}
		bottom = f.base - 12
		top = f.base + i32((TQ - r2) * i64(f.height) / TQ) + perturb
	}
	return
}

ground_form_span :: proc(seed: u64, x, z: i32) -> (bottom, top: i32) {
	return ground_form_interval(
		seed,
		ground_form(seed, floor_div(x, 128), floor_div(z, 128)),
		x,
		z,
	)
}

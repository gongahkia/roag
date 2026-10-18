package sandbox

Biome :: enum u8 {Ocean, Coast, Plains, Forest, Desert, Mountains, Sky_Islands}
BIOME_NAMES := [?]string{"OCEAN", "COAST", "PLAINS", "FOREST", "DESERT", "MOUNTAINS", "SKY ISLANDS"}

Terrain_Column :: struct {
	height: i32,
	biome: Biome,
	slope, sky: i64,
	island_bottom, island_top: i32,
	form_bottom, form_top: i32,
}

// A selected region owns its islands, but never replaces the ground below.
sky_region :: proc(seed: u64, gx, gz: i32) -> (x, z: i32, active: bool) {
	h := hash3(seed + 3001, gx, 0, gz)
	return gx * 512 + 256 + i32(h >> 8 & 63) - 32,
		gz * 512 + 256 + i32(h >> 16 & 63) - 32, h % 12 == 0
}

sky_weight :: proc(seed: u64, x, z: i32) -> i64 {
	gx, gz := floor_div(x, 512), floor_div(z, 512)
	weight: i64
	for dz in -1 ..= 1 {for dx in -1 ..= 1 {
		cx, cz, active := sky_region(seed, gx + i32(dx), gz + i32(dz))
		if !active {continue}
		rx, rz := i64(x - cx), i64(z - cz)
		d2 := rx * rx + rz * rz
		weight = max(weight, qsmooth(clamp((176 * 176 - d2) * TQ / (176 * 176 - 144 * 144), 0, TQ)))
	}}
	return weight
}

// The cheap base sampler has no trees or feature recursion. All climate and
// landform transitions happen before integer height quantization.
terrain_base_column :: proc(seed: u64, x, z: i32) -> Terrain_Column {
	continental := noise_scaled(seed + 2001, x, z, 768)
	land := qramp(TQ * 28 / 100, TQ * 49 / 100, continental)
	mountain := qmul(land, qramp(TQ * 55 / 100, TQ * 79 / 100, noise_scaled(seed + 2002, x, z, 320)))
	detail := terrain_fbm(seed + 2100, x, z, 128)
	ridge := terrain_fbm(seed + 2200, x, z, 192, true)
	h := 32 * TQ + land * 34 + qmul(detail.value * 18, land)
	h += qmul(mountain, 42 * TQ + clamp(ridge.value + TQ / 2, 0, TQ) * 82)
	sky := sky_weight(seed, x, z)
	h = qlerp(h, 68 * TQ + detail.value * 18, sky)
	moisture := noise_scaled(seed + 2003, x, z, 384)
	column := Terrain_Column {
		height = i32(clamp(h / TQ, 18, 189)),
		sky = sky,
		slope = (abs(detail.dx) + abs(detail.dz)) * 18 / 128 +
			qmul(mountain, (abs(ridge.dx) + abs(ridge.dz)) * 82 / 192),
		island_bottom = -1, island_top = -1, form_bottom = -1, form_top = -1,
	}
	if column.height < SEA_LEVEL - 2 {column.biome = .Ocean}
	else if column.height <= SEA_LEVEL + 3 {column.biome = .Coast}
	else if mountain > TQ / 3 && sky < TQ / 2 {column.biome = .Mountains}
	else if moisture < TQ * 32 / 100 {column.biome = .Desert}
	else if moisture > TQ * 56 / 100 {column.biome = .Forest}
	else {column.biome = .Plains}
	if sky >= TQ / 2 {column.biome = .Sky_Islands}
	return column
}

island_span :: proc(seed: u64, x, z: i32) -> (bottom, top: i32) {
	bottom, top = -1, -1
	gx, gz := floor_div(x, 128), floor_div(z, 128)
	for dz in -1 ..= 1 {for dx in -1 ..= 1 {
		ix, iz := gx + i32(dx), gz + i32(dz)
		h := hash3(seed + 3101, ix, 0, iz)
		if h % 4 == 0 {continue}
		cx, cz := ix * 128 + 64 + i32(h >> 8 & 15) - 8, iz * 128 + 64 + i32(h >> 16 & 15) - 8
		rx, rz := i64(28 + (h >> 24) % 17), i64(24 + (h >> 32) % 21)
		ox, oz := i64(x - cx), i64(z - cz)
		if abs(ox) > rx || abs(oz) > rz {continue}
		if sky_weight(seed, cx, cz) < TQ * 3 / 4 {continue}
		r2 := ox * ox * TQ / (rx * rx) + oz * oz * TQ / (rz * rz)
		if r2 >= TQ {continue}
		// Spacing and radius bounds prevent horizontal overlap between islands.
		top = 176 + i32((h >> 40) % 41) - i32(qmul(r2, r2) * 6 / TQ)
		top += i32((noise_scaled(seed + 3102, x, z, 19) - TQ / 2) * 5 / TQ)
		bottom = top - 2 - i32((TQ - r2) * i64(18 + (h >> 48) % 15) / TQ)
		return
	}}
	return
}

// Grounded formations are one bounded vertical interval per column. Their
// descriptors depend on the base landscape only, avoiding recursive sampling.
ground_form_span :: proc(seed: u64, x, z: i32) -> (bottom, top: i32) {
	bottom, top = -1, -1
	gx, gz := floor_div(x, 128), floor_div(z, 128)
	for dz in -1 ..= 1 {for dx in -1 ..= 1 {
		ix, iz := gx + i32(dx), gz + i32(dz)
		h := hash3(seed + 3201, ix, 0, iz)
		if h % 16 != 0 {continue}
		cx, cz := ix * 128 + 64, iz * 128 + 64
		a, b := i64(x - cx), i64(z - cz)
		if h >> 8 & 1 != 0 {a, b = b, a}
		if abs(a) > 30 || abs(b) > 16 {continue}
		center := terrain_base_column(seed, cx, cz)
		if center.height <= SEA_LEVEL + 4 || center.height > 150 || center.sky > TQ / 4 {continue}
		height := i32(24 + (h >> 16) % 15)
		kind := (h >> 32) % 3
		if kind == 0 {
			if abs(a) > 26 || abs(b) > 5 {continue}
			left := terrain_base_column(seed, cx - 25, cz).height
			right := terrain_base_column(seed, cx + 25, cz).height
			if h >> 8 & 1 != 0 {
				left = terrain_base_column(seed, cx, cz - 25).height
				right = terrain_base_column(seed, cx, cz + 25).height
			}
			if abs(left - right) > 10 || center.height > min(left, right) + 8 {continue}
			base := min(left, right) - 2
			top = base + i32(i64(height) * integer_sqrt(max(0, TQ * TQ - a * a * TQ * TQ / (26 * 26))) / TQ)
			bottom = base
			if abs(a) < 20 {bottom = base + i32(i64(height - 6) * integer_sqrt(max(0, TQ * TQ - a * a * TQ * TQ / (20 * 20))) / TQ) + 1}
		} else if kind == 1 {
			if a < -12 || a > 26 || abs(b) > 10 {continue}
			top = center.height + height / 2 + i32((10 - abs(b)) / 3)
			bottom = top - 5
			if a < -5 {bottom = center.height - 12}
		} else {
			r2 := a * a * TQ / 144 + b * b * TQ / 144
			if r2 > TQ {continue}
			bottom = center.height - 12
			top = center.height + i32((TQ - r2) * i64(height) / TQ)
		}
		return
	}}
	return
}

terrain_column_v3 :: proc(seed: u64, x, z: i32) -> Terrain_Column {
	column := terrain_base_column(seed, x, z)
	if column.sky != 0 {column.island_bottom, column.island_top = island_span(seed, x, z)}
	column.form_bottom, column.form_top = ground_form_span(seed, x, z)
	return column
}

tree_v3 :: proc(seed: u64, gx, gz: i32) -> Tree {
	h := hash3(seed + 700, gx, 0, gz)
	x, z := gx * 8 + 2 + i32(h % 4), gz * 8 + 2 + i32((h >> 8) % 4)
	c := terrain_column_v3(seed, x, z)
	chance: u64 = 11
	if c.biome == .Forest {chance = 2}
	if c.biome == .Sky_Islands {chance = 7}
	active := c.height > SEA_LEVEL + 1 && c.slope < TQ && c.biome != .Desert && c.biome != .Mountains && h % chance == 0
	height := c.height
	if c.island_top >= 0 && h >> 40 & 1 != 0 {height = c.island_top}
	if c.form_top >= c.height {active = false}
	return {x, z, height, active}
}

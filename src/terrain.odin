package sandbox

GENERATOR_VERSION :: 3
SEA_LEVEL :: 52

// Small deterministic ellipsoid veins, including across negative chunk borders.
ore_block :: proc(seed: u64, p: I3) -> Block {
	g := I3{floor_div(p.x, 7), floor_div(p.y, 7), floor_div(p.z, 7)}
	h := hash3(seed + 1801, g.x, g.y, g.z)
	center := g * 7 + I3{1 + i32(h % 5), 1 + i32((h >> 8) % 5), 1 + i32((h >> 16) % 5)}
	d := p - center
	if d.x * d.x + 2 * d.y * d.y + d.z * d.z > 6 {return .Stone}
	roll := (h >> 24) % 100
	if p.y <= 24 && roll < 9 {return .Diamond_Ore}
	if p.y <= 32 && roll < 24 {return .Redstone_Ore}
	if p.y <= 40 && roll < 39 {return .Gold_Ore}
	if p.y <= 64 && roll < 62 {return .Iron_Ore}
	if p.y >= 24 && p.y <= 80 && roll < 79 {return .Copper_Ore}
	if p.y <= 100 && roll < 94 {return .Coal_Ore}
	return .Stone
}

legacy_terrain_height :: proc(seed: u64, x, z: i32) -> i32 {
	a := noise2(seed, f64(x) / 160, f64(z) / 160)
	b := noise2(seed + 1, f64(x) / 43, f64(z) / 43)
	c := noise2(seed + 2, f64(x) / 17, f64(z) / 17)
	return 38 + i32(a * a * 66 + b * 19 + c * 5)
}

terrain_height :: proc(seed: u64, x, z: i32, generator: u32 = GENERATOR_VERSION) -> i32 {
	if generator >= 3 {return terrain_column_v3(seed, x, z).height}
	return legacy_terrain_height(seed, x, z)
}

cave :: proc(seed: u64, x, y, z: i32) -> bool {
	// Interpolated 3-D value field: continuous caves across chunk boundaries.
	ix, iy, iz := floor_div(x, 12), floor_div(y, 12), floor_div(z, 12)
	fx, fy, fz :=
		smooth(f64(x - ix * 12) / 12), smooth(f64(y - iy * 12) / 12), smooth(f64(z - iz * 12) / 12)
	v: [2]f64
	for k in 0 ..< 2 {
		a := f64(hash3(seed + 91, ix, iy, iz + i32(k)) & 65535) / 65535
		b := f64(hash3(seed + 91, ix + 1, iy, iz + i32(k)) & 65535) / 65535
		c := f64(hash3(seed + 91, ix, iy + 1, iz + i32(k)) & 65535) / 65535
		d := f64(hash3(seed + 91, ix + 1, iy + 1, iz + i32(k)) & 65535) / 65535
		v[k] = lerp(lerp(a, b, fx), lerp(c, d, fx), fy)
	}
	return lerp(v[0], v[1], fz) > 0.69
}

Tree :: struct {
	x, z, h: i32,
	active:  bool,
}

tree_at :: proc(seed: u64, gx, gz: i32, generator: u32 = GENERATOR_VERSION) -> Tree {
	if generator >= 3 {return tree_v3(seed, gx, gz)}
	h := hash3(seed + 700, gx, 0, gz)
	x, z := gx * 8 + 2 + i32(h % 4), gz * 8 + 2 + i32((h >> 8) % 4)
	height := legacy_terrain_height(seed, x, z)
	return {x, z, height, h % 5 == 0 && (generator < 2 || height >= SEA_LEVEL)}
}

base_block :: proc(seed: u64, p: I3, h: i32, generator: u32 = GENERATOR_VERSION) -> Block {
	if p.y < 0 || p.y >= HEIGHT {
		return .Air
	}
	if p.y == 0 {
		return .Bedrock
	}
	if p.y <= h {
		if p.y > 5 && p.y < h - 5 && cave(seed, p.x, p.y, p.z) {
			if generator >= 2 && p.y <= 12 {return .Lava}
			return .Air
		}
		if p.y == h {
			if h < 53 {
				return .Sand
			}
			return .Grass
		}
		if p.y >= h - 3 {
			return .Dirt
		}
		if generator >= 2 {return ore_block(seed, p)}
		return .Stone
	}
	if generator >= 2 && p.y <= SEA_LEVEL {return .Water}
	return .Air
}

generated_block :: proc(seed: u64, p: I3, generator: u32 = GENERATOR_VERSION) -> Block {
	if generator >= 3 {
		column := terrain_column_v3(seed, p.x, p.z)
		features := terrain_features(seed, chunk_of(p), generator)
		return terrain_block_v3(seed, p, column, &features)
	}
	h := legacy_terrain_height(seed, p.x, p.z)
	b := base_block(seed, p, h, generator)
	if b != .Air || p.y < 0 || p.y >= HEIGHT {
		return b
	}
	gx, gz := floor_div(p.x, 8), floor_div(p.z, 8)
	for dz in -1 ..= 1 {
		for dx in -1 ..= 1 {
			t := tree_at(seed, gx + i32(dx), gz + i32(dz), generator)
			if !t.active {
				continue
			}
			if p.x == t.x && p.z == t.z && p.y > t.h && p.y <= t.h + 5 {
				return .Wood
			}
			if abs(p.x - t.x) <= 2 && abs(p.z - t.z) <= 2 && p.y >= t.h + 3 && p.y <= t.h + 6 {
				if abs(p.x - t.x) + abs(p.z - t.z) + abs(p.y - (t.h + 4)) <= 5 {
					b = .Leaves
				}
			}
		}
	}
	return b
}

package sandbox

// Generator 5: shared climate weights blend landform profiles before rounding.
// All arithmetic that chooses blocks is integer. These are geological shape
// approximations, not a tectonic, sediment-transport, or climate simulation.
Geo_Climate :: struct {
	temperature, moisture, continental, mountain, variety, volcanic: i64,
}

geography_climate :: proc(seed: u64, x, z: i32) -> Geo_Climate {
	land := noise_scaled(seed + 2001, x, z, 768)
	return {
		noise_scaled(seed + 5001, x, z, 640),
		noise_scaled(seed + 5002, x, z, 448),
		land,
		qramp(TQ * 52 / 100, TQ * 80 / 100, noise_scaled(seed + 6002, x, z, 512)),
		noise_scaled(seed + 5003, x, z, 224),
		noise_scaled(seed + 5005, x, z, 256),
	}
}

geography_weights :: proc(c: Geo_Climate) -> [Biome]i64 {
	w: [Biome]i64
	cold := TQ - qramp(TQ * 30 / 100, TQ * 40 / 100, c.temperature)
	hot := qramp(TQ * 58 / 100, TQ * 68 / 100, c.temperature)
	temperate := TQ - cold - hot
	dry := TQ - qramp(TQ * 30 / 100, TQ * 42 / 100, c.moisture)
	wet := qramp(TQ * 54 / 100, TQ * 66 / 100, c.moisture)
	middle := TQ - dry - wet
	forest := qramp(TQ * 40 / 100, TQ * 53 / 100, c.moisture)
	birch := TQ - qramp(TQ * 28 / 100, TQ * 42 / 100, c.variety)
	cherry := qramp(TQ * 59 / 100, TQ * 73 / 100, c.variety)
	mesa := qramp(TQ * 43 / 100, TQ * 62 / 100, c.variety)
	w[.Snowy_Plains] = qmul(cold, TQ - forest)
	w[.Taiga] = qmul(cold, forest)
	w[.Desert] = qmul(qmul(hot, dry), TQ - mesa)
	w[.Badlands] = qmul(qmul(hot, dry), mesa)
	w[.Savanna] = qmul(hot, middle)
	w[.Jungle] = qmul(hot, wet)
	w[.Plains] = qmul(temperate, dry + qmul(middle, TQ - cherry))
	w[.Cherry_Grove] = qmul(temperate, qmul(middle + wet, cherry))
	w[.Birch_Forest] = qmul(qmul(temperate, wet), birch)
	w[.Forest] = qmul(qmul(temperate, wet), max(0, TQ - birch - cherry))
	// Wetlands occupy low relief humid terrain, not arbitrary steep hills.
	swamp := qmul(qmul(temperate, wet), TQ - qramp(TQ / 8, TQ / 2, c.mountain))
	swamp = qmul(swamp, qramp(TQ * 64 / 100, TQ * 77 / 100, c.moisture))
	for &v in w {v = qmul(v, TQ - swamp)}
	w[.Swamp] += swamp
	return w
}

// Smooth terraces preserve broad benches with short, steep risers. The mapping
// is continuous, including negative input and the boundary between two steps.
geography_terrace :: proc(value: i64, steps: i64) -> i64 {
	v := value * steps
	i := qfloor(v, TQ)
	return (i * TQ + qramp(TQ * 60 / 100, TQ * 92 / 100, v - i * TQ)) / steps
}

geography_dune :: proc(x, z: i32, warp: i64) -> i64 {
	phase := (i64(x) * 4 + i64(z)) * TQ / 224 + warp * 2
	phase -= qfloor(phase, TQ) * TQ
	// Long windward ramp and short slip face; a common global wind direction.
	return phase < TQ * 3 / 4 ? qsmooth(phase * 4 / 3) : qsmooth((TQ - phase) * 4)
}

geography_dry_column :: proc(seed: u64, x, z: i32) -> Terrain_Column {
	climate := geography_climate(seed, x, z)
	weights := geography_weights(climate)
	// Two broad warps bend the ridge, terrace, and dune domains coherently.
	wx := x + i32((noise_scaled(seed + 6100, x, z, 320) - TQ / 2) * 40 / TQ)
	wz := z + i32((noise_scaled(seed + 6101, x, z, 320) - TQ / 2) * 40 / TQ)
	b := noise_d2(seed + 6102, i64(wx) * TQ / 224, i64(wz) * TQ / 224)
	m := noise_d2(seed + 6103, i64(wx) * TQ / 80, i64(wz) * TQ / 80)
	f := noise_d2(seed + 6104, i64(wx) * TQ / 24, i64(wz) * TQ / 24)
	broad, mid, fine := b.value, m.value, f.value - TQ / 2
	profiles: [Biome]i64
	profiles[.Plains] = 61 * TQ + broad * 8 + mid * 2 + fine
	profiles[.Forest] = 66 * TQ + broad * 20 + mid * 8 + fine * 2
	profiles[.Birch_Forest] = 64 * TQ + broad * 14 + mid * 4 + fine
	profiles[.Cherry_Grove] = 70 * TQ + broad * 18 + geography_terrace(mid, 3) * 10 + fine
	profiles[.Desert] = 60 * TQ + broad * 12 + geography_dune(wx, wz, broad) * 8
	profiles[.Badlands] = 64 * TQ + geography_terrace(broad, 4) * 44 + fine * 2
	profiles[.Savanna] =
		63 * TQ + broad * 12 + qramp(TQ * 68 / 100, TQ * 86 / 100, mid) * 20 + fine
	profiles[.Jungle] = 68 * TQ + broad * 26 + mid * 16 + fine * 3
	profiles[.Swamp] = 52 * TQ + (mid - TQ / 2) * 3 + fine / 2
	profiles[.Snowy_Plains] = 61 * TQ + broad * 6 + mid * 2 + fine / 2
	profiles[.Taiga] = 62 * TQ + broad * 13 + mid * 2 + fine
	if weights[.Taiga] > 0 {
		// Elongated glacial hills, with a separate direction from dune winds.
		drumlin := noise_d2(seed + 6105, i64(wx) * TQ / 64, i64(wz) * TQ / 192).value
		profiles[.Taiga] += qsmooth(drumlin) * 9
	}
	// Normalize truncated fixed-point weights rather than leaving height holes.
	h, total: i64
	biome := Biome.Plains
	best: i64
	for v, i in weights {
		h += v * profiles[i] / TQ; total += v
		if v > best {best = v; biome = Biome(i)}
	}
	h = total > 0 ? h * TQ / total : profiles[.Plains]
	land := qramp(TQ * 27 / 100, TQ * 48 / 100, climate.continental)
	mountain := qmul(land, climate.mountain)
	ridge: Noise_D2
	if mountain > 0 {
		ridge = terrain_fbm(seed + 6200, wx, wz, 224, true)
		r := clamp(ridge.value + TQ / 2, 0, TQ)
		alpine := 94 * TQ + r * 116 + mid * 10
		// Broad-bottomed troughs cut through the mountain mass; the drainage
		// graph adds connected river valleys to this glacial shaping signal.
		trough := TQ - qramp(TQ * 8 / 100, TQ * 31 / 100, abs(mid - TQ / 2) * 2)
		alpine -= qmul(trough, 24 * TQ)
		h = qlerp(h, alpine, mountain)
		if mountain >
		   TQ /
			   2 {biome = climate.temperature < TQ * 46 / 100 || h > 130 * TQ ? Biome.Snowy_Peaks : Biome.Mountains}
	}
	// Continental shelves deepen offshore; mountain coastlines remain steeper.
	ocean := 15 * TQ + broad * 12 + land * 13
	h = qlerp(ocean, h, land)
	volcanic := qmul(qramp(TQ * 73 / 100, TQ * 84 / 100, climate.volcanic), TQ - mountain)
	volcanic = qmul(volcanic, TQ - qramp(TQ * 64 / 100, TQ * 76 / 100, climate.temperature))
	volcanic = qmul(volcanic, qramp(TQ * 28 / 100, TQ * 40 / 100, climate.temperature))
	dome := 64 * TQ + mid * 17 - qramp(TQ * 91 / 100, TQ * 98 / 100, climate.volcanic) * 16
	h = qlerp(h, dome, volcanic)
	if volcanic > TQ / 2 {biome = .Mushroom_Fields}
	sky := sky_weight(seed, x, z)
	h = qlerp(h, 66 * TQ + broad * 8, sky)
	column := Terrain_Column {
		height        = i32(clamp(h / TQ, 8, 228)),
		biome         = biome,
		temperature   = climate.temperature,
		moisture      = climate.moisture,
		sky           = sky,
		relief        = mountain,
		water_level   = SEA_LEVEL,
		slope         = (abs(b.dx) + abs(b.dz)) *
			22 /
			224 + (abs(m.dx) + abs(m.dz)) * 8 / 80 + qmul(mountain, (abs(ridge.dx) + abs(ridge.dz)) * 116 / 224),
		island_bottom = -1,
		island_top    = -1,
		form_bottom   = -1,
		form_top      = -1,
	}
	if column.height <
	   SEA_LEVEL -
		   3 {column.biome = .Ocean} else if column.height <= SEA_LEVEL + 3 && biome != .Swamp {column.biome = .Coast}
	if column.biome == .Swamp {column.water_level = SEA_LEVEL + 2}
	if sky >= TQ / 2 {column.biome = .Sky_Islands}
	return column
}

geography_column :: proc(seed: u64, x, z: i32, rivers: ^River_Set = nil) -> Terrain_Column {
	c := geography_dry_column(seed, x, z)
	local: River_Set
	set := rivers
	if set == nil {local = river_set_for(seed, chunk_of({x, 0, z})); set = &local}
	river_carve(&c, x, z, set)
	return c
}

// Descriptor height queries share a prepared drainage set whenever possible.
terrain_reference_column :: proc(
	seed: u64,
	x, z: i32,
	generator: u32,
	rivers: ^River_Set = nil,
) -> Terrain_Column {
	if generator >= 5 {return geography_column(seed, x, z, rivers)}
	return terrain_base_for(seed, x, z, generator)
}

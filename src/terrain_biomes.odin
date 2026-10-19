package sandbox

// Version 4 keeps the fixed-point, coordinate-only terrain model. Climate is
// sampled once per column; broad masks blend landform heights before choosing
// the discrete surface/vegetation biome. No world-sized maps or erosion grids.
terrain_base_v4 :: proc(seed: u64, x, z: i32) -> Terrain_Column {
	c := terrain_base_column(seed, x, z)
	c.temperature = noise_scaled(seed + 5001, x, z, 640)
	c.moisture = noise_scaled(seed + 5002, x, z, 448)
	variety := noise_scaled(seed + 5003, x, z, 224)
	t, m := c.temperature, c.moisture
	// Warm wet lowlands become shallow marshes through a continuous mask.
	wet := qmul(qramp(TQ * 60 / 100, TQ * 73 / 100, m), qramp(TQ * 34 / 100, TQ * 43 / 100, t))
	wet = qmul(wet, TQ - qramp(TQ * 60 / 100, TQ * 68 / 100, t))
	wet = qmul(wet, TQ - qramp(76 * TQ, 100 * TQ, i64(c.height) * TQ))
	land := qramp(SEA_LEVEL * TQ, (SEA_LEVEL + 7) * TQ, i64(c.height) * TQ)
	wet = qmul(wet, land)
	h := i64(c.height) * TQ
	marsh := 51 * TQ + noise_scaled(seed + 5004, x, z, 48) * 5
	h = qlerp(h, marsh, wet)
	// A broad dry mesa mask shapes plateaus without stepping at biome borders.
	dry := qmul(
		qramp(TQ * 59 / 100, TQ * 70 / 100, t),
		TQ - qramp(TQ * 28 / 100, TQ * 40 / 100, m),
	)
	mesa := qmul(dry, qramp(TQ * 45 / 100, TQ * 62 / 100, variety))
	h += qmul(mesa, land * 22)
	c.height = i32(clamp(h / TQ, 18, 211))
	if c.height <
	   SEA_LEVEL -
		   2 {c.biome = .Ocean} else if c.height <= SEA_LEVEL + 3 && wet < TQ / 2 {c.biome = .Coast} else if c.biome == .Mountains {
		c.biome = c.height >= 112 || t < TQ * 42 / 100 ? Biome.Snowy_Peaks : Biome.Mountains
	} else if wet >= TQ / 2 {
		c.biome = .Swamp
	} else if t < TQ * 34 / 100 {
		c.biome = m > TQ * 46 / 100 ? Biome.Taiga : Biome.Snowy_Plains
	} else if t > TQ * 61 / 100 {
		if m <
		   TQ *
			   38 /
			   100 {c.biome = variety > TQ / 2 ? Biome.Badlands : Biome.Desert} else if m < TQ * 56 / 100 {c.biome = .Savanna} else {c.biome = .Jungle}
	} else if noise_scaled(seed + 5005, x, z, 256) > TQ * 77 / 100 {
		c.biome = .Mushroom_Fields
	} else if m < TQ * 34 / 100 {
		c.biome = t > TQ / 2 ? Biome.Desert : Biome.Plains
	} else if m > TQ / 2 {
		c.biome =
			variety < TQ * 35 / 100 ? Biome.Birch_Forest : (variety > TQ * 64 / 100 ? Biome.Cherry_Grove : Biome.Forest)
	} else {
		c.biome = variety > TQ * 70 / 100 ? Biome.Cherry_Grove : Biome.Plains
	}
	if c.sky >= TQ / 2 {c.biome = .Sky_Islands}
	return c
}

terrain_base_for :: proc(seed: u64, x, z: i32, generator: u32) -> Terrain_Column {
	if generator >= 5 {return geography_dry_column(seed, x, z)}
	if generator >= 4 {return terrain_base_v4(seed, x, z)}
	return terrain_base_column(seed, x, z)
}

terrain_column :: proc(
	seed: u64,
	x, z: i32,
	generator: u32,
	features: ^Terrain_Features = nil,
) -> Terrain_Column {
	if generator < 4 {return terrain_column_v3(seed, x, z, features)}
	rivers: ^River_Set
	if features != nil {rivers = &features.rivers}
	c := generator >= 5 ? geography_column(seed, x, z, rivers) : terrain_base_v4(seed, x, z)
	if features == nil {
		c.island_bottom, c.island_top = island_span(seed, x, z)
		c.form_bottom, c.form_top = ground_form_interval(
			seed,
			ground_form(seed, floor_div(x, 128), floor_div(z, 128), generator),
			x,
			z,
		)
	} else {
		for f in features.islands[:features.form_count] {
			lo, hi := island_form_span(seed, f, x, z)
			if lo >= 0 {c.island_bottom, c.island_top = lo, hi; break}
		}
		for f in features.forms[:features.form_count] {
			lo, hi := ground_form_interval(seed, f, x, z)
			if lo >= 0 {c.form_bottom, c.form_top = lo, hi; break}
		}
	}
	return c
}

biome_surface :: proc(c: Terrain_Column, y: i32) -> Block {
	top := y == c.height
	#partial switch c.biome {
	case .Ocean, .Coast, .Desert:
		return .Sand
	case .Badlands:
		switch floor_div(y, 4) % 6 {
		case 0:
			return .White_Terracotta
		case 1, 4:
			return .Red_Terracotta
		case 2:
			return .Yellow_Terracotta
		case:
			return .Terracotta
		}
	case .Snowy_Plains, .Taiga, .Snowy_Peaks:
		if top {return .Snow}
		if c.biome == .Snowy_Peaks {return .Stone}
		if c.biome == .Taiga && y == c.height - 1 {return .Podzol}
		return .Dirt
	case .Mushroom_Fields:
		return top ? Block.Mycelium : Block.Dirt
	case .Swamp:
		return top ? Block.Swamp_Grass : Block.Mud
	case .Savanna:
		return top ? Block.Dry_Grass : Block.Dirt
	case .Jungle:
		return top ? Block.Lush_Grass : Block.Dirt
	case:
		if c.slope > TQ || c.height > 140 {return .Stone}
		return top ? Block.Grass : Block.Dirt
	}
}

terrain_block_v4 :: proc(
	seed: u64,
	p: I3,
	c: Terrain_Column,
	features: ^Terrain_Features,
) -> Block {
	if p.y < 0 || p.y >= HEIGHT {return .Air}
	if p.y == 0 {return .Bedrock}
	lake, reserved := cave_lake_sample(p, features.caves[:features.cave_count])
	if reserved {return lake}
	b: Block = .Air
	if p.y <= c.height {
		caved, carved := cave_sample_v3(p, c.height, features.caves[:features.cave_count])
		if carved {b = caved} else if p.y >= c.height - 3 || c.biome == .Badlands && p.y >= c.height - 24 {
			b = biome_surface(c, p.y)
		} else {b = ore_block(seed, p)}
	} else if p.y <= SEA_LEVEL {
		b = p.y == SEA_LEVEL && c.temperature < TQ * 34 / 100 ? Block.Ice : Block.Water
	}
	// Marsh ponds are bounded by their continuous ground field, not queued edits.
	if c.biome == .Swamp && p.y > c.height && p.y <= SEA_LEVEL + 2 {b = .Water}
	if c.form_bottom >= 0 && p.y >= c.form_bottom && p.y <= c.form_top {b = .Stone}
	if c.island_bottom >= 0 && p.y >= c.island_bottom && p.y <= c.island_top {
		b =
			p.y == c.island_top ? Block.Grass : (p.y >= c.island_top - 3 ? Block.Dirt : Block.Stone)
	}
	if b == .Air {return vegetation_block(p, features.plants[:features.plant_count])}
	return b
}

package sandbox

geography_surface :: proc(c: Terrain_Column, y: i32) -> Block {
	if c.rocky {return .Stone}
	top := y == c.height
	depth := c.height - y
	if c.river > TQ / 3 {
		if c.temperature < TQ * 34 / 100 && top && c.height >= c.water_level {return .Snow}
		if c.relief > TQ / 2 {return .Cobblestone}
		return c.biome == .Swamp || c.biome == .Jungle ? Block.Mud : Block.Sand
	}
	#partial switch c.biome {
	case .Ocean:
		return c.height < 32 ? Block.Mud : Block.Sand
	case .Coast:
		return c.relief > TQ / 3 && c.slope > TQ / 2 ? Block.Stone : Block.Sand
	case .Desert:
		return .Sand
	case .Badlands:
		return biome_surface(c, y)
	case .Snowy_Peaks, .Mountains:
		snow_line := 102 + i32(c.temperature * 64 / TQ)
		if top && (c.height >= snow_line || c.temperature < TQ * 36 / 100) {return .Snow}
		if depth <= 1 && c.temperature < TQ * 34 / 100 && c.river > 0 {return .Ice}
		return .Stone
	case .Snowy_Plains:
		return top ? Block.Snow : Block.Dirt
	case .Taiga:
		if top {return c.temperature < TQ * 34 / 100 ? Block.Snow : Block.Podzol}
		return depth == 1 ? Block.Podzol : Block.Dirt
	case .Swamp:
		return top && c.height >= c.water_level ? Block.Swamp_Grass : Block.Mud
	case .Mushroom_Fields:
		return top ? Block.Mycelium : Block.Stone
	case:
		if c.slope > TQ && c.relief > TQ / 3 {return .Stone}
		if !top {return .Dirt}
		if c.biome == .Savanna {return .Dry_Grass}
		if c.biome == .Jungle {return .Lush_Grass}
		return .Grass
	}
}

terrain_block_v5 :: proc(
	seed: u64,
	p: I3,
	c: Terrain_Column,
	features: ^Terrain_Features,
) -> Block {
	if p.y < 0 || p.y >= HEIGHT {return .Air}
	if p.y == 0 {return .Bedrock}
	lake, reserved := cave_lake_sample(p, features.caves[:features.cave_count])
	// Surface drainage owns its bed and banks. A buried cave cannot puncture it
	// or push an underground lake shell up through the river's open channel.
	seal := c.river > 0 && p.y >= c.height - 3
	if reserved && !seal {return lake}
	b := Block.Air
	if p.y <= c.height {
		caved, carved := cave_sample_v3(p, c.height, features.caves[:features.cave_count])
		soil: i32 = 4
		#partial switch c.biome {
		case .Jungle, .Swamp:
			soil = 7
		case .Forest, .Cherry_Grove:
			soil = 5
		case .Badlands:
			soil = 24
		case .Mushroom_Fields:
			soil = 3
		}
		if carved &&
		   !seal {b = caved} else if p.y >= c.height - soil {b = geography_surface(c, p.y)} else {b = ore_block(seed, p)}
	} else if p.y <= c.water_level {
		frozen := c.temperature < TQ * 34 / 100 || c.water_level > 128
		b = p.y == c.water_level && frozen ? Block.Ice : Block.Water
	}
	// Keep existing bounded fantasy formations, but never dam a river with one.
	if c.river == 0 && c.form_bottom >= 0 && p.y >= c.form_bottom && p.y <= c.form_top {b = .Stone}
	if c.island_bottom >= 0 && p.y >= c.island_bottom && p.y <= c.island_top {
		b =
			p.y == c.island_top ? Block.Grass : (p.y >= c.island_top - 3 ? Block.Dirt : Block.Stone)
	}
	if b == .Air {return vegetation_block(p, features.plants[:features.plant_count])}
	return b
}

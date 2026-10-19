package sandbox

Plant_Kind :: enum u8 {
	Oak,
	Birch,
	Spruce,
	Acacia,
	Jungle,
	Cherry,
	Red_Mushroom,
	Brown_Mushroom,
	Cactus,
}
Plant :: struct {
	x, z, h, height: i32,
	kind:            Plant_Kind,
	active:          bool,
}

// Cell centers are at least two blocks from their border. Canopies reach at
// most four blocks; a one-cell horizontal halo and 16 vertical blocks suffice.
plant_at :: proc(seed: u64, gx, gz: i32, generator: u32 = 4, rivers: ^River_Set = nil) -> Plant {
	h := hash3(seed + 5700, gx, 0, gz)
	p := Plant {
		x      = gx * 8 + 2 + i32(h % 4),
		z      = gz * 8 + 2 + i32((h >> 8) % 4),
		height = 5 + i32(h >> 16 & 1),
	}
	c: Terrain_Column
	if generator >= 5 {
		c = geography_column(seed, p.x, p.z, rivers)
		c.island_bottom, c.island_top = island_span(seed, p.x, p.z)
		c.form_bottom, c.form_top = ground_form_interval(
			seed,
			ground_form(seed, floor_div(p.x, 128), floor_div(p.z, 128), generator, rivers),
			p.x,
			p.z,
		)
	} else {c = terrain_column(seed, p.x, p.z, 4)}
	p.h = c.height
	chance: u64 = 12
	#partial switch c.biome {
	case .Forest:
		chance = 2
	case .Birch_Forest:
		p.kind = .Birch; chance = 2; p.height = 7
	case .Cherry_Grove:
		p.kind = .Cherry; chance = 3; p.height = 6
	case .Taiga:
		p.kind = .Spruce; chance = 3; p.height = 10 + i32(h >> 17 & 1)
	case .Savanna:
		p.kind = .Acacia; chance = 7; p.height = 6
	case .Jungle:
		p.kind = .Jungle; chance = 2; p.height = 12 + i32((h >> 18) % 3)
	case .Swamp:
		chance = 4; p.height = 6
	case .Mushroom_Fields:
		p.kind = h >> 40 & 1 == 0 ? Plant_Kind.Red_Mushroom : Plant_Kind.Brown_Mushroom; chance = 4
		p.height = 5
	case .Desert, .Badlands:
		p.kind = .Cactus; chance = 9; p.height = 2 + i32((h >> 18) % 3)
	case .Ocean, .Coast, .Mountains, .Snowy_Peaks, .Snowy_Plains:
		return p
	case .Sky_Islands:
		chance = 7
		if c.island_top >= 0 && h >> 40 & 1 != 0 {p.h = c.island_top}
	case:
	}
	p.active = p.h > SEA_LEVEL + 1 && c.slope < TQ && c.form_top < c.height && h % chance == 0
	if generator >= 5 && p.h <= c.water_level {p.active = false}
	if p.active {
		// Avoid trunks hanging over a cliff or cactus shoulders embedded in it.
		for dz: i32 = -1; dz <= 1; dz += 2 {for dx: i32 = -1; dx <= 1; dx += 2 {
				if p.h == c.height &&
				   abs(
					   terrain_reference_column(seed, p.x + dx, p.z + dz, generator, rivers).height -
					   p.h,
				   ) >
					   1 {p.active = false}
			}}
	}
	return p
}

vegetation_block :: proc(p: I3, plants: []Plant) -> Block {
	result: Block = .Air
	for t in plants {
		x, z, y := p.x - t.x, p.z - t.z, p.y - t.h
		if y <= 0 || y > t.height + 2 || abs(x) > 4 || abs(z) > 4 {continue}
		wood, leaves := Block.Wood, Block.Leaves
		#partial switch t.kind {
		case .Birch:
			wood, leaves = .Birch_Wood, .Birch_Leaves
		case .Spruce:
			wood, leaves = .Spruce_Wood, .Spruce_Leaves
		case .Acacia:
			wood, leaves = .Acacia_Wood, .Acacia_Leaves
		case .Jungle:
			wood, leaves = .Jungle_Wood, .Jungle_Leaves
		case .Cherry:
			wood, leaves = .Cherry_Wood, .Cherry_Leaves
		case .Red_Mushroom, .Brown_Mushroom:
			if x == 0 && z == 0 && y < t.height {return .Mushroom_Stem}
			red := t.kind == .Red_Mushroom
			if (red &&
				   y >= t.height - 1 &&
				   y <= t.height + 1 &&
				   max(abs(x), abs(z)) <= 2 - max(0, y - t.height)) ||
			   (!red &&
					   y == t.height &&
					   abs(x) + abs(z) <= 5 &&
					   max(abs(x), abs(z)) <=
						   3) {result = red ? Block.Red_Mushroom : Block.Brown_Mushroom}
			continue
		case .Cactus:
			if x == 0 && z == 0 && y <= t.height {return .Cactus}
			continue
		case:
		}
		// Acacias bend toward +X; all geometry remains within the four-block halo.
		bend: i32 = t.kind == .Acacia && y >= t.height - 2 ? 1 : 0
		trunk_top := t.kind == .Spruce ? t.height - 3 : t.height
		if x == bend && z == 0 && y <= trunk_top {return wood}
		// A horizontal joint joins the bend face-to-face instead of at a corner.
		if t.kind == .Acacia && y == t.height - 2 && x == 0 && z == 0 {return wood}
		leaf := false
		if t.kind == .Spruce {
			radius := max(0, (t.height + 1 - y) / 3)
			leaf =
				y >= 3 &&
				y <= t.height + 1 &&
				max(abs(x), abs(z)) <= min(3, radius) &&
				(y % 3 != 0 || abs(x) + abs(z) <= radius)
		} else if t.kind == .Acacia {
			leaf =
				y >= t.height &&
				y <= t.height + 1 &&
				max(abs(x - 1), abs(z)) <= 3 &&
				abs(x - 1) + abs(z) <= 5
		} else {
			radius: i32 = t.kind == .Cherry || t.kind == .Jungle ? 4 : 2
			leaf =
				y >= t.height - 2 &&
				y <= t.height + 1 &&
				max(abs(x), abs(z)) <= radius &&
				abs(x) + abs(z) + abs(y - (t.height - 1)) <= radius + 2
		}
		if leaf {result = leaves}
	}
	return result
}

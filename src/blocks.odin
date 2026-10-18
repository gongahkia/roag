package sandbox

CREATIVE_BLOCKS := [?]Block {
	.Grass,
	.Dirt,
	.Stone,
	.Sand,
	.Wood,
	.Leaves,
	.Water,
	.Lava,
	.Obsidian,
	.Coal_Ore,
	.Iron_Ore,
	.Copper_Ore,
	.Gold_Ore,
	.Diamond_Ore,
	.Redstone_Ore,
	.Cobblestone,
}
BLOCK_NAMES := [?]string {
	"AIR",
	"GRASS",
	"DIRT",
	"STONE",
	"SAND",
	"WOOD",
	"LEAVES",
	"BEDROCK",
	"COAL ORE",
	"IRON ORE",
	"COPPER ORE",
	"GOLD ORE",
	"DIAMOND ORE",
	"REDSTONE ORE",
	"OBSIDIAN",
	"COBBLESTONE",
	"WATER",
	"LAVA",
}
BLOCK_COLORS := [?]F4 {
	{0, 0, 0, 0},
	{0.38, 0.6, 0.25, 1},
	{0.5, 0.35, 0.23, 1},
	{0.5, 0.53, 0.56, 1},
	{0.86, 0.79, 0.59, 1},
	{0.45, 0.31, 0.19, 1},
	{0.25, 0.47, 0.23, 1},
	{0.25, 0.27, 0.3, 1},
	{0.14, 0.15, 0.16, 1},
	{0.80, 0.65, 0.5, 1},
	{0.80, 0.45, 0.23, 1},
	{0.95, 0.78, 0.18, 1},
	{0.29, 0.90, 0.86, 1},
	{0.79, 0.14, 0.19, 1},
	{0.17, 0.11, 0.25, 1},
	{0.47, 0.49, 0.52, 1},
	{0.16, 0.43, 0.82, 1},
	{0.95, 0.36, 0.06, 1},
}

// Repeatable graphical fixture: elevated waterfall/lavafall and an ore wall.
fluid_demo :: proc(w: ^World) {
	for z in 0 ..= 24 {
		for x in 0 ..= 24 {
			world_apply(w, {w.sequence + 1, {i32(x), 84, i32(z)}, .Stone})
			if x == 0 || x == 24 || z == 0 || z == 24 {
				for y in 85 ..= 87 {world_apply(w, {w.sequence + 1, {i32(x), i32(y), i32(z)}, .Cobblestone})}
			}
		}
	}
	for i in 0 ..< 8 {
		for y in 85 ..= 88 {
			for x in 0 ..< 2 {world_apply(w, {w.sequence + 1, {2 + i32(i * 3 + x), i32(y), 2}, Block(int(Block.Coal_Ore) + i)})}
		}
	}
	world_apply(w, {w.sequence + 1, {6, 90, 8}, .Water})
	world_apply(w, {w.sequence + 1, {15, 90, 8}, .Lava})
}

package sandbox

Block :: enum u8 {
	Air,
	Grass,
	Dirt,
	Stone,
	Sand,
	Wood,
	Leaves,
	Bedrock,
	Coal_Ore,
	Iron_Ore,
	Copper_Ore,
	Gold_Ore,
	Diamond_Ore,
	Redstone_Ore,
	Obsidian,
	Cobblestone,
	Water,
	Lava,
	Water_1,
	Water_2,
	Water_3,
	Water_4,
	Water_5,
	Water_6,
	Water_7,
	Water_Falling,
	Lava_1,
	Lava_2,
	Lava_3,
	Lava_Falling,
	Snow,
	Ice,
	Cactus,
	Terracotta,
	Red_Terracotta,
	Yellow_Terracotta,
	White_Terracotta,
	Birch_Wood,
	Birch_Leaves,
	Spruce_Wood,
	Spruce_Leaves,
	Acacia_Wood,
	Acacia_Leaves,
	Jungle_Wood,
	Jungle_Leaves,
	Cherry_Wood,
	Cherry_Leaves,
	Mushroom_Stem,
	Red_Mushroom,
	Brown_Mushroom,
	Mycelium,
	Mud,
	Podzol,
	Dry_Grass,
	Lush_Grass,
	Swamp_Grass,
}

// Six material bits retain the eight-byte vertex. Serialized IDs 0..29 stay stable.
BLOCK_COUNT :: int(Block.Swamp_Grass) + 1
#assert(BLOCK_COUNT <= 64 && int(Block.Water) == 16 && int(Block.Lava) == 17)

block_valid :: proc(b: Block) -> bool {return u8(b) <= u8(Block.Swamp_Grass)}
fluid_kind :: proc(b: Block) -> Block {
	if b == .Water || (b >= .Water_1 && b <= .Water_Falling) {return .Water}
	if b == .Lava || (b >= .Lava_1 && b <= .Lava_Falling) {return .Lava}
	return .Air
}
is_fluid :: proc(b: Block) -> bool {return fluid_kind(b) != .Air}
is_solid :: proc(b: Block) -> bool {return b != .Air && !is_fluid(b)}
fluid_level :: proc(b: Block) -> int {
	if b >= .Water_1 && b <= .Water_7 {return int(b) - int(Block.Water_1) + 1}
	if b >= .Lava_1 && b <= .Lava_3 {return int(b) - int(Block.Lava_1) + 1}
	return 0
}
fluid_height :: proc(b, above: Block) -> u8 {
	if fluid_kind(above) == fluid_kind(b) || b == .Water_Falling || b == .Lava_Falling {return 8}
	step := fluid_kind(b) == .Lava ? 2 : 1
	return u8(max(1, 7 - fluid_level(b) * step))
}

// Ice is solid for collisions and fluids, but shares the existing blended pass.
is_translucent :: proc(b: Block) -> bool {return fluid_kind(b) == .Water || b == .Ice}

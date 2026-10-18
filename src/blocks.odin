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
}

// Shader material packing reserves five bits; flowing states normalize to the
// source material before upload. Water/lava source IDs are also the atlas layers.
#assert(int(Block.Lava_Falling) < 32 && int(Block.Water) == 16 && int(Block.Lava) == 17)

block_valid :: proc(b: Block) -> bool {return u8(b) <= u8(Block.Lava_Falling)}
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

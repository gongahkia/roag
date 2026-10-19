package sandbox

Biome :: enum u8 {
	Ocean,
	Coast,
	Plains,
	Forest,
	Desert,
	Mountains,
	Sky_Islands,
	Snowy_Plains,
	Taiga,
	Savanna,
	Jungle,
	Swamp,
	Badlands,
	Birch_Forest,
	Cherry_Grove,
	Mushroom_Fields,
	Snowy_Peaks,
}
BIOME_NAMES := [?]string {
	"OCEAN",
	"COAST",
	"PLAINS",
	"FOREST",
	"DESERT",
	"MOUNTAINS",
	"SKY ISLANDS",
	"SNOWY PLAINS",
	"TAIGA",
	"SAVANNA",
	"JUNGLE",
	"SWAMP",
	"BADLANDS",
	"BIRCH FOREST",
	"CHERRY GROVE",
	"MUSHROOM FIELDS",
	"SNOWY PEAKS",
}

Terrain_Column :: struct {
	height:                    i32,
	water_level:               i32,
	river, relief:             i64,
	temperature, moisture:     i64,
	biome:                     Biome,
	rocky:                     bool,
	slope, sky:                i64,
	island_bottom, island_top: i32,
	form_bottom, form_top:     i32,
}

// A selected region owns its islands, but never replaces the ground below.
sky_region :: proc(seed: u64, gx, gz: i32) -> (x, z: i32, active: bool) {
	h := hash3(seed + 3001, gx, 0, gz)
	return gx * 512 + 256 + i32(h >> 8 & 63) - 32,
		gz * 512 + 256 + i32(h >> 16 & 63) - 32,
		h % 12 == 0
}

sky_weight :: proc(seed: u64, x, z: i32) -> i64 {
	gx, gz := floor_div(x, 512), floor_div(z, 512)
	weight: i64
	for dz in -1 ..= 1 {for dx in -1 ..= 1 {
			cx, cz, active := sky_region(seed, gx + i32(dx), gz + i32(dz))
			if !active {continue}
			rx, rz := i64(x - cx), i64(z - cz)
			d2 := rx * rx + rz * rz
			weight = max(
				weight,
				qsmooth(clamp((176 * 176 - d2) * TQ / (176 * 176 - 144 * 144), 0, TQ)),
			)
		}}
	return weight
}

// The cheap base sampler has no trees or feature recursion. All climate and
// landform transitions happen before integer height quantization.
terrain_base_column :: proc(seed: u64, x, z: i32) -> Terrain_Column {
	continental := noise_scaled(seed + 2001, x, z, 768)
	land := qramp(TQ * 28 / 100, TQ * 49 / 100, continental)
	mountain := qmul(
		land,
		qramp(TQ * 55 / 100, TQ * 79 / 100, noise_scaled(seed + 2002, x, z, 320)),
	)
	detail := terrain_fbm(seed + 2100, x, z, 128)
	ridge := terrain_fbm(seed + 2200, x, z, 192, true)
	h := 32 * TQ + land * 34 + qmul(detail.value * 18, land)
	h += qmul(mountain, 42 * TQ + clamp(ridge.value + TQ / 2, 0, TQ) * 82)
	sky := sky_weight(seed, x, z)
	h = qlerp(h, 68 * TQ + detail.value * 18, sky)
	moisture := noise_scaled(seed + 2003, x, z, 384)
	column := Terrain_Column {
		height        = i32(clamp(h / TQ, 18, 189)),
		sky           = sky,
		slope         = (abs(detail.dx) + abs(detail.dz)) *
			18 /
			128 + qmul(mountain, (abs(ridge.dx) + abs(ridge.dz)) * 82 / 192),
		island_bottom = -1,
		island_top    = -1,
		form_bottom   = -1,
		form_top      = -1,
	}
	if column.height <
	   SEA_LEVEL -
		   2 {column.biome = .Ocean} else if column.height <= SEA_LEVEL + 3 {column.biome = .Coast} else if mountain > TQ / 3 && sky < TQ / 2 {column.biome = .Mountains} else if moisture < TQ * 32 / 100 {column.biome = .Desert} else if moisture > TQ * 56 / 100 {column.biome = .Forest} else {column.biome = .Plains}
	if sky >= TQ / 2 {column.biome = .Sky_Islands}
	return column
}

terrain_column_v3 :: proc(
	seed: u64,
	x, z: i32,
	features: ^Terrain_Features = nil,
) -> Terrain_Column {
	column := terrain_base_column(seed, x, z)
	if features == nil {
		column.island_bottom, column.island_top = island_span(seed, x, z)
		column.form_bottom, column.form_top = ground_form_span(seed, x, z)
	} else {
		for f in features.islands[:features.form_count] {
			lo, hi := island_form_span(seed, f, x, z)
			if lo >= 0 {column.island_bottom, column.island_top = lo, hi; break}
		}
		for f in features.forms[:features.form_count] {
			lo, hi := ground_form_interval(seed, f, x, z)
			if lo >= 0 {column.form_bottom, column.form_top = lo, hi; break}
		}
	}
	return column
}

tree_v3 :: proc(seed: u64, gx, gz: i32) -> Tree {
	h := hash3(seed + 700, gx, 0, gz)
	x, z := gx * 8 + 2 + i32(h % 4), gz * 8 + 2 + i32((h >> 8) % 4)
	c := terrain_column_v3(seed, x, z)
	chance: u64 = 11
	if c.biome == .Forest {chance = 2}
	if c.biome == .Sky_Islands {chance = 7}
	active :=
		c.height > SEA_LEVEL + 1 &&
		c.slope < TQ &&
		c.biome != .Desert &&
		c.biome != .Mountains &&
		h % chance == 0
	height := c.height
	if c.island_top >= 0 && h >> 40 & 1 != 0 {height = c.island_top}
	if c.form_top >= c.height {active = false}
	return {x, z, height, active}
}

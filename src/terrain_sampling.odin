package sandbox

// Job-local immutable descriptors are reused by all 34^3 samples. The 64-block
// cave cells extend by at most four blocks; at most 2^3 touch a padded chunk.
Terrain_Features :: struct {
	rivers:      River_Set,
	islands:     [4]Island_Form,
	forms:       [4]Ground_Form,
	form_count:  int,
	plants:      [64]Plant,
	plant_count: int,
	trees:       [64]Tree,
	tree_count:  int,
	caves:       [8]Cave_Cell,
	cave_count:  int,
}

terrain_features :: proc(seed: u64, key: I3, generator: u32) -> Terrain_Features {
	result: Terrain_Features
	if generator >= 5 {result.rivers = river_set_for(seed, key)}
	lo, hi := key * 32 - I3{1, 1, 1}, key * 32 + I3{32, 32, 32}
	if generator >= 3 {
		for z := floor_div(lo.z, 128); z <= floor_div(hi.z, 128); z += 1 {
			for x := floor_div(lo.x, 128); x <= floor_div(hi.x, 128); x += 1 {
				result.islands[result.form_count] = island_form(seed, x, z)
				result.forms[result.form_count] = ground_form(
					seed,
					x,
					z,
					generator,
					&result.rivers,
				)
				result.form_count += 1
			}
		}
	}
	for z := floor_div(lo.z, 8) - 1; z <= floor_div(hi.z, 8) + 1; z += 1 {
		for x := floor_div(lo.x, 8) - 1; x <= floor_div(hi.x, 8) + 1; x += 1 {
			if generator >= 4 {
				plant := plant_at(seed, x, z, generator, &result.rivers)
				if plant.active && plant.h + 16 >= lo.y && plant.h < hi.y {
					result.plants[result.plant_count] = plant; result.plant_count += 1
				}
				continue
			}
			tree := tree_at(seed, x, z, generator)
			if tree.active && tree.h + 6 >= lo.y && tree.h < hi.y {
				result.trees[result.tree_count] = tree; result.tree_count += 1
			}
		}
	}
	if generator >= 3 {
		for z := floor_div(lo.z - 4, 64); z <= floor_div(hi.z + 4, 64); z += 1 {
			for y := max(0, floor_div(lo.y - 4, 64));
			    y <= min(2, floor_div(hi.y + 4, 64));
			    y += 1 {
				for x := floor_div(lo.x - 4, 64); x <= floor_div(hi.x + 4, 64); x += 1 {
					result.caves[result.cave_count] = cave_cell_v3(
						seed,
						{x, y, z},
						generator,
						&result.rivers,
					)
					result.cave_count += 1
				}
			}
		}
	}
	return result
}

terrain_tree_block :: proc(p: I3, trees: []Tree) -> Block {
	if p.y <= 0 || p.y >= HEIGHT {return .Air}
	block: Block = .Air
	for t in trees {
		if p.x == t.x && p.z == t.z && p.y > t.h && p.y <= t.h + 5 {return .Wood}
		if abs(p.x - t.x) <= 2 &&
		   abs(p.z - t.z) <= 2 &&
		   p.y >= t.h + 3 &&
		   p.y <= t.h + 6 &&
		   abs(p.x - t.x) + abs(p.z - t.z) + abs(p.y - (t.h + 4)) <= 5 {block = .Leaves}
	}
	return block
}

terrain_block_v3 :: proc(
	seed: u64,
	p: I3,
	column: Terrain_Column,
	features: ^Terrain_Features,
) -> Block {
	if p.y < 0 || p.y >= HEIGHT {return .Air}
	if p.y == 0 {return .Bedrock}
	// The lake's watertight shell also wins at an unusually low neighboring
	// surface column; this prevents a cliff from exposing a natural leak.
	lake_block, reserved := cave_lake_sample(p, features.caves[:features.cave_count])
	if reserved {return lake_block}
	block: Block = .Air
	if p.y <= column.height {
		caved, carved := cave_sample_v3(p, column.height, features.caves[:features.cave_count])
		if carved {
			block = caved
		} else if p.y >= column.height - 3 {
			if column.biome == .Desert || column.biome == .Coast || column.biome == .Ocean {
				block = .Sand
			} else if column.slope > TQ || column.height > 140 {
				block = .Stone
			} else {
				block = p.y == column.height ? Block.Grass : Block.Dirt
			}
		} else {
			block = ore_block(seed, p)
		}
	} else if p.y <= SEA_LEVEL {
		block = .Water
	}
	if column.form_bottom >= 0 &&
	   p.y >= column.form_bottom &&
	   p.y <= column.form_top {block = .Stone}
	if column.island_bottom >= 0 && p.y >= column.island_bottom && p.y <= column.island_top {
		block = .Stone
		if p.y == column.island_top {
			block = .Grass
		} else if p.y >= column.island_top - 3 {
			block = .Dirt
		}
	}
	if block == .Air {return terrain_tree_block(p, features.trees[:features.tree_count])}
	return block
}

Column_Cache_Entry :: struct {
	x, z:      i32,
	seed:      u64,
	generator: u32,
	column:    Terrain_Column,
	valid:     bool,
}
Feature_Cache_Entry :: struct {
	key:       I3,
	seed:      u64,
	generator: u32,
	features:  Terrain_Features,
	valid:     bool,
}

world_generated_cached :: proc(w: ^World, p: I3) -> Block {
	if p.y < 0 || p.y >= HEIGHT {return .Air}
	key := chunk_of(p)
	f := &w.feature_cache[hash3(w.seed, key.x, key.y, key.z) % u64(len(w.feature_cache))]
	if !f.valid || f.key != key || f.seed != w.seed || f.generator != w.generator {
		f^ = {key, w.seed, w.generator, terrain_features(w.seed, key, w.generator), true}
	}
	c := &w.column_cache[hash3(w.seed, p.x, 0, p.z) % u64(len(w.column_cache))]
	if !c.valid || c.x != p.x || c.z != p.z || c.seed != w.seed || c.generator != w.generator {
		c^ = {
			p.x,
			p.z,
			w.seed,
			w.generator,
			terrain_column(w.seed, p.x, p.z, w.generator, &f.features),
			true,
		}
	}
	if w.generator >= 5 {return terrain_block_v5(w.seed, p, c.column, &f.features)}
	if w.generator >= 4 {return terrain_block_v4(w.seed, p, c.column, &f.features)}
	return terrain_block_v3(w.seed, p, c.column, &f.features)
}

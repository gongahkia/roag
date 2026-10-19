package sandbox

// A light value is two independent four-bit channels: sky | (warm block << 4).
// Every scattered step costs at least one. A 16-voxel halo therefore contains
// every possible nonzero contributor to the mesher's one-voxel sampling halo.
LIGHT_HALO :: 16
LIGHT_SIDE :: CHUNK + 2 * LIGHT_HALO
LIGHT_CELLS :: LIGHT_SIDE * LIGHT_SIDE * LIGHT_SIDE
VERTEX_POSITION_MASK :: 63
VERTEX_LIGHT_SHIFT :: 6

light_absorption :: proc(b: Block) -> u8 {
	if b == .Air {return 0}
	if fluid_kind(b) == .Water {return 2}
	if b == .Ice ||
	   b == .Leaves ||
	   b == .Birch_Leaves ||
	   b == .Spruce_Leaves ||
	   b == .Acacia_Leaves ||
	   b == .Jungle_Leaves ||
	   b == .Cherry_Leaves {return 1}
	return 15
}

light_emission :: proc(b: Block) -> u8 {
	return b == .Lamp || fluid_kind(b) == .Lava ? 15 : 0
}

light_index :: proc(x, y, z: int) -> int {return x + LIGHT_SIDE * (y + LIGHT_SIDE * z)}

// Bounded ring with one queued entry per cell. Rebuilding the affected chunks
// from immutable sources handles both addition and removal without stale light.
light_flood :: proc(blocks, light: []u8, region: ^Light_Region = nil) {
	queue := make([]u32, LIGHT_CELLS); defer delete(queue)
	queued := make([]bool, LIGHT_CELLS); defer delete(queued)
	head, tail, count := 0, 0, 0
	for value, i in light {
		if (value & 15) > 1 || (value >> 4) > 1 {
			queue[tail] = u32(i); tail = (tail + 1) % LIGHT_CELLS; count += 1; queued[i] = true
		}
	}
	steps := [6]int {
		1,
		-1,
		LIGHT_SIDE,
		-LIGHT_SIDE,
		LIGHT_SIDE * LIGHT_SIDE,
		-LIGHT_SIDE * LIGHT_SIDE,
	}
	for count > 0 {
		i := int(queue[head]); head = (head + 1) % LIGHT_CELLS; count -= 1; queued[i] = false
		value := light[i]
		x, y, z := i % LIGHT_SIDE, (i / LIGHT_SIDE) % LIGHT_SIDE, i / (LIGHT_SIDE * LIGHT_SIDE)
		for n, dir in NORMALS {
			nx, ny, nz := x + int(n.x), y + int(n.y), z + int(n.z)
			if nx < 0 ||
			   nx >= LIGHT_SIDE ||
			   ny < 0 ||
			   ny >= LIGHT_SIDE ||
			   nz < 0 ||
			   nz >= LIGHT_SIDE {continue}
			j := i + steps[dir]
			if blocks[j] == 255 {blocks[j] = u8(light_region_block(region, nx, ny, nz))}
			cost := max(1, int(light_absorption(Block(blocks[j]))))
			sky := max(int(light[j] & 15), int(value & 15) - cost)
			warm := max(int(light[j] >> 4), int(value >> 4) - cost)
			next := u8(sky | (warm << 4))
			if next == light[j] {continue}
			light[j] = next
			if !queued[j] {
				assert(count < LIGHT_CELLS)
				queue[tail] = u32(j); tail = (tail + 1) % LIGHT_CELLS; count += 1; queued[j] = true
			}
		}
	}
}

// Used by lighting's column sampler; all-height features retain the same
// authoritative terrain rules and include roofs, trees, islands and lake shells.
light_terrain_block :: proc(
	seed: u64,
	p: I3,
	c: Terrain_Column,
	f: ^Terrain_Features,
	generator: u32,
) -> Block {
	if p.y < 0 {return .Bedrock}
	if p.y >= HEIGHT {return .Air}
	if generator >= 5 {return terrain_block_v5(seed, p, c, f)}
	if generator >= 4 {return terrain_block_v4(seed, p, c, f)}
	if generator == 3 {return terrain_block_v3(seed, p, c, f)}
	b := base_block(seed, p, c.height, generator)
	return b == .Air ? terrain_tree_block(p, f.trees[:f.tree_count]) : b
}

lighting_build :: proc(job: Mesh_Job, output: []u8) {
	blocks := make([]u8, LIGHT_CELLS); defer delete(blocks)
	light := make([]u8, LIGHT_CELLS); defer delete(light)
	for &b in blocks {b = 255}
	region := new(Light_Region); defer free(region)
	light_region_init(region, job)
	defer light_region_destroy(region)
	for z in 0 ..< LIGHT_SIDE {for x in 0 ..< LIGHT_SIDE {
			sky := 15
			// Air above the conservative natural bound needs no terrain evaluation.
			// Overrides are still consulted all the way to the world ceiling.
			for wy := i32(HEIGHT - 1); wy >= region.lo.y + LIGHT_SIDE; wy -= 1 {
				b := light_region_world_block(region, x, wy, z)
				sky = max(0, sky - int(light_absorption(b)))
				if sky == 0 {break}
			}
			for y := LIGHT_SIDE - 1; y >= 0 && sky > 0; y -= 1 {
				b := light_region_block(region, x, y, z)
				i := light_index(x, y, z); blocks[i] = u8(b)
				sky = max(0, sky - int(light_absorption(b)))
				light[i] = u8(sky) | (light_emission(b) << 4)
			}
		}}
	// All natural emitters in generators 2–5 are confined to y=1..12.
	// Edited sources may be anywhere. Other underground voxels are sampled only
	// when reached by the flood, avoiding a full 64^3 terrain regeneration.
	if job.generator >= 2 {
		for wy := max(i32(1), region.lo.y);
		    wy <= min(i32(12), region.lo.y + LIGHT_SIDE - 1);
		    wy += 1 {
			for z in 0 ..< LIGHT_SIDE {for x in 0 ..< LIGHT_SIDE {
					y := int(wy - region.lo.y); i := light_index(x, y, z)
					b := light_region_block(region, x, y, z)
					blocks[i] = u8(b); light[i] |= light_emission(b) << 4
				}}
		}
	}
	for e in job.edits {
		p := e.position - region.lo
		if light_emission(e.block) > 0 &&
		   p.x >= 0 &&
		   p.x < LIGHT_SIDE &&
		   p.y >= 0 &&
		   p.y < LIGHT_SIDE &&
		   p.z >= 0 &&
		   p.z < LIGHT_SIDE {
			i := light_index(int(p.x), int(p.y), int(p.z))
			blocks[i] = u8(e.block); light[i] |= light_emission(e.block) << 4
		}
	}
	light_flood(blocks, light, region)
	for z in 0 ..< PAD {for y in 0 ..< PAD {for x in 0 ..< PAD {
				output[block_index(x, y, z)] =
					light[light_index(x + LIGHT_HALO - 1, y + LIGHT_HALO - 1, z + LIGHT_HALO - 1)]
			}}}
}

face_light :: proc(data, light: []u8, p: I3, dir: int) -> [4]u8 {
	if len(light) == 0 {return {15, 15, 15, 15}}
	q := p + NORMALS[dir]
	u, v := U_AXIS[dir], V_AXIS[dir]
	result: [4]u8
	for c in 0 ..< 4 {
		a, b := q, q
		a[u] += c == 1 || c == 2 ? 1 : -1
		b[v] += c >= 2 ? 1 : -1
		corner := a; corner[v] = b[v]
		positions := [4]I3{q, a, b, corner}
		sky, warm, count := 0, 0, 0
		for at, i in positions {
			if i == 3 &&
			   light_absorption(Block(sample(data, a))) == 15 &&
			   light_absorption(Block(sample(data, b))) == 15 {continue}
			if light_absorption(Block(sample(data, at))) == 15 {continue}
			value := sample(light, at)
			sky += int(value & 15); warm += int(value >> 4); count += 1
		}
		// Emitters illuminate their own face even when adjacent to another solid.
		sky = (sky + count / 2) / max(count, 1)
		warm = max(int(light_emission(Block(sample(data, p)))), (warm + count / 2) / max(count, 1))
		result[c] = u8(sky) | (u8(warm) << 4)
	}
	return result
}

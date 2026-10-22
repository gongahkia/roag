package sandbox

import "core:time"

Mesh_Job :: struct {
	key:       I3,
	revision:  u64,
	seed:      u64,
	edits:     [dynamic]Edit_Command,
	naive:     bool,
	generator: u32,
	unlit:     bool,
	fast_light: bool,
}

Mesh_Result :: struct {
	key:                       I3,
	revision:                  u64,
	mesh:                      Mesh,
	generation_ms, meshing_ms: f64,
	lighting_ms:               f64,
}

build_job :: proc(job: Mesh_Job) -> Mesh_Result {
	start := time.tick_now()
	generator := job.generator == 0 ? u32(GENERATOR_VERSION) : job.generator
	data := make([]u8, PAD * PAD * PAD)
	defer delete(data)
	terrain_fill_chunk(job.seed, job.key, generator, data)
	for e in job.edits {
		p := e.position - job.key * 32 + I3{1, 1, 1}
		if p.x >= 0 && p.x < PAD && p.y >= 0 && p.y < PAD && p.z >= 0 && p.z < PAD {
			data[block_index(int(p.x), int(p.y), int(p.z))] = u8(e.block)
		}
	}
	generated := time.tick_now()
	light: []u8
	defer delete(light)
	exposure := mesh_exposure(data)
	lighting_started := time.tick_now()
	if !job.unlit && exposure.faces > 0 {
		light = make([]u8, PAD * PAD * PAD)
		lighting_job := job; lighting_job.generator = generator
		lighting_build(lighting_job, light)
	}
	lit := time.tick_now()
	mesh := mesh_build(data, !job.naive, light, &exposure)
	return {
		job.key,
		job.revision,
		mesh,
		time.duration_milliseconds(time.tick_diff(start, generated)),
		time.duration_milliseconds(time.tick_since(lit)) +
		time.duration_milliseconds(time.tick_diff(generated, lighting_started)),
		len(light) > 0 ? time.duration_milliseconds(time.tick_diff(lighting_started, lit)) : 0,
	}
}

// Shared padded sampling entry point used by workers and byte-for-byte tests.
terrain_fill_chunk :: proc(seed: u64, key: I3, generator: u32, data: []u8) {
	features := terrain_features(seed, key, generator)
	// Height/tree work is shared by each vertical column.
	for z in 0 ..< PAD {
		for x in 0 ..< PAD {
			wx, wz := key.x * 32 + i32(x) - 1, key.z * 32 + i32(z) - 1
			column: Terrain_Column
			if generator >=
			   3 {column = terrain_column(seed, wx, wz, generator, &features)} else {column.height = legacy_terrain_height(seed, wx, wz)}
			for y in 0 ..< PAD {
				wy := key.y * 32 + i32(y) - 1
				p := I3{wx, wy, wz}
				b: Block
				if generator >=
				   5 {b = terrain_block_v5(seed, p, column, &features)} else if generator >= 4 {b = terrain_block_v4(seed, p, column, &features)} else if generator == 3 {b = terrain_block_v3(seed, p, column, &features)} else {
					b = base_block(seed, p, column.height, generator)
					if b == .Air {b = terrain_tree_block(p, features.trees[:features.tree_count])}
				}
				data[block_index(x, y, z)] = u8(b)
			}
		}
	}
}

package sandbox

import "core:time"

Mesh_Job :: struct {
	key:       I3,
	revision:  u64,
	seed:      u64,
	edits:     [dynamic]Edit_Command,
	naive:     bool,
	generator: u32,
}

Mesh_Result :: struct {
	key:                       I3,
	revision:                  u64,
	mesh:                      Mesh,
	generation_ms, meshing_ms: f64,
}

build_job :: proc(job: Mesh_Job) -> Mesh_Result {
	start := time.tick_now()
	generator := job.generator == 0 ? u32(GENERATOR_VERSION) : job.generator
	data := make([]u8, PAD * PAD * PAD)
	defer delete(data)
	terrain_fill_chunk(job.seed, job.key, generator, data)
	for e in job.edits {
		p := e.position - job.key * 32 + I3{1, 1, 1}
		data[block_index(int(p.x), int(p.y), int(p.z))] = u8(e.block)
	}
	generated := time.tick_now()
	mesh := mesh_build(data, !job.naive)
	return {
		job.key,
		job.revision,
		mesh,
		time.duration_milliseconds(time.tick_diff(start, generated)),
		time.duration_milliseconds(time.tick_since(generated)),
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
			if generator >= 3 {column = terrain_column_v3(seed, wx, wz)}
			else {column.height = legacy_terrain_height(seed, wx, wz)}
			for y in 0 ..< PAD {
				wy := key.y * 32 + i32(y) - 1
				p := I3{wx, wy, wz}
				b: Block
				if generator >= 3 {b = terrain_block_v3(seed, p, column, &features)}
				else {
					b = base_block(seed, p, column.height, generator)
					if b == .Air {b = terrain_tree_block(p, features.trees[:features.tree_count])}
				}
				data[block_index(x, y, z)] = u8(b)
			}
		}
	}
}

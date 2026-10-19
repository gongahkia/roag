package sandbox

import "core:testing"

@(test)
test_deterministic_terrain_and_padding :: proc(t: ^testing.T) {
	for x in -36 ..< 36 {
		for z in -2 ..< 2 {
			a := terrain_height(42, i32(x), i32(z), 2)
			b := terrain_height(42, i32(x), i32(z), 2)
			testing.expect(t, a == b && a >= 38 && a < 128)
			testing.expect(t, generated_block(42, {i32(x), 0, i32(z)}) == .Bedrock)
			testing.expect(t, generated_block(42, {i32(x), 256, i32(z)}) == .Air)
		}
	}
	fixtures := [][3]i32 {
		{-512, 77, 44},
		{-33, -33, 66},
		{-1, 0, 71},
		{0, 0, 71},
		{31, 32, 65},
		{32, 31, 65},
		{1024, -999, 58},
	}
	for f in fixtures {
		testing.expect(t, terrain_height(42, f[0], f[1], 1) == f[2])
		testing.expect(t, terrain_height(42, f[0], f[1], 2) == f[2])
	}
	// Worker column generation must exactly match random-access world queries.
	w: World
	world_init(&w, 42)
	defer world_destroy(&w)
	job := Mesh_Job {
		key      = {-1, 2, 0},
		revision = 9,
		seed     = 42,
	}
	result := build_job(job)
	defer mesh_destroy(&result.mesh)
	// Check generated mesh area against the authoritative sampler, including halo.
	data := make([]u8, PAD * PAD * PAD)
	defer delete(data)
	for z in 0 ..< PAD {
		for y in 0 ..< PAD {
			for x in 0 ..< PAD {
				p := job.key * 32 + I3{i32(x) - 1, i32(y) - 1, i32(z) - 1}
				data[block_index(x, y, z)] = u8(world_get(&w, p))
			}
		}
	}
	reference := mesh_build(data)
	defer mesh_destroy(&reference)
	testing.expect(t, result.mesh.faces == reference.faces)
	testing.expect(t, len(result.mesh.vertices) == len(reference.vertices))
	for v, i in reference.vertices {
		if i < len(result.mesh.vertices) {
			testing.expect(t, v == result.mesh.vertices[i])
		}
	}
}

@(test)
test_ore_depths_natural_fluids_and_worker_agreement :: proc(t: ^testing.T) {
	counts: [6]int
	for z in -24 ..< 24 {for y in 1 ..< 100 {for x in -24 ..< 24 {
				p := I3{i32(x), i32(y), i32(z)}
				b := ore_block(42, p)
				if b >= .Coal_Ore && b <= .Redstone_Ore {counts[int(b) - int(Block.Coal_Ore)] += 1}
				if b == .Diamond_Ore {testing.expect(t, p.y <= 24)}
				if b == .Gold_Ore {testing.expect(t, p.y <= 40)}
			}}}
	for count in counts {testing.expect(t, count > 0)}
	water_found, lava_found := false, false
	for z in -128 ..< 128 {for x in -128 ..< 128 {
			h := terrain_height(42, i32(x), i32(z), 3)
			if h <
			   SEA_LEVEL {water_found = water_found || generated_block(42, {i32(x), SEA_LEVEL, i32(z)}, 3) == .Water}
			lava_found = lava_found || generated_block(42, {i32(x), 8, i32(z)}, 3) == .Lava
		}}
	testing.expect(t, water_found && lava_found)
	for version: u32 = 1; version <= 2; version += 1 {
		w: World; world_init(&w, 42); w.generator = version
		job := Mesh_Job {
			key       = {-1, 0, 0},
			seed      = 42,
			generator = version,
		}
		result := build_job(job)
		data := make([]u8, PAD * PAD * PAD)
		for z in 0 ..< PAD {for y in 0 ..< PAD {for x in 0 ..< PAD {
					data[block_index(x, y, z)] = u8(
						world_get(&w, job.key * 32 + I3{i32(x - 1), i32(y - 1), i32(z - 1)}),
					)
				}}}
		expected := mesh_build(data)
		testing.expect(
			t,
			len(result.mesh.vertices) == len(expected.vertices) &&
			result.mesh.water_indices == expected.water_indices,
		)
		for v, i in expected.vertices {if i < len(result.mesh.vertices) {testing.expect(t, v == result.mesh.vertices[i])}}
		mesh_destroy(&result.mesh); mesh_destroy(&expected); delete(data); world_destroy(&w)
	}
}

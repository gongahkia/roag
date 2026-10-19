package sandbox

import "core:mem"
import "core:testing"

@(test)
test_worker_scratch_does_not_consume_world_edit_allowance :: proc(t: ^testing.T) {
	world_tracker: mem.Tracking_Allocator
	mem.tracking_allocator_init(&world_tracker, context.allocator)
	defer mem.tracking_allocator_destroy(&world_tracker)
	w: World
	world_init(&w, 42, mem.tracking_allocator(&world_tracker))
	defer world_destroy(&w)
	w.tracker = &world_tracker
	w.allocation_limit = 8 * MiB
	worker_scratch := make([]u8, 5 * MiB); defer delete(worker_scratch)
	testing.expect(t, world_apply(&w, {1, {0, 90, 0}, .Lamp}))
	testing.expect(t, world_tracker.current_memory_allocated < MiB)
	w.allocation_limit = WORLD_EDIT_HEADROOM_BYTES
	testing.expect(t, !world_apply(&w, {2, {1, 90, 0}, .Lamp}), "Actual edit exhaustion must still refuse allocations")
}

@(test)
test_lazy_lighting_matches_exhaustive_terrain_reference :: proc(t: ^testing.T) {
	// Reference evaluates every voxel and every sky cell, with no empty-sky
	// bounds and no lazy sampling. It catches missed emitters and bad bounds.
	for key, fixture in ([3]I3{{-1, 0, 0}, {0, 1, 0}, {25, 5, 5}}) {
		version := fixture == 0 ? u32(2) : u32(5)
		region := new(Light_Region)
		job := Mesh_Job {
			key       = key,
			seed      = 42,
			generator = version,
		}
		light_region_init(region, job)
		blocks := make([]u8, LIGHT_CELLS)
		light := make([]u8, LIGHT_CELLS)
		output := make([]u8, PAD * PAD * PAD)
		for z in 0 ..< LIGHT_SIDE {for x in 0 ..< LIGHT_SIDE {
				c := region.columns[x + LIGHT_SIDE * z]
				f := &region.features[(x + LIGHT_HALO) / CHUNK + 3 * ((z + LIGHT_HALO) / CHUNK)]
				sky := 15
				for wy := i32(HEIGHT - 1); wy >= region.lo.y + LIGHT_SIDE; wy -= 1 {
					b := light_terrain_block(
						job.seed,
						{region.lo.x + i32(x), wy, region.lo.z + i32(z)},
						c,
						f,
						version,
					)
					sky = max(0, sky - int(light_absorption(b)))
				}
				for y := LIGHT_SIDE - 1; y >= 0; y -= 1 {
					p := region.lo + I3{i32(x), i32(y), i32(z)}
					b := light_terrain_block(job.seed, p, c, f, version)
					i := light_index(x, y, z)
					blocks[i] = u8(b)
					sky = max(0, sky - int(light_absorption(b)))
					light[i] = u8(sky) | (light_emission(b) << 4)
					if x % 13 == 0 && y % 17 == 0 && z % 11 == 0 && p.y >= 0 {
						testing.expect(
							t,
							b == generated_block(job.seed, p, version),
							"All-height descriptors must match authoritative terrain",
						)
					}
				}
			}}
		light_flood(blocks, light)
		lighting_build(job, output)
		for z in 0 ..< PAD {for y in 0 ..< PAD {for x in 0 ..< PAD {
					testing.expect(
						t,
						output[block_index(x, y, z)] ==
						light[light_index(x + LIGHT_HALO - 1, y + LIGHT_HALO - 1, z + LIGHT_HALO - 1)],
					)
				}}}
		light_region_destroy(region); free(region)
		delete(blocks); delete(light); delete(output)
	}
}

@(test)
test_light_propagation_occlusion_and_transmission :: proc(t: ^testing.T) {
	blocks := make([]u8, LIGHT_CELLS); defer delete(blocks)
	light := make([]u8, LIGHT_CELLS); defer delete(light)
	// A sealed one-cell-wide tunnel makes distance/absorption unambiguous.
	for &b in blocks {b = u8(Block.Stone)}
	for x in 5 ..< 45 {blocks[light_index(x, 20, 20)] = u8(Block.Air)}
	source := light_index(20, 20, 20)
	blocks[source] = u8(Block.Lamp); light[source] = 15 << 4
	light_flood(blocks, light)
	testing.expect(t, light[light_index(21, 20, 20)] >> 4 == 14)
	testing.expect(t, light[light_index(34, 20, 20)] >> 4 == 1)
	testing.expect(t, light[light_index(35, 20, 20)] == 0)
	testing.expect(t, light[light_index(20, 21, 20)] == 0)
	for &v in light {v = 0}
	blocks[light_index(21, 20, 20)] = u8(Block.Water)
	blocks[light_index(22, 20, 20)] = u8(Block.Ice)
	blocks[light_index(23, 20, 20)] = u8(Block.Birch_Leaves)
	light[source] = 15 // Independently propagate the sky channel.
	light_flood(blocks, light)
	testing.expect(t, light[light_index(21, 20, 20)] == 13)
	testing.expect(t, light[light_index(22, 20, 20)] == 12)
	testing.expect(t, light[light_index(23, 20, 20)] == 11)
	testing.expect(t, light[light_index(24, 20, 20)] == 10)
	testing.expect(t, light_emission(.Lava_3) == 15 && light_emission(.Water) == 0)
}

@(test)
test_lighting_roofs_emitter_removal_and_chunk_seams :: proc(t: ^testing.T) {
	w: World; world_init(&w, 42); defer world_destroy(&w)
	// All-height ceiling, across the positive and negative chunk boundaries.
	for z: i32 = -16; z <= 47; z += 1 {for x: i32 = -48; x <= 79; x += 1 {
			world_apply(&w, {w.sequence + 1, {x, 255, z}, .Stone})
		}}
	p := I3{31, 220, 10}
	world_apply(&w, {w.sequence + 1, p, .Lamp})
	a := make([]u8, PAD * PAD * PAD); defer delete(a)
	b := make([]u8, PAD * PAD * PAD); defer delete(b)
	for kx: i32 = -1; kx <= 0; kx += 1 {
		ka, kb := I3{kx, 6, 0}, I3{kx + 1, 6, 0}
		ea, eb := world_snapshot(&w, ka, true), world_snapshot(&w, kb, true)
		lighting_build({key = ka, seed = w.seed, generator = w.generator, edits = ea}, a)
		lighting_build({key = kb, seed = w.seed, generator = w.generator, edits = eb}, b)
		for z in 1 ..= CHUNK {for y in 1 ..= CHUNK {
				testing.expect(t, a[block_index(32, y, z)] == b[block_index(0, y, z)])
				testing.expect(t, a[block_index(33, y, z)] == b[block_index(1, y, z)])
			}}
		delete(ea); delete(eb)
	}
	testing.expect(
		t,
		sample(a, {5, 25, 5}) & 15 == 0,
		"High unloaded ceiling must shadow this chunk",
	)
	testing.expect(t, sample(b, {0, 28, 10}) >> 4 == 14, "Lamp must light the adjacent chunk")
	world_apply(&w, {w.sequence + 1, p, .Air})
	e := world_snapshot(&w, {1, 6, 0}, true)
	lighting_build({key = {1, 6, 0}, seed = w.seed, generator = w.generator, edits = e}, b)
	delete(e)
	testing.expect(t, sample(b, {0, 28, 10}) == 0, "Removing emitter must remove all its light")
	// Remove the ceiling directly over the sample: full-strength sunlight travels
	// down a shaft much longer than the light halo and across a vertical boundary.
	world_apply(&w, {w.sequence + 1, {32, 255, 10}, .Air})
	e = world_snapshot(&w, {1, 6, 0}, true)
	lighting_build({key = {1, 6, 0}, seed = w.seed, generator = w.generator, edits = e}, b)
	delete(e)
	testing.expect(t, sample(b, {0, 28, 10}) & 15 == 15)
}

@(test)
test_lighting_invalidation_and_snapshot_reach :: proc(t: ^testing.T) {
	s: Stream; s.chunks = make(map[I3]^Chunk_State); defer delete(s.chunks)
	for k in ([5]I3{{0, 0, 0}, {1, 6, 0}, {0, 7, 0}, {2, 0, 0}, {-1, 0, 0}}) {
		c := new(Chunk_State); c.complete = true; s.chunks[k] = c
	}
	defer for _, c in s.chunks {free(c)}
	stream_invalidate(&s, {31, 200, 10})
	testing.expect(t, !s.chunks[{0, 0, 0}].complete && !s.chunks[{1, 6, 0}].complete)
	testing.expect(
		t,
		s.chunks[{0, 7, 0}].complete &&
		s.chunks[{2, 0, 0}].complete &&
		s.chunks[{-1, 0, 0}].complete,
	)
	w: World; world_init(&w, 42); defer world_destroy(&w)
	for p in ([5]I3{{-16, 255, 0}, {47, 50, 47}, {-17, 255, 0}, {48, 90, 0}, {0, 10, 0}}) {world_apply(&w, {w.sequence + 1, p, .Stone})}
	e := world_snapshot(&w, {0, 2, 0}, true); defer delete(e)
	testing.expect(t, len(e) == 2, "Snapshot needs distant roofs and 16-cell light halo only")
}

@(test)
test_lit_vertex_packing_greedy_coverage_and_heap_bound :: proc(t: ^testing.T) {
	tracker: mem.Tracking_Allocator; mem.tracking_allocator_init(&tracker, context.allocator)
	defer mem.tracking_allocator_destroy(&tracker)
	context.allocator = mem.tracking_allocator(&tracker)
	data := make([]u8, PAD * PAD * PAD); defer delete(data)
	light := make([]u8, PAD * PAD * PAD); defer delete(light)
	for &v in light {v = 0xa7}
	for z in 1 ..= CHUNK {for x in 1 ..= CHUNK {data[block_index(x, 5, z)] = u8(Block.Stone)}}
	m := mesh_build(data, true, light)
	testing.expect(t, size_of(Vertex) == 8 && mesh_area(m) == reference_faces(data))
	check_winding(t, m); check_coverage(t, data, m)
	for v in m.vertices {
		testing.expect(t, (v.x >> VERTEX_LIGHT_SHIFT) == 0xa7)
		testing.expect(t, (v.x & VERTEX_POSITION_MASK) <= CHUNK)
	}
	mesh_destroy(&m)
	// Maximum exposed geometry plus an edited lighting neighborhood fits the
	// existing 16 MiB worker reservation; scratch must be released before meshing.
	edits := make([dynamic]Edit_Command); defer delete(edits)
	for z: i32 = -LIGHT_HALO;
	    z < CHUNK + LIGHT_HALO;
	    z += 1 {for y: i32 = -LIGHT_HALO; y < CHUNK + LIGHT_HALO; y += 1 {for x: i32 = -LIGHT_HALO; x < CHUNK + LIGHT_HALO; x += 1 {
				append(
					&edits,
					Edit_Command {
						0,
						{x, y + 192, z},
						(x + y + z) % 2 == 0 ? Block.Stone : Block.Air,
					},
				)
			}}}
	testing.expect(t, len(edits) == MAX_EDITS)
	r := build_job({key = {0, 6, 0}, seed = 42, generator = 5, edits = edits})
	testing.expect(t, !r.mesh.overflow && tracker.peak_memory_allocated <= WORKER_MEMORY_BYTES)
	mesh_destroy(&r.mesh)
}

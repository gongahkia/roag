package sandbox

import "core:fmt"
import "core:os"
import "core:testing"

test_fluid_setup :: proc(w: ^World, s: ^Fluid_Sim) {
	world_init(w, 42); fluids_init(s)
	s.center = {0, 0, 0}
	for z in -10 ..= 10 {
		for x in -10 ..= 10 {world_apply(w, {w.sequence + 1, {i32(x), 180, i32(z)}, .Stone})}
	}
}
test_fluid_ticks :: proc(s: ^Fluid_Sim, w: ^World, first, count: int) {
	for i in first ..< first + count {
		// A fixed work budget, independent of the machine running the test.
		for _ in 0 ..< 16 {fluids_step(s, w, u64(i), timed = false)}
	}
}

@(test)
test_water_gravity_spread_and_source_removal :: proc(t: ^testing.T) {
	w: World; s: Fluid_Sim
	test_fluid_setup(&w, &s)
	defer world_destroy(&w); defer fluids_destroy(&s)
	p := I3{0, 184, 0}
	world_apply(&w, {w.sequence + 1, p, .Water}); fluids_notify(&s, &w, p)
	test_fluid_ticks(&s, &w, 1, 3)
	testing.expect(t, world_get(&w, {0, 183, 0}) == .Water_Falling)
	testing.expect(t, world_get(&w, {1, 184, 0}) == .Air) // No sideways fan in midair.
	test_fluid_ticks(&s, &w, 4, 18)
	testing.expect(t, fluid_kind(world_get(&w, {4, 181, 0})) == .Water)
	testing.expect(t, world_get(&w, {8, 181, 0}) == .Air)
	world_apply(&w, {w.sequence + 1, p, .Air}); fluids_notify(&s, &w, p, .Water)
	test_fluid_ticks(&s, &w, 22, 120)
	for z in -8 ..= 8 {for x in -8 ..= 8 {
			testing.expect(t, !is_fluid(world_get(&w, {i32(x), 181, i32(z)})))
		}}
}

@(test)
test_lava_is_slower_and_water_reacts_to_sources_and_flow :: proc(t: ^testing.T) {
	w: World; s: Fluid_Sim
	test_fluid_setup(&w, &s)
	defer world_destroy(&w); defer fluids_destroy(&s)
	p := I3{0, 181, 0}
	world_apply(&w, {w.sequence + 1, p, .Lava}); fluids_notify(&s, &w, p)
	test_fluid_ticks(&s, &w, 1, 3)
	testing.expect(t, world_get(&w, {1, 181, 0}) == .Air)
	test_fluid_ticks(&s, &w, 4, 20)
	testing.expect(t, world_get(&w, {1, 181, 0}) == .Lava_1)
	testing.expect(t, world_get(&w, {4, 181, 0}) == .Air)
	world_apply(&w, {w.sequence + 1, {-1, 181, 0}, .Water})
	testing.expect(t, fluid_next(&w, p) == .Obsidian)
	world_apply(&w, {w.sequence + 1, {2, 181, 0}, .Water})
	testing.expect(t, fluid_next(&w, {1, 181, 0}) == .Cobblestone)
}

@(test)
test_water_sources_regenerate_and_obstructions_redirect_flow :: proc(t: ^testing.T) {
	w: World; s: Fluid_Sim
	test_fluid_setup(&w, &s)
	defer world_destroy(&w); defer fluids_destroy(&s)
	world_apply(&w, {w.sequence + 1, {-1, 181, 0}, .Water})
	world_apply(&w, {w.sequence + 1, {1, 181, 0}, .Water})
	testing.expect(t, fluid_next(&w, {0, 181, 0}) == .Water)
	world_apply(&w, {w.sequence + 1, {0, 181, 0}, .Stone})
	testing.expect(t, fluid_next(&w, {0, 181, 0}) == .Stone)
	world_apply(&w, {w.sequence + 1, {0, 180, 0}, .Air})
	world_apply(&w, {w.sequence + 1, {0, 181, 0}, .Air})
	testing.expect(t, fluid_next(&w, {0, 181, 0}) != .Water)
}

@(test)
test_fluid_queue_budget_and_chunk_border :: proc(t: ^testing.T) {
	w: World; world_init(&w, 7); defer world_destroy(&w)
	s: Fluid_Sim; fluids_init(&s); defer fluids_destroy(&s)
	s.center = {0, 0, 0}
	for z in 0 ..< 90 {for x in 0 ..< 90 {fluid_schedule(&s, {i32(x - 32), 190, i32(z - 32)})}}
	testing.expect(t, s.count <= FLUID_QUEUE_CAPACITY)
	fluids_step(&s, &w, 1, 7, false)
	testing.expect(t, s.updates <= 7 && s.changed_count <= 7)
	world_apply(&w, {w.sequence + 1, {31, 190, 0}, .Water})
	world_apply(&w, {w.sequence + 1, {31, 189, 0}, .Stone})
	testing.expect(t, fluid_next(&w, {32, 190, 0}) == .Water_1)
	s.center = {20, 0, 20}
	fluids_step(&s, &w, 2, 7, false)
	testing.expect(t, s.updates == 0)
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
			h := terrain_height(42, i32(x), i32(z))
			if h <
			   SEA_LEVEL {water_found = water_found || generated_block(42, {i32(x), SEA_LEVEL, i32(z)}) == .Water}
			lava_found = lava_found || generated_block(42, {i32(x), 8, i32(z)}) == .Lava
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

@(test)
test_fluid_mesh_levels_transparency_and_solid_boundaries :: proc(t: ^testing.T) {
	data := make([]u8, PAD * PAD * PAD); defer delete(data)
	data[block_index(2, 2, 2)] = u8(Block.Water_3)
	data[block_index(3, 2, 2)] = u8(Block.Diamond_Ore)
	m := mesh_build(data); defer mesh_destroy(&m)
	testing.expect(t, m.water_indices == 5 * 6)
	testing.expect(t, len(m.indices) - m.water_indices == 6 * 6)
	for v in m.vertices {
		material := Block((v.data >> 3) & 31)
		if material == .Water &&
		   v.data & (1 << 14) != 0 {testing.expect(t, ((v.data >> 10) & 15) == 4)}
	}
	data[block_index(2, 2, 2)] = u8(Block.Lava)
	lava := mesh_build(data); defer mesh_destroy(&lava)
	testing.expect(t, lava.water_indices == 0 && len(lava.indices) == 11 * 6)
}

@(test)
test_save_generator_version_legacy_and_fluid_levels :: proc(t: ^testing.T) {
	dir, err := os.make_directory_temp("", "voxel-fluid-test-*", context.allocator)
	testing.expect(t, err == nil); if err != nil {return}
	defer delete(dir); defer os.remove(dir)
	path := fmt.tprintf("%s/world.vox", dir); defer os.remove(path)
	w: World; world_init(&w, 42); defer world_destroy(&w)
	world_apply(&w, {1, {0, 180, 0}, .Water_5})
	world_apply(&w, {2, {0, 181, 0}, .Diamond_Ore})
	testing.expect(t, world_save(&w, {0, 190, 0}, path))
	loaded: World; world_init(&loaded, 7); defer world_destroy(&loaded)
	pos: V3
	testing.expect(t, world_load(&loaded, &pos, path))
	testing.expect(t, loaded.generator == 2 && world_get(&loaded, {0, 180, 0}) == .Water_5)
	// A minimal genuine VOX1 file must keep its old terrain algorithm on re-save.
	legacy := make([dynamic]u8); defer delete(legacy)
	append(&legacy, ..([]u8{'V', 'O', 'X', '1'})); put64(&legacy, 42); put64(&legacy, 0)
	legacy_pos := V3{0, 190, 0}
	for v in legacy_pos {put64(&legacy, transmute(u64)v)}
	put32(&legacy, 0)
	testing.expect(t, os.write_entire_file(path, legacy[:]) == nil)
	testing.expect(t, world_load(&loaded, &pos, path) && loaded.generator == 1)
	testing.expect(t, world_save(&loaded, pos, path))
	testing.expect(t, world_load(&loaded, &pos, path) && loaded.generator == 1)
}

package sandbox

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
	world_apply(&w, {w.sequence + 1, {3, 181, 0}, .Water_7})
	world_apply(&w, {w.sequence + 1, {4, 181, 0}, .Lava_1})
	testing.expect(t, fluid_next(&w, {3, 181, 0}) == .Water_7)
	// Opening a route next to lava also respects its four-tick delay.
	q := I3{0, 180, 0}
	world_apply(&w, {w.sequence + 1, q, .Air}); fluids_notify(&s, &w, q, .Stone)
	queued_delay: u64
	for i in 0 ..< s.count {
		e := s.queue[(s.head + i) % FLUID_QUEUE_CAPACITY]
		if e.position == q {queued_delay = e.due - s.tick}
	}
	testing.expect(t, queued_delay == 4)
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
	for z in 0 ..< 100 {for x in 0 ..< 100 {fluid_schedule(&s, {i32(x - 32), 190, i32(z - 32)})}}
	testing.expect(t, s.count == FLUID_QUEUE_CAPACITY && s.overflow > 0)
	fluids_step(&s, &w, 1, 7, false)
	testing.expect(t, s.updates <= 7 && s.changed_count <= 7)
	world_apply(&w, {w.sequence + 1, {31, 190, 0}, .Water})
	world_apply(&w, {w.sequence + 1, {31, 189, 0}, .Stone})
	testing.expect(t, fluid_next(&w, {32, 190, 0}) == .Water_1)
	s.center = {20, 0, 20}
	fluids_step(&s, &w, 2, 7, false)
	testing.expect(t, s.updates == 0)
}

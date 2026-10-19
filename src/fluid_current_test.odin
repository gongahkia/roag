package sandbox

import "core:testing"

test_current_channel :: proc(w: ^World, start: i32 = 0, deep: bool = false) {
	world_init(w, 42); w.generator = 2
	for x := start - 12; x <= start + 12; x += 1 {
		for z: i32 = -2; z <= 2; z += 1 {world_apply(w, {w.sequence + 1, {x, 180, z}, .Stone})}
		for y: i32 = 181; y <= 184; y += 1 {
			for z in ([2]i32{-1, 1}) {world_apply(w, {w.sequence + 1, {x, y, z}, .Stone})}
		}
	}
	for y: i32 = 181; y <= (deep ? 184 : 181); y += 1 {
		world_apply(w, {w.sequence + 1, {start - 1, y, 0}, .Water})
		for i: i32 = 0; i < 7; i += 1 {
			world_apply(w, {w.sequence + 1, {start + i, y, 0}, Block(int(Block.Water_1) + int(i))})
		}
	}
}

@(test)
test_water_currents_follow_levels_in_all_directions_and_across_chunks :: proc(t: ^testing.T) {
	for origin in ([3]i32{-1, 0, 31}) {
		for direction in ([4]I3{{1, 0, 0}, {-1, 0, 0}, {0, 0, 1}, {0, 0, -1}}) {
			w: World; world_init(&w, 42); w.generator = 2; defer world_destroy(&w)
			p := I3{origin, 181, origin}
			world_apply(&w, {w.sequence + 1, p, .Water_3})
			world_apply(&w, {w.sequence + 1, p - direction, .Water_2})
			world_apply(&w, {w.sequence + 1, p + direction, .Water_4})
			flow := water_current(&w, p)
			testing.expect(t, flow == V3{f64(direction.x), 0, f64(direction.z)} * WATER_CURRENT_SPEED)
			world_apply(&w, {w.sequence + 1, p - direction, .Air})
			world_apply(&w, {w.sequence + 1, p + direction, .Air})
			testing.expect(t, water_current(&w, p) == V3{}, "Drained neighbors must stop the current")
		}
	}
}

@(test)
test_water_current_reaches_thin_flows_and_drops_without_entering_solids :: proc(t: ^testing.T) {
	w: World; world_init(&w, 42); w.generator = 2; defer world_destroy(&w)
	p := I3{0, 181, 0}
	world_apply(&w, {w.sequence + 1, p, .Water_7})
	world_apply(&w, {w.sequence + 1, p - I3{1, 0, 0}, .Water_6})
	testing.expect(t, water_current(&w, p).x == WATER_CURRENT_SPEED)
	world_apply(&w, {w.sequence + 1, p - I3{1, 0, 0}, .Stone})
	world_apply(&w, {w.sequence + 1, p + I3{1, -1, 0}, .Water_Falling})
	testing.expect(t, water_current(&w, p).x == WATER_CURRENT_SPEED, "Open drops must pull toward falling water")
	world_apply(&w, {w.sequence + 1, p + I3{1, 0, 0}, .Stone})
	testing.expect(t, water_current(&w, p) == V3{})
	world_apply(&w, {w.sequence + 1, p, .Water_Falling})
	testing.expect(t, water_current(&w, p).y == -WATER_FALL_SPEED)
	world_apply(&w, {w.sequence + 1, p, .Lava_Falling})
	testing.expect(t, water_current(&w, p) == V3{}, "Water current forces do not apply to lava")
	world_apply(&w, {w.sequence + 1, p, .Water_7})
	world_apply(&w, {w.sequence + 1, p + I3{1, 0, 0}, .Water_Falling})
	testing.expect(t, water_current(&w, p).x == WATER_CURRENT_SPEED, "Thin water must flow into an adjacent waterfall")
	world_apply(&w, {w.sequence + 1, p, .Water_Falling})
	testing.expect(t, water_current(&w, p) == V3{0, -WATER_FALL_SPEED, 0}, "A wide falling sheet must not pull sideways into itself")
}

@(test)
test_still_water_has_no_current_and_shallow_contact_respects_surface :: proc(t: ^testing.T) {
	w: World; world_init(&w, 42); w.generator = 2; defer world_destroy(&w)
	for z: i32 = -2; z <= 2; z += 1 {for x: i32 = -2; x <= 2; x += 1 {
			world_apply(&w, {w.sequence + 1, {x, 181, z}, .Water})
		}}
	testing.expect(t, water_current(&w, {0, 181, 0}) == V3{})
	for z: i32 = -2; z <= 2; z += 1 {for x: i32 = -2; x <= 2; x += 1 {
			world_apply(&w, {w.sequence + 1, {x, 181, z}, .Air})
		}}
	world_apply(&w, {w.sequence + 1, {0, 181, 0}, .Water_7})
	world_apply(&w, {w.sequence + 1, {-1, 181, 0}, .Water_6})
	eye := test_player_eye()
	contact := player_water_contact(&w, eye)
	testing.expect(t, abs(contact.immersion - 0.125 / PLAYER_HEIGHT) < 1e-9)
	testing.expect(t, contact.current.x == WATER_CURRENT_SPEED && player_fluid(&w, eye) == .Air)
	eye.y += 0.125
	testing.expect(t, player_water_contact(&w, eye).immersion == 0, "No force above the actual shallow surface")
}

@(test)
test_generated_rivers_carry_downstream_but_lakes_and_oceans_stay_calm :: proc(t: ^testing.T) {
	w: World; world_init(&w, 42); defer world_destroy(&w)
	for lake in ([2]bool{false, true}) {
		p, ok := terrain_find_river(42, lake)
		testing.expect(t, ok); if !ok {continue}
		p.y -= 1 // Liquid below the surface, including frozen river surfaces.
		testing.expect(t, world_get(&w, p) == .Water)
		flow := water_current(&w, p)
		if lake {testing.expect(t, flow == V3{})} else {
			f := world_features_cached(&w, p)
			c := world_column_cached(&w, p, f)
			r, _, _, found := river_nearest_reach(c^, p.x, p.z, &f.rivers)
			testing.expect(t, found && !r.lake)
			down := r.b - r.a
			testing.expect(t, flow.x * f64(down.x) + flow.z * f64(down.z) > 0)
			testing.expect(t, abs(flow.x * flow.x + flow.z * flow.z - WATER_CURRENT_SPEED * WATER_CURRENT_SPEED) < 1e-9)
			p.y += 12
			world_apply(&w, {w.sequence + 1, p, .Water})
			testing.expect(t, water_current(&w, p) == V3{}, "Placed water above a river must not inherit its current")
		}
	}
	testing.expect(t, water_current(&w, {2, 45, 15}) == V3{}, "Ocean water stays calm")
}

@(test)
test_player_is_carried_by_simulated_water_and_stops_when_it_drains :: proc(t: ^testing.T) {
	w: World; sim: Fluid_Sim
	test_fluid_setup(&w, &sim); defer world_destroy(&w); defer fluids_destroy(&sim)
	source := I3{0, 184, 0}
	world_apply(&w, {w.sequence + 1, source, .Water}); fluids_notify(&sim, &w, source)
	test_fluid_ticks(&sim, &w, 1, 24)
	p: Player; eye := test_player_eye(); eye.x = 1.5
	for _ in 0 ..< 120 {player_update(&p, &w, &eye, {speed = 14}, 1.0 / 60)}
	testing.expect(t, eye.x > 3 && !player_collides(&w, eye), "Flow must carry an idle walking player")
	world_apply(&w, {w.sequence + 1, source, .Air}); fluids_notify(&sim, &w, source, .Water)
	test_fluid_ticks(&sim, &w, 25, 120)
	stopped := eye
	for _ in 0 ..< 60 {player_update(&p, &w, &eye, {speed = 14}, 1.0 / 60)}
	testing.expect(t, abs(eye.x - stopped.x) < 1e-9 && p.velocity.x == 0)
}

@(test)
test_player_can_swim_against_current_and_current_cannot_push_through_walls :: proc(t: ^testing.T) {
	w: World; test_current_channel(&w, deep = true); defer world_destroy(&w)
	p: Player; eye := test_player_eye(); eye.x = 2.5
	for _ in 0 ..< 60 {player_update(&p, &w, &eye, {move = {-1, 0, 0}, speed = 14}, 1.0 / 60)}
	testing.expect(t, eye.x < 2.2 && !player_collides(&w, eye), "Swimming must overcome the current")
	for y: i32 = 181; y <= 184; y += 1 {world_apply(&w, {w.sequence + 1, {3, y, 0}, .Stone})}
	p = {}; eye = test_player_eye(); eye.x = 2.5
	for _ in 0 ..< 120 {player_update(&p, &w, &eye, {speed = 14}, 1.0 / 60)}
	testing.expect(t, abs(eye.x - (3 - PLAYER_RADIUS)) < 1e-6 && !player_collides(&w, eye))
}

@(test)
test_waterfalls_pull_down_but_allow_swimming_and_leave_spectators_free :: proc(t: ^testing.T) {
	w: World; test_player_world(&w); defer world_destroy(&w)
	for y: i32 = 181; y <= 190; y += 1 {world_apply(&w, {w.sequence + 1, {0, y, 0}, .Water_Falling})}
	p: Player; eye := test_player_eye(); eye.y += 3
	start := eye
	for _ in 0 ..< 12 {player_update(&p, &w, &eye, {speed = 14}, 1.0 / 60)}
	testing.expect(t, eye.y < start.y - 0.15 && p.velocity.y < -1.5)
	eye = start; p = {}
	player_update(&p, &w, &eye, {speed = 14, ascend = true}, 0.1)
	testing.expect(t, eye.y > start.y + 0.1 && p.velocity.y > 0, "Space must still let the player climb a waterfall")
	p.spectator = true; eye = start
	player_update(&p, &w, &eye, {speed = 14}, 0.1)
	testing.expect(t, eye == start && p.velocity == V3{})
}

@(test)
test_current_motion_matches_frame_rates_at_negative_chunk_boundary :: proc(t: ^testing.T) {
	w: World; test_current_channel(&w, -2); defer world_destroy(&w)
	slow, fast: Player
	a, b := test_player_eye(), test_player_eye(); a.x = -1.5; b = a
	for _ in 0 ..< 60 {player_update(&slow, &w, &a, {speed = 14}, 1.0 / 30)}
	for _ in 0 ..< 480 {player_update(&fast, &w, &b, {speed = 14}, 1.0 / 240)}
	testing.expect(t, a.x > 0.5 && abs(a.x - b.x) < 1e-9 && abs(a.y - b.y) < 1e-9)
}

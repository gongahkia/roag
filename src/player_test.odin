package sandbox

import "core:math"
import "core:testing"

test_player_world :: proc(w: ^World) {
	world_init(w, 42)
	for z in -16 ..= 16 {for x in -16 ..= 16 {
			world_apply(w, {w.sequence + 1, {i32(x), 180, i32(z)}, .Stone})
		}}
}
test_player_eye :: proc() -> V3 {return {0.5, 181 + PLAYER_EYE, 0.5}}

@(test)
test_player_gravity_ground_contact_and_no_drift :: proc(t: ^testing.T) {
	w: World; test_player_world(&w); defer world_destroy(&w)
	p: Player; eye := test_player_eye(); eye.y += 4
	for _ in 0 ..< 120 {player_update(&p, &w, &eye, {speed = 14}, 1.0 / 60)}
	testing.expect(t, abs(eye.y - (181 + PLAYER_EYE)) < 1e-6)
	testing.expect(t, p.grounded && p.velocity.y == 0 && !player_collides(&w, eye))
	for _ in 0 ..< 120 {player_update(&p, &w, &eye, {speed = 14}, 1.0 / 60)}
	testing.expect(t, abs(eye.y - (181 + PLAYER_EYE)) < 1e-6)
}

@(test)
test_player_sweeps_do_not_tunnel_across_chunk_boundaries :: proc(t: ^testing.T) {
	w: World; world_init(&w, 42); defer world_destroy(&w)
	for z in -1 ..= 1 {for y in 180 ..= 184 {
			world_apply(&w, {w.sequence + 1, {32, i32(y), i32(z)}, .Diamond_Ore})
			world_apply(&w, {w.sequence + 1, {-33, i32(y), i32(z)}, .Leaves})
		}}
	eye := test_player_eye()
	testing.expect(t, player_sweep_axis(&w, &eye, 0, 100))
	testing.expect(t, abs(eye.x - (32 - PLAYER_RADIUS)) < 1e-6 && !player_collides(&w, eye))
	eye = test_player_eye()
	testing.expect(t, player_sweep_axis(&w, &eye, 0, -100))
	testing.expect(t, abs(eye.x - (-32 + PLAYER_RADIUS)) < 1e-6 && !player_collides(&w, eye))
	world_apply(&w, {w.sequence + 1, {0, 185, 0}, .Stone})
	eye = test_player_eye()
	testing.expect(t, player_sweep_axis(&w, &eye, 1, 100))
	testing.expect(t, abs(eye.y - (185 - PLAYER_HEIGHT + PLAYER_EYE)) < 1e-6)
}

@(test)
test_player_slides_along_walls_and_diagonal_speed_is_normalized :: proc(t: ^testing.T) {
	w: World; test_player_world(&w); defer world_destroy(&w)
	for z in -8 ..= 8 {for y in 181 ..= 184 {
			world_apply(&w, {w.sequence + 1, {3, i32(y), i32(z)}, .Stone})
		}}
	p: Player; eye := test_player_eye()
	for _ in 0 ..< 120 {player_update(&p, &w, &eye, {move = {1, 0, 1}, speed = 14}, 1.0 / 60)}
	testing.expect(t, abs(eye.x - (3 - PLAYER_RADIUS)) < 1e-6)
	testing.expect(t, abs(eye.z - (0.5 + 9 / math.sqrt(2.0))) < 1e-6)
	testing.expect(t, !player_collides(&w, eye) && p.grounded)
}

@(test)
test_player_jump_landing_held_key_and_ceiling :: proc(t: ^testing.T) {
	w: World; test_player_world(&w); defer world_destroy(&w)
	p: Player; eye := test_player_eye()
	player_set_spectator(&p, &w, &eye, false)
	player_update(&p, &w, &eye, {speed = 14, jump = true, ascend = true}, 1.0 / 60)
	peak := eye.y
	for _ in 0 ..< 120 {
		player_update(&p, &w, &eye, {speed = 14, ascend = true}, 1.0 / 60)
		peak = max(peak, eye.y)
	}
	testing.expect(t, peak > 182.62 + 1.1 && peak < 182.62 + 1.5)
	testing.expect(t, p.grounded && abs(eye.y - 182.62) < 1e-6)
	world_apply(&w, {w.sequence + 1, {0, 183, 0}, .Stone})
	player_update(&p, &w, &eye, {speed = 14, jump = true}, 1.0 / 60)
	for _ in 0 ..< 60 {
		player_update(&p, &w, &eye, {speed = 14}, 1.0 / 60)
		testing.expect(
			t,
			eye.y <= 183 - PLAYER_HEIGHT + PLAYER_EYE + 1e-6 && !player_collides(&w, eye),
		)
	}
	testing.expect(t, p.grounded)
}

@(test)
test_player_fixed_step_matches_at_different_frame_rates :: proc(t: ^testing.T) {
	w: World; test_player_world(&w); defer world_destroy(&w)
	slow, fast: Player
	a, b := test_player_eye(), test_player_eye()
	player_set_spectator(&slow, &w, &a, false); player_set_spectator(&fast, &w, &b, false)
	for i in 0 ..< 60 {player_update(&slow, &w, &a, {move = {1, 0, 0}, speed = 14, jump = i == 0}, 1.0 / 30)}
	for i in 0 ..< 240 {player_update(&fast, &w, &b, {move = {1, 0, 0}, speed = 14, jump = i == 0}, 1.0 / 120)}
	for axis in 0 ..< 3 {testing.expect(t, abs(a[axis] - b[axis]) < 1e-6)}
	testing.expect(t, slow.grounded && fast.grounded && abs(a.x - 9.5) < 1e-6)
}

@(test)
test_player_spectator_toggle_and_embedded_save_recovery :: proc(t: ^testing.T) {
	w: World; test_player_world(&w); defer world_destroy(&w)
	p: Player; eye := test_player_eye()
	player_set_spectator(&p, &w, &eye, true)
	for _ in 0 ..< 3 {player_update(&p, &w, &eye, {speed = 14, descend = true}, 0.1)}
	testing.expect(t, p.spectator && eye.y < 180)
	eye.y = 180.8 // A saved noclip position embedded in the platform.
	player_set_spectator(&p, &w, &eye, false)
	testing.expect(t, !p.spectator && p.grounded && abs(eye.y - 182.62) < 1e-6)
	testing.expect(t, !player_collides(&w, eye))
	world_apply(&w, {w.sequence + 1, {0, 181, 0}, .Obsidian})
	player_update(&p, &w, &eye, {speed = 14}, 0)
	testing.expect(t, !player_collides(&w, eye) && abs(eye.y - 183.62) < 1e-6)
}

@(test)
test_player_placement_overlap_and_fluid_swimming :: proc(t: ^testing.T) {
	w: World; test_player_world(&w); defer world_destroy(&w)
	eye := test_player_eye()
	testing.expect(
		t,
		player_overlaps_block(eye, {0, 181, 0}) && player_overlaps_block(eye, {0, 182, 0}),
	)
	testing.expect(
		t,
		!player_overlaps_block(eye, {0, 180, 0}) && !player_overlaps_block(eye, {1, 181, 0}),
	)
	for y in 181 ..= 186 {world_apply(&w, {w.sequence + 1, {0, i32(y), 0}, .Water})}
	testing.expect(t, !player_collides(&w, eye) && player_fluid(&w, eye) == .Water)
	p: Player
	player_update(&p, &w, &eye, {speed = 14, ascend = true}, 0.1)
	testing.expect(t, eye.y > 182.9 && p.velocity.y > 0 && !player_collides(&w, eye))
}

package sandbox

import "core:testing"

@(test)
test_coordinates :: proc(t: ^testing.T) {
	testing.expect(t, floor_div(-1, 32) == -1)
	testing.expect(t, floor_div(-32, 32) == -1)
	testing.expect(t, floor_div(-33, 32) == -2)
	testing.expect(t, chunk_of({-1, 255, -33}) == I3{-1, 7, -2})
}

@(test)
test_world_commands_and_snapshot :: proc(t: ^testing.T) {
	w: World
	world_init(&w, 42)
	defer world_destroy(&w)
	testing.expect(t, world_apply(&w, {1, {-1, 100, 0}, .Wood}))
	testing.expect(t, !world_apply(&w, {1, {-1, 100, 0}, .Stone}))
	testing.expect(t, !world_apply(&w, {2, {0, 0, 0}, .Air}))
	testing.expect(t, !world_apply(&w, {2, {0, 256, 0}, .Stone}))
	testing.expect(t, world_get(&w, {-1, 100, 0}) == .Wood)
	testing.expect(t, world_apply(&w, {2, {2000, 100, 2000}, .Stone}))
	snapshot := world_snapshot(&w, {0, 3, 0})
	defer delete(snapshot)
	testing.expect(t, len(snapshot) == 1)
	if len(snapshot) == 1 {
		testing.expect(t, snapshot[0].position == I3{-1, 100, 0})
		world_apply(&w, {3, {-1, 100, 0}, .Stone})
		testing.expect(t, snapshot[0].block == .Wood)
	}
}

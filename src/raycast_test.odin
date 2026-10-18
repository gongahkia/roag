package sandbox

import "core:testing"

@(test)
test_raycast :: proc(t: ^testing.T) {
	w: World
	world_init(&w, 42)
	defer world_destroy(&w)
	world_apply(&w, {1, {-2, 200, 0}, .Stone})
	hit := raycast(&w, {0.5, 200.5, 0.5}, {-1, 0, 0}, 8)
	testing.expect(t, hit.hit && hit.block == I3{-2, 200, 0} && hit.adjacent == I3{-1, 200, 0})
	miss := raycast(&w, {0.5, 200.5, 0.5}, {-1, 0, 0}, 1)
	testing.expect(t, !miss.hit)
	inside := raycast(&w, {-1.5, 200.5, 0.5}, {0, 1, 0}, 8)
	testing.expect(t, inside.hit && inside.block == I3{-2, 200, 0})
}

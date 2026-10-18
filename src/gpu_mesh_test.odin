package sandbox

import "core:testing"

@(test)
test_gpu_cache_capacity_admission :: proc(t: ^testing.T) {
	r: Renderer
	r.pool_count = 3
	r.pool[0].bytes = 56 * 10
	r.pool[1].bytes = 56 * 15
	r.pool[2].bytes = 56 * 12
	testing.expect(t, mesh_pool_slot(&r, 56 * 9) == 0)
	testing.expect(t, mesh_pool_slot(&r, 56 * 11) == 2)
	testing.expect(t, mesh_pool_slot(&r, 56 * 16) == -1)
	testing.expect(t, mesh_pool_slot(&r, 56 * 3) == -1)
}

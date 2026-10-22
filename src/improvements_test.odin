package sandbox

import "core:fmt"
import "core:os"
import "core:testing"

@(test)
test_restore_compacts_world_and_fluid_recovery :: proc(t: ^testing.T) {
	w: World; world_init(&w, 42); defer world_destroy(&w)
	s: Fluid_Sim; fluids_init(&s); defer fluids_destroy(&s)
	p, q := I3{0, 220, 0}, I3{1, 220, 0}
	original := world_get(&w, p)
	world_apply(&w, {1, p, .Wood}); world_apply(&w, {2, q, .Lamp})
	fluids_reseed(&s, &w, {0, 220, 0}, 0)
	testing.expect(t, world_apply(&w, {3, p, original}) && w.edit_count == 1)
	b := w.edits[chunk_of(q)]
	testing.expect(t, len(b.positions) == 1 && b.positions[0] == q && b.blocks[q].index == 0)
	world_apply(&w, {4, q, world_generated_cached(&w, q)})
	testing.expect(t, w.edit_count == 0 && len(w.edits) == 0)
	// Recovery keeps keys rather than dangling pointers to reclaimed buckets.
	fluids_reseed(&s, &w, {0, 220, 0}, 0.1)
	world_apply(&w, {5, p, original})
	testing.expect(t, w.sequence == 5 && w.edit_count == 0)
}

@(test)
test_edit_replacement_and_restoration_at_capacity :: proc(t: ^testing.T) {
	w: World; world_init(&w, 42); defer world_destroy(&w)
	p := I3{0, 220, 0}
	world_apply(&w, {1, p, .Wood})
	w.edit_count = MAX_EDITS // Exercise admission without allocating 262144 cells.
	testing.expect(t, !world_apply(&w, {2, {1, 220, 0}, .Wood}))
	testing.expect(t, world_apply(&w, {2, p, .Lamp}))
	testing.expect(t, world_apply(&w, {3, p, .Air}) && w.edit_count == MAX_EDITS - 1)
	w.edit_count = 0
}

@(test)
test_performance_settings_migration_and_scrolling :: proc(t: ^testing.T) {
	s, ok := settings_decode("VOXSET1 move_speed 14 mouse_sensitivity 1 vsync 1 fps_limit 0")
	testing.expect(t, ok && s.render_distance == 4 && s.lighting == .Full)
	settings_preset(&s, .Potato); testing.expect(t, s.render_distance == 2 && s.lighting == .Fast)
	settings_adjust(&s, 5, 1); testing.expect(t, s.render_distance == 3 && s.preset == .Custom)
	settings_preset(&s, .Far); testing.expect(t, s.render_distance == 8 && s.lighting == .Full)
	s.render_distance = 33; testing.expect(t, !settings_valid(s))
	for size in ([2]I3{{320, 240, 0}, {960, 540, 0}}) {
		l := settings_layout(size.x, size.y)
		testing.expect(t, l.rows > 0 && l.row_top + f32(l.rows) * l.row_height <= l.height - 56)
	}
}

@(test)
test_fast_light_preserves_emission_without_sky_scatter :: proc(t: ^testing.T) {
	blocks := make([]u8, LIGHT_CELLS); defer delete(blocks)
	full := make([]u8, LIGHT_CELLS); defer delete(full)
	fast := make([]u8, LIGHT_CELLS); defer delete(fast)
	i := light_index(32, 32, 32); full[i] = 255; fast[i] = 255
	light_flood(blocks, full); light_flood(blocks, fast, nil, true)
	testing.expect(t, full[i + 1] == 238 && fast[i + 1] == 224)
	for value, index in full {testing.expect(t, value >> 4 == fast[index] >> 4)}
}

@(test)
test_batched_invalidation_and_readiness :: proc(t: ^testing.T) {
	s: Stream; s.chunks = make(map[I3]^Chunk_State); defer delete(s.chunks)
	s.radius = 1; s.next_revision = 1
	c := new(Chunk_State); defer free(c); c.complete = true; c.revision = 1
	s.chunks[{0, 2, 0}] = c
	requested, ready := stream_readiness(&s)
	testing.expect(t, requested == 40 && ready == 1)
	positions := [3]I3{{0, 90, 0}, {1, 90, 0}, {0, 90, 0}}
	stream_invalidate_many(&s, positions[:])
	testing.expect(t, c.revision == 2 && !c.complete && c.edit_pending)
	_, ready = stream_readiness(&s); testing.expect(t, ready == 0)
}

@(test)
test_async_save_snapshot_coalescing_and_failure :: proc(t: ^testing.T) {
	dir, err := os.make_directory_temp("", "voxel-autosave-*", context.allocator); testing.expect(t, err == nil)
	if err != nil {return}
	defer delete(dir); defer os.remove(dir)
	path := fmt.tprintf("%s/world.vox", dir); defer os.remove(path)
	a := new(App); defer free(a)
	world_init(&a.world, 42); defer world_destroy(&a.world)
	a.config.save_path = path; a.position = {0, 230, 0}
	world_apply(&a.world, {1, {0, 220, 0}, .Wood})
	a.now = 1; session_save_world(a); session_update_save(a)
	testing.expect(t, a.save.handle != nil)
	world_apply(&a.world, {2, {0, 220, 0}, .Lamp}); session_save_world(a)
	save_collect(&a.save, a.now, true)
	w: World; world_init(&w, 0); defer world_destroy(&w)
	pos: V3; testing.expect(t, world_load(&w, &pos, path) && world_get(&w, {0, 220, 0}) == .Wood)
	session_update_save(a); save_collect(&a.save, a.now, true)
	testing.expect(t, !a.save.failed && a.save.saved_sequence == 2)
	testing.expect(t, world_load(&w, &pos, path) && world_get(&w, {0, 220, 0}) == .Lamp)
	a.now = 1 + AUTOSAVE_SECONDS; a.position.x = 1
	session_update_save(a); testing.expect(t, a.save.handle != nil)
	save_collect(&a.save, a.now, true)
	a.config.save_path = fmt.tprintf("%s/impossible.vox", path)
	session_save_world(a); session_update_save(a); save_collect(&a.save, a.now, true)
	testing.expect(t, a.save.failed)
	testing.expect(t, world_load(&w, &pos, path) && pos.x == 1)
	// Includes a queued request and guarantees the newest state at shutdown.
	a.config.save_path = path; a.position.x = 2
	session_finish_save(a); testing.expect(t, world_load(&w, &pos, path) && pos.x == 2)
}

package sandbox

import "core:testing"

@(test)
test_creative_repeat_and_pick :: proc(t: ^testing.T) {
	r: Edit_Repeat
	testing.expect(t, edit_repeat_due(&r, 0, true, false, 0))
	testing.expect(t, !edit_repeat_due(&r, 0, true, true, 0.2))
	testing.expect(t, edit_repeat_due(&r, 0, true, true, 0.3))
	testing.expect(t, !edit_repeat_due(&r, 0, true, true, 0.31))
	testing.expect(t, edit_repeat_due(&r, 0, true, true, 0.43))
	testing.expect(t, !edit_repeat_due(&r, 0, false, true, 0.5))
	testing.expect(t, edit_repeat_due(&r, 0, true, false, 0.51))
	testing.expect(t, creative_pick(.Water_Falling) == creative_pick(.Water))
	testing.expect(t, creative_pick(.Lava_2) == creative_pick(.Lava))
	testing.expect(t, creative_pick(.Bedrock) == -1)
}

@(test)
test_creative_undo_redo_and_bounds :: proc(t: ^testing.T) {
	a := new(App); defer free(a)
	world_init(&a.world, 42); defer world_destroy(&a.world)
	a.stream.chunks = make(map[I3]^Chunk_State); defer delete(a.stream.chunks)
	fluids_init(&a.fluids); defer fluids_destroy(&a.fluids)
	a.player.spectator = true; a.position = {0, 254, 0}
	p := I3{0, 220, 0}; original := world_get(&a.world, p)
	testing.expect(t, creative_apply(a, p, .Wood))
	testing.expect(t, creative_history(a, false) && world_get(&a.world, p) == original)
	testing.expect(t, a.world.edit_count == 0)
	testing.expect(t, creative_history(a, true) && world_get(&a.world, p) == .Wood)
	testing.expect(t, creative_history(a, false))
	testing.expect(t, creative_apply(a, p, .Stone) && !creative_history(a, true))
	// Simulation changes must never be overwritten by an obsolete undo record.
	world_apply(&a.world, {a.world.sequence + 1, p, .Obsidian})
	testing.expect(t, !creative_history(a, false) && world_get(&a.world, p) == .Obsidian)
	a.player.spectator = false; a.position = {0.5, 221.62, 0.5}
	testing.expect(t, !creative_apply(a, p, .Lamp))
	h: Edit_History
	for i in 0 ..< EDIT_HISTORY_CAPACITY + 10 {edit_history_push(&h, {{i32(i), 220, 0}, .Air, .Wood})}
	testing.expect(t, h.count == EDIT_HISTORY_CAPACITY && h.cursor == EDIT_HISTORY_CAPACITY)
	testing.expect(t, h.entries[h.start].position.x == 10)
}

@(test)
test_palette_search :: proc(t: ^testing.T) {
	p: Palette_State
	copy(p.query[:], "CHERRY"); p.length = 6
	results, count := palette_matches(&p)
	testing.expect(t, count == 2)
	for i in results[:count] {testing.expect(t, CREATIVE_BLOCKS[i] == .Cherry_Wood || CREATIVE_BLOCKS[i] == .Cherry_Leaves)}
	copy(p.query[:], "NONEXISTENT"); p.length = 11
	_, count = palette_matches(&p); testing.expect(t, count == 0)
	p.length = 0; _, count = palette_matches(&p); testing.expect(t, count == len(CREATIVE_BLOCKS))
	l := palette_layout(320, 240)
	testing.expect(t, l.rows >= 1 && l.row_top + f32(l.rows) * l.row_height < l.height - 26)
}

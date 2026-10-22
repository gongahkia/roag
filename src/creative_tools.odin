package sandbox

EDIT_HISTORY_CAPACITY :: 256
Edit_Record :: struct {position: I3, before, after: Block}
Edit_History :: struct {
	entries: [EDIT_HISTORY_CAPACITY]Edit_Record,
	start, count, cursor: int,
}
Edit_Repeat :: struct {next: [2]f64}

edit_history_push :: proc(h: ^Edit_History, e: Edit_Record) {
	h.count = h.cursor // A new edit drops the redo branch.
	if h.count == EDIT_HISTORY_CAPACITY {
		h.start = (h.start + 1) % EDIT_HISTORY_CAPACITY; h.count -= 1; h.cursor -= 1
	}
	h.entries[(h.start + h.count) % EDIT_HISTORY_CAPACITY] = e
	h.count += 1; h.cursor = h.count
}

edit_repeat_due :: proc(r: ^Edit_Repeat, button: int, down, old: bool, now: f64) -> bool {
	if !down {r.next[button] = 0; return false}
	if !old {r.next[button] = now + 0.25; return true}
	if r.next[button] > 0 && now >= r.next[button] {r.next[button] = now + 0.12; return true}
	return false
}

creative_pick :: proc(block: Block) -> int {
	// Flowing fluid cells select their placeable source.
	b := is_fluid(block) ? fluid_kind(block) : block
	for candidate, i in CREATIVE_BLOCKS {if candidate == b {return i}}
	return -1
}

// Every manual edit, undo and redo shares collision, fluid and revision rules.
creative_apply :: proc(a: ^App, p: I3, block: Block, remember: bool = true) -> bool {
	if !a.player.spectator && is_solid(block) && player_overlaps_block(a.position, p) {return false}
	before := world_get(&a.world, p)
	if before == block {return false}
	if !world_apply(&a.world, {a.world.sequence + 1, p, block}) {return false}
	stream_invalidate(&a.stream, p)
	fluids_notify(&a.fluids, &a.world, p, before)
	if remember {edit_history_push(&a.history, {p, before, block})}
	return true
}

creative_history :: proc(a: ^App, redo: bool) -> bool {
	h := &a.history
	if (redo && h.cursor == h.count) || (!redo && h.cursor == 0) {return false}
	index := redo ? h.cursor : h.cursor - 1
	e := h.entries[(h.start + index) % EDIT_HISTORY_CAPACITY]
	expected := redo ? e.before : e.after
	// Simulation may have changed the cell since the edit. Preserve that state
	// and consume the stale record so it cannot trap the rest of the history.
	if world_get(&a.world, e.position) != expected {
		h.cursor += redo ? 1 : -1; return false
	}
	if !creative_apply(a, e.position, redo ? e.after : e.before, false) {return false}
	h.cursor += redo ? 1 : -1
	return true
}

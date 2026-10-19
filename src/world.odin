package sandbox

import "core:mem"
import "core:sync"

Edit_Command :: struct {
	sequence: u64,
	position: I3,
	block:    Block,
}

Edit_Bucket :: struct {
	blocks:    map[I3]Block,
	positions: [dynamic]I3,
}

World :: struct {
	seed:             u64,
	edits:            map[I3]^Edit_Bucket,
	sequence:         u64,
	edit_count:       int,
	tracker:          ^mem.Tracking_Allocator,
	allocation_limit: int,
	generator:        u32,
	column_cache:     [256]Column_Cache_Entry,
	feature_cache:    [8]Feature_Cache_Entry,
}

MAX_EDITS :: 262144

world_init :: proc(w: ^World, seed: u64) {
	w.seed = seed
	w.generator = GENERATOR_VERSION
	w.edits = make(map[I3]^Edit_Bucket)
}

world_destroy :: proc(w: ^World) {
	for _, b in w.edits {
		delete(b.blocks)
		delete(b.positions)
		free(b)
	}
	delete(w.edits)
}

world_get :: proc(w: ^World, p: I3) -> Block {
	if b, ok := w.edits[chunk_of(p)]; ok {
		if v, exists := b.blocks[p]; exists {
			return v
		}
	}
	if w.generator >= 3 {return world_generated_cached(w, p)}
	return generated_block(w.seed, p, w.generator)
}

world_apply :: proc(w: ^World, e: Edit_Command) -> bool {
	if e.sequence <= w.sequence ||
	   e.position.y <= 0 ||
	   e.position.y >= HEIGHT ||
	   abs(i64(e.position.x)) > 100000000 ||
	   abs(i64(e.position.z)) > 100000000 ||
	   !block_valid(e.block) {
		return false
	}
	k := chunk_of(e.position)
	b, ok := w.edits[k]
	if w.tracker != nil {
		sync.mutex_lock(&w.tracker.mutex)
		used := w.tracker.current_memory_allocated
		sync.mutex_unlock(&w.tracker.mutex)
		if int(used) + WORLD_EDIT_HEADROOM_BYTES > w.allocation_limit {
			return false
		}
	}
	if !ok {
		if w.edit_count >= MAX_EDITS {
			return false
		}
		b = new(Edit_Bucket)
		b.blocks = make(map[I3]Block)
		w.edits[k] = b
	}
	_, exists := b.blocks[e.position]
	if !exists && w.edit_count >= MAX_EDITS {
		return false
	}
	if !exists {
		w.edit_count += 1
		append(&b.positions, e.position)
	}
	b.blocks[e.position] = e.block
	w.sequence = e.sequence
	return true
}
// Snapshot only edits in the 27 touching chunks, never scan the entire world.
world_snapshot :: proc(w: ^World, k: I3) -> [dynamic]Edit_Command {
	out := make([dynamic]Edit_Command)
	for z in -1 ..= 1 {
		for y in -1 ..= 1 {
			for x in -1 ..= 1 {
				n := k + I3{i32(x), i32(y), i32(z)}
				if b, ok := w.edits[n]; ok {
					for p, v in b.blocks {
						local := p - k * 32
						if local.x >= -1 &&
						   local.x <= 32 &&
						   local.y >= -1 &&
						   local.y <= 32 &&
						   local.z >= -1 &&
						   local.z <= 32 {
							append(&out, Edit_Command{0, p, v})
						}
					}
				}
			}
		}
	}
	return out
}

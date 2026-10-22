package sandbox

import "core:mem"
import "core:sync"

Edit_Command :: struct {
	sequence: u64,
	position: I3,
	block:    Block,
}

World_Edit :: struct {
	block: Block,
	index: int,
}

Edit_Bucket :: struct {
	blocks:    map[I3]World_Edit,
	positions: [dynamic]I3,
}

World :: struct {
	seed:             u64,
	edits:            map[I3]^Edit_Bucket,
	sequence:         u64,
	edit_count:       int,
	tracker:          ^mem.Tracking_Allocator,
	allocation_limit: int,
	allocator:        mem.Allocator,
	generator:        u32,
	column_cache:     [256]Column_Cache_Entry,
	feature_cache:    [8]Feature_Cache_Entry,
}

MAX_EDITS :: 262144

world_init :: proc(w: ^World, seed: u64, allocator := context.allocator) {
	context.allocator = allocator
	w.allocator = allocator
	w.seed = seed
	w.generator = GENERATOR_VERSION
	w.edits = make(map[I3]^Edit_Bucket)
}

world_destroy :: proc(w: ^World) {
	context.allocator = w.allocator
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
			return v.block
		}
	}
	if w.generator >= 3 {return world_generated_cached(w, p)}
	return generated_block(w.seed, p, w.generator)
}

world_apply :: proc(w: ^World, e: Edit_Command, compact: bool = true) -> bool {
	context.allocator = w.allocator
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
	existing: World_Edit
	exists := false
	if ok {existing, exists = b.blocks[e.position]}
	// Restoring procedural terrain releases both the override and its recovery
	// index. Existing replacements/removals must remain possible under pressure.
	if compact {
		generated := w.generator >= 3 ? world_generated_cached(w, e.position) : generated_block(w.seed, e.position, w.generator)
		if e.block == generated {
			if exists {
				last := b.positions[len(b.positions) - 1]
				b.positions[existing.index] = last
				value := b.blocks[last]; value.index = existing.index; b.blocks[last] = value
				resize(&b.positions, len(b.positions) - 1)
				delete_key(&b.blocks, e.position); w.edit_count -= 1
				if len(b.blocks) == 0 {
					delete(b.blocks); delete(b.positions); free(b); delete_key(&w.edits, k)
				}
			}
			w.sequence = e.sequence
			return true
		}
	}
	if !exists {
		if w.edit_count >= MAX_EDITS {return false}
		if w.tracker != nil {
			sync.mutex_lock(&w.tracker.mutex)
			used := w.tracker.current_memory_allocated
			sync.mutex_unlock(&w.tracker.mutex)
			if int(used) + WORLD_EDIT_HEADROOM_BYTES > w.allocation_limit {return false}
		}
		if !ok {
			b = new(Edit_Bucket); b.blocks = make(map[I3]World_Edit); w.edits[k] = b
		}
		existing.index = len(b.positions)
		append(&b.positions, e.position); w.edit_count += 1
	}
	existing.block = e.block; b.blocks[e.position] = existing
	w.sequence = e.sequence
	return true
}
// Read bounded edit buckets, never scan the world. Lit jobs also need roofs
// above the 16-cell horizontal halo, including chunks that are not resident.
world_snapshot :: proc(w: ^World, k: I3, lighting: bool = false) -> [dynamic]Edit_Command {
	out := make([dynamic]Edit_Command)
	for z in -1 ..= 1 {
		for y in (lighting ? 0 : int(k.y) - 1) ..= (lighting ? 7 : int(k.y) + 1) {
			for x in -1 ..= 1 {
				n := I3{k.x + i32(x), i32(y), k.z + i32(z)}
				if b, ok := w.edits[n]; ok {
					for p, v in b.blocks {
						local := p - k * 32
						margin := lighting ? LIGHT_HALO : 1
						if local.x >= -i32(margin) &&
						   local.x < i32(CHUNK + margin) &&
						   local.y >= -i32(margin) &&
						   (lighting || local.y <= 32) &&
						   local.z >= -i32(margin) &&
						   local.z < i32(CHUNK + margin) {
							append(&out, Edit_Command{0, p, v.block})
						}
					}
				}
			}
		}
	}
	return out
}

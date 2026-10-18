package sandbox

import "core:fmt"
import "core:math"
import "core:os"

// Portable binary save: explicit little endian fields, never raw struct padding.
put32 :: proc(out: ^[dynamic]u8, v: u32) {
	for s in 0 ..< 4 {
		append(out, u8(v >> u32(s * 8)))
	}
}

put64 :: proc(out: ^[dynamic]u8, v: u64) {
	for s in 0 ..< 8 {
		append(out, u8(v >> u32(s * 8)))
	}
}

get32 :: proc(data: []u8, at: ^int) -> u32 {
	v: u32
	for s in 0 ..< 4 {
		v |= u32(data[at^]) << u32(s * 8)
		at^ += 1
	}
	return v
}

get64 :: proc(data: []u8, at: ^int) -> u64 {
	v: u64
	for s in 0 ..< 8 {
		v |= u64(data[at^]) << u32(s * 8)
		at^ += 1
	}
	return v
}

world_save :: proc(w: ^World, pos: V3, path: string) -> bool {
	out := make([dynamic]u8)
	defer delete(out)
	append(&out, ..([]u8{'V', 'O', 'X', '2'}))
	put64(&out, w.seed)
	put64(&out, w.sequence)
	for v in pos {
		put64(&out, transmute(u64)v)
	}
	put32(&out, u32(w.edit_count))
	put32(&out, w.generator)
	for _, b in w.edits {
		for p, v in b.blocks {
			for c in p {
				put32(&out, transmute(u32)c)
			}
			append(&out, u8(v))
		}
	}
	temporary := fmt.tprintf("%s.tmp", path)
	if os.write_entire_file(temporary, out[:]) != nil {
		return false
	}
	return os.rename(temporary, path) == nil
}

world_load :: proc(w: ^World, pos: ^V3, path: string) -> bool {
	file, err := os.open(path)
	if err != nil {
		return false
	}
	defer os.close(file)
	length, size_err := os.file_size(file)
	if size_err != nil || length < 48 || length > 52 + MAX_EDITS * 13 {
		return false
	}
	data, ok := os.read_entire_file(file, context.allocator)
	if ok != nil {
		return false
	}
	defer delete(data)
	if len(data) < 48 || (string(data[:4]) != "VOX1" && string(data[:4]) != "VOX2") {
		return false
	}
	at := 4
	seed := get64(data, &at)
	sequence := get64(data, &at)
	p: V3
	for &v in p {
		v = transmute(f64)get64(data, &at)
		if math.is_nan(v) || math.is_inf(v) || abs(v) > 100000000 {
			return false
		}
	}
	count := int(get32(data, &at))
	generator: u32 = 1
	if string(data[:4]) == "VOX2" {
		if len(data) < 52 {return false}
		generator = get32(data, &at)
	}
	if generator < 1 ||
	   generator > GENERATOR_VERSION ||
	   count > MAX_EDITS ||
	   len(data) != at + count * 13 {
		return false
	}
	candidate: World
	world_init(&candidate, seed)
	candidate.generator = generator
	candidate.tracker = w.tracker
	candidate.allocation_limit = w.allocation_limit
	for _ in 0 ..< count {
		q: I3
		for &v in q {
			v = transmute(i32)get32(data, &at)
		}
		b := Block(data[at])
		at += 1
		if string(data[:4]) == "VOX1" &&
		   u8(b) > u8(Block.Bedrock) {world_destroy(&candidate); return false}
		if bucket, ok := candidate.edits[chunk_of(q)]; ok {
			if q in bucket.blocks {
				world_destroy(&candidate)
				return false
			}
		}
		if !world_apply(&candidate, {candidate.sequence + 1, q, b}) {
			world_destroy(&candidate)
			return false
		}
	}
	candidate.sequence = max(sequence, candidate.sequence)
	world_destroy(w)
	w^ = candidate
	pos^ = p
	return true
}

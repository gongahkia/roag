package sandbox

import "core:math"
import "core:time"

FLUID_QUEUE_CAPACITY :: 8192
FLUID_FRAME_UPDATES :: 128
FLUID_RADIUS :: 2 // Five-by-five chunk columns, independent of render distance.
Fluid_Event :: struct {
	position: I3,
	due:      u64,
}
Fluid_Sim :: struct {
	queue:                                     [FLUID_QUEUE_CAPACITY]Fluid_Event,
	head, count:                               int,
	scheduled:                                 map[I3]bool,
	tick:                                      u64,
	center:                                    I3,
	seed_buckets:                              [200]^Edit_Bucket,
	seed_count, seed_bucket, seed_position:    int,
	next_seed:                                 f64,
	changed:                                   [FLUID_FRAME_UPDATES]I3,
	changed_count, updates, overflow, refused: int,
	last_ms:                                   f64,
}

fluid_in_range :: proc(s: ^Fluid_Sim, p: I3) -> bool {
	k := chunk_of(p)
	return(
		p.y > 0 &&
		p.y < HEIGHT &&
		abs(k.x - s.center.x) <= FLUID_RADIUS &&
		abs(k.z - s.center.z) <= FLUID_RADIUS \
	)
}
fluids_init :: proc(s: ^Fluid_Sim) {
	s.scheduled = make(map[I3]bool, FLUID_QUEUE_CAPACITY)
	s.center = {0x7fffffff, 0, 0}
}
fluids_destroy :: proc(s: ^Fluid_Sim) {delete(s.scheduled)}

fluid_schedule :: proc(s: ^Fluid_Sim, p: I3, delay: u64 = 1) {
	if !fluid_in_range(s, p) || p in s.scheduled {return}
	if s.count == FLUID_QUEUE_CAPACITY {s.overflow += 1; return}
	s.queue[(s.head + s.count) % FLUID_QUEUE_CAPACITY] = {p, s.tick + delay}
	s.count += 1
	s.scheduled[p] = true
}

// Notify every topology change, including replacing a source with air/solid.
fluids_notify :: proc(s: ^Fluid_Sim, w: ^World, p: I3, previous: Block = .Air) {
	b := world_get(w, p)
	delay: u64 = fluid_kind(b) == .Lava || fluid_kind(previous) == .Lava ? 4 : 1
	if b == .Air || is_fluid(b) {fluid_schedule(s, p, delay)}
	for n in NORMALS {
		q := p + n
		nb := world_get(w, q)
		if is_fluid(nb) {fluid_schedule(s, q, fluid_kind(nb) == .Lava ? 4 : 1)}
		if nb == .Air && (is_fluid(b) || is_fluid(previous)) {fluid_schedule(s, q, delay)}
	}
}

// Source-fed voxel flow: gravity first, then finite horizontal reach. Removing
// the feed raises downstream levels until they disappear; water and lava never
// sustain each other's flow. Natural untouched lakes remain asleep until edited.
fluid_next :: proc(w: ^World, p: I3) -> Block {
	current := world_get(w, p)
	if fluid_kind(current) == .Lava {
		for n in NORMALS {
			if fluid_kind(world_get(w, p + n)) == .Water {
				return current == .Lava ? .Obsidian : .Cobblestone
			}
		}
	}
	if is_solid(current) || current == .Water || current == .Lava {return current}
	above := world_get(w, p + I3{0, 1, 0})
	kind := fluid_kind(above)
	if kind != .Air {
		if is_fluid(current) && fluid_kind(current) != kind {return .Cobblestone}
		return kind == .Water ? .Water_Falling : .Lava_Falling
	}
	best_water, best_lava, water_sources := 99, 99, 0
	for n in NORMALS {
		if n.y != 0 {continue}
		q := p + n
		neighbor := world_get(w, q)
		family := fluid_kind(neighbor)
		if family == .Air {continue}
		below := world_get(w, q + I3{0, -1, 0})
		if !is_solid(below) && below != family {continue}
		if neighbor == .Water {water_sources += 1}
		level := fluid_level(neighbor) + 1
		if family == .Water {best_water = min(best_water, level)}
		if family == .Lava {best_lava = min(best_lava, level)}
	}
	if water_sources >= 2 && is_solid(world_get(w, p + I3{0, -1, 0})) {return .Water}
	if best_water <= 7 {return Block(int(Block.Water_1) + best_water - 1)}
	if best_lava <= 3 {return Block(int(Block.Lava_1) + best_lava - 1)}
	return .Air
}

fluids_reseed :: proc(s: ^Fluid_Sim, w: ^World, pos: V3, now: f64) {
	center := chunk_of({i32(math.floor(pos.x)), 0, i32(math.floor(pos.z))})
	if center.x != s.center.x ||
	   center.z != s.center.z ||
	   (s.seed_bucket >= s.seed_count && now >= s.next_seed) {
		s.center = center
		s.seed_count, s.seed_bucket, s.seed_position = 0, 0, 0
		for z in -FLUID_RADIUS ..= FLUID_RADIUS {
			for x in -FLUID_RADIUS ..= FLUID_RADIUS {
				for y in 0 ..< HEIGHT / CHUNK {
					if b, ok := w.edits[center + I3{i32(x), i32(y), i32(z)}]; ok {
						s.seed_buckets[s.seed_count] = b; s.seed_count += 1
					}
				}
			}
		}
		s.next_seed = now + 5
	}
	// Indexed positions keep recovery from save/load, range changes, or queue
	// saturation incremental, without walking a large hash bucket every frame.
	for _ in 0 ..< 16 {
		if s.seed_bucket >= s.seed_count {break}
		b := s.seed_buckets[s.seed_bucket]
		if s.seed_position >= len(b.positions) {s.seed_bucket += 1; s.seed_position = 0; continue}
		p := b.positions[s.seed_position]; s.seed_position += 1
		fluids_notify(s, w, p)
	}
}

fluids_step :: proc(
	s: ^Fluid_Sim,
	w: ^World,
	tick: u64,
	budget: int = FLUID_FRAME_UPDATES,
	timed: bool = true,
) {
	start := time.tick_now()
	s.tick = tick
	s.changed_count, s.updates = 0, 0
	checks := min(s.count, min(budget, FLUID_FRAME_UPDATES))
	for i in 0 ..< checks {
		e := s.queue[s.head]; s.head = (s.head + 1) % FLUID_QUEUE_CAPACITY; s.count -= 1
		if e.due > tick {
			s.queue[(s.head + s.count) % FLUID_QUEUE_CAPACITY] = e; s.count += 1
			continue
		}
		delete_key(&s.scheduled, e.position)
		if !fluid_in_range(s, e.position) {continue}
		before := world_get(w, e.position)
		after := fluid_next(w, e.position)
		s.updates += 1
		if before != after {
			if world_apply(w, {w.sequence + 1, e.position, after}) {
				s.changed[s.changed_count] = e.position; s.changed_count += 1
				fluids_notify(s, w, e.position, before)
			} else {s.refused += 1}
		}
		if timed && i % 8 == 7 && time.duration_milliseconds(time.tick_since(start)) >= 1 {break}
	}
	s.last_ms = time.duration_milliseconds(time.tick_since(start))
}

package sandbox

import "core:math"
import "core:mem"
import "core:sync"
import "core:time"

Chunk_State :: struct {
	gpu:                                           GPU_Mesh,
	revision, job_revision:                        u64,
	running, complete, oversized, budget_deferred: bool,
	edit_pending, wait_for_quiet:                  bool,
	last_edit:                                     time.Tick,
}

Stream :: struct {
	chunks:                                                    map[I3]^Chunk_State,
	workers:                                                   [8]Worker,
	worker_count:                                              int,
	next_revision:                                             u64,
	naive_mesh:                                                bool,
	unlit:                                                     bool,
	center:                                                    I3,
	radius, configured_radius:                                 int,
	mesh_budget, device_mesh_limit, cpu_budget, upload_budget: int,
	pressure, stale, oversized, generated:                     int,
	last_generation_ms, last_meshing_ms:                       f64,
	last_lighting_ms, total_lighting_ms:                       f64,
	total_generation_ms, total_meshing_ms, peak_generation_ms: f64,
	last_upload_bytes, peak_upload_bytes:                      int,
	tracker:                                                   ^mem.Tracking_Allocator,
}

cpu_bytes :: proc(s: ^Stream) -> int {
	sync.mutex_lock(&s.tracker.mutex)
	v := int(s.tracker.current_memory_allocated)
	sync.mutex_unlock(&s.tracker.mutex)
	return v
}

stream_init :: proc(
	s: ^Stream,
	workers, radius, mesh_budget, cpu_budget, upload_budget: int,
	tracker: ^mem.Tracking_Allocator,
) {
	s.chunks = make(map[I3]^Chunk_State)
	s.worker_count = workers
	s.radius = radius
	s.configured_radius = radius
	s.device_mesh_limit = mesh_budget
	s.mesh_budget = mesh_budget
	s.cpu_budget = cpu_budget
	s.upload_budget = upload_budget
	s.tracker = tracker
	s.center = {0x7fffffff, 0, 0}
	for i in 0 ..< workers {
		worker_start(&s.workers[i])
	}
}

stream_destroy :: proc(s: ^Stream, r: ^Renderer) {
	for i in 0 ..< s.worker_count {
		worker_stop(&s.workers[i])
	}
	for i in 0 ..< s.worker_count {
		worker_join_release(&s.workers[i], r)
	}
	for _, c in s.chunks {
		mesh_release(r, &c.gpu)
		free(c)
	}
	delete(s.chunks)
	s.chunks = nil
}

distance2 :: proc(k, center: I3) -> int {
	dx, dz := int(k.x - center.x), int(k.z - center.z)
	return dx * dx + dz * dz
}

stream_reconcile :: proc(s: ^Stream, r: ^Renderer, pos: V3, force: bool = false) {
	center := chunk_of({i32(math.floor(pos.x)), i32(math.floor(pos.y)), i32(math.floor(pos.z))})
	if center.x == s.center.x && center.z == s.center.z && !force {
		return
	}
	moved := center.x != s.center.x || center.z != s.center.z
	s.center = center
	if moved {
		for _, c in s.chunks {
			c.budget_deferred = false
		}
	}
	removals := make([dynamic]I3, 0, 128, context.temp_allocator)
	for k, _ in s.chunks {
		if distance2(k, center) > (s.radius + 1) * (s.radius + 1) {
			append(&removals, k)
		}
	}
	for k in removals {
		c := s.chunks[k]
		mesh_recycle(r, &c.gpu)
		free(c)
		delete_key(&s.chunks, k)
	}
	for z in -s.radius ..= s.radius {
		for x in -s.radius ..= s.radius {
			if x * x + z * z > s.radius * s.radius {
				continue
			}
			for y in 0 ..< 8 {
				k := I3{center.x + i32(x), i32(y), center.z + i32(z)}
				if k not_in s.chunks {
					if cpu_bytes(s) + cpu_reservation(s.worker_count) > s.cpu_budget {
						continue
					}
					s.next_revision += 1
					c := new(Chunk_State)
					c.revision = s.next_revision
					s.chunks[k] = c
				}
			}
		}
	}
}

stream_pressure :: proc(
	s: ^Stream,
	r: ^Renderer,
	pos: V3,
	needed: int = 0,
	protected: I3 = {0x7fffffff, 0, 0},
) {
	s.pressure += 1
	renderer_trim_pool(r)
	if s.radius > 1 {
		s.radius -= 1
		stream_reconcile(s, r, pos, true)
	}
	renderer_trim_pool(r)
	// A resize can lower the budget even at minimum draw distance.
	for r.mesh_bytes + needed > s.mesh_budget {
		farthest: ^Chunk_State
		score := -1
		for k, c in s.chunks {
			if c.gpu.vao == 0 || k == protected {
				continue
			}
			d := distance2(k, s.center) * 16 + abs(int(k.y) - int(s.center.y))
			if d > score {
				score = d
				farthest = c
			}
		}
		if farthest == nil {
			break
		}
		mesh_release(r, &farthest.gpu)
		farthest.complete = false
		farthest.budget_deferred = true
	}
	if r.mesh_bytes + needed > s.mesh_budget {
		if c, ok := s.chunks[protected]; ok {
			mesh_release(r, &c.gpu)
		}
	}
	// Only the main thread owns upload state. Workers do not touch these handles.
	for i in 0 ..< s.worker_count {
		if r.mesh_bytes + needed <= s.mesh_budget {
			break
		}
		w := &s.workers[i]
		if w.upload.vao == 0 {
			continue
		}
		mesh_release(r, &w.upload)
		w.uploaded = 0
		if c, ok := s.chunks[w.result.key]; ok {
			s.next_revision += 1
			c.revision = s.next_revision
			c.running = false
			c.budget_deferred = true
		}
	}
}

result_current :: proc(s: ^Stream, key: I3, revision: u64) -> bool {
	c, ok := s.chunks[key]
	return ok && c.revision == revision
}

stream_collect :: proc(s: ^Stream, r: ^Renderer, pos: V3) {
	uploaded := 0
	for i in 0 ..< s.worker_count {
		w := &s.workers[i]
		sync.mutex_lock(&w.mutex)
		if !w.ready {
			sync.mutex_unlock(&w.mutex)
			continue
		}
		result := &w.result
		bytes := len(result.mesh.vertices) * size_of(Vertex) + len(result.mesh.indices) * 4
		c, exists := s.chunks[result.key]
		if !result_current(s, result.key, result.revision) {
			s.stale += 1
			mesh_release(r, &w.upload)
			if exists &&
			   c.job_revision == result.revision &&
			   c.edit_pending {c.wait_for_quiet = true}
		} else if result.mesh.overflow {
			c.oversized = true
			c.complete = true
			c.edit_pending, c.wait_for_quiet = false, false
			s.oversized += 1
		} else {
			if w.upload.vao == 0 && bytes > 0 {
				if r.mesh_bytes + mesh_allocation_cost(r, result.mesh) > s.mesh_budget {
					if exists && c.job_revision == result.revision {
						c.running = false
					}
					if bytes > s.mesh_budget {
						c.complete = true
						c.oversized = true
						s.oversized += 1
					}
					stream_pressure(s, r, pos, bytes, result.key)
					mesh_destroy(&result.mesh)
					w.ready = false
					w.busy = false
					w.uploaded = 0
					sync.mutex_unlock(&w.mutex)
					continue
				}
				w.upload = mesh_allocate(r, result.mesh)
				if w.upload.vao == 0 {
					c.running = false
					s.device_mesh_limit = max(MiB, s.mesh_budget * 3 / 4)
					s.mesh_budget = s.device_mesh_limit
					stream_pressure(s, r, pos)
					mesh_destroy(&result.mesh)
					w.ready = false
					w.busy = false
					w.uploaded = 0
					sync.mutex_unlock(&w.mutex)
					continue
				}
			}
			if bytes > 0 && uploaded < s.upload_budget {
				count := mesh_upload_part(
					w.upload,
					result.mesh,
					w.uploaded,
					s.upload_budget - uploaded,
				)
				w.uploaded += count
				uploaded += count
			}
			if w.uploaded < bytes {
				sync.mutex_unlock(&w.mutex)
				continue
			}
			mesh_recycle(r, &c.gpu)
			c.gpu = w.upload
			w.upload = {}
			c.complete = true
			c.edit_pending, c.wait_for_quiet = false, false
			s.generated += 1
			s.last_generation_ms = result.generation_ms
			s.last_meshing_ms = result.meshing_ms
			s.last_lighting_ms = result.lighting_ms
			s.total_lighting_ms += result.lighting_ms
			s.total_generation_ms += result.generation_ms
			s.total_meshing_ms += result.meshing_ms
			s.peak_generation_ms = max(s.peak_generation_ms, result.generation_ms)
		}
		if exists && c.job_revision == result.revision {
			c.running = false
		}
		mesh_destroy(&result.mesh)
		w.ready = false
		w.busy = false
		w.uploaded = 0
		sync.mutex_unlock(&w.mutex)
	}
	s.last_upload_bytes = uploaded
	s.peak_upload_bytes = max(s.peak_upload_bytes, uploaded)
}

stream_schedule :: proc(s: ^Stream, w: ^World, pos: V3) {
	now := time.tick_now()
	for i in 0 ..< s.worker_count {
		worker := &s.workers[i]
		sync.mutex_lock(&worker.mutex)
		if worker.busy {
			sync.mutex_unlock(&worker.mutex)
			continue
		}
		busy_count := 0
		for j in 0 ..< s.worker_count {
			if s.workers[j].busy {
				busy_count += 1
			}
		}
		if cpu_bytes(s) + cpu_reservation(busy_count + 1) > s.cpu_budget {
			sync.mutex_unlock(&worker.mutex)
			continue
		}
		best: ^Chunk_State
		best_key: I3
		score: f64 = 1e30
		for k, c in s.chunks {
			if c.running ||
			   c.complete ||
			   c.budget_deferred ||
			   distance2(k, s.center) > s.radius * s.radius {
				continue
			}
			if chunk_waiting_for_edits(c, now) {continue}
			delta := V3 {
				f64(k.x) * 32 + 16 - pos.x,
				f64(k.y) * 32 + 16 - pos.y,
				f64(k.z) * 32 + 16 - pos.z,
			}
			d := delta.x * delta.x + delta.z * delta.z + delta.y * delta.y * 0.5
			// Refresh existing geometry before generating distant terrain.
			if c.gpu.vao != 0 {
				d *= 0.1
			}
			if d < score {
				score = d
				best = c
				best_key = k
			}
		}
		if best != nil {
			best.running = true
			best.job_revision = best.revision
			worker.job = {
				best_key,
				best.revision,
				w.seed,
				world_snapshot(w, best_key, !s.unlit),
				s.naive_mesh,
				w.generator,
				s.unlit,
			}
			worker.busy = true
			sync.sema_post(&worker.wake)
		}
		sync.mutex_unlock(&worker.mutex)
	}
}

stream_invalidate :: proc(s: ^Stream, p: I3) {
	now := time.tick_now()
	// A roof edit can change direct sunlight arbitrarily far below it. Sideways
	// and upward light travels at most 14 cells; include the vertex/AO halo too.
	margin := s.unlit ? 1 : LIGHT_HALO
	lo, hi :=
		chunk_of(p - I3{i32(margin), i32(margin), i32(margin)}),
		chunk_of(p + I3{i32(margin), i32(margin), i32(margin)})
	if !s.unlit {lo.y = 0}
	for z := lo.z;
	    z <= hi.z;
	    z += 1 {for y := max(0, lo.y); y <= min(7, hi.y); y += 1 {for x := lo.x; x <= hi.x; x += 1 {
				if c, ok := s.chunks[I3{x, y, z}]; ok {
					s.next_revision += 1
					c.revision = s.next_revision
					c.complete = false
					c.oversized = false
					c.budget_deferred = false
					c.edit_pending = true
					c.last_edit = now
				}
			}}}
}

// An invalidated in-flight job must not repeatedly monopolize the nearest
// worker slot. Keep its previous mesh and let unaffected chunks make progress.
chunk_waiting_for_edits :: proc(c: ^Chunk_State, now: time.Tick) -> bool {
	return c.wait_for_quiet && time.tick_diff(c.last_edit, now) < 50 * time.Millisecond
}

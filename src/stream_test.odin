package sandbox

import "core:fmt"
import "core:mem"
import "core:sync"
import "core:testing"
import "core:time"

@(test)
test_stale_jobs_and_corner_invalidation :: proc(t: ^testing.T) {
	s: Stream
	s.chunks = make(map[I3]^Chunk_State)
	defer delete(s.chunks)
	for z in -1 ..= 0 {
		for y in 1 ..= 2 {
			for x in -1 ..= 0 {
				k := I3{i32(x), i32(y), i32(z)}
				c := new(Chunk_State)
				s.next_revision += 1
				c.revision = s.next_revision
				c.complete = true
				s.chunks[k] = c
			}
		}
	}
	defer for _, c in s.chunks {
		free(c)
	}
	key := I3{0, 2, 0}
	old := s.chunks[key].revision
	testing.expect(t, result_current(&s, key, old))
	stream_invalidate(&s, {0, 64, 0})
	testing.expect(t, !result_current(&s, key, old))
	for _, c in s.chunks {
		testing.expect(t, !c.complete)
	}
	free(s.chunks[key])
	delete_key(&s.chunks, key)
	testing.expect(t, !result_current(&s, key, old))
	// The new resident at an identical coordinate must never accept an old job.
	c := new(Chunk_State)
	s.next_revision += 1
	c.revision = s.next_revision
	s.chunks[key] = c
	testing.expect(t, !result_current(&s, key, old))
}

@(test)
test_worker_shutdown_and_accounting :: proc(t: ^testing.T) {
	tracker: mem.Tracking_Allocator
	mem.tracking_allocator_init(&tracker, context.allocator)
	defer mem.tracking_allocator_destroy(&tracker)
	context.allocator = mem.tracking_allocator(&tracker)
	for pass in 0 ..< 4 {
		w: World
		world_init(&w, 42)
		world_apply(&w, {1, {0, 64, 0}, .Stone})
		s: Stream
		r: Renderer
		stream_init(&s, 2, 1, 8 * MiB, 128 * MiB, MiB, &tracker)
		stream_reconcile(&s, &r, {0, 70, 0})
		stream_schedule(&s, &w, {0, 70, 0})
		if pass % 2 == 0 {
			time.sleep(10 * time.Millisecond)
		}
		stream_destroy(&s, &r)
		world_destroy(&w)
	}
	sync.mutex_lock(&tracker.mutex)
	remaining := tracker.current_memory_allocated
	sync.mutex_unlock(&tracker.mutex)
	testing.expect(t, remaining == 0, fmt.tprintf("leaked %d bytes", remaining))
}

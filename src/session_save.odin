package sandbox

import "base:runtime"
import "core:os"
import "core:sync"
import "core:thread"

AUTOSAVE_SECONDS :: 60
Save_State :: struct {
	handle: ^thread.Thread,
	mutex: sync.Mutex,
	data: []u8,
	path: string,
	done, succeeded, requested, failed: bool,
	sequence, saved_sequence: u64,
	position, saved_position: V3,
	next_auto, feedback_until: f64,
}

// The writer owns an immutable snapshot. It never reads the live world or GL.
save_writer :: proc(t: ^thread.Thread) {
	defer runtime.default_temp_allocator_destroy(auto_cast context.temp_allocator.data)
	s := cast(^Save_State)t.data
	os.make_directory_all(os.dir(s.path))
	ok := world_write(s.data, s.path)
	delete(s.data)
	sync.mutex_lock(&s.mutex)
	s.succeeded = ok; s.done = true
	sync.mutex_unlock(&s.mutex)
}

save_collect :: proc(s: ^Save_State, now: f64, wait: bool = false) {
	if s.handle == nil {return}
	sync.mutex_lock(&s.mutex); done := s.done; sync.mutex_unlock(&s.mutex)
	if !done && !wait {return}
	thread.join(s.handle); thread.destroy(s.handle); s.handle = nil
	s.failed = !s.succeeded
	if s.succeeded {s.saved_sequence = s.sequence; s.saved_position = s.position}
	s.feedback_until = now + (s.failed ? 10 : 3)
}

session_update_save :: proc(a: ^App) {
	if a.config.no_save {return}
	s := &a.save
	save_collect(s, a.now)
	if s.next_auto == 0 {s.next_auto = a.now + AUTOSAVE_SECONDS}
	if a.now >= s.next_auto {
		s.next_auto = a.now + AUTOSAVE_SECONDS
		if a.world.sequence != s.saved_sequence || a.position != s.saved_position {s.requested = true}
	}
	// At most one snapshot and writer; repeated requests coalesce to the latest
	// world after the current write finishes. The 4 MiB save reserve covers it.
	if s.requested && s.handle == nil {
		s.requested = false; s.done = false; s.failed = false
		s.path = a.config.save_path; s.sequence = a.world.sequence; s.position = a.position
		s.data = world_encode(&a.world, a.position)
		s.handle = thread.create(save_writer); s.handle.data = s; s.handle.init_context = context
		thread.start(s.handle)
	}
}

session_finish_save :: proc(a: ^App) {
	if a.config.no_save {return}
	save_collect(&a.save, a.now, true)
	// Finish pending work before the final snapshot so an old writer can never
	// replace the final position or edits during shutdown.
	os.make_directory_all(os.dir(a.config.save_path))
	a.save.failed = !world_save(&a.world, a.position, a.config.save_path)
	if a.save.failed {os.write_string(os.stderr, "Final world save failed\n")}
}

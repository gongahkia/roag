package sandbox

import "base:runtime"
import "core:sync"
import "core:thread"

Worker :: struct {
	mutex:             sync.Mutex,
	wake:              sync.Sema,
	handle:            ^thread.Thread,
	job:               Mesh_Job,
	result:            Mesh_Result,
	busy, ready, stop: bool,
	upload:            GPU_Mesh,
	uploaded:          int,
}

worker_main :: proc(t: ^thread.Thread) {
	defer runtime.default_temp_allocator_destroy(auto_cast context.temp_allocator.data)
	w := cast(^Worker)t.data
	for {
		sync.sema_wait(&w.wake)
		sync.mutex_lock(&w.mutex)
		stop := w.stop
		job := w.job
		sync.mutex_unlock(&w.mutex)
		if stop {
			break
		}
		result := build_job(job)
		delete(job.edits)
		sync.mutex_lock(&w.mutex)
		w.result = result
		w.ready = true
		sync.mutex_unlock(&w.mutex)
	}
}

worker_start :: proc(w: ^Worker) {
	w.handle = thread.create(worker_main)
	w.handle.data = w
	w.handle.init_context = context
	thread.start(w.handle)
}

worker_stop :: proc(w: ^Worker) {
	sync.mutex_lock(&w.mutex)
	w.stop = true
	sync.mutex_unlock(&w.mutex)
	sync.sema_post(&w.wake)
}

// All workers receive stop requests before any are joined. Upload handles remain
// main-thread-owned and are released while the graphics context is still alive.
worker_join_release :: proc(w: ^Worker, r: ^Renderer) {
	thread.join(w.handle)
	thread.destroy(w.handle)
	if w.ready {
		mesh_destroy(&w.result.mesh)
	} else if w.busy {
		delete(w.job.edits)
	}
	mesh_release(r, &w.upload)
}

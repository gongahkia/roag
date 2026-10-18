package sandbox

import "core:math"
import "core:os"
import "core:slice"
import "core:sync"

Process_Sample :: struct {
	cpu_seconds:                            f64,
	rss_bytes, peak_rss_bytes, page_faults: u64,
	cpu_valid, ram_valid:                   bool,
}


HISTORY_CAPACITY :: 240
SAMPLE_INTERVAL :: 0.25
History :: struct {
	values:      [HISTORY_CAPACITY]f64,
	next, count: int,
}
Frame_Stats :: struct {
	mean_ms, min_ms, max_ms, p50_ms, p95_ms, p99_ms, fps, low_1pct_fps: f64,
}
Telemetry :: struct {
	frame_history, gpu_history, cpu_history, ram_history:                      History,
	frames:                                                                    Frame_Stats,
	process, previous_process:                                                 Process_Sample,
	last_sample, previous_cpu_wall:                                            f64,
	logical_cores, frame_count, previous_cpu_frame, previous_generated:        int,
	cpu_percent, cpu_cores, cpu_ms_per_frame, chunks_per_second, main_work_ms: f64,
	heap_bytes, heap_peak_bytes, resident, ready, queued, busy, mesh_chunks:   int,
	slow_33ms, slow_50ms:                                                      int,
	worst_frame_ms:                                                            f64,
	cpu_interval_valid:                                                        bool,
}

history_push :: proc(h: ^History, v: f64) {
	if math.is_nan(v) || math.is_inf(v) {return}
	h.values[h.next] = v; h.next = (h.next + 1) % HISTORY_CAPACITY
	h.count = min(h.count + 1, HISTORY_CAPACITY)
}

history_at :: proc(h: ^History, index: int) -> f64 {
	assert(index >= 0 && index < h.count)
	return h.values[(h.next - h.count + index + HISTORY_CAPACITY) % HISTORY_CAPACITY]
}

frame_statistics :: proc(h: ^History) -> Frame_Stats {
	if h.count == 0 {return {}}
	sorted: [HISTORY_CAPACITY]f64
	sum: f64
	for i in 0 ..< h.count {sorted[i] = history_at(h, i); sum += sorted[i]}
	slice.sort(sorted[:h.count])
	n := h.count
	worst_count := max(1, int(math.ceil(f64(n) * 0.01)))
	worst_total: f64
	for i in n - worst_count ..< n {worst_total += sorted[i]}
	mean := sum / f64(n)
	return {
		mean_ms = mean,
		min_ms = sorted[0],
		max_ms = sorted[n - 1],
		p50_ms = sorted[int(f64(n - 1) * 0.50)],
		p95_ms = sorted[int(f64(n - 1) * 0.95)],
		p99_ms = sorted[int(f64(n - 1) * 0.99)],
		fps = mean > 0 ? 1000 / mean : 0,
		low_1pct_fps = worst_total > 0 ? 1000 * f64(worst_count) / worst_total : 0,
	}
}

normalized_cpu :: proc(cpu_delta, wall_delta: f64, cores: int) -> f64 {
	if wall_delta <= 0 || cpu_delta < 0 || cores <= 0 {return 0}
	return clamp(100 * cpu_delta / wall_delta / f64(cores), 0, 100)
}

telemetry_init :: proc(t: ^Telemetry, now: f64) {
	t.logical_cores = max(1, os.get_processor_core_count())
	t.last_sample = now; t.previous_cpu_wall = now
	t.process = process_sample(); t.previous_process = t.process
	if t.process.ram_valid {history_push(&t.ram_history, f64(t.process.rss_bytes) / MiB)}
}

// Expensive OS queries, percentiles, and registry scans run at 4 Hz, including
// when F3 is hidden. Histories have fixed storage and never allocate per frame.
telemetry_update :: proc(t: ^Telemetry, s: ^Stream, r: ^Renderer, now, frame_ms: f64) {
	if t.frame_count > 0 && frame_ms > 0 {
		history_push(&t.frame_history, frame_ms)
		if r.gpu_samples > 0 {history_push(&t.gpu_history, r.gpu_ms)}
		t.worst_frame_ms = max(t.worst_frame_ms, frame_ms)
		if frame_ms > 1000.0 / 30 {t.slow_33ms += 1}
		if frame_ms > 50 {t.slow_50ms += 1}
	}
	t.frame_count += 1
	if now - t.last_sample < SAMPLE_INTERVAL && t.frames.fps > 0 {return}
	interval := now - t.last_sample
	t.frames = frame_statistics(&t.frame_history)
	current := process_sample()
	if current.cpu_valid && t.previous_process.cpu_valid && now > t.previous_cpu_wall {
		delta := current.cpu_seconds - t.previous_process.cpu_seconds
		wall := now - t.previous_cpu_wall
		t.cpu_percent = normalized_cpu(delta, wall, t.logical_cores)
		t.cpu_cores = max(0, delta / wall)
		count := t.frame_count - t.previous_cpu_frame
		t.cpu_ms_per_frame = count > 0 ? max(0, delta) * 1000 / f64(count) : 0
		t.cpu_interval_valid = true
		history_push(&t.cpu_history, t.cpu_percent)
	}
	if current.cpu_valid {
		t.previous_process =
			current; t.previous_cpu_wall = now; t.previous_cpu_frame = t.frame_count
	}
	if current.ram_valid {history_push(&t.ram_history, f64(current.rss_bytes) / MiB)}
	current.peak_rss_bytes = max(t.process.peak_rss_bytes, current.peak_rss_bytes)
	t.cpu_interval_valid = t.cpu_interval_valid && current.cpu_valid
	t.process = current
	sync.mutex_lock(&s.tracker.mutex)
	t.heap_bytes = int(s.tracker.current_memory_allocated)
	t.heap_peak_bytes = int(s.tracker.peak_memory_allocated)
	sync.mutex_unlock(&s.tracker.mutex)
	t.resident = len(s.chunks); t.ready = 0; t.queued = 0; t.busy = 0; t.mesh_chunks = 0
	for _, c in s.chunks {
		if c.complete {t.ready += 1}
		if !c.complete && !c.running && !c.budget_deferred {t.queued += 1}
		if c.gpu.vao != 0 {t.mesh_chunks += 1}
	}
	for i in 0 ..< s.worker_count {if s.workers[i].busy {t.busy += 1}}
	if interval > 0 {t.chunks_per_second = f64(s.generated - t.previous_generated) / interval}
	t.previous_generated = s.generated; t.last_sample = now
}

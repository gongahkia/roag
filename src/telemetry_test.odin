package sandbox

import "core:math"
import "core:testing"

@(test)
test_telemetry_ring_wrap_and_invalid_values :: proc(t: ^testing.T) {
	history: History
	for i in 0 ..< 280 {history_push(&history, f64(i))}
	testing.expect(t, history.count == HISTORY_CAPACITY)
	testing.expect(t, history_at(&history, 0) == 40)
	testing.expect(t, history_at(&history, 239) == 279)
	history_push(&history, math.inf_f64(1))
	testing.expect(t, history.count == 240 && history_at(&history, 239) == 279)
}

@(test)
test_frame_statistics_and_one_percent_low :: proc(t: ^testing.T) {
	history: History
	for _ in 0 ..< 99 {history_push(&history, 10)}
	history_push(&history, 100)
	stats := frame_statistics(&history)
	testing.expect(t, abs(stats.mean_ms - 10.9) < 0.00001)
	testing.expect(t, abs(stats.fps - 1000 / 10.9) < 0.00001)
	testing.expect(t, stats.low_1pct_fps == 10)
	testing.expect(t, stats.min_ms == 10 && stats.max_ms == 100)
	testing.expect(t, stats.p50_ms == 10 && stats.p95_ms == 10 && stats.p99_ms == 10)
	empty: History
	testing.expect(t, frame_statistics(&empty).fps == 0)
}

@(test)
test_cpu_utilization_uses_all_logical_cores :: proc(t: ^testing.T) {
	testing.expect(t, normalized_cpu(0.25, 0.5, 4) == 12.5)
	testing.expect(t, normalized_cpu(2, 0.5, 4) == 100)
	testing.expect(t, normalized_cpu(-1, 0.5, 4) == 0)
	testing.expect(t, normalized_cpu(1, 0, 4) == 0)
	testing.expect(t, normalized_cpu(1, 1, 0) == 0)
}

@(test)
test_native_process_measurements :: proc(t: ^testing.T) {
	sample := process_sample()
	testing.expect(t, sample.cpu_valid && sample.cpu_seconds >= 0)
	testing.expect(t, sample.ram_valid && sample.rss_bytes > 0)
	testing.expect(t, sample.peak_rss_bytes >= sample.rss_bytes)
}

@(test)
test_completed_frame_measurement_includes_pacing_and_swap :: proc(t: ^testing.T) {
	stats: Telemetry
	r := Renderer {
		gpu_ms      = 0.01,
		gpu_samples = 1,
	}
	// A fast GPU doesn't turn two completed 60 Hz frames into 100,000 FPS.
	telemetry_record_frame(&stats, &r, 1000.0 / 60)
	telemetry_record_frame(&stats, &r, 1000.0 / 60)
	testing.expect(t, stats.frame_count == 2 && stats.frame_history.count == 2)
	testing.expect(t, abs(frame_statistics(&stats.frame_history).fps - 60) < 0.0001)
}

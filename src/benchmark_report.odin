package sandbox

import "core:fmt"
import "core:os"

benchmark_start :: proc(a: ^App) {
	if a.config.benchmark != "" {
		os.make_directory("benchmarks")
		path := fmt.tprintf(
			"benchmarks/%s%s.csv",
			a.config.benchmark,
			a.config.naive_mesh ? "-naive" : "",
		)
		handle, err := os.create(path)
		if err != nil {
			fmt.eprintln("Cannot write benchmark:", err)
			os.exit(1)
		}
		a.benchmark_file = handle
		fmt.fprintln(
			a.benchmark_file,
			"frame,elapsed_s,frame_ms,p95_ms,p99_ms,gpu_ms,cpu_bytes,mesh_bytes,graphics_reserved_bytes,chunks,radius,draws,triangles,generation_ms,meshing_ms,pressure,stale,upload_bytes,gpu_budget_bytes,cpu_budget_bytes,upload_budget_bytes,fps,low_1pct_fps,process_cpu_percent,rss_bytes,rss_peak_bytes,cpu_sample_valid,ram_sample_valid,heap_peak_bytes,main_work_ms,chunks_per_second,queued_jobs,busy_workers,display_hz,frame_cap,swap_ms,pacing_wait_ms,software_renderer,fluid_queue,fluid_updates,fluid_changes,fluid_overflow,fluid_refused,fluid_ms",
		)
		fmt.println("Benchmark:", path)
	}
}

benchmark_record :: proc(a: ^App, ms: f64) {
	if a.benchmark_file != nil {
		fmt.fprintf(
			a.benchmark_file,
			"%d,%.4f,%.4f,%.4f,%.4f,%.4f,%d,%d,%d,%d,%d,%d,%d,%.4f,%.4f,%d,%d,%d,%d,%d,%d,%.4f,%.4f,%.4f,%d,%d,%d,%d,%d,%.4f,%.4f,%d,%d,%d,%d,%.4f,%.4f,%d,%d,%d,%d,%d,%d,%.4f\n",
			a.frame,
			a.pacing.completed_at - a.start,
			ms,
			a.telemetry.frames.p95_ms,
			a.telemetry.frames.p99_ms,
			a.renderer.gpu_ms,
			cpu_bytes(&a.stream),
			a.renderer.mesh_bytes,
			graphics_reserved_bytes(a.renderer.mesh_bytes, int(a.width), int(a.height)),
			len(a.stream.chunks),
			a.stream.radius,
			a.renderer.draw_calls,
			a.renderer.triangles,
			a.stream.last_generation_ms,
			a.stream.last_meshing_ms,
			a.stream.pressure,
			a.stream.stale,
			a.stream.last_upload_bytes,
			a.config.gpu_budget,
			a.config.cpu_budget,
			a.stream.upload_budget,
			a.telemetry.frames.fps,
			a.telemetry.frames.low_1pct_fps,
			a.telemetry.cpu_percent,
			a.telemetry.process.rss_bytes,
			a.telemetry.process.peak_rss_bytes,
			int(a.telemetry.cpu_interval_valid && a.telemetry.process.cpu_valid),
			int(a.telemetry.process.ram_valid),
			a.telemetry.heap_peak_bytes,
			a.telemetry.main_work_ms,
			a.telemetry.chunks_per_second,
			a.telemetry.queued,
			a.telemetry.busy,
			a.pacing.display_hz,
			frame_target(a.settings, a.pacing.display_hz),
			a.pacing.swap_ms,
			a.pacing.wait_ms,
			int(a.pacing.software_renderer),
			a.fluids.count,
			a.fluids.updates,
			a.fluids.changed_count,
			a.fluids.overflow,
			a.fluids.refused,
			a.fluids.last_ms,
		)
	}
}

package sandbox

import "core:fmt"
import "core:mem"
import "core:os"
import "core:time"

// Headless matched-coordinate benchmark. Both versions sample exactly the same
// padded chunks, alternating order between repeats. No display or GPU required.
benchmark_generation :: proc(c: Config) {
	keys: [40]I3
	count := 0
	for i in 0 ..< len(BIOME_NAMES) {
		p, ok := terrain_find_landscape(c.seed, Biome(i), 5)
		if !ok {fmt.eprintf("Missing geography fixture %s\n", BIOME_NAMES[i]); os.exit(2)}
		p.y = geography_column(c.seed, p.x, p.z).height
		keys[count] = chunk_of(p); count += 1
		keys[count] = chunk_of(p + I3{0, 32, 0}); count += 1
	}
	for lake in ([2]bool{false, true}) {
		p, ok := terrain_find_river(c.seed, lake)
		if ok {p.y = geography_column(c.seed, p.x, p.z).height
			keys[count] = chunk_of(p); count += 1}
	}
	os.make_directory_all("benchmarks")
	file, err := os.create("benchmarks/generation.csv")
	if err != nil {fmt.eprintln("Cannot write generation benchmark"); os.exit(1)}
	defer os.close(file)
	fmt.fprintln(
		file,
		"fixture,repeat,generator,x,y,z,generation_ms,meshing_ms,vertices,indices,worker_heap_bytes,digest",
	)
	tracker: mem.Tracking_Allocator
	mem.tracking_allocator_init(&tracker, context.allocator)
	context.allocator = mem.tracking_allocator(&tracker)
	defer mem.tracking_allocator_destroy(&tracker)
	data := make([]u8, PAD * PAD * PAD); defer delete(data)
	repeats := clamp(c.frames == 0 ? 3 : c.frames, 1, 20)
	for repeat in 0 ..< repeats + 1 {
		for pass in 0 ..< 2 {
			version := u32(4 + ((pass + repeat) % 2))
			for key, i in keys[:count] {
				start := time.tick_now()
				terrain_fill_chunk(c.seed, key, version, data)
				generation_ms := time.duration_milliseconds(time.tick_since(start))
				start = time.tick_now()
				mesh := mesh_build(data)
				meshing_ms := time.duration_milliseconds(time.tick_since(start))
				if repeat > 0 {
					digest := u64(14695981039346656037)
					for b in data {digest = (digest ~ u64(b)) * 1099511628211}
					fmt.fprintf(
						file,
						"%d,%d,%d,%d,%d,%d,%.6f,%.6f,%d,%d,%d,%d\n",
						i,
						repeat,
						version,
						key.x,
						key.y,
						key.z,
						generation_ms,
						meshing_ms,
						len(mesh.vertices),
						len(mesh.indices),
						tracker.current_memory_allocated,
						digest,
					)
				}
				mesh_destroy(&mesh)
			}
		}
	}
	fmt.printf(
		"Generation comparison: %d fixtures x %d repeats x generators 4/5; peak worker heap %.3f MiB\n",
		count,
		repeats,
		f64(tracker.peak_memory_allocated) / MiB,
	)
}

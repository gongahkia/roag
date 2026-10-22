package sandbox

import "core:fmt"
import "core:mem"
import "core:os"
import "core:time"

benchmark_lighting :: proc(c: Config) {
	keys: [40]I3
	count := 0
	for i in 0 ..< len(BIOME_NAMES) {
		p, ok := terrain_find_landscape(c.seed, Biome(i), 5)
		if !ok {os.exit(2)}
		p.y = geography_column(c.seed, p.x, p.z).height
		keys[count] = chunk_of(p); count += 1
		p.y = max(0, p.y - 32)
		keys[count] = chunk_of(p); count += 1
	}
	keys[count] = {0, 0, 0}; count += 1
	os.make_directory_all("benchmarks")
	f, err := os.create("benchmarks/lighting.csv")
	if err != nil {os.exit(1)}
	defer os.close(f)
	fmt.fprintln(
		f,
		"fixture,repeat,lit,x,y,z,generation_ms,lighting_ms,meshing_ms,total_ms,vertices,indices,peak_worker_bytes,digest",
	)
	tracker: mem.Tracking_Allocator
	mem.tracking_allocator_init(&tracker, context.allocator)
	defer mem.tracking_allocator_destroy(&tracker)
	context.allocator = mem.tracking_allocator(&tracker)
	repeats := clamp(c.frames == 0 ? 3 : c.frames, 1, 20)
	for repeat in 0 ..= repeats {
		for pass in 0 ..< 2 {
			lit := (pass + repeat) % 2 == 0
			for key, i in keys[:count] {
				tracker.peak_memory_allocated = 0
				start := time.tick_now()
				result := build_job({key = key, seed = c.seed, generator = 5, unlit = !lit, fast_light = c.lighting == .Fast})
				elapsed := time.duration_milliseconds(time.tick_since(start))
				digest := u64(14695981039346656037)
				for v in result.mesh.vertices {digest = (digest ~ u64(v.x) ~ (u64(v.y) << 16) ~ (u64(v.z) << 32) ~ (u64(v.data) << 48)) * 1099511628211}
				if repeat > 0 {
					fmt.fprintf(
						f,
						"%d,%d,%d,%d,%d,%d,%.6f,%.6f,%.6f,%.6f,%d,%d,%d,%d\n",
						i,
						repeat,
						int(lit),
						key.x,
						key.y,
						key.z,
						result.generation_ms,
						result.lighting_ms,
						result.meshing_ms,
						elapsed,
						len(result.mesh.vertices),
						len(result.mesh.indices),
						tracker.peak_memory_allocated,
						digest,
					)
				}
				mesh_destroy(&result.mesh)
				assert(tracker.current_memory_allocated == 0)
			}
		}
	}
	fmt.printf("Lighting comparison: %d fixtures x %d repeats x lit/unlit\n", count, repeats)
}

benchmark_light_room :: proc(a: ^App) {
	// Roof straddles x=32. The right half receives sun through a skylight;
	// the left half is enclosed and illuminated by a lamp against the back wall.
	for z: i32 = 0;
	    z <= 18;
	    z += 1 {for y: i32 = 88; y <= 98; y += 1 {for x: i32 = 20; x <= 44; x += 1 {
				b := Block.Air
				if y == 88 || y == 98 || x == 20 || x == 44 || z == 0 || z == 18 {b = .Stone}
				if y == 98 && x >= 37 && x <= 41 && z >= 4 && z <= 8 {b = .Air}
				if x == 25 && y == 91 && z == 1 {b = .Lamp}
				world_apply(&a.world, {a.world.sequence + 1, {x, y, z}, b})
			}}}
	a.benchmark_view = true
	a.benchmark_camera = {32, 92, 16}
	a.benchmark_target = {32, 91, 1}
}

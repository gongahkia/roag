package sandbox

import "core:fmt"
import "core:sync"
import "core:testing"
import "core:time"

@(test)
test_v3_benchmark_flight_repeats_all_coordinates :: proc(t: ^testing.T) {
	a := new(App); defer free(a)
	world_init(&a.world, 42); defer world_destroy(&a.world)
	a.config.benchmark = "flight"
	for second in 0 ..< 24 {
		a.elapsed = 2 + f64(second) + 0.25
		benchmark_step(a)
		first := a.position
		a.elapsed += 24
		benchmark_step(a)
		testing.expect(t, a.position == first)
	}
}

@(test)
test_fixed_noise_derivatives_and_coordinate_extremes :: proc(t: ^testing.T) {
	for i: i64 = -7; i <= 7; i += 1 {
		x, z := i * TQ / 3 + 1234, i * TQ / 5 + 777
		n := noise_d2(42, x, z)
		eps :: i64(256)
		dx := (noise_d2(42, x + eps, z).value - noise_d2(42, x - eps, z).value) * TQ / (2 * eps)
		dz := (noise_d2(42, x, z + eps).value - noise_d2(42, x, z - eps).value) * TQ / (2 * eps)
		testing.expect(t, abs(dx - n.dx) < 1024 && abs(dz - n.dz) < 1024)
		testing.expect(t, n.value >= 0 && n.value <= TQ)
	}
	extremes := []I3 {
		{-100000000, 0, 100000000},
		{100000000, 0, -100000000},
		{-1, 0, -1},
		{0, 0, 0},
	}
	for p in extremes {
		c := terrain_column_v3(0xffffffffffffffff, p.x, p.z)
		testing.expect(t, c.height >= 18 && c.height <= 189)
	}
}

@(test)
test_v3_landscapes_islands_and_grounded_landmarks :: proc(t: ^testing.T) {
	counts: [int(Biome.Sky_Islands) + 1]int
	forms: [3]int
	for z: i32 = -4096; z <= 4096; z += 64 {for x: i32 = -4096; x <= 4096; x += 64 {
			c := terrain_column_v3(42, x, z)
			counts[int(c.biome)] += 1
			if c.island_bottom >= 0 {testing.expect(t, c.island_bottom > c.height + 24)}
			testing.expect(t, c.height >= 18 && c.height <= 189)
			if c.form_top >= 0 {
				h := hash3(42 + 3201, floor_div(x, 128), 0, floor_div(z, 128))
				forms[(h >> 32) % 3] += 1
				testing.expect(t, c.form_top < 240)
			}
		}}
	for count in counts {testing.expect(t, count > 0, fmt.tprintf("missing biome: %v", counts))}
	for count in forms {testing.expect(t, count > 0, fmt.tprintf("missing landmark: %v", forms))}
	p, found := terrain_find_island(42)
	testing.expect(t, found)
	if found {
		c := terrain_column_v3(42, p.x, p.z)
		testing.expect(
			t,
			c.biome == .Sky_Islands && c.island_bottom > c.height + 24 && c.island_top < 234,
		)
		w: World; world_init(&w, 42); w.generator = 3; defer world_destroy(&w)
		testing.expect(t, world_get(&w, {p.x, c.island_top, p.z}) == .Grass)
		testing.expect(t, world_get(&w, {p.x, c.island_bottom - 8, p.z}) == .Air)
		testing.expect(t, is_solid(world_get(&w, {p.x, c.height, p.z})))
	}
	fmt.printf("V3 biome samples %v / landmarks %v / island %v\n", counts, forms, p)
}

@(test)
test_v3_spawn_256_seeds_and_fallback :: proc(t: ^testing.T) {
	fallbacks := 0
	for seed: u64 = 0; seed < 256; seed += 1 {
		w: World; world_init(&w, seed); w.generator = 3
		p := world_spawn(&w)
		testing.expect(
			t,
			p.y > SEA_LEVEL + PLAYER_EYE && !player_collides(&w, p),
			fmt.tprintf("seed %d spawn %v", seed, p),
		)
		probe := p; probe.y -= 0.01
		testing.expect(t, player_collides(&w, probe))
		testing.expect(t, w.edit_count == 0 || w.edit_count == 36)
		if w.edit_count > 0 {fallbacks += 1}
		world_destroy(&w)
	}
	fmt.printf("V3 spawn fallback platforms %d / 256 seeds\n", fallbacks)
	w: World; world_init(&w, 42); w.generator = 3; defer world_destroy(&w)
	p := world_spawn_fallback(&w)
	testing.expect(t, w.edit_count == 36 && !player_collides(&w, p))
	probe := p; probe.y -= 0.01
	testing.expect(t, player_collides(&w, probe))
}

@(test)
test_v3_padded_worker_queries_and_visitation_order :: proc(t: ^testing.T) {
	w: World; world_init(&w, 42); w.generator = 3; defer world_destroy(&w)
	island, found := terrain_find_island(42); testing.expect(t, found)
	lake, wet := terrain_find_lake(42); testing.expect(t, wet)
	keys := []I3 {
		{-1, 1, -1},
		{0, 1, 0},
		{1, 2, -1},
		chunk_of(island),
		chunk_of(lake.center),
		{3124999, 2, -3125000},
	}
	data := make([]u8, PAD * PAD * PAD); defer delete(data)
	digests: [6]u64
	for pass in 0 ..< 2 {
		for j in 0 ..< len(keys) {
			i := pass == 0 ? j : len(keys) - 1 - j
			key := keys[i]
			terrain_fill_chunk(42, key, 3, data)
			digest := u64(14695981039346656037)
			for b in data {digest = (digest ~ u64(b)) * 1099511628211}
			if pass == 0 {digests[i] = digest} else {testing.expect(t, digests[i] == digest)}
			for z in 0 ..< PAD {for x in 0 ..< PAD {for y in 0 ..< PAD {
						p := key * 32 + I3{i32(x) - 1, i32(y) - 1, i32(z) - 1}
						expected := world_get(&w, p)
						actual := Block(data[block_index(x, y, z)])
						if actual != expected {
							testing.expect(
								t,
								false,
								fmt.tprintf(
									"worker/query mismatch at %v: %v != %v",
									p,
									actual,
									expected,
								),
							)
							return
						}
					}}}
		}
	}
	fmt.printf("V3 chunk digests %v\n", digests)
	// These bytes are checked by every native CI target; no platform-specific
	// expected values or floating point tolerances are permitted for block IDs.
	expected := [6]u64 {
		9836086169243759077,
		9836086169243759077,
		2006285744477976517,
		8285797756327207706,
		14686864127645864856,
		7936854463243241457,
	}
	testing.expect(t, digests == expected)
}

@(test)
test_v3_formation_edges_and_shared_cave_ports :: proc(t: ^testing.T) {
	w: World; world_init(&w, 42); w.generator = 3; defer world_destroy(&w)
	data := make([]u8, PAD * PAD * PAD); defer delete(data)
	seen: [3]bool
	for gz: i32 = -16; gz <= 16; gz += 1 {for gx: i32 = -16; gx <= 16; gx += 1 {
			f := ground_form(42, gx, gz)
			if !f.active || seen[f.kind] {continue}
			seen[f.kind] = true
			key := chunk_of({f.x - 24, f.base + f.height / 2, f.z})
			terrain_fill_chunk(42, key, 3, data)
			for z in 0 ..< PAD {for x in 0 ..< PAD {for y in 0 ..< PAD {
						p := key * 32 + I3{i32(x) - 1, i32(y) - 1, i32(z) - 1}
						testing.expect(t, Block(data[block_index(x, y, z)]) == world_get(&w, p))
					}}}
		}}
	for present in seen {testing.expect(t, present)}
	island, found := terrain_find_island(42); testing.expect(t, found)
	// Compare cached/uncached spans at the entire island perimeter, where a
	// formation can cross from the sky biome into its blended boundary.
	for z := island.z - 48; z <= island.z + 48; z += 3 {
		for x := island.x - 48; x <= island.x + 48; x += 3 {
			c := terrain_column_v3(42, x, z)
			if c.island_top < 0 {continue}
			p := I3{x, c.island_top, z}
			testing.expect(t, world_get(&w, p) == generated_block(42, p, 3))
		}
	}
	ports := 0
	for z: i32 = -12; z <= 12; z += 1 {for x: i32 = -12; x <= 12; x += 1 {
			cell := cave_cell_v3(42, {x, 0, z})
			for axis in 0 ..< 3 {
				key := cell.key; key[axis] += 1
				other := cave_cell_v3(42, key)
				if other.count == 0 {continue}
				for i := 1; i < cell.count; i += 2 {
					p := cell.segments[i].b
					if p[axis] != key[axis] * 64 || p.y <= 4 {continue}
					matched := false
					for j := 1; j < other.count; j += 2 {
						if other.segments[j].b == p {matched = true; break}
					}
					testing.expect(t, matched)
					testing.expect(t, !is_solid(world_get(&w, p)))
					ports += 1
				}
			}
		}}
	testing.expect(t, ports >= 16)
	fmt.printf("V3 shared cave ports checked %d\n", ports)
}

@(test)
test_v3_island_collision_and_generated_lake_swimming :: proc(t: ^testing.T) {
	w: World; world_init(&w, 42); w.generator = 3; defer world_destroy(&w)
	island, found := terrain_find_island(42); testing.expect(t, found)
	eye := V3{f64(island.x) + 0.5, f64(island.y + 1) + PLAYER_EYE, f64(island.z) + 0.5}
	player: Player
	for _ in 0 ..< 120 {player_update(&player, &w, &eye, {}, 1.0 / 120.0)}
	testing.expect(t, player.grounded && abs(eye.y - (f64(island.y + 1) + PLAYER_EYE)) < 0.001)
	lake, wet := terrain_find_lake(42); testing.expect(t, wet)
	eye = {f64(lake.center.x) + 0.5, f64(lake.waterline), f64(lake.center.z) + 0.5}
	initial := eye.y
	player = {}
	for _ in 0 ..< 60 {player_update(&player, &w, &eye, {ascend = true, speed = 14}, 1.0 / 120.0)}
	testing.expect(t, eye.y > initial + 0.5 && !player_collides(&w, eye))
}

@(test)
test_v3_cave_connections_lake_containment_and_edits :: proc(t: ^testing.T) {
	lake, found := terrain_find_lake(42); testing.expect(t, found)
	if !found {return}
	w: World; world_init(&w, 42); w.generator = 3; defer world_destroy(&w)
	water := 0
	directions := []I3{{-1, 0, 0}, {1, 0, 0}, {0, -1, 0}, {0, 1, 0}, {0, 0, -1}, {0, 0, 1}}
	for z := lake.center.z - lake.radius.z; z <= lake.center.z + lake.radius.z; z += 1 {
		for x := lake.center.x - lake.radius.x; x <= lake.center.x + lake.radius.x; x += 1 {
			for y := lake.center.y - lake.radius.y; y <= lake.waterline; y += 1 {
				p := I3{x, y, z}
				if world_get(&w, p) != .Water {continue}
				water += 1
				for d in directions {
					if d.y > 0 && y == lake.waterline {continue}
					testing.expect(
						t,
						world_get(&w, p + d) != .Air,
						fmt.tprintf("lake leaked at %v", p + d),
					)
				}
			}
		}
	}
	testing.expect(t, water > 100)
	for axis in 0 ..< 3 {
		neighbor := lake.key; neighbor[axis] += 1
		n := cave_cell_v3(42, neighbor)
		if n.count == 0 {continue}
		for i in 0 ..< lake.count {for j in 0 ..< n.count {
				if lake.segments[i].b[axis] != neighbor[axis] * 64 ||
				   n.segments[j].b[axis] != neighbor[axis] * 64 {continue}
				testing.expect(t, lake.segments[i].b == n.segments[j].b)
			}}
	}
	// Opening a wall is a normal edit and seeds the existing bounded fluid queue.
	p := lake.center; p.y = lake.waterline
	for world_get(&w, p) == .Water {p.x += 1}
	testing.expect(t, is_solid(world_get(&w, p)))
	f: Fluid_Sim; fluids_init(&f); defer fluids_destroy(&f)
	fluids_reseed(&f, &w, {f64(p.x), f64(p.y), f64(p.z)}, 0)
	for dx: i32 = 0; dx <= 3; dx += 1 {
		q := p + I3{dx, 0, 0}; previous := world_get(&w, q)
		world_apply(&w, {w.sequence + 1, q, .Air}); fluids_notify(&f, &w, q, previous)
	}
	for tick: u64 = 0; tick < 100; tick += 1 {fluids_step(&f, &w, tick)}
	testing.expect(t, fluid_kind(world_get(&w, p)) == .Water)
	fmt.printf("V3 lake %v / water voxels %d\n", lake.center, water)
}

@(test)
test_v3_meshes_match_with_one_and_two_workers :: proc(t: ^testing.T) {
	jobs := [2]Mesh_Job {
		{key = {-1, 1, 0}, seed = 42, generator = 3},
		{key = {1, 1, -1}, seed = 42, generator = 3},
	}
	reference: [2]Mesh_Result
	for job, i in jobs {reference[i] = build_job(job)}
	defer for &r in reference {mesh_destroy(&r.mesh)}
	workers: [2]Worker
	for &w, i in workers {
		worker_start(&w)
		sync.mutex_lock(&w.mutex); w.job = jobs[i]; w.busy = true; sync.mutex_unlock(&w.mutex)
		sync.sema_post(&w.wake)
	}
	defer {
		for &w in workers {worker_stop(&w)}
		renderer: Renderer
		for &w in workers {worker_join_release(&w, &renderer)}
	}
	for &w, i in workers {
		ready := false
		for _ in 0 ..< 5000 {
			sync.mutex_lock(&w.mutex); ready = w.ready; sync.mutex_unlock(&w.mutex)
			if ready {break}
			time.sleep(time.Millisecond)
		}
		testing.expect(t, ready)
		if !ready {return}
		testing.expect(t, len(w.result.mesh.vertices) == len(reference[i].mesh.vertices))
		for v, j in reference[i].mesh.vertices {
			if j < len(w.result.mesh.vertices) {testing.expect(t, v == w.result.mesh.vertices[j])}
		}
	}
}

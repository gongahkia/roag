package sandbox

import "core:fmt"
import "core:sync"
import "core:testing"
import "core:time"

@(test)
test_v4_climate_biome_coverage_and_surfaces :: proc(t: ^testing.T) {
	counts: [len(BIOME_NAMES)]int
	for z: i32 = -4096; z <= 4096; z += 64 {for x: i32 = -4096; x <= 4096; x += 64 {
			c := terrain_base_v4(42, x, z)
			counts[int(c.biome)] += 1
			testing.expect(t, c.height >= 18 && c.height <= 211)
			testing.expect(
				t,
				c.temperature >= 0 && c.temperature <= TQ && c.moisture >= 0 && c.moisture <= TQ,
			)
		}}
	for count, i in counts {testing.expect(t, count > 0, fmt.tprintf("missing %s: %v", BIOME_NAMES[i], counts))}
	fmt.printf("V4 biome samples %v\n", counts)
	for biome in ([]Biome{.Snowy_Plains, .Taiga, .Snowy_Peaks, .Badlands, .Mushroom_Fields, .Savanna, .Jungle, .Swamp}) {
		p, ok := terrain_find_landscape(42, biome, 4); testing.expect(t, ok)
		if !ok {continue}
		c := terrain_base_v4(42, p.x, p.z)
		b := biome_surface(c, c.height)
		#partial switch biome {
		case .Snowy_Plains, .Taiga, .Snowy_Peaks:
			testing.expect(t, b == .Snow)
		case .Badlands:
			testing.expect(t, b >= .Terracotta && b <= .White_Terracotta)
		case .Mushroom_Fields:
			testing.expect(t, b == .Mycelium)
		case .Savanna:
			testing.expect(t, b == .Dry_Grass)
		case .Jungle:
			testing.expect(t, b == .Lush_Grass)
		case .Swamp:
			testing.expect(t, b == .Swamp_Grass)
		}
	}
}

@(test)
test_v4_all_biomes_worker_padding_and_versioned_caches :: proc(t: ^testing.T) {
	w: World; world_init(&w, 42); w.generator = 4; defer world_destroy(&w)
	data := make([]u8, PAD * PAD * PAD); defer delete(data)
	digests: [len(BIOME_NAMES)]u64
	for biome_index in 0 ..< len(BIOME_NAMES) {
		p, found := terrain_find_landscape(42, Biome(biome_index), 4)
		testing.expect(t, found, BIOME_NAMES[biome_index]); if !found {continue}
		key := chunk_of(p)
		terrain_fill_chunk(42, key, 4, data)
		digest := u64(14695981039346656037)
		for b in data {digest = (digest ~ u64(b)) * 1099511628211}
		digests[biome_index] = digest
		for z in 0 ..< PAD {for x in 0 ..< PAD {for y in 0 ..< PAD {
					q := key * 32 + I3{i32(x) - 1, i32(y) - 1, i32(z) - 1}
					actual, expected := Block(data[block_index(x, y, z)]), world_get(&w, q)
					if actual !=
					   expected {testing.expect(t, false, fmt.tprintf("%s seam %v: %v != %v", BIOME_NAMES[biome_index], q, actual, expected)); return}
				}}}
		// Reusing a World with another generator must not reuse climate caches.
		for version in ([]u32{3, 4, 3, 4}) {
			w.generator = version
			testing.expect(t, world_get(&w, p) == generated_block(42, p, version))
		}
	}
	fmt.printf("V4 golden digests %v\n", digests)
	expected := [len(BIOME_NAMES)]u64 {
		10162303833109508561,
		3139823846842602821,
		3809568365160953384,
		7786832479126974859,
		12671310754574651260,
		14124723925369866208,
		13523010218446874527,
		9768381346033739630,
		749159589035565899,
		17950656146294206981,
		7623239127512025159,
		8451502944563339136,
		6608910311678176012,
		2744374874488493900,
		6376777185734763144,
		12744152577531925890,
		15165773541676537787,
	}
	testing.expect(t, digests == expected)
}

@(test)
test_v4_vegetation_shapes_halos_and_spawn :: proc(t: ^testing.T) {
	seen: [9]bool
	for biome_index in 0 ..< len(BIOME_NAMES) {
		p, ok := terrain_find_landscape(42, Biome(biome_index), 4); if !ok {continue}
		for z := floor_div(p.z, 8) - 12;
		    z <= floor_div(p.z, 8) + 12;
		    z += 1 {for x := floor_div(p.x, 8) - 12; x <= floor_div(p.x, 8) + 12; x += 1 {
				plant := plant_at(42, x, z); if !plant.active {continue}
				if seen[int(plant.kind)] {continue}
				seen[int(plant.kind)] = true
				testing.expect(t, plant.height <= 14 && plant.h + plant.height + 2 < HEIGHT)
				// Enumerate the whole descriptor box, including its empty perimeter.
				for dy: i32 = 1;
				    dy <= 17;
				    dy += 1 {for dz: i32 = -5; dz <= 5; dz += 1 {for dx: i32 = -5; dx <= 5; dx += 1 {
							b := vegetation_block(
								{plant.x + dx, plant.h + dy, plant.z + dz},
								[]Plant{plant},
							)
							if abs(dx) == 5 ||
							   abs(dz) == 5 ||
							   dy > 16 {testing.expect(t, b == .Air)}
						}}}
				// One specimen of each kind suffices for the exhaustive bounds scan.
				break
			}}
	}
	for present, i in seen {testing.expect(t, present, fmt.tprintf("missing plant kind %d", i))}
	fallbacks := 0
	for seed: u64 = 0; seed < 128; seed += 1 {
		w: World; world_init(&w, seed); w.generator = 4
		p := world_spawn(&w)
		testing.expect(t, !player_collides(&w, p) && p.y > SEA_LEVEL + PLAYER_EYE)
		below := p; below.y -= 0.01
		testing.expect(t, player_collides(&w, below))
		if w.edit_count > 0 {fallbacks += 1}
		world_destroy(&w)
	}
	fmt.printf("V4 spawn fallbacks %d / 128 seeds\n", fallbacks)
}

@(test)
test_v4_six_bit_materials_ice_and_palette :: proc(t: ^testing.T) {
	data := make([]u8, PAD * PAD * PAD); defer delete(data)
	for b in CREATIVE_BLOCKS {
		data[block_index(2, 2, 2)] = u8(b)
		m := mesh_build(data)
		testing.expect(t, len(m.vertices) == 24)
		for v in m.vertices {
			expected := is_fluid(b) ? fluid_kind(b) : b
			testing.expect(t, Block((v.data >> 3) & VERTEX_MATERIAL_MASK) == expected)
			testing.expect(t, (v.data >> VERTEX_AO_SHIFT) & 3 == 3)
		}
		mesh_destroy(&m)
	}
	testing.expect(t, is_solid(.Ice) && !is_fluid(.Ice) && is_translucent(.Ice))
	data[block_index(2, 2, 2)] = u8(Block.Ice)
	data[block_index(3, 2, 2)] = u8(Block.Ice)
	m := mesh_build(data); defer mesh_destroy(&m)
	testing.expect(t, len(m.vertices) == 24 && m.water_indices == 36)
	data[block_index(3, 2, 2)] = u8(Block.Stone)
	n := mesh_build(data); defer mesh_destroy(&n)
	testing.expect(t, n.water_indices == 30 && len(n.indices) - n.water_indices == 36)
	testing.expect(t, CREATIVE_PAGES == 5 && len(CREATIVE_BLOCKS) == 42 && size_of(Vertex) == 8)
}

@(test)
test_v4_ice_extremes_and_tall_canopy_chunk_edges :: proc(t: ^testing.T) {
	w: World; world_init(&w, 42); w.generator = 4; defer world_destroy(&w)
	frozen := false
	for z: i32 = -1024; z <= 1024 && !frozen; z += 32 {for x: i32 = -1024; x <= 1024; x += 32 {
			c := terrain_base_v4(42, x, z)
			if c.height >= SEA_LEVEL - 1 || c.temperature >= TQ * 34 / 100 {continue}
			testing.expect(t, world_get(&w, {x, SEA_LEVEL, z}) == .Ice)
			testing.expect(t, world_get(&w, {x, SEA_LEVEL - 1, z}) == .Water)
			frozen = true; break
		}}
	testing.expect(t, frozen)
	for p in ([]I3{{-100000000, 50, 100000000}, {100000000, 50, -100000000}, {-1, 100, -1}}) {
		c := terrain_column(0xffffffffffffffff, p.x, p.z, 4)
		testing.expect(t, c.height >= 18 && c.height <= 211)
		testing.expect(t, block_valid(generated_block(0xffffffffffffffff, p, 4)))
	}
	data := make([]u8, PAD * PAD * PAD); defer delete(data)
	for biome in ([]Biome{.Jungle, .Cherry_Grove, .Taiga}) {
		p, ok := terrain_find_landscape(42, biome, 4); testing.expect(t, ok)
		checked := false
		for z := floor_div(p.z, 8) - 8;
		    z <= floor_div(p.z, 8) + 8 && !checked;
		    z += 1 {for x := floor_div(p.x, 8) - 8; x <= floor_div(p.x, 8) + 8; x += 1 {
				plant := plant_at(42, x, z); if !plant.active {continue}
				key := chunk_of({plant.x, plant.h + plant.height + 1, plant.z})
				terrain_fill_chunk(42, key, 4, data)
				for dz in 0 ..< PAD {for dx in 0 ..< PAD {for dy in 0 ..< PAD {
							q := key * 32 + I3{i32(dx) - 1, i32(dy) - 1, i32(dz) - 1}
							if Block(data[block_index(dx, dy, dz)]) !=
							   world_get(
								   &w,
								   q,
							   ) {testing.expect(t, false, fmt.tprintf("canopy seam %v", q)); return}
						}}}
				checked = true; break
			}}
		testing.expect(t, checked)
	}
}

@(test)
test_v4_meshes_match_between_concurrent_workers :: proc(
	t: ^testing.T,
) {check_concurrent_terrain_meshes(t, 4)}

check_concurrent_terrain_meshes :: proc(t: ^testing.T, generator: u32) {
	jobs: [2]Mesh_Job
	for biome, i in ([2]Biome{.Jungle, .Cherry_Grove}) {
		p, ok := terrain_find_landscape(42, biome, generator); testing.expect(t, ok)
		jobs[i] = {
			key       = chunk_of(p),
			seed      = 42,
			generator = generator,
		}
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
			if ready {break}; time.sleep(time.Millisecond)
		}
		testing.expect(t, ready); if !ready {return}
		testing.expect(
			t,
			len(w.result.mesh.vertices) == len(reference[i].mesh.vertices) &&
			len(w.result.mesh.indices) == len(reference[i].mesh.indices),
		)
		for v, j in reference[i].mesh.vertices {if j < len(w.result.mesh.vertices) {testing.expect(t, v == w.result.mesh.vertices[j])}}
		for v, j in reference[i].mesh.indices {if j < len(w.result.mesh.indices) {testing.expect(t, v == w.result.mesh.indices[j])}}
	}
}

@(test)
test_v4_vegetation_remains_face_connected :: proc(t: ^testing.T) {
	for kind: int = 0; kind < 9; kind += 1 {
		plant := Plant {
			kind   = Plant_Kind(kind),
			height = 10,
			h      = 60,
			active = true,
		}
		queue: [9 * 18 * 9]I3
		occupied, visited: [9 * 18 * 9]bool
		count, head, tail := 0, 0, 0
		for z: i32 = -4;
		    z <= 4;
		    z += 1 {for y: i32 = 1; y <= 18; y += 1 {for x: i32 = -4; x <= 4; x += 1 {
					p := I3{x, y, z}
					b := vegetation_block({x, 60 + y, z}, []Plant{plant})
					if b == .Air {continue}
					index := int(x + 4 + 9 * (y - 1 + 18 * (z + 4)))
					occupied[index] = true; count += 1
					if tail == 0 {queue[0] = p; visited[index] = true; tail = 1}
				}}}
		for head < tail {
			p := queue[head]; head += 1
			for n in NORMALS {
				q := p + n
				if abs(q.x) > 4 || abs(q.z) > 4 || q.y < 1 || q.y > 18 {continue}
				index := int(q.x + 4 + 9 * (q.y - 1 + 18 * (q.z + 4)))
				if !occupied[index] || visited[index] {continue}
				visited[index] = true; queue[tail] = q; tail += 1
			}
		}
		testing.expect(
			t,
			count > 0 && tail == count,
			fmt.tprintf("disconnected plant %d: %d/%d", kind, tail, count),
		)
	}
}

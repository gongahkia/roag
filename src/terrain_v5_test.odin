package sandbox

import "core:fmt"
import "core:testing"

@(test)
test_v5_biome_geography_distribution :: proc(t: ^testing.T) {
	counts, heights, roughness: [len(BIOME_NAMES)]i64
	for z: i32 = -4096; z <= 4096; z += 64 {for x: i32 = -4096; x <= 4096; x += 64 {
			c := geography_dry_column(42, x, z)
			counts[int(c.biome)] += 1; heights[int(c.biome)] += i64(c.height)
			roughness[int(c.biome)] += i64(
				abs(geography_dry_column(42, x + 2, z).height - c.height),
			)
			testing.expect(t, c.height >= 8 && c.height <= 228)
		}}
	means: [len(BIOME_NAMES)]i64
	for count, i in counts {
		testing.expect(t, count > 0, fmt.tprintf("missing v5 %s: %v", BIOME_NAMES[i], counts))
		means[i] = heights[i] * 100 / max(1, count)
	}
	testing.expect(t, means[int(Biome.Plains)] < means[int(Biome.Forest)])
	testing.expect(t, means[int(Biome.Birch_Forest)] < means[int(Biome.Jungle)])
	testing.expect(t, means[int(Biome.Swamp)] < means[int(Biome.Plains)])
	testing.expect(t, means[int(Biome.Snowy_Peaks)] > means[int(Biome.Forest)] + 2000)
	fmt.printf(
		"V5 biome counts %v / mean heights x100 %v / gradients %v\n",
		counts,
		means,
		roughness,
	)
	for seed in ([]u64{0, 42, 0xffffffffffffffff}) {for p in ([]I3{{-100000000, 50, 100000000}, {100000000, 120, -100000000}, {-1, 64, -1}}) {
			c := geography_column(seed, p.x, p.z)
			testing.expect(
				t,
				c.height >= 8 && c.height <= 228 && block_valid(generated_block(seed, p, 5)),
			)
		}}
}

@(test)
test_v5_all_biomes_padded_queries_and_golden_chunks :: proc(t: ^testing.T) {
	w: World; world_init(&w, 42); defer world_destroy(&w)
	data := make([]u8, PAD * PAD * PAD); defer delete(data)
	digests: [len(BIOME_NAMES)]u64
	for i in 0 ..< len(BIOME_NAMES) {
		p, ok := terrain_find_landscape(
			42,
			Biome(i),
			5,
		); testing.expect(t, ok, BIOME_NAMES[i]); if !ok {continue}
		p.y = geography_column(42, p.x, p.z).height
		key := chunk_of(p)
		terrain_fill_chunk(42, key, 5, data)
		digest := u64(14695981039346656037)
		for b in data {digest = (digest ~ u64(b)) * 1099511628211}
		digests[i] = digest
		for z in 0 ..< PAD {for x in 0 ..< PAD {for y in 0 ..< PAD {
					q := key * 32 + I3{i32(x) - 1, i32(y) - 1, i32(z) - 1}
					actual, expected := Block(data[block_index(x, y, z)]), world_get(&w, q)
					if actual !=
					   expected {testing.expect(t, false, fmt.tprintf("v5 %s padding at %v: %v != %v", BIOME_NAMES[i], q, actual, expected)); return}
				}}}
		for version in ([]u32{4, 5, 3, 5}) {
			w.generator = version
			testing.expect(t, world_get(&w, p) == generated_block(42, p, version))
		}
	}
	fmt.printf("V5 golden digests %v\n", digests)
	expected := [len(BIOME_NAMES)]u64 {
		10096591280673462559,
		13304158260861012539,
		10451022035058305963,
		3403611120663444,
		11159293630703837868,
		18086006992855410969,
		506461552626657295,
		8263712067721152265,
		12026828065221375174,
		17206187212722986746,
		16708736472258246468,
		10280853424301777972,
		3860286201332337373,
		13762340226583495058,
		9269088576987334022,
		4700247227029328093,
		12811463657307353019,
	}
	testing.expect(t, digests == expected)
}

@(test)
test_v5_drainage_downhill_continuity_and_order :: proc(t: ^testing.T) {
	seen := make(map[I3]River_Reach); defer delete(seen)
	checked := 0
	for z: i32 = -12; z <= 12; z += 1 {for x: i32 = -12; x <= 12; x += 1 {
			set := river_set_for(42, {x * 4, 0, z * 4})
			testing.expect(t, set.count <= 16)
			for r in set.reaches[:set.count] {
				old, exists := seen[r.a]
				if exists {testing.expect(t, r == old)} else {seen[r.a] = r}
				if r.lake {continue}
				a := drain_node(42, floor_div(r.a.x, DRAIN_CELL), floor_div(r.a.z, DRAIN_CELL))
				b := drain_node(
					42,
					floor_div(r.target.x, DRAIN_CELL),
					floor_div(r.target.z, DRAIN_CELL),
				)
				testing.expect(
					t,
					b.potential < a.potential && r.a.y >= r.mid.y && r.mid.y >= r.b.y,
				)
				if !exists && checked < 64 && r.a.y > SEA_LEVEL + 2 {
					previous := r.a.y + 1
					for step: i32 = 0; step <= 16; step += 1 {
						a, b := r.a, r.mid
						along := step
						if step > 8 {a, b = r.mid, r.b; along = step - 8}
						p := a + (b - a) * along / 8
						c := geography_column(42, p.x, p.z)
						testing.expect(
							t,
							c.water_level <= previous + 1,
							fmt.tprintf("uphill surface at %v", p),
						)
						if geography_dry_column(42, p.x, p.z).height < SEA_LEVEL {
							testing.expect(
								t,
								c.water_level == SEA_LEVEL,
								fmt.tprintf("raised coastal river at %v", p),
							)
						}
						previous = c.water_level
					}
					checked += 1
				}
			}
		}}
	testing.expect(t, checked >= 16)
	fmt.printf("V5 canonical reaches %d / traced downhill reaches %d\n", len(seen), checked)
}

@(test)
test_v5_lake_containment_edits_and_drainage_seams :: proc(t: ^testing.T) {
	w: World; world_init(&w, 42); defer world_destroy(&w)
	p, ok := terrain_find_river(42, true); testing.expect(t, ok); if !ok {return}
	water := 0
	for z := p.z - 24; z <= p.z + 24; z += 1 {for x := p.x - 24; x <= p.x + 24; x += 1 {
			c := geography_column(42, x, z)
			if c.water_level != p.y || c.height >= p.y {continue}
			q := I3{x, p.y, z}
			b := world_get(&w, q)
			testing.expect(t, b == .Water || b == .Ice)
			if b == .Ice {continue}
			water += 1
			for n in ([]I3{{-1, 0, 0}, {1, 0, 0}, {0, 0, -1}, {0, 0, 1}, {0, -1, 0}}) {
				testing.expect(
					t,
					world_get(&w, q + n) != .Air,
					fmt.tprintf("uncontained lake at %v", q + n),
				)
			}
		}}
	testing.expect(t, water > 20)
	data := make([]u8, PAD * PAD * PAD); defer delete(data)
	for lake in ([2]bool{false, true}) {
		center, found := terrain_find_river(42, lake); testing.expect(t, found)
		for offset in ([]I3{{0, 0, 0}, {-32, 0, -32}, {32, 0, 32}, {0, -32, 0}}) {
			key := chunk_of(center + offset)
			terrain_fill_chunk(42, key, 5, data)
			for z in 0 ..< PAD {for x in 0 ..< PAD {for y in 0 ..< PAD {
						q := key * 32 + I3{i32(x) - 1, i32(y) - 1, i32(z) - 1}
						if Block(data[block_index(x, y, z)]) !=
						   world_get(
							   &w,
							   q,
						   ) {testing.expect(t, false, fmt.tprintf("drainage seam %v", q)); return}
					}}}
		}
	}
	previous := world_get(&w, p)
	testing.expect(t, world_apply(&w, {w.sequence + 1, p, .Stone}) && world_get(&w, p) == .Stone)
	testing.expect(
		t,
		world_apply(&w, {w.sequence + 1, p, previous}) && world_get(&w, p) == previous,
	)
	fmt.printf("V5 lake %v / sampled surface water %d\n", p, water)
}

@(test)
test_v5_spawn_128_seeds_and_bounded_features :: proc(t: ^testing.T) {
	fallbacks := 0
	for seed: u64 = 0; seed < 128; seed += 1 {
		w: World; world_init(&w, seed)
		p := world_spawn(&w)
		testing.expect(t, !player_collides(&w, p) && p.y > SEA_LEVEL + PLAYER_EYE)
		below := p; below.y -= 0.01
		testing.expect(t, player_collides(&w, below))
		if w.edit_count > 0 {fallbacks += 1}
		world_destroy(&w)
	}
	testing.expect(t, size_of(Terrain_Features) < 16 * 1024 && size_of(Vertex) == 8)
	fmt.printf(
		"V5 spawn fallbacks %d / 128 seeds; descriptor bytes %d\n",
		fallbacks,
		size_of(Terrain_Features),
	)
}

@(test)
test_v5_concurrent_terrain_meshes :: proc(t: ^testing.T) {check_concurrent_terrain_meshes(t, 5)}

package sandbox

// Immutable per-job sampling metadata. No persistent dense world/light map and
// no worker reads of mutable World state. Unknown voxels are evaluated lazily.
Light_Region :: struct {
	job:       Mesh_Job,
	lo:        I3,
	features:  [9]Terrain_Features,
	columns:   [LIGHT_SIDE * LIGHT_SIDE]Terrain_Column,
	tops:      [LIGHT_SIDE * LIGHT_SIDE]i32,
	overrides: []u8,
}

light_region_init :: proc(r: ^Light_Region, job: Mesh_Job) {
	r.job = job
	r.lo = job.key * 32 - I3{LIGHT_HALO, LIGHT_HALO, LIGHT_HALO}
	if len(job.edits) > 0 {
		r.overrides = make([]u8, LIGHT_SIDE * LIGHT_SIDE * HEIGHT)
		for &b in r.overrides {b = 255}
		for e in job.edits {
			p := e.position - r.lo
			if p.x >= 0 &&
			   p.x < LIGHT_SIDE &&
			   p.z >= 0 &&
			   p.z < LIGHT_SIDE &&
			   e.position.y >= 0 &&
			   e.position.y < HEIGHT {
				r.overrides[int(p.x) + LIGHT_SIDE * (int(p.z) + LIGHT_SIDE * int(e.position.y))] =
					u8(e.block)
			}
		}
	}
	for dz: i32 = -1; dz <= 1; dz += 1 {for dx: i32 = -1; dx <= 1; dx += 1 {
			key := job.key + I3{dx, 0, dz}
			f := &r.features[int(dx + 1 + 3 * (dz + 1))]
			f^ = terrain_features(job.seed, key, job.generator, true)
			// Conservative bounds only skip guaranteed empty sky. All-height cave
			// lakes can reserve a rock shell; plants include their full crown height.
			top: i32 = SEA_LEVEL + 2
			for p in f.plants[:f.plant_count] {top = max(top, p.h + p.height + 2)}
			for p in f.trees[:f.tree_count] {top = max(top, p.h + 6)}
			for c in f.caves[:f.cave_count] {if c.lake {top = max(top, c.center.y + c.radius.y + 2)}}
			for wz := max(r.lo.z, key.z * 32);
			    wz < min(r.lo.z + LIGHT_SIDE, key.z * 32 + CHUNK);
			    wz += 1 {
				for wx := max(r.lo.x, key.x * 32);
				    wx < min(r.lo.x + LIGHT_SIDE, key.x * 32 + CHUNK);
				    wx += 1 {
					i := int(wx - r.lo.x) + LIGHT_SIDE * int(wz - r.lo.z)
					c := &r.columns[i]
					if job.generator >=
					   3 {c^ = terrain_column(job.seed, wx, wz, job.generator, f)} else {c.height = legacy_terrain_height(job.seed, wx, wz)}
					r.tops[i] = max(top, c.height, c.island_top, c.form_top)
				}
			}
		}}
}

light_region_destroy :: proc(r: ^Light_Region) {delete(r.overrides)}

light_region_world_block :: proc(r: ^Light_Region, x: int, wy: i32, z: int) -> Block {
	if wy < 0 {return .Bedrock}
	if wy >= HEIGHT {return .Air}
	if len(r.overrides) > 0 {
		v := r.overrides[x + LIGHT_SIDE * (z + LIGHT_SIDE * int(wy))]
		if v != 255 {return Block(v)}
	}
	i := x + LIGHT_SIDE * z
	if wy > r.tops[i] {return .Air}
	f := &r.features[(x + LIGHT_HALO) / CHUNK + 3 * ((z + LIGHT_HALO) / CHUNK)]
	return light_terrain_block(
		r.job.seed,
		{r.lo.x + i32(x), wy, r.lo.z + i32(z)},
		r.columns[i],
		f,
		r.job.generator,
	)
}

light_region_block :: proc(r: ^Light_Region, x, y, z: int) -> Block {
	return light_region_world_block(r, x, r.lo.y + i32(y), z)
}

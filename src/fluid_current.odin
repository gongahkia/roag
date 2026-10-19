package sandbox

WATER_CURRENT_SPEED :: 1.6 // Blocks/second, independent of the movement setting.
WATER_FALL_SPEED :: 2.0

water_flow_head :: proc(b: Block) -> int {
	// The two thinnest states share a rendered height but still have distinct
	// flow levels, so the final cell must continue carrying water downstream.
	return b == .Water || b == .Water_Falling ? 8 : 7 - fluid_level(b)
}

// Full source blocks in generated rivers have equal local heights along long
// stretches. Use the same reach that carved their channel to carry swimmers
// downstream there too. Lakes, oceans, and water placed above terrain stay calm.
water_river_direction :: proc(w: ^World, p: I3) -> V3 {
	if w.generator < 5 || p.y < 0 || p.y >= HEIGHT {return {}}
	f := world_features_cached(w, p)
	c := world_column_cached(w, p, f)
	if c.river != TQ || p.y <= c.height || p.y > c.water_level {return {}}
	if terrain_block_v5(w.seed, p, c^, f) != .Water {return {}}
	// Incoming reaches share an endpoint with the lake they feed. Carving may
	// select that reach on a distance tie; the lake's interior is still calm.
	for lake in f.rivers.reaches[:f.rivers.count] {
		if !lake.lake || c.water_level != lake.a.y {continue}
		dx, dz := i64(p.x - lake.a.x), i64(p.z - lake.a.z)
		if dx * dx + dz * dz <= i64(lake.width) * i64(lake.width) {return {}}
	}
	r, _, _, found := river_nearest_reach(c^, p.x, p.z, &f.rivers)
	if !found || r.lake {return {}}
	d1, _ := river_segment_sample(p.x, p.z, r.a, r.mid)
	d2, _ := river_segment_sample(p.x, p.z, r.mid, r.b)
	delta := d2 < d1 ? r.b - r.mid : r.mid - r.a
	return normalize({f64(delta.x), 0, f64(delta.z)})
}

// Derive motion from live fluid levels, so source removal, new outlets, walls,
// and chunk boundaries affect currents immediately. No persistent flow grid.
water_current :: proc(w: ^World, p: I3) -> V3 {
	b := world_get(w, p)
	if fluid_kind(b) != .Water {return {}}
	height := water_flow_head(b)
	flow: V3
	for n in NORMALS {
		if n.y != 0 {continue}
		q := p + n
		neighbor := world_get(w, q)
		head: int
		if fluid_kind(neighbor) == .Water {
			head = water_flow_head(neighbor)
			// A falling column is an outlet until it reaches supported ground.
			if neighbor == .Water_Falling && b != .Water_Falling &&
			   !is_solid(world_get(w, q + I3{0, -1, 0})) {head = -1}
		} else if neighbor == .Air {
			below := world_get(w, q + I3{0, -1, 0})
			if fluid_kind(below) != .Water {continue}
			head = min(-1, water_flow_head(below) - 8)
		} else {continue}
		flow += V3{f64(n.x), 0, f64(n.z)} * f64(height - head)
	}
	flow = normalize(flow)
	if flow == (V3{}) && b == .Water {flow = water_river_direction(w, p)}
	flow *= WATER_CURRENT_SPEED
	if b == .Water_Falling {flow.y = -WATER_FALL_SPEED}
	return flow
}

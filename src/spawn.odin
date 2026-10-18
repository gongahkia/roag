package sandbox

// The spiral is bounded to 1,089 candidate columns in a 256-block-wide area.
// Existing saves retain their camera position and never invoke this policy.
world_spawn :: proc(w: ^World) -> V3 {
	for ring: i32 = 0; ring <= 16; ring += 1 {
		for z := -ring; z <= ring; z += 1 {for x := -ring; x <= ring; x += 1 {
			if max(abs(x), abs(z)) != ring {continue}
			wx, wz := 8 + x * 8, 8 + z * 8
			c := terrain_column_v3(w.seed, wx, wz)
			if c.height <= SEA_LEVEL + 1 || c.slope > TQ / 2 {continue}
			low, high := c.height, c.height
			for dz: i32 = -1; dz <= 1; dz += 1 {for dx: i32 = -1; dx <= 1; dx += 1 {
				h := terrain_height(w.seed, wx + dx, wz + dz, w.generator)
				low = min(low, h); high = max(high, h)
			}}
			if high - low > 1 {continue}
			eye := V3{f64(wx) + 0.5, f64(c.height + 1) + PLAYER_EYE, f64(wz) + 0.5}
			if !is_solid(world_get(w, {wx, c.height, wz})) || player_collides(w, eye) {continue}
			return eye
		}}
	}
	// A deliberately small persisted fallback handles seeds with no dry site.
	y := max(SEA_LEVEL + 2, terrain_height(w.seed, 8, 8, w.generator) + 1)
	for z: i32 = 7; z <= 9; z += 1 {for x: i32 = 7; x <= 9; x += 1 {
		world_apply(w, {w.sequence + 1, {x, y, z}, .Stone})
		for dy: i32 = 1; dy <= 3; dy += 1 {world_apply(w, {w.sequence + 1, {x, y + dy, z}, .Air})}
	}}
	return {8.5, f64(y + 1) + PLAYER_EYE, 8.5}
}

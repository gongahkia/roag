package sandbox

DRAIN_CELL :: 128
DRAIN_MAX_NODES :: 100
River_Reach :: struct {
	a, mid, b:    I3,
	target:       I3,
	width:        i32,
	lake, outlet: bool,
}
River_Set :: struct {
	reaches: [16]River_Reach,
	count:   int,
}
Drain_Node :: struct {
	position:  I3,
	potential: i64,
	rain:      i32,
	down:      int,
}

// A smooth coarse potential establishes a strict downhill DAG. Water grades
// are monotone projections of this potential. Local minima become lakes, so
// this infinite world does not require a global search for an ocean outlet.
drain_node :: proc(seed: u64, gx, gz: i32) -> Drain_Node {
	h := hash3(seed + 6400, gx, 0, gz)
	x, z :=
		gx * DRAIN_CELL +
		64 +
		i32((h >> 8) % 33) -
		16,
		gz * DRAIN_CELL +
		64 +
		i32((h >> 16) % 33) -
		16
	c := geography_dry_column(seed, x, z)
	potential := i64(c.height) * TQ + c.moisture / 4
	return {
		position = {x, i32(clamp(i64(c.height) - 8, SEA_LEVEL, 136)), z},
		potential = potential,
		rain = 1 + i32(c.moisture * 2 / TQ),
		down = -1,
	}
}

river_set_for :: proc(seed: u64, key: I3) -> River_Set {
	// Include the tree-anchor halo. At most four by four reaches touch it.
	gx0, gz0 :=
		floor_div(key.x * 32 - 16, DRAIN_CELL) - 1, floor_div(key.z * 32 - 16, DRAIN_CELL) - 1
	gx1, gz1 :=
		floor_div(key.x * 32 + 47, DRAIN_CELL) + 1, floor_div(key.z * 32 + 47, DRAIN_CELL) + 1
	width, depth := int(gx1 - gx0) + 7, int(gz1 - gz0) + 7
	nodes: [DRAIN_MAX_NODES]Drain_Node
	assert(width <= 10 && depth <= 10)
	for z in 0 ..< depth {for x in 0 ..< width {
			nodes[x + z * width] = drain_node(seed, gx0 + i32(x) - 3, gz0 + i32(z) - 3)
		}}
	for z in 1 ..< depth - 1 {for x in 1 ..< width - 1 {
			i := x + z * width
			best := nodes[i].potential
			for dz in -1 ..= 1 {for dx in -1 ..= 1 {
					j := x + dx + (z + dz) * width
					if nodes[j].potential < best {best = nodes[j].potential; nodes[i].down = j}
				}}
		}}
	for z in 1 ..< depth - 1 {for x in 1 ..< width - 1 {
			i := x + z * width
			if nodes[i].down >= 0 || nodes[i].position.y <= SEA_LEVEL {continue}
			rim := nodes[i].position.y
			for o in ([]I3{{24, 0, 0}, {-24, 0, 0}, {0, 0, 24}, {0, 0, -24}, {17, 0, 17}, {17, 0, -17}, {-17, 0, 17}, {-17, 0, -17}}) {
				p := nodes[i].position + o
				rim = min(rim, geography_dry_column(seed, p.x, p.z).height - 2)
			}
			nodes[i].position.y = max(SEA_LEVEL, rim)
		}}
	result: River_Set
	for gz := gz0; gz <= gz1; gz += 1 {for gx := gx0; gx <= gx1; gx += 1 {
			i := int(gx - gx0) + 3 + (int(gz - gz0) + 3) * width
			n := nodes[i]
			flow, incoming := n.rain, 0
			for dz in -1 ..= 1 {for dx in -1 ..= 1 {
					j := i + dx + dz * width
					if nodes[j].down != i {continue}
					incoming += 1; flow += nodes[j].rain
					for ez in -1 ..= 1 {for ex in -1 ..= 1 {
							k := j + ex + ez * width
							if nodes[k].down == j {flow += nodes[k].rain}
						}}
				}}
			if incoming == 0 {continue}
			r := River_Reach {
				a     = n.position,
				width = 2 + min(4, flow / 3),
			}
			if n.down < 0 {
				if n.position.y <= SEA_LEVEL {continue}
				r.b =
					n.position; r.mid = n.position; r.lake = true; r.width = 10 + min(8, flow / 2)
			} else {
				r.b = nodes[n.down].position
				if r.a.y == SEA_LEVEL && r.b.y == SEA_LEVEL {continue}
				r.mid = (r.a + r.b) / 2
				h := hash3(seed + 6402, gx, 0, gz)
				if abs(r.b.x - r.a.x) >
				   abs(
					   r.b.z - r.a.z,
				   ) {r.mid.z += i32(h % 17) - 8} else {r.mid.x += i32(h % 17) - 8}
			}
			if !r.lake {r.target = r.b; river_clip_coast(seed, &r)}
			result.reaches[result.count] = r; result.count += 1
		}}
	return result
}

river_segment_sample :: proc(x, z: i32, a, b: I3) -> (distance2, level: i64) {
	dx, dz := i64(b.x - a.x), i64(b.z - a.z)
	px, pz := i64(x - a.x), i64(z - a.z)
	t := clamp((px * dx + pz * dz) * TQ / max(1, dx * dx + dz * dz), 0, TQ)
	qx, qz := px * TQ - dx * t, pz * TQ - dz * t
	return (qx * qx + qz * qz) / (TQ * TQ), i64(a.y) * TQ + i64(b.y - a.y) * t
}

river_carve :: proc(c: ^Terrain_Column, x, z: i32, set: ^River_Set) {
	// Ocean terrain already belongs to the sea-level water body. A stream
	// cannot manufacture elevated embankments across an existing ocean.
	if c.height < SEA_LEVEL {return}
	best := i64(1 << 60)
	best_d, best_level: i64
	best_r: River_Reach
	for r in set.reaches[:set.count] {
		margin := r.lake ? r.width + 8 : min(28, r.width + 14 + i32(c.relief * 8 / TQ))
		if x < min(r.a.x, min(r.mid.x, r.b.x)) - margin ||
		   x > max(r.a.x, max(r.mid.x, r.b.x)) + margin ||
		   z < min(r.a.z, min(r.mid.z, r.b.z)) - margin ||
		   z > max(r.a.z, max(r.mid.z, r.b.z)) + margin {continue}
		d, level := river_segment_sample(x, z, r.a, r.mid)
		d2, l2 := river_segment_sample(x, z, r.mid, r.b)
		if d2 < d {d = d2; level = l2}
		if d >= i64(margin * margin) {continue}
		score := d * TQ / i64(margin * margin)
		if score < best {best = score; best_d = d; best_level = level; best_r = r}
	}
	if best == i64(1 << 60) {return}
	r := best_r
	d := integer_sqrt(best_d)
	margin := r.lake ? r.width + 8 : min(28, r.width + 14 + i32(c.relief * 8 / TQ))
	if d <= i64(r.width) {
		c.height = min(c.height, i32(best_level / TQ) - 2 - i32((i64(r.width) - d) / 3))
		c.water_level = max(SEA_LEVEL, i32(best_level / TQ))
		c.river = TQ
	} else {
		blend := qramp(i64(r.width), i64(margin), d)
		// The immediate bank stays above water, including low-lying crossings.
		bank := best_level + TQ + (d - i64(r.width)) * TQ / 2
		c.height = i32(qlerp(bank, i64(c.height) * TQ, blend) / TQ)
		if d <= i64(r.width) + 2 {c.height = max(c.height, i32(best_level / TQ))}
		c.river = TQ - blend
	}
	c.slope = max(c.slope, c.river / 3)
}

terrain_find_river :: proc(seed: u64, lake: bool) -> (p: I3, ok: bool) {
	for ring: i32 = 0; ring <= 16; ring += 1 {
		for z := -ring; z <= ring; z += 1 {for x := -ring; x <= ring; x += 1 {
				if max(abs(x), abs(z)) != ring {continue}
				set := river_set_for(seed, {x * 4, 0, z * 4})
				for r in set.reaches[:set.count] {
					if r.lake != lake || r.a.y <= SEA_LEVEL + 2 {continue}
					p = r.lake ? r.a : r.mid
					c := geography_column(seed, p.x, p.z)
					if c.height < c.water_level &&
					   c.water_level > SEA_LEVEL + 2 {p.y = c.water_level; return p, true}
				}
			}}
	}
	return {}, false
}

// Coastal reaches terminate at the first sea-level shore crossing. This avoids
// an elevated water grade carrying a causeway across a bay between coarse nodes.
river_clip_coast :: proc(seed: u64, r: ^River_Reach) {
	middle := geography_dry_column(seed, r.mid.x, r.mid.z)
	if r.b.y > SEA_LEVEL + 12 && middle.height > SEA_LEVEL + 14 {return}
	previous := r.a
	for step: i32 = 1; step <= 16; step += 1 {
		a, b := r.a, r.mid
		along := step
		if step > 8 {a, b = r.mid, r.b; along = step - 8}
		p := a + (b - a) * along / 8
		if geography_dry_column(seed, p.x, p.z).height >= SEA_LEVEL {previous = p; continue}
		lo, hi := previous, p
		for _ in 0 ..< 5 {
			mid := (lo + hi) / 2
			if geography_dry_column(seed, mid.x, mid.z).height >=
			   SEA_LEVEL {lo = mid} else {hi = mid}
		}
		r.b = hi; r.b.y = SEA_LEVEL; r.outlet = true
		if step <= 8 {r.mid = (r.a + r.b) / 2} else {r.mid.y = (r.a.y + r.b.y) / 2}
		return
	}
}

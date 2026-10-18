package sandbox

Cave_Segment :: struct {a, b: I3, radius: i32}
Cave_Cell :: struct {
	key, center, radius: I3,
	segments: [13]Cave_Segment,
	count: int,
	chamber, lake: bool,
	waterline: i32,
}

cave_cell_v3 :: proc(seed: u64, key: I3) -> Cave_Cell {
	h := hash3(seed + 4001, key.x, key.y, key.z)
	origin := key * 64
	cell := Cave_Cell {
		key = key,
		center = origin + I3{24 + i32(h >> 8 & 15), 24 + i32(h >> 16 & 15), 24 + i32(h >> 24 & 15)},
		radius = {8 + i32((h >> 32) % 11), 7 + i32((h >> 40) % 8), 8 + i32((h >> 48) % 11)},
	}
	if key.y < 0 || key.y > 2 {return cell}
	surface := terrain_base_column(seed, cell.center.x, cell.center.z).height
	if cell.center.y > surface - 8 {return cell}
	cell.chamber = h % 3 == 0
	cell.lake = cell.chamber && (h >> 4) % 5 == 0 && cell.center.y - cell.radius.y - 2 >= 16
	cell.waterline = cell.center.y - 2
	if cell.lake {
		for dz: i32 = -1; dz <= 1; dz += 1 {for dx: i32 = -1; dx <= 1; dx += 1 {
			if terrain_base_column(seed, cell.center.x + dx * (cell.radius.x + 2), cell.center.z + dz * (cell.radius.z + 2)).height <
			   cell.center.y + cell.radius.y + 4 {cell.lake = false}
		}}
	}
	for axis in 0 ..< 3 {for side in 0 ..< 2 {
		// Both cells hash the same canonical face, including negative positions.
		face := key
		face[axis] += i32(side)
		f := hash3(seed + 4100 + u64(axis), face.x, face.y, face.z)
		if f % 4 == 0 {continue}
		port := origin + I3{16 + i32((f >> 8) % 33), 16 + i32((f >> 16) % 33), 16 + i32((f >> 24) % 33)}
		port[axis] = face[axis] * 64
		bend := (cell.center + port) / 2
		bend[(axis + 1) % 3] += i32((f >> 32) % 13) - 6
		radius := 2 + i32((f >> 40) % 3)
		cell.segments[cell.count] = {cell.center, bend, radius}; cell.count += 1
		cell.segments[cell.count] = {bend, port, radius}; cell.count += 1
	}}
	// Deliberate entrances stay within their owning cell plus the tunnel radius.
	if (h >> 52) % 8 == 0 && surface <= origin.y + 58 {
		ex, ez := cell.center.x + 14, cell.center.z - 11
		eh := terrain_base_column(seed, ex, ez).height
		if eh <= origin.y + 58 && eh > SEA_LEVEL + 2 {
			cell.segments[cell.count] = {cell.center, {ex, eh + 2, ez}, 3}; cell.count += 1
		}
	}
	return cell
}

ellipsoid_inside :: proc(p, center, radius: I3) -> bool {
	d := p - center
	if abs(d.x) > radius.x || abs(d.y) > radius.y || abs(d.z) > radius.z {return false}
	sum: i64
	for axis in 0 ..< 3 {sum += i64(d[axis]) * i64(d[axis]) * TQ / (i64(radius[axis]) * i64(radius[axis]))}
	return sum <= TQ
}

capsule_inside :: proc(p: I3, segment: Cave_Segment) -> bool {
	for axis in 0 ..< 3 {
		if p[axis] < min(segment.a[axis], segment.b[axis]) - segment.radius ||
		   p[axis] > max(segment.a[axis], segment.b[axis]) + segment.radius {return false}
	}
	d := segment.b - segment.a
	v := p - segment.a
	length, dot: i64
	for axis in 0 ..< 3 {length += i64(d[axis]) * i64(d[axis]); dot += i64(v[axis]) * i64(d[axis])}
	t := clamp(dot * TQ / max(length, 1), 0, TQ)
	distance: i64
	for axis in 0 ..< 3 {
		x := i64(v[axis]) * TQ - i64(d[axis]) * t
		distance += x * x
	}
	return distance <= i64(segment.radius * segment.radius) * TQ * TQ
}

// Wet chambers take precedence over every tunnel, including neighboring cells.
// The outer shell below water level is reserved solid rock, not a fluid dam
// assembled through edits. It can still be mined normally by the player.
cave_lake_sample :: proc(p: I3, cells: []Cave_Cell) -> (block: Block, reserved: bool) {
	for &cell in cells {
		if !cell.lake || p.y > cell.waterline + 1 {continue}
		if ellipsoid_inside(p, cell.center, cell.radius) {
			if p.y <= cell.waterline {return .Water, true}
		} else if ellipsoid_inside(p, cell.center, cell.radius + I3{2, 2, 2}) {
			return .Stone, true
		}
	}
	return .Air, false
}

cave_sample_v3 :: proc(p: I3, ground_height: i32, cells: []Cave_Cell) -> (block: Block, carved: bool) {
	if p.y <= 4 {return .Air, false}
	for &cell in cells {
		if cell.chamber && p.y < ground_height - 5 && ellipsoid_inside(p, cell.center, cell.radius) {
			return p.y <= 12 ? Block.Lava : Block.Air, true
		}
		for segment in cell.segments[:cell.count] {
			if capsule_inside(p, segment) {return p.y <= 12 ? Block.Lava : Block.Air, true}
		}
	}
	return .Air, false
}

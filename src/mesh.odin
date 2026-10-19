package sandbox

MAX_QUADS :: 6 * CHUNK * CHUNK * CHUNK
Vertex :: struct #packed {
	x, y, z, data: u16,
}

#assert(size_of(Vertex) == 8)

VERTEX_AO_SHIFT :: 9
VERTEX_HEIGHT_SHIFT :: 11
VERTEX_TOP_BIT :: 15
VERTEX_MATERIAL_MASK :: 63

Mesh :: struct {
	vertices:      [dynamic]Vertex,
	indices:       [dynamic]u32,
	overflow:      bool,
	faces:         int,
	water_indices: int, // Blended water and solid ice indices, after opaque geometry.
}

Face :: struct {
	material:       u8,
	ao:             [4]u8,
	height, bottom: u8,
}

face_equal :: proc(a, b: Face) -> bool {
	return a.material == b.material && a.ao == b.ao && a.height == b.height && a.bottom == b.bottom
}

face_mergeable :: proc(a: Face) -> bool {
	// Height/bottom compatibility preserves steps while allowing flat pools and
	// full-height waterfalls to use the same greedy merging as solid terrain.
	return a.ao[0] == a.ao[1] && a.ao[0] == a.ao[2] && a.ao[0] == a.ao[3]
}
NORMALS := [6]I3{{1, 0, 0}, {-1, 0, 0}, {0, 1, 0}, {0, -1, 0}, {0, 0, 1}, {0, 0, -1}}
U_AXIS := [6]int{1, 2, 2, 0, 0, 1}
V_AXIS := [6]int{2, 1, 0, 2, 1, 0}
sample :: proc(data: []u8, p: I3) -> u8 {
	return data[block_index(int(p.x + 1), int(p.y + 1), int(p.z + 1))]
}

solid :: proc(data: []u8, p: I3) -> int {
	if is_solid(Block(sample(data, p))) {
		return 1
	}
	return 0
}

face_visible :: proc(data: []u8, p: I3, dir: int) -> bool {
	b := Block(sample(data, p))
	n := Block(sample(data, p + NORMALS[dir]))
	if b == .Air {return false}
	if b == .Ice {return n == .Air || is_fluid(n)}
	if !is_fluid(b) {return n == .Air || is_fluid(n) || n == .Ice}
	if is_solid(n) {return false}
	if fluid_kind(b) != fluid_kind(n) {return true}
	if dir == 2 || dir == 3 {return false}
	q := p + NORMALS[dir]
	return(
		fluid_height(b, Block(sample(data, p + I3{0, 1, 0}))) >
		fluid_height(n, Block(sample(data, q + I3{0, 1, 0}))) \
	)
}

face_ao :: proc(data: []u8, p: I3, dir: int) -> [4]u8 {
	q := p + NORMALS[dir]
	u, v := U_AXIS[dir], V_AXIS[dir]
	result: [4]u8
	for c in 0 ..< 4 {
		su, sv: i32 = -1, -1
		if c == 1 || c == 2 {
			su = 1
		}
		if c >= 2 {
			sv = 1
		}
		a, b := q, q
		a[u] += su
		b[v] += sv
		corner := a
		corner[v] += sv
		s1, s2 := solid(data, a), solid(data, b)
		if s1 == 1 && s2 == 1 {
			result[c] = 0
		} else {
			result[c] = u8(3 - s1 - s2 - solid(data, corner))
		}
	}
	return result
}

mesh_destroy :: proc(m: ^Mesh) {
	delete(m.vertices)
	delete(m.indices)
	m^ = {}
}

mesh_build :: proc(data: []u8, greedy: bool = true) -> Mesh {
	m: Mesh
	// Reserve the exposed-face upper bound once; greedy merging only shrinks it.
	face_count := 0
	has_water := false
	for z in 0 ..< CHUNK {
		for y in 0 ..< CHUNK {
			for x in 0 ..< CHUNK {
				p := I3{i32(x), i32(y), i32(z)}
				if sample(data, p) == 0 {
					continue
				}
				if is_translucent(Block(sample(data, p))) {has_water = true}
				for dir in 0 ..< 6 {
					if face_visible(data, p, dir) {
						face_count += 1
					}
				}
			}
		}
	}
	if face_count == 0 {
		return m
	}
	if face_count > MAX_QUADS {
		m.overflow = true
		return m
	}
	m.vertices = make([dynamic]Vertex, 0, face_count * 4)
	m.indices = make([dynamic]u32, 0, face_count * 6)
	mask: [CHUNK * CHUNK]Face
	opaque_indices := 0
	for pass in 0 ..< (has_water ? 2 : 1) {
		if pass == 1 {opaque_indices = len(m.indices)}
		for dir in 0 ..< 6 {
			axis := dir / 2
			u, v := U_AXIS[dir], V_AXIS[dir]
			for plane in 0 ..< CHUNK {
				mask = {}
				for j in 0 ..< CHUNK {
					for i in 0 ..< CHUNK {
						p: I3
						p[axis] = i32(plane)
						p[u] = i32(i)
						p[v] = i32(j)
						b := sample(data, p)
						if is_translucent(Block(b)) != (pass == 1) {continue}
						if face_visible(data, p, dir) {
							f := Face {
								material = b,
								ao       = face_ao(data, p, dir),
							}
							if is_fluid(Block(b)) {
								f.material = u8(fluid_kind(Block(b))); f.ao = {3, 3, 3, 3}
								f.height = fluid_height(
									Block(b),
									Block(sample(data, p + I3{0, 1, 0})),
								)
								n := p + NORMALS[dir]
								if dir != 2 &&
								   dir != 3 &&
								   fluid_kind(Block(sample(data, n))) == Block(f.material) {
									f.bottom = fluid_height(
										Block(sample(data, n)),
										Block(sample(data, n + I3{0, 1, 0})),
									)
								}
							}
							mask[i + j * CHUNK] = f
							m.faces += 1
						}
					}
				}
				for j := 0; j < CHUNK; j += 1 {
					for i := 0; i < CHUNK; {
						f := mask[i + j * CHUNK]
						if f.material == 0 {
							i += 1
							continue
						}
						width, height := 1, 1
						if greedy && face_mergeable(f) {
							for i + width < CHUNK && face_equal(f, mask[i + width + j * CHUNK]) {
								width += 1
							}
							grow: for j + height < CHUNK {
								for k in 0 ..< width {
									if !face_equal(f, mask[i + k + (j + height) * CHUNK]) {
										break grow
									}
								}
								height += 1
							}
						}
						if len(m.indices) / 6 >= MAX_QUADS {
							m.overflow = true
							return m
						}
						p: I3
						p[axis] = i32(plane)
						if dir % 2 == 0 {
							p[axis] += 1
						}
						p[u] = i32(i)
						p[v] = i32(j)
						base := u32(len(m.vertices))
						for c in 0 ..< 4 {
							q := p
							if c == 1 || c == 2 {
								q[u] += i32(width)
							}
							if c >= 2 {
								q[v] += i32(height)
							}
							packed :=
								u16(dir) |
								(u16(f.material) << 3) |
								(u16(f.ao[c]) << VERTEX_AO_SHIFT)
							if is_fluid(Block(f.material)) {
								top := q.y > i32(plane) && axis == 1
								if axis !=
								   1 {top = (u == 1 && (c == 1 || c == 2)) || (v == 1 && c >= 2)}
								h := top ? f.height : f.bottom
								packed |= u16(h) << VERTEX_HEIGHT_SHIFT
								if top {packed |= 1 << VERTEX_TOP_BIT}
							}
							append(&m.vertices, Vertex{u16(q.x), u16(q.y), u16(q.z), packed})
						}
						if int(f.ao[0]) + int(f.ao[2]) > int(f.ao[1]) + int(f.ao[3]) {
							append(
								&m.indices,
								base,
								base + 1,
								base + 3,
								base + 1,
								base + 2,
								base + 3,
							)
						} else {
							append(&m.indices, base, base + 1, base + 2, base, base + 2, base + 3)
						}
						for y in 0 ..< height {
							for x in 0 ..< width {
								mask[i + x + (j + y) * CHUNK] = {}
							}
						}
						i += width
					}
				}
			}
		}
	}
	if has_water {m.water_indices = len(m.indices) - opaque_indices}
	return m
}

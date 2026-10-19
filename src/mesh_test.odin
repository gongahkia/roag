package sandbox

import "core:testing"

mesh_area :: proc(m: Mesh) -> int {
	area := 0
	for q := 0; q < len(m.vertices); q += 4 {
		a, b, c := m.vertices[q], m.vertices[q + 1], m.vertices[q + 3]
		u := abs(int(a.x) - int(b.x)) + abs(int(a.y) - int(b.y)) + abs(int(a.z) - int(b.z))
		v := abs(int(a.x) - int(c.x)) + abs(int(a.y) - int(c.y)) + abs(int(a.z) - int(c.z))
		area += u * v
	}
	return area
}

reference_faces :: proc(data: []u8) -> int {
	count := 0
	for z in 0 ..< CHUNK {
		for y in 0 ..< CHUNK {
			for x in 0 ..< CHUNK {
				p := I3{i32(x), i32(y), i32(z)}
				if sample(data, p) != 0 {
					for n in NORMALS {
						if sample(data, p + n) == 0 {
							count += 1
						}
					}
				}
			}
		}
	}
	return count
}

check_winding :: proc(t: ^testing.T, m: Mesh) {
	for i := 0; i < len(m.indices); i += 3 {
		a, b, c :=
			m.vertices[m.indices[i]], m.vertices[m.indices[i + 1]], m.vertices[m.indices[i + 2]]
		u := V3{f64(b.x) - f64(a.x), f64(b.y) - f64(a.y), f64(b.z) - f64(a.z)}
		v := V3{f64(c.x) - f64(a.x), f64(c.y) - f64(a.y), f64(c.z) - f64(a.z)}
		n := cross(u, v)
		dir := int(a.data & 7)
		expected := NORMALS[dir]
		testing.expect(
			t,
			n.x * f64(expected.x) + n.y * f64(expected.y) + n.z * f64(expected.z) > 0,
		)
	}
}

@(test)
test_mesher_shapes_and_winding :: proc(t: ^testing.T) {
	data := make([]u8, PAD * PAD * PAD)
	defer delete(data)
	empty := mesh_build(data)
	defer mesh_destroy(&empty)
	testing.expect(t, len(empty.vertices) == 0)
	for z in 1 ..= CHUNK {
		for y in 1 ..= CHUNK {
			for x in 1 ..= CHUNK {
				data[block_index(x, y, z)] = u8(Block.Stone)
			}
		}
	}
	solid_mesh := mesh_build(data)
	defer mesh_destroy(&solid_mesh)
	testing.expect(t, len(solid_mesh.indices) == 36)
	testing.expect(t, mesh_area(solid_mesh) == 6 * CHUNK * CHUNK)
	check_winding(t, solid_mesh)
	// Adjacent solid chunk suppresses an entire shared face.
	for z in 1 ..= CHUNK {
		for y in 1 ..= CHUNK {
			data[block_index(PAD - 1, y, z)] = u8(Block.Stone)
		}
	}
	neighbor := mesh_build(data)
	defer mesh_destroy(&neighbor)
	testing.expect(t, mesh_area(neighbor) == 5 * CHUNK * CHUNK)
	check_winding(t, neighbor)
	for &b in data {
		b = 0
	}
	for z in 1 ..= CHUNK {
		for y in 1 ..= CHUNK {
			for x in 1 ..= CHUNK {
				if hash3(83, i32(x), i32(y), i32(z)) % 7 == 0 {
					data[block_index(x, y, z)] = u8(Block.Stone)
				}
			}
		}
	}
	irregular := mesh_build(data)
	defer mesh_destroy(&irregular)
	testing.expect(t, mesh_area(irregular) == reference_faces(data))
	check_winding(t, irregular)
	check_coverage(t, data, irregular)
}

@(test)
test_checkerboard_is_bounded_and_complete :: proc(t: ^testing.T) {
	data := make([]u8, PAD * PAD * PAD)
	defer delete(data)
	for z in 1 ..= CHUNK {
		for y in 1 ..= CHUNK {
			for x in 1 ..= CHUNK {
				if (x + y + z) % 2 == 0 {
					data[block_index(x, y, z)] = u8(Block.Stone)
				}
			}
		}
	}
	m := mesh_build(data)
	defer mesh_destroy(&m)
	testing.expect(t, !m.overflow)
	testing.expect(t, mesh_area(m) == reference_faces(data))
	testing.expect(t, len(m.indices) / 6 == 98304)
	testing.expect(t, cap(m.vertices) * size_of(Vertex) + cap(m.indices) * 4 <= 6 * MiB)
}

check_coverage :: proc(t: ^testing.T, data: []u8, m: Mesh) {
	faces := make(map[[4]i32]bool)
	defer delete(faces)
	for q := 0; q < len(m.vertices); q += 4 {
		a, b, c := m.vertices[q], m.vertices[q + 1], m.vertices[q + 3]
		dir := int(a.data & 7)
		u, v := U_AXIS[dir], V_AXIS[dir]
		origin := I3{i32(a.x), i32(a.y), i32(a.z)}
		ub := I3{i32(b.x), i32(b.y), i32(b.z)}
		vc := I3{i32(c.x), i32(c.y), i32(c.z)}
		width, height := int(ub[u] - origin[u]), int(vc[v] - origin[v])
		for y in 0 ..< height {
			for x in 0 ..< width {
				p := origin
				p[u] += i32(x)
				p[v] += i32(y)
				k := [4]i32{p.x, p.y, p.z, i32(dir)}
				testing.expect(t, k not_in faces)
				faces[k] = true
			}
		}
	}
	count := 0
	for z in 0 ..< CHUNK {
		for y in 0 ..< CHUNK {
			for x in 0 ..< CHUNK {
				p := I3{i32(x), i32(y), i32(z)}
				if sample(data, p) == 0 {
					continue
				}
				for n, dir in NORMALS {
					if sample(data, p + n) != 0 {
						continue
					}
					f := p
					if dir % 2 == 0 {
						f[dir / 2] += 1
					}
					testing.expect(t, [4]i32{f.x, f.y, f.z, i32(dir)} in faces)
					count += 1
				}
			}
		}
	}
	testing.expect(t, len(faces) == count)
}

@(test)
test_fluid_mesh_levels_transparency_and_solid_boundaries :: proc(t: ^testing.T) {
	data := make([]u8, PAD * PAD * PAD); defer delete(data)
	data[block_index(2, 2, 2)] = u8(Block.Water_3)
	data[block_index(3, 2, 2)] = u8(Block.Diamond_Ore)
	m := mesh_build(data); defer mesh_destroy(&m)
	testing.expect(t, m.water_indices == 5 * 6)
	testing.expect(t, len(m.indices) - m.water_indices == 6 * 6)
	for v in m.vertices {
		material := Block((v.data >> 3) & VERTEX_MATERIAL_MASK)
		if material == .Water &&
		   v.data & (1 << VERTEX_TOP_BIT) !=
			   0 {testing.expect(t, ((v.data >> VERTEX_HEIGHT_SHIFT) & 15) == 4)}
	}
	data[block_index(2, 2, 2)] = u8(Block.Lava)
	lava := mesh_build(data); defer mesh_destroy(&lava)
	testing.expect(t, lava.water_indices == 0 && len(lava.indices) == 11 * 6)
}

@(test)
test_flat_pools_and_waterfalls_still_merge :: proc(t: ^testing.T) {
	data := make([]u8, PAD * PAD * PAD); defer delete(data)
	for z in 2 ..< 18 {for x in 2 ..< 18 {data[block_index(x, 2, z)] = u8(Block.Water)}}
	pool := mesh_build(data); defer mesh_destroy(&pool)
	testing.expect(t, len(pool.vertices) == 24 && pool.water_indices == 36)
	for &v in data {v = 0}
	for y in 2 ..< 18 {data[block_index(2, y, 2)] = u8(Block.Water_Falling)}
	fall := mesh_build(data); defer mesh_destroy(&fall)
	testing.expect(t, len(fall.vertices) == 24 && fall.water_indices == 36)
}

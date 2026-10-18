package sandbox

import "core:fmt"
import gl "vendor:OpenGL"

GPU_Mesh :: struct {
	vao, vbo, ibo: u32,
	count:         i32,
	water_count:   i32,
	bytes:         int,
}

mesh_pool_slot :: proc(r: ^Renderer, bytes: int) -> int {
	best := -1
	for i in 0 ..< r.pool_count {
		capacity := r.pool[i].bytes
		if capacity >= bytes &&
		   capacity <= bytes * 2 &&
		   (best < 0 || capacity < r.pool[best].bytes) {
			best = i
		}
	}
	return best
}

mesh_allocation_cost :: proc(r: ^Renderer, m: Mesh) -> int {
	bytes := len(m.vertices) * size_of(Vertex) + len(m.indices) * 4
	if mesh_pool_slot(r, bytes) >= 0 {
		return 0
	}
	return bytes
}

mesh_allocate :: proc(r: ^Renderer, m: Mesh) -> GPU_Mesh {
	if len(m.indices) == 0 {
		return {}
	}
	bytes := len(m.vertices) * size_of(Vertex) + len(m.indices) * 4
	if slot := mesh_pool_slot(r, bytes); slot >= 0 {
		g := r.pool[slot]
		r.pool_count -= 1
		r.pool[slot] = r.pool[r.pool_count]
		r.pool_bytes -= g.bytes
		g.count = i32(len(m.indices))
		g.water_count = i32(m.water_indices)
		r.reused_buffers += 1
		return g
	}
	g: GPU_Mesh
	g.count = i32(len(m.indices))
	g.water_count = i32(m.water_indices)
	g.bytes = bytes
	gl.GenVertexArrays(1, &g.vao)
	gl.GenBuffers(1, &g.vbo)
	gl.GenBuffers(1, &g.ibo)
	gl.BindVertexArray(g.vao)
	gl.BindBuffer(gl.ARRAY_BUFFER, g.vbo)
	gl.BufferData(gl.ARRAY_BUFFER, len(m.vertices) * size_of(Vertex), nil, gl.STATIC_DRAW)
	gl.BindBuffer(gl.ELEMENT_ARRAY_BUFFER, g.ibo)
	gl.BufferData(gl.ELEMENT_ARRAY_BUFFER, len(m.indices) * 4, nil, gl.STATIC_DRAW)
	gl.VertexAttribIPointer(0, 4, gl.UNSIGNED_SHORT, size_of(Vertex), 0)
	gl.EnableVertexAttribArray(0)
	if error := gl.GetError(); error != gl.NO_ERROR {
		fmt.eprintf("GPU mesh allocation failed (0x%x); reducing residency\n", error)
		gl.DeleteVertexArrays(1, &g.vao)
		gl.DeleteBuffers(1, &g.vbo)
		gl.DeleteBuffers(1, &g.ibo)
		return {}
	}
	r.mesh_bytes += g.bytes
	r.peak_bytes = max(r.peak_bytes, r.mesh_bytes)
	return g
}

mesh_upload_part :: proc(g: GPU_Mesh, m: Mesh, offset, limit: int) -> int {
	vertex_bytes := len(m.vertices) * size_of(Vertex)
	remaining := min(limit, vertex_bytes + len(m.indices) * 4 - offset)
	done := 0
	gl.BindVertexArray(g.vao)
	if offset < vertex_bytes {
		count := min(remaining, vertex_bytes - offset)
		gl.BindBuffer(gl.ARRAY_BUFFER, g.vbo)
		gl.BufferSubData(
			gl.ARRAY_BUFFER,
			offset,
			count,
			rawptr(uintptr(raw_data(m.vertices)) + uintptr(offset)),
		)
		done += count
		remaining -= count
	}
	if remaining > 0 {
		at := max(offset + done - vertex_bytes, 0)
		gl.BindBuffer(gl.ELEMENT_ARRAY_BUFFER, g.ibo)
		gl.BufferSubData(
			gl.ELEMENT_ARRAY_BUFFER,
			at,
			remaining,
			rawptr(uintptr(raw_data(m.indices)) + uintptr(at)),
		)
		done += remaining
	}
	return done
}

mesh_release :: proc(r: ^Renderer, g: ^GPU_Mesh) {
	if g.vao != 0 {
		gl.DeleteVertexArrays(1, &g.vao)
		gl.DeleteBuffers(1, &g.vbo)
		gl.DeleteBuffers(1, &g.ibo)
		r.mesh_bytes -= g.bytes
	}
	g^ = {}
}

mesh_recycle :: proc(r: ^Renderer, g: ^GPU_Mesh) {
	if g.vao != 0 && r.pool_count < len(r.pool) && r.pool_bytes + g.bytes <= 2 * MiB {
		r.pool[r.pool_count] = g^
		r.pool_count += 1
		r.pool_bytes += g.bytes
		g^ = {}
	} else {
		mesh_release(r, g)
	}
}

renderer_trim_pool :: proc(r: ^Renderer) {
	for i in 0 ..< r.pool_count {
		mesh_release(r, &r.pool[i])
	}
	r.pool_count = 0
	r.pool_bytes = 0
}

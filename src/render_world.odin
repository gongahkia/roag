package sandbox

import "core:math"
import "core:slice"
import gl "vendor:OpenGL"

Draw_Item :: struct {
	gpu:      ^GPU_Mesh,
	offset:   F3,
	distance: f32,
}

draw_nearer :: proc(a, b: Draw_Item) -> bool {
	return a.distance < b.distance
}

outline :: proc(r: ^Renderer, p: I3, camera: V3, vp: Mat4) {
	minp := F3 {
		f32(f64(p.x) - camera.x) - 0.003,
		f32(f64(p.y) - camera.y) - 0.003,
		f32(f64(p.z) - camera.z) - 0.003,
	}
	for a in 0 ..< 3 {
		for u in 0 ..< 2 {
			for v in 0 ..< 2 {
				q := minp
				q[(a + 1) % 3] += f32(u) * 1.006
				q[(a + 2) % 3] += f32(v) * 1.006
				e := q
				e[a] += 1.006
				append(
					&r.flats,
					Flat_Vertex{q, {0.98, 0.96, 0.73, 1}},
					Flat_Vertex{e, {0.98, 0.96, 0.73, 1}},
				)
			}
		}
	}
	gl.Disable(gl.CULL_FACE)
	flat_flush(r, vp, gl.LINES)
}

Render_View :: struct {
	position:      V3,
	forward:       V3,
	hit:           Ray_Hit,
	width, height: i32,
	frame:         int,
	elapsed:       f64,
	draw_outline:  bool,
}

renderer_world :: proc(
	r: ^Renderer,
	s: ^Stream,
	items: ^[dynamic]Draw_Item,
	w: ^World,
	view: Render_View,
) -> Block {
	renderer_begin(r, view.width, view.height, view.frame)
	vp := camera_matrix(view.forward, f32(view.width) / f32(view.height), f32((s.radius + 2) * 32))
	gl.UniformMatrix4fv(r.vp_loc, 1, false, &vp[0])
	gl.Uniform1f(r.fog_loc, f32(s.radius * 32))
	gl.Uniform1f(r.time_loc, f32(math.mod(view.elapsed, 4096)))
	camera_p := I3 {
		i32(math.floor(view.position.x)),
		i32(math.floor(view.position.y)),
		i32(math.floor(view.position.z)),
	}
	camera_block := world_get(w, camera_p)
	immersion := fluid_kind(camera_block)
	if immersion != .Air &&
	   view.position.y - math.floor(view.position.y) >=
		   f64(fluid_height(camera_block, world_get(w, camera_p + I3{0, 1, 0}))) /
			   8 {immersion = .Air}
	gl.Uniform1i(r.immersion_loc, immersion == .Water ? 1 : (immersion == .Lava ? 2 : 0))
	clear(&items^)
	for k, c in s.chunks {
		if c.gpu.vao == 0 {
			continue
		}
		r.cull_tested += 1
		offset := F3 {
			f32(f64(k.x) * 32 - view.position.x),
			f32(f64(k.y) * 32 - view.position.y),
			f32(f64(k.z) * 32 - view.position.z),
		}
		if !visible(vp, offset) {
			continue
		}
		r.visible_chunks += 1
		center := offset + F3{16, 16, 16}
		append(
			&items^,
			Draw_Item {
				&c.gpu,
				offset,
				center.x * center.x + center.y * center.y + center.z * center.z,
			},
		)
	}
	slice.sort_by(items^[:], draw_nearer)
	for item in items^ {
		count := item.gpu.count - item.gpu.water_count
		if count == 0 {continue}
		gl.Uniform3f(r.offset_loc, item.offset.x, item.offset.y, item.offset.z)
		gl.BindVertexArray(item.gpu.vao)
		gl.DrawElements(gl.TRIANGLES, count, gl.UNSIGNED_INT, nil)
		r.draw_calls += 1
		r.triangles += int(count) / 3
	}
	// Opaque terrain/lava establish depth. Water blends back to front without
	// writing depth, using the same compact buffers and no extra framebuffer.
	gl.Enable(gl.BLEND); gl.BlendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA)
	gl.DepthMask(false); gl.Disable(gl.CULL_FACE)
	for i := len(items^) - 1; i >= 0; i -= 1 {
		item := items^[i]
		if item.gpu.water_count == 0 {continue}
		gl.Uniform3f(r.offset_loc, item.offset.x, item.offset.y, item.offset.z)
		gl.BindVertexArray(item.gpu.vao)
		offset := uintptr(item.gpu.count - item.gpu.water_count) * 4
		gl.DrawElements(gl.TRIANGLES, item.gpu.water_count, gl.UNSIGNED_INT, rawptr(offset))
		r.draw_calls += 1; r.triangles += int(item.gpu.water_count) / 3
	}
	gl.DepthMask(true); gl.Disable(gl.BLEND)
	if view.hit.hit && view.draw_outline {
		outline(r, view.hit.block, view.position, vp)
	}

	return immersion
}

package sandbox

import "core:math"

camera_matrix :: proc(forward: V3, aspect, far: f32) -> Mat4 {
	f := normalize(forward)
	s := normalize(cross(f, {0, 1, 0}))
	u := cross(s, f)
	view := Mat4 {
		f32(s.x),
		f32(u.x),
		f32(-f.x),
		0,
		f32(s.y),
		f32(u.y),
		f32(-f.y),
		0,
		f32(s.z),
		f32(u.z),
		f32(-f.z),
		0,
		0,
		0,
		0,
		1,
	}
	t := f32(1 / math.tan(75.0 * math.PI / 360))
	near: f32 = 0.08
	projection := Mat4 {
		t / aspect,
		0,
		0,
		0,
		0,
		t,
		0,
		0,
		0,
		0,
		(far + near) / (near - far),
		-1,
		0,
		0,
		2 * far * near / (near - far),
		0,
	}
	return mat_mul(projection, view)
}

visible :: proc(vp: Mat4, offset: F3) -> bool {
	center := offset + F3{16, 16, 16}
	for axis in 0 ..< 3 {
		for sign: f32 = -1; sign <= 1; sign += 2 {
			a := vp[3] + sign * vp[axis]
			b := vp[7] + sign * vp[4 + axis]
			c := vp[11] + sign * vp[8 + axis]
			d := vp[15] + sign * vp[12 + axis]
			if a * center.x + b * center.y + c * center.z + d + 16 * (abs(a) + abs(b) + abs(c)) <
			   0 {
				return false
			}
		}
	}
	return true
}

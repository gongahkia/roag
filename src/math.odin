package sandbox

import "core:math"

identity :: proc() -> Mat4 {
	return {1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1}
}

mat_mul :: proc(a, b: Mat4) -> Mat4 {
	r: Mat4
	for c in 0 ..< 4 {
		for row in 0 ..< 4 {
			for k in 0 ..< 4 {
				r[c * 4 + row] += a[k * 4 + row] * b[c * 4 + k]
			}
		}
	}
	return r
}

normalize :: proc(v: V3) -> V3 {
	l := math.sqrt(v.x * v.x + v.y * v.y + v.z * v.z)
	if l == 0 {
		return {}
	}
	return v / l
}

cross :: proc(a, b: V3) -> V3 {
	return {a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x}
}

direction :: proc(yaw, pitch: f64) -> V3 {
	return {math.cos(pitch) * math.cos(yaw), math.sin(pitch), math.cos(pitch) * math.sin(yaw)}
}

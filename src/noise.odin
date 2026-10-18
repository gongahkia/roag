package sandbox

import "core:math"

hash :: proc(x: u64) -> u64 {
	v := x
	v = (v ~ (v >> 30)) * 0xbf58476d1ce4e5b9
	v = (v ~ (v >> 27)) * 0x94d049bb133111eb
	return v ~ (v >> 31)
}

hash3 :: proc(seed: u64, x, y, z: i32) -> u64 {
	return hash(
		seed ~
		u64(i64(x)) * 0x9e3779b185ebca87 ~
		u64(i64(y)) * 0xc2b2ae3d27d4eb4f ~
		u64(i64(z)) * 0x165667b19e3779f9,
	)
}

lerp :: proc(a, b, t: f64) -> f64 {
	return a + (b - a) * t
}

smooth :: proc(t: f64) -> f64 {
	return t * t * (3 - 2 * t)
}

noise2 :: proc(seed: u64, x, z: f64) -> f64 {
	ix, iz := i32(math.floor(x)), i32(math.floor(z))
	fx, fz := smooth(x - f64(ix)), smooth(z - f64(iz))
	a := f64(hash3(seed, ix, 0, iz) & 65535) / 65535
	b := f64(hash3(seed, ix + 1, 0, iz) & 65535) / 65535
	c := f64(hash3(seed, ix, 0, iz + 1) & 65535) / 65535
	d := f64(hash3(seed, ix + 1, 0, iz + 1) & 65535) / 65535
	return lerp(lerp(a, b, fx), lerp(c, d, fx), fz)
}

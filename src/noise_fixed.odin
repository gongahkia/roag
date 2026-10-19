package sandbox

// Q16 arithmetic defines generator 3 independently of floating point codegen.
// Products use i64; inputs are bounded to the game's +/-100 million coordinates.
TQ :: i64(65536)
Noise_D2 :: struct {
	value, dx, dz: i64,
}

qmul :: proc(a, b: i64) -> i64 {return a * b / TQ}
qfloor :: proc(a, b: i64) -> i64 {
	q := a / b
	if a % b < 0 {q -= 1}
	return q
}
qsmooth :: proc(t: i64) -> i64 {return qmul(qmul(t, t), 3 * TQ - 2 * t)}
qramp :: proc(lo, hi, v: i64) -> i64 {return qsmooth(clamp((v - lo) * TQ / (hi - lo), 0, TQ))}
qlerp :: proc(a, b, t: i64) -> i64 {return a + qmul(b - a, t)}

noise_d2 :: proc(seed: u64, x, z: i64) -> Noise_D2 {
	ix, iz := qfloor(x, TQ), qfloor(z, TQ)
	fx, fz := x - ix * TQ, z - iz * TQ
	u, v := qsmooth(fx), qsmooth(fz)
	du, dv := 6 * qmul(fx, TQ - fx), 6 * qmul(fz, TQ - fz)
	a := i64(hash3(seed, i32(ix), 0, i32(iz)) & 65535)
	b := i64(hash3(seed, i32(ix + 1), 0, i32(iz)) & 65535)
	c := i64(hash3(seed, i32(ix), 0, i32(iz + 1)) & 65535)
	d := i64(hash3(seed, i32(ix + 1), 0, i32(iz + 1)) & 65535)
	return {
		qlerp(qlerp(a, b, u), qlerp(c, d, u), v),
		qmul(qlerp(b - a, d - c, v), du),
		qmul(qlerp(c - a, d - b, u), dv),
	}
}

noise_scaled :: proc(seed: u64, x, z, scale: i32) -> i64 {
	return noise_d2(seed, i64(x) * TQ / i64(scale), i64(z) * TQ / i64(scale)).value
}

// Derivatives are transformed back into the base domain before accumulating.
// Their magnitude damps high-frequency detail; it is an erosion-inspired
// shaping signal, not the exact derivative of the resulting damped function.
terrain_fbm :: proc(seed: u64, x, z, scale: i32, ridged := false) -> Noise_D2 {
	px, pz := i64(x) * TQ / i64(scale), i64(z) * TQ / i64(scale)
	u, v, amplitude := TQ, i64(0), TQ
	result: Noise_D2
	for octave in 0 ..< 4 {
		n := noise_d2(seed + u64(octave) * 101, px, pz)
		value := n.value - TQ / 2
		if ridged {
			sign: i64 = 1
			if value > 0 {sign = -1}
			value = TQ / 2 - 2 * abs(value)
			n.dx *= 2 * sign; n.dz *= 2 * sign
		}
		weight := amplitude * (i64(1) << u32(octave))
		result.dx += qmul(qmul(u, n.dx) + qmul(v, n.dz), weight)
		result.dz += qmul(-qmul(v, n.dx) + qmul(u, n.dz), weight)
		damping := TQ * TQ / (TQ + qmul(result.dx, result.dx) + qmul(result.dz, result.dz))
		result.value += qmul(qmul(value, amplitude), damping)
		px, pz = (8 * px - 6 * pz) / 5, (6 * px + 8 * pz) / 5
		u, v = (4 * u - 3 * v) / 5, (3 * u + 4 * v) / 5
		amplitude /= 2
	}
	return result
}

integer_sqrt :: proc(n: i64) -> i64 {
	if n <= 0 {return 0}
	x := n; y := (x + 1) / 2
	for y < x {x = y; y = (x + n / x) / 2}
	return x
}

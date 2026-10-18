package sandbox

floor_div :: proc(x, d: i32) -> i32 {
	q := x / d
	if x % d < 0 {
		q -= 1
	}
	return q
}

chunk_of :: proc(p: I3) -> I3 {
	return {floor_div(p.x, 32), floor_div(p.y, 32), floor_div(p.z, 32)}
}

block_index :: proc(x, y, z: int) -> int {
	return x + PAD * (y + PAD * z)
}

package sandbox

import "core:math"

Ray_Hit :: struct {
	hit:             bool,
	block, adjacent: I3,
}

raycast :: proc(w: ^World, origin, direction: V3, reach: f64) -> Ray_Hit {
	p := I3{i32(math.floor(origin.x)), i32(math.floor(origin.y)), i32(math.floor(origin.z))}
	step: I3
	delta, tmax: V3
	for a in 0 ..< 3 {
		if abs(direction[a]) < 1e-12 {
			delta[a] = 1e30
			tmax[a] = 1e30
			continue
		}
		step[a] = 1
		if direction[a] < 0 {
			step[a] = -1
		}
		delta[a] = abs(1 / direction[a])
		boundary := f64(p[a])
		if step[a] > 0 {
			boundary += 1
		}
		tmax[a] = (boundary - origin[a]) / direction[a]
	}
	prev := p
	for _ in 0 ..< 128 {
		if world_get(w, p) != .Air {
			return {true, p, prev}
		}
		a := 0
		if tmax[1] < tmax[a] {
			a = 1
		}
		if tmax[2] < tmax[a] {
			a = 2
		}
		if tmax[a] > reach {
			break
		}
		prev = p
		p[a] += step[a]
		tmax[a] += delta[a]
	}
	return {}
}

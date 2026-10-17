package gameplay

import "math"

const navStep = .65
const navMinX = -9.1
const navMinZ = -.65

// A shared breadth-first distance field is rebuilt at 4 Hz. Enemies follow it
// around cover when line of sight is blocked; no per-enemy path allocation.
func (r *Run) buildFlow() {
	r.flowW = 29
	r.flowH = int(math.Ceil((r.World.Rooms[len(r.World.Rooms)-1].Bounds.MaxZ-navMinZ)/navStep)) + 1
	n := r.flowW * r.flowH
	if len(r.flow) != n {
		r.flow = make([]int, n)
	}
	for i := range r.flow {
		r.flow[i] = -2
		if r.World.Walkable(r.navPosition(i), .39, r.Cleared) {
			r.flow[i] = -1
		}
	}
	start := r.navIndex(r.Player.Position)
	if start < 0 {
		return
	}
	// A player can stand closer to cover than an enemy's footprint allows.
	if r.flow[start] == -2 {
		best := math.Inf(1)
		candidate := -1
		for i, d := range r.flow {
			if d == -1 {
				if dist := distance(r.navPosition(i), r.Player.Position); dist < best {
					best = dist
					candidate = i
				}
			}
		}
		start = candidate
	}
	if start < 0 {
		return
	}
	r.flow[start] = 0
	queue := make([]int, 1, n)
	queue[0] = start
	for head := 0; head < len(queue); head++ {
		i := queue[head]
		for _, j := range r.neighbors(i) {
			if j >= 0 && r.flow[j] == -1 {
				r.flow[j] = r.flow[i] + 1
				queue = append(queue, j)
			}
		}
	}
}

func (r *Run) navPosition(i int) Vec3 {
	return Vec3{X: navMinX + float64(i%r.flowW)*navStep, Z: navMinZ + float64(i/r.flowW)*navStep}
}
func (r *Run) navIndex(p Vec3) int {
	if r.flowW == 0 {
		return -1
	}
	x, z := int(math.Round((p.X-navMinX)/navStep)), int(math.Round((p.Z-navMinZ)/navStep))
	if x < 0 || x >= r.flowW || z < 0 || z >= r.flowH {
		return -1
	}
	return z*r.flowW + x
}
func (r *Run) neighbors(i int) [4]int {
	n := [4]int{-1, -1, -1, -1}
	if i%r.flowW > 0 {
		n[0] = i - 1
	}
	if i%r.flowW < r.flowW-1 {
		n[1] = i + 1
	}
	if i >= r.flowW {
		n[2] = i - r.flowW
	}
	if i+r.flowW < len(r.flow) {
		n[3] = i + r.flowW
	}
	return n
}
func (r *Run) chaseDirection(pos Vec3) Vec3 {
	i := r.navIndex(pos)
	if i < 0 || len(r.flow) == 0 {
		return Vec3{}
	}
	best := i
	for _, j := range r.neighbors(i) {
		if j >= 0 && r.flow[j] >= 0 && (r.flow[best] < 0 || r.flow[j] < r.flow[best]) {
			best = j
		}
	}
	return r.navPosition(best).Sub(pos).Flat().Unit()
}

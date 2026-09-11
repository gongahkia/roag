package game

import "math"

type World struct {
	W, H      int
	Cells     [][]byte
	Doors     map[Point]bool // true means open
	Hides     map[Point]bool
	Items     []Item
	Generator Point
	Exit      Point
}

func NewWorld() *World {
	w := &World{W: 41, H: 25, Doors: map[Point]bool{}, Hides: map[Point]bool{}}
	w.Cells = make([][]byte, w.H)
	for y := range w.Cells {
		w.Cells[y] = make([]byte, w.W)
		for x := range w.Cells[y] {
			w.Cells[y][x] = '#'
		}
	}
	room := func(x1, y1, x2, y2 int) {
		for y := y1; y <= y2; y++ {
			for x := x1; x <= x2; x++ {
				w.Cells[y][x] = '.'
			}
		}
	}
	corridor := room
	room(1, 1, 9, 8)
	room(13, 1, 24, 8)
	room(29, 1, 39, 8)
	room(1, 11, 39, 13)
	room(1, 16, 11, 23)
	room(15, 16, 26, 23)
	room(31, 16, 39, 23)
	corridor(4, 8, 6, 11)
	corridor(18, 8, 20, 11)
	corridor(34, 8, 36, 11)
	corridor(5, 13, 7, 16)
	corridor(20, 13, 22, 16)
	corridor(34, 13, 36, 16)
	// partitions create loops and useful line-of-sight breaks.
	for y := 2; y <= 7; y++ {
		w.Cells[y][17] = '#'
		w.Cells[y][34] = '#'
	}
	w.Cells[5][17] = '.'
	w.Cells[3][34] = '.'
	for x := 3; x <= 9; x++ {
		w.Cells[19][x] = '#'
	}
	w.Cells[19][6] = '.'
	for x := 17; x <= 24; x++ {
		w.Cells[19][x] = '#'
	}
	w.Cells[19][22] = '.'
	for y := 17; y <= 22; y++ {
		w.Cells[y][35] = '#'
	}
	w.Cells[21][35] = '.'
	for _, p := range []Point{{5, 10}, {19, 10}, {35, 10}, {6, 14}, {21, 14}, {35, 14}, {17, 5}, {34, 3}, {6, 19}, {22, 19}, {35, 21}} {
		w.Doors[p] = false
		w.Cells[p.Y][p.X] = '.'
	}
	for _, p := range []Point{{2, 7}, {8, 2}, {14, 7}, {23, 2}, {30, 7}, {38, 2}, {2, 22}, {10, 17}, {16, 22}, {25, 17}, {32, 22}, {38, 17}} {
		w.Hides[p] = true
	}
	w.Generator = Point{3, 3}
	w.Exit = Point{39, 12}
	w.Items = []Item{
		{At: Vec{7.5, 5.5}, Kind: Fuse}, {At: Vec{21.5, 3.5}, Kind: Fuse}, {At: Vec{37.5, 20.5}, Kind: Fuse},
		{At: Vec{15.5, 7.5}, Kind: Decoy}, {At: Vec{25.5, 17.5}, Kind: Decoy},
		{At: Vec{30.5, 3.5}, Kind: Flare}, {At: Vec{9.5, 22.5}, Kind: Flare},
		{At: Vec{23.5, 7.5}, Kind: Battery}, {At: Vec{16.5, 17.5}, Kind: StunCharge},
	}
	return w
}

func (w *World) Inside(p Point) bool { return p.X >= 0 && p.Y >= 0 && p.X < w.W && p.Y < w.H }
func (w *World) Blocked(p Point) bool {
	if !w.Inside(p) || w.Cells[p.Y][p.X] == '#' {
		return true
	}
	if open, door := w.Doors[p]; door {
		return !open
	}
	return false
}
func (w *World) Passable(v Vec) bool {
	r := 0.18
	return !w.Blocked(Vec{v.X - r, v.Y - r}.Point()) && !w.Blocked(Vec{v.X + r, v.Y - r}.Point()) && !w.Blocked(Vec{v.X - r, v.Y + r}.Point()) && !w.Blocked(Vec{v.X + r, v.Y + r}.Point())
}
func (w *World) LineClear(a, b Vec) bool {
	d := b.Sub(a)
	distance := d.Len()
	if distance == 0 {
		return true
	}
	step := d.Normalized().Mul(.08)
	for p, n := a, 0.0; n < distance; p, n = p.Add(step), n+.08 {
		if w.Blocked(p.Point()) {
			return false
		}
	}
	return true
}
func (w *World) Ray(from Vec, angle, max float64) (float64, Point) {
	d := Vec{math.Cos(angle), math.Sin(angle)}.Mul(.035)
	p := from
	for dist := 0.0; dist < max; dist += .035 {
		p = p.Add(d)
		if w.Blocked(p.Point()) {
			return dist, p.Point()
		}
	}
	return max, p.Point()
}

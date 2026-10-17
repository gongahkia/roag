// Package gameplay owns the fixed-step game rules. It depends only on the Go
// standard library: a window, renderer, sound device, and wall clock are optional.
package gameplay

import (
	"math"
	"math/rand"
)

const (
	TickRate     = 60
	RoomCount    = 5
	RunTicks     = 5 * 60 * TickRate
	PlayerRadius = 0.28
	EyeHeight    = 1.45
)

type Vec3 struct{ X, Y, Z float64 }

func (v Vec3) Add(w Vec3) Vec3      { return Vec3{v.X + w.X, v.Y + w.Y, v.Z + w.Z} }
func (v Vec3) Sub(w Vec3) Vec3      { return Vec3{v.X - w.X, v.Y - w.Y, v.Z - w.Z} }
func (v Vec3) Scale(s float64) Vec3 { return Vec3{v.X * s, v.Y * s, v.Z * s} }
func (v Vec3) Length() float64      { return math.Sqrt(v.X*v.X + v.Y*v.Y + v.Z*v.Z) }
func (v Vec3) Unit() Vec3 {
	if n := v.Length(); n > 0 {
		return v.Scale(1 / n)
	}
	return Vec3{}
}
func (v Vec3) Flat() Vec3        { return Vec3{X: v.X, Z: v.Z} }
func distance(a, b Vec3) float64 { return a.Sub(b).Length() }

type Rect struct{ MinX, MinZ, MaxX, MaxZ float64 }

func (r Rect) Contains(x, z float64) bool {
	return x >= r.MinX && x <= r.MaxX && z >= r.MinZ && z <= r.MaxZ
}

type Solid struct {
	Bounds   Rect
	Height   float64
	Material string
}

type Spawn struct {
	Position Vec3
	Kind     EnemyKind
}
type Room struct {
	Bounds Rect
	Spawns []Spawn
	Gate   Solid
}

// World is generated once and is immutable for the lifetime of a run. Gates
// derive their state from State.Cleared, rather than mutating the level.
type World struct {
	Seed        int64
	Rooms       []Room
	Floors      []Rect
	Solids      []Solid
	Spawn, Exit Vec3
}

func Generate(seed int64) *World {
	rng := rand.New(rand.NewSource(seed))
	w := &World{Seed: seed, Spawn: Vec3{Z: 2.5}}
	z := 0.0
	for i := 0; i < RoomCount; i++ {
		half := 6.0 + float64(rng.Intn(3))
		depth := 14.0 + float64(rng.Intn(3))
		r := Room{Bounds: Rect{-half, z, half, z + depth}, Gate: Solid{Rect{-1.55, z + depth - .15, 1.55, z + depth + .15}, 3.6, "rune"}}
		w.Floors = append(w.Floors, r.Bounds)
		// Walls leave a central doorway at either end. Corridor rails bridge rooms.
		w.Solids = append(w.Solids,
			Solid{Rect{-half - .25, z - .25, -half, z + depth + .25}, 3.6, "dark"},
			Solid{Rect{half, z - .25, half + .25, z + depth + .25}, 3.6, "dark"},
			Solid{Rect{-half, z + depth, -1.55, z + depth + .25}, 3.6, "stone"},
			Solid{Rect{1.55, z + depth, half, z + depth + .25}, 3.6, "stone"})
		if i == 0 {
			w.Solids = append(w.Solids, Solid{Rect{-half, -.25, half, 0}, 3.6, "stone"})
		} else {
			w.Solids = append(w.Solids,
				Solid{Rect{-half, z - .25, -1.55, z}, 3.6, "stone"},
				Solid{Rect{1.55, z - .25, half, z}, 3.6, "stone"})
		}
		// Seeded cover layouts always leave the central route and spawn lanes open.
		for side := -1; side <= 1; side += 2 {
			x := float64(side) * (3 + float64(rng.Intn(2))*.5)
			pz := z + 4.5 + float64(rng.Intn(2))
			w.Solids = append(w.Solids, Solid{Rect{x - .6, pz - .6, x + .6, pz + .6}, 2.6, "stone"})
		}
		for n := 0; n < 3+i; n++ {
			x := (float64(n%3)-1)*3.2 + rng.Float64()*.5 - .25
			pz := z + depth - 3 - float64(n/3)*2
			kind := Charger
			if (n+i)%3 == 1 {
				kind = Caster
			}
			r.Spawns = append(r.Spawns, Spawn{Vec3{X: x, Z: pz}, kind})
		}
		w.Rooms = append(w.Rooms, r)
		if i < RoomCount-1 {
			w.Floors = append(w.Floors, Rect{-1.55, z + depth, 1.55, z + depth + 4})
			w.Solids = append(w.Solids,
				Solid{Rect{-1.8, z + depth, -1.55, z + depth + 4}, 2.4, "dark"},
				Solid{Rect{1.55, z + depth, 1.8, z + depth + 4}, 2.4, "dark"})
		}
		z += depth + 4
	}
	last := &w.Rooms[len(w.Rooms)-1]
	w.Exit = Vec3{Z: last.Bounds.MaxZ - 1.4}
	return w
}

func (w *World) Walkable(pos Vec3, radius float64, cleared []bool) bool {
	// Test the footprint against the union of floors, including doorway seams.
	for _, o := range [...]Vec3{{}, {X: radius}, {X: -radius}, {Z: radius}, {Z: -radius},
		{X: radius * .707, Z: radius * .707}, {X: -radius * .707, Z: radius * .707},
		{X: radius * .707, Z: -radius * .707}, {X: -radius * .707, Z: -radius * .707}} {
		inside := false
		for _, r := range w.Floors {
			if r.Contains(pos.X+o.X, pos.Z+o.Z) {
				inside = true
				break
			}
		}
		if !inside {
			return false
		}
	}
	blocked := func(s Solid) bool {
		if pos.Y >= s.Height-.001 {
			return false
		}
		r := s.Bounds
		x := math.Max(r.MinX, math.Min(pos.X, r.MaxX))
		z := math.Max(r.MinZ, math.Min(pos.Z, r.MaxZ))
		return math.Hypot(pos.X-x, pos.Z-z) < radius
	}
	for _, s := range w.Solids {
		if blocked(s) {
			return false
		}
	}
	for i, r := range w.Rooms {
		if !cleared[i] && blocked(r.Gate) {
			return false
		}
	}
	return true
}

func (r *Run) move(pos, delta Vec3, radius float64, ignoreID uint32) Vec3 {
	height := 1.8
	if ignoreID != 0 {
		height = 1.95
	}
	overlap := func(a, b Vec3, otherRadius, otherHeight float64) bool {
		return a.Y < b.Y+otherHeight && a.Y+height > b.Y && distance(a.Flat(), b.Flat()) < radius+otherRadius
	}
	free := func(next Vec3) bool {
		if !r.World.Walkable(next, radius, r.Cleared) {
			return false
		}
		if ignoreID != 0 && overlap(next, r.Player.Position, PlayerRadius, 1.8) {
			return false
		}
		for _, e := range r.Enemies {
			if e.ID != ignoreID && e.Health > 0 && overlap(next, e.Position, .38, 1.95) {
				return false
			}
		}
		return true
	}
	// Substeps also cover fast dashes and charging enemies without tunneling.
	steps := max(1, int(math.Ceil(delta.Length()/.12)))
	delta = delta.Scale(1 / float64(steps))
	for i := 0; i < steps; i++ {
		next := pos
		next.X += delta.X
		if free(next) {
			pos.X = next.X
		}
		next = pos
		next.Z += delta.Z
		if free(next) {
			pos.Z = next.Z
		}
	}
	return pos
}

// RayBox returns the entry distance along a unit ray, or +Inf on a miss.
func RayBox(origin, dir, lo, hi Vec3) float64 {
	tmin, tmax := 0.0, math.Inf(1)
	for _, a := range [][4]float64{{origin.X, dir.X, lo.X, hi.X}, {origin.Y, dir.Y, lo.Y, hi.Y}, {origin.Z, dir.Z, lo.Z, hi.Z}} {
		if math.Abs(a[1]) < 1e-10 {
			if a[0] < a[2] || a[0] > a[3] {
				return math.Inf(1)
			}
			continue
		}
		a0, a1 := (a[2]-a[0])/a[1], (a[3]-a[0])/a[1]
		if a0 > a1 {
			a0, a1 = a1, a0
		}
		tmin = math.Max(tmin, a0)
		tmax = math.Min(tmax, a1)
		if tmax < tmin {
			return math.Inf(1)
		}
	}
	return tmin
}

func (w *World) Raycast(origin, dir Vec3, limit float64, cleared []bool) float64 {
	d := limit
	check := func(s Solid) {
		r := s.Bounds
		d = math.Min(d, RayBox(origin, dir, Vec3{r.MinX, 0, r.MinZ}, Vec3{r.MaxX, s.Height, r.MaxZ}))
	}
	for _, s := range w.Solids {
		check(s)
	}
	for i, r := range w.Rooms {
		if !cleared[i] {
			check(r.Gate)
		}
	}
	if dir.Y < 0 {
		d = math.Min(d, -origin.Y/dir.Y)
	}
	return d
}

func (w *World) visible(a, b Vec3, cleared []bool) bool {
	d := distance(a, b)
	return d < .01 || w.Raycast(a, b.Sub(a).Scale(1/d), d, cleared) >= d-.01
}

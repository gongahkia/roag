// Package arena turns gameplay state into software-rendered geometry and UI.
// It never changes the simulation; its effects are disposable client state.
package arena

import (
	"math"

	"rend3rer/internal/gameplay"
	"rend3rer/internal/soft3d"
)

type spark struct {
	position, velocity gameplay.Vec3
	life               int
	color              uint32
}
type View struct {
	static                         [][]soft3d.Quad
	textures                       map[string]*soft3d.Texture
	sparks                         []spark
	ShotTicks, HitTicks, HurtTicks int
	Message                        string
	MessageTicks                   int
	Ticks                          int
}

func New(w *gameplay.World) *View {
	v := &View{textures: make(map[string]*soft3d.Texture), static: make([][]soft3d.Quad, len(w.Rooms))}
	for name, c := range map[string]uint32{"stone": 0x66777f, "dark": 0x283c49, "gold": 0xcca264, "rune": 0x49c8cc, "red": 0xbc524b, "cyan": 0x66ced5, "white": 0xf9eed0, "black": 0x101f2a} {
		t := &soft3d.Texture{W: 16, H: 16, Pix: make([]uint32, 256)}
		for y := 0; y < 16; y++ {
			for x := 0; x < 16; x++ {
				col := c
				if name == "stone" || name == "dark" {
					if y%8 == 0 || (x+(y/8)*4)%8 == 0 {
						col = shade(c, .55)
					} else {
						col = shade(c, .92+float64((x*13+y*7)%5)*.025)
					}
				}
				if name == "gold" && (x%8 == 0 || y%8 == 0) {
					col = shade(c, 1.2)
				}
				t.Pix[y*16+x] = col
			}
		}
		v.textures[name] = t
	}
	for room, rr := range w.Rooms {
		q := &v.static[room]
		b := rr.Bounds
		// Small floor patches keep the intentionally affine mapping readable.
		for z := b.MinZ; z < b.MaxZ; z += 2 {
			for x := b.MinX; x < b.MaxX; x += 2 {
				color := "stone"
				if int((x-b.MinX)/2+(z-b.MinZ)/2)%2 == 0 {
					color = "dark"
				}
				v.floor(q, x, z, math.Min(x+2, b.MaxX), math.Min(z+2, b.MaxZ), 0, color)
			}
		}
		if room < len(w.Rooms)-1 {
			v.floor(q, -1.55, b.MaxZ, 1.55, b.MaxZ+4, 0, "dark")
		}
		for _, s := range w.Solids {
			if s.Bounds.MinZ > b.MaxZ+4 || s.Bounds.MaxZ < b.MinZ {
				continue
			}
			v.solid(q, s, 1)
			// Warm caps and narrow seams tie the arena to the original shrine.
			r := s.Bounds
			v.box(q, gameplay.Vec3{X: r.MinX, Y: s.Height - .12, Z: r.MinZ}, gameplay.Vec3{X: r.MaxX, Y: s.Height, Z: r.MaxZ}, "gold", 1)
		}
		for _, x := range []float64{-1.7, 1.7} {
			v.box(q, gameplay.Vec3{X: x - .12, Z: b.MaxZ - .2}, gameplay.Vec3{X: x + .12, Y: 3.8, Z: b.MaxZ + .25}, "gold", 1)
		}
		v.box(q, gameplay.Vec3{X: -1.8, Y: 3.6, Z: b.MaxZ - .2}, gameplay.Vec3{X: 1.8, Y: 3.85, Z: b.MaxZ + .25}, "gold", 1)
		// Side lanterns provide strong room silhouettes without dynamic lighting.
		for _, x := range []float64{b.MinX + .25, b.MaxX - .25} {
			for _, z := range []float64{b.MinZ + 3, b.MaxZ - 3} {
				v.box(q, gameplay.Vec3{X: x - .12, Y: 1.7, Z: z - .16}, gameplay.Vec3{X: x + .12, Y: 2.15, Z: z + .16}, "rune", 1)
			}
		}
	}
	return v
}

func shade(c uint32, f float64) uint32 {
	clamp := func(n uint32) uint32 { return uint32(math.Min(255, float64(n)*f)) }
	return clamp((c>>16)&255)<<16 | clamp((c>>8)&255)<<8 | clamp(c&255)
}
func vec(p gameplay.Vec3) soft3d.Vec3 { return soft3d.Vec3{X: p.X, Y: p.Y, Z: p.Z} }
func (v *View) quad(dst *[]soft3d.Quad, a, b, c, d gameplay.Vec3, material string, u, t, brightness float64) {
	*dst = append(*dst, soft3d.Quad{V: [4]soft3d.Vertex{{Pos: vec(a)}, {Pos: vec(b), U: u}, {Pos: vec(c), U: u, V: t}, {Pos: vec(d), V: t}}, Texture: v.textures[material], Tint: [3]float64{brightness, brightness, brightness}, TwoSided: true})
}
func (v *View) box(dst *[]soft3d.Quad, lo, hi gameplay.Vec3, material string, brightness float64) {
	a, b := gameplay.Vec3{X: lo.X, Y: lo.Y, Z: lo.Z}, gameplay.Vec3{X: hi.X, Y: lo.Y, Z: lo.Z}
	c, d := gameplay.Vec3{X: hi.X, Y: lo.Y, Z: hi.Z}, gameplay.Vec3{X: lo.X, Y: lo.Y, Z: hi.Z}
	e, f := gameplay.Vec3{X: lo.X, Y: hi.Y, Z: lo.Z}, gameplay.Vec3{X: hi.X, Y: hi.Y, Z: lo.Z}
	g, h := gameplay.Vec3{X: hi.X, Y: hi.Y, Z: hi.Z}, gameplay.Vec3{X: lo.X, Y: hi.Y, Z: hi.Z}
	// Proper winding allows back-face culling for opaque boxes.
	start := len(*dst)
	v.quad(dst, e, f, b, a, material, 1, 1, brightness*.8)
	v.quad(dst, f, g, c, b, material, 1, 1, brightness*.7)
	v.quad(dst, g, h, d, c, material, 1, 1, brightness*.9)
	v.quad(dst, h, e, a, d, material, 1, 1, brightness*.75)
	v.quad(dst, h, g, f, e, material, 1, 1, brightness)
	for i := start; i < len(*dst); i++ {
		(*dst)[i].TwoSided = false
	}
}
func (v *View) floor(q *[]soft3d.Quad, x0, z0, x1, z1, y float64, material string) {
	v.quad(q, gameplay.Vec3{X: x0, Y: y, Z: z0}, gameplay.Vec3{X: x1, Y: y, Z: z0}, gameplay.Vec3{X: x1, Y: y, Z: z1}, gameplay.Vec3{X: x0, Y: y, Z: z1}, material, (x1-x0)/2, (z1-z0)/2, 1)
}
func (v *View) solid(q *[]soft3d.Quad, s gameplay.Solid, b float64) {
	r := s.Bounds
	v.box(q, gameplay.Vec3{X: r.MinX, Z: r.MinZ}, gameplay.Vec3{X: r.MaxX, Y: s.Height, Z: r.MaxZ}, s.Material, b)
}

func (v *View) Update(events []gameplay.Event) {
	v.Ticks++
	v.ShotTicks = max(0, v.ShotTicks-1)
	v.HitTicks = max(0, v.HitTicks-1)
	v.HurtTicks = max(0, v.HurtTicks-1)
	v.MessageTicks = max(0, v.MessageTicks-1)
	alive := v.sparks[:0]
	for _, s := range v.sparks {
		s.life--
		if s.life > 0 {
			s.position = s.position.Add(s.velocity.Scale(1.0 / 60))
			s.velocity.Y -= .06
			alive = append(alive, s)
		}
	}
	v.sparks = alive
	for _, e := range events {
		switch e.Kind {
		case gameplay.Shot:
			v.ShotTicks = 7
			v.burst(e.Target, 0xe3b875, 4)
		case gameplay.Hit:
			v.HitTicks = 8
			v.burst(e.Position, 0xffdd9b, 5)
		case gameplay.Kill:
			v.burst(e.Position, 0xd7745a, 14)
		case gameplay.Hurt:
			v.HurtTicks = 16
		case gameplay.RoomClear:
			v.Message = "CHAMBER PURIFIED  +12 HEALTH"
			v.MessageTicks = 150
		case gameplay.UpgradeTaken:
			v.Message = "RELIC BOUND. THE NEXT GATE IS OPEN."
			v.MessageTicks = 150
		case gameplay.Healed:
			v.Message = "TIDE ESSENCE  +22 HEALTH"
			v.MessageTicks = 90
		}
	}
}
func (v *View) burst(p gameplay.Vec3, c uint32, n int) {
	for i := 0; i < n; i++ {
		a := float64(i)*2.399 + float64(v.Ticks)
		v.sparks = append(v.sparks, spark{p, gameplay.Vec3{X: math.Sin(a) * 2, Y: 1 + math.Cos(a*3), Z: math.Cos(a) * 2}, 18 + i%10, c})
	}
}

func (v *View) Quads(r *gameplay.Run, cam soft3d.Camera) []soft3d.Quad {
	q := make([]soft3d.Quad, 0, 1100)
	cameraPosition := gameplay.Vec3{X: cam.Position.X, Y: cam.Position.Y, Z: cam.Position.Z}
	// Rendering follows the camera, including backtracking. The simulation's
	// Room tracks encounter progress and can stay ahead of the player's location.
	nearRoom := 0
	best := math.Inf(1)
	for i, room := range r.World.Rooms {
		if d := math.Abs((room.Bounds.MinZ+room.Bounds.MaxZ)/2 - cam.Position.Z); d < best {
			best = d
			nearRoom = i
		}
	}
	for i := max(0, nearRoom-1); i <= min(len(v.static)-1, nearRoom+1); i++ {
		q = append(q, v.static[i]...)
	}
	for i, room := range r.World.Rooms {
		if i < nearRoom-1 || i > nearRoom+1 {
			continue
		}
		if !r.Cleared[i] {
			b := room.Gate.Bounds
			for x := b.MinX; x < b.MaxX; x += .5 {
				v.box(&q, gameplay.Vec3{X: x, Z: b.MinZ}, gameplay.Vec3{X: x + .22, Y: 3.6, Z: b.MaxZ}, "rune", .8)
			}
			// The solid backing matches the simulation's closed gate hitbox.
			v.solid(&q, gameplay.Solid{Bounds: b, Height: 3.6, Material: "dark"}, .8)
		}
	}
	for _, e := range r.Enemies {
		if e.Health <= 0 || distance2(e.Position, cameraPosition) > 35 {
			continue
		}
		p := e.Position
		mat := "red"
		if e.Kind == gameplay.Caster {
			mat = "cyan"
			p.Y = .12 + math.Sin(float64(r.Tick)*.055+float64(e.ID))*.1
		}
		if e.Flash > 0 {
			mat = "white"
		}
		bright := 1.0
		if e.Windup > 0 {
			bright = 1.3 + math.Sin(float64(e.Windup)*.6)*.35
		}
		v.box(&q, p.Add(gameplay.Vec3{X: -.32, Y: .4, Z: -.28}), p.Add(gameplay.Vec3{X: .32, Y: 1.35, Z: .28}), mat, bright)
		v.box(&q, p.Add(gameplay.Vec3{X: -.26, Y: 1.4, Z: -.26}), p.Add(gameplay.Vec3{X: .26, Y: 1.9, Z: .26}), "dark", 1)
		// An eye on every face keeps silhouettes readable from any direction.
		v.box(&q, p.Add(gameplay.Vec3{X: -.28, Y: 1.57, Z: -.28}), p.Add(gameplay.Vec3{X: .28, Y: 1.68, Z: .28}), "gold", bright)
		for _, x := range []float64{-.49, .34} {
			v.box(&q, p.Add(gameplay.Vec3{X: x, Y: .65, Z: -.15}), p.Add(gameplay.Vec3{X: x + .15, Y: 1.22, Z: .15}), mat, bright)
		}
		if e.Kind == gameplay.Charger {
			for _, x := range []float64{-.28, .08} {
				v.box(&q, p.Add(gameplay.Vec3{X: x, Z: -.2}), p.Add(gameplay.Vec3{X: x + .2, Y: .42, Z: .2}), "dark", 1)
			}
		}
		if e.Windup > 0 {
			v.floor(&q, p.X-.65, p.Z-.65, p.X+.65, p.Z+.65, .025, mat)
		}
	}
	for _, b := range r.Projectiles {
		v.box(&q, b.Position.Add(gameplay.Vec3{X: -.13, Y: -.13, Z: -.13}), b.Position.Add(gameplay.Vec3{X: .13, Y: .13, Z: .13}), "cyan", 1.5)
	}
	for _, p := range r.Pickups {
		if !p.Taken && distance2(p.Position, cameraPosition) < 35 {
			y := .45 + math.Sin(float64(v.Ticks)*.06)*.1
			v.box(&q, p.Position.Add(gameplay.Vec3{X: -.2, Y: y, Z: -.2}), p.Position.Add(gameplay.Vec3{X: .2, Y: y + .35, Z: .2}), "cyan", 1.3)
			v.box(&q, p.Position.Add(gameplay.Vec3{X: -.07, Y: y - .1, Z: -.24}), p.Position.Add(gameplay.Vec3{X: .07, Y: y + .45, Z: .24}), "white", 1)
		}
	}
	if nearRoom >= len(r.Cleared)-2 {
		p := r.World.Exit
		v.box(&q, p.Add(gameplay.Vec3{X: -.9, Z: -.35}), p.Add(gameplay.Vec3{X: .9, Y: .18, Z: .35}), "gold", 1)
		mat := "dark"
		if r.Cleared[len(r.Cleared)-1] {
			mat = "cyan"
		}
		v.box(&q, p.Add(gameplay.Vec3{X: -.55, Y: .18, Z: -.12}), p.Add(gameplay.Vec3{X: .55, Y: 2.5, Z: .12}), mat, 1.3)
	}
	// Tiny camera-facing quads give impact particles a depth-tested footprint.
	right := gameplay.Vec3{X: math.Cos(cam.Yaw) * .035, Z: -math.Sin(cam.Yaw) * .035}
	up := gameplay.Vec3{Y: .035}
	for _, s := range v.sparks {
		t := &soft3d.Texture{W: 1, H: 1, Pix: []uint32{s.color}}
		start := len(q)
		v.quad(&q, s.position.Sub(right).Sub(up), s.position.Add(right).Sub(up), s.position.Add(right).Add(up), s.position.Sub(right).Add(up), "white", 1, 1, 1)
		q[start].Texture = t
	}
	return q
}
func distance2(a, b gameplay.Vec3) float64 { return a.Sub(b).Length() }

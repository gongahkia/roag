package soft3d

import "math"

type Scene struct {
	Static     []Quad
	textures   map[string]*Texture
	orbit      *OrbitSpec
	reflection *ReflectionSpec
}

func (l *Level) BuildScene() (*Scene, error) {
	if err := l.Validate(); err != nil {
		return nil, err
	}
	s := &Scene{textures: make(map[string]*Texture, len(l.Materials)), orbit: l.Animations.Orbit, reflection: l.Animations.Reflection}
	for name, pattern := range l.Materials {
		s.textures[name] = makeTexture(pattern)
	}
	for _, g := range l.Geometry {
		tex := s.textures[g.Material]
		switch g.Type {
		case "box":
			for _, x := range repeat(g.RepeatX) {
				for _, z := range repeat(g.RepeatZ) {
					lo, hi := g.Min.vec(), g.Max.vec()
					lo.X, hi.X = lo.X+x, hi.X+x
					lo.Z, hi.Z = lo.Z+z, hi.Z+z
					box(&s.Static, lo, hi, tex, g.Tile[0], g.Tile[1])
				}
			}
		case "quad":
			q := makeQuad(g.Vertices[0].vec(), g.Vertices[1].vec(), g.Vertices[2].vec(), g.Vertices[3].vec(), tex, g.Tile[0], g.Tile[1])
			q.TwoSided = g.TwoSided
			s.Static = append(s.Static, q)
		}
	}
	return s, nil
}

func (s *Scene) Quads(tick float64) []Quad {
	q := make([]Quad, 0, len(s.Static)+16)
	q = append(q, s.Static...)
	if o := s.orbit; o != nil {
		for i := 0; i < o.Count; i++ {
			a := tick*o.Speed + float64(i)*2*math.Pi/float64(o.Count)
			b := a + o.Arc
			r := o.Radius
			y := o.Center[1] + o.Wobble*math.Sin(tick+float64(i))
			quad := makeQuad(
				Vec3{o.Center[0] + r*math.Cos(a), y - o.Height/2, o.Center[2] + r*math.Sin(a)},
				Vec3{o.Center[0] + r*math.Cos(b), y - o.Height/2, o.Center[2] + r*math.Sin(b)},
				Vec3{o.Center[0] + r*math.Cos(b+0.12), y + o.Height/2, o.Center[2] + r*math.Sin(b+0.12)},
				Vec3{o.Center[0] + r*math.Cos(a+0.12), y + o.Height/2, o.Center[2] + r*math.Sin(a+0.12)},
				s.textures[o.Material], 1, 1,
			)
			quad.TwoSided = true
			q = append(q, quad)
		}
	}
	if a := s.reflection; a != nil {
		for i := 0; i < a.Count; i++ {
			z := a.Center[2] + float64(i)*a.Spacing
			w := a.Width + a.Wobble*math.Sin(tick*a.Speed+float64(i))
			x, y := a.Center[0], a.Center[1]
			strip := makeQuad(Vec3{x - w, y, z}, Vec3{x + w, y, z}, Vec3{x + w + 0.3, y, z + 0.08}, Vec3{x - w - 0.3, y, z + 0.08}, s.textures[a.Material], 1, 1)
			strip.TwoSided = true
			q = append(q, strip)
		}
	}
	return q
}

func makeQuad(a, b, c, d Vec3, tex *Texture, u, v float64) Quad {
	return Quad{V: [4]Vertex{{a, 0, 0}, {b, u, 0}, {c, u, v}, {d, 0, v}}, Texture: tex, Tint: [3]float64{1, 1, 1}}
}

func box(dst *[]Quad, lo, hi Vec3, tex *Texture, u, v float64) {
	a, b := Vec3{lo.X, lo.Y, lo.Z}, Vec3{hi.X, lo.Y, lo.Z}
	c, d := Vec3{hi.X, lo.Y, hi.Z}, Vec3{lo.X, lo.Y, hi.Z}
	e, f := Vec3{lo.X, hi.Y, lo.Z}, Vec3{hi.X, hi.Y, lo.Z}
	g, h := Vec3{hi.X, hi.Y, hi.Z}, Vec3{lo.X, hi.Y, hi.Z}
	*dst = append(*dst,
		makeQuad(e, f, b, a, tex, u, v), makeQuad(f, g, c, b, tex, u, v),
		makeQuad(g, h, d, c, tex, u, v), makeQuad(h, e, a, d, tex, u, v),
		makeQuad(h, g, f, e, tex, u, v),
	)
}

func makeTexture(kind string) *Texture {
	const size = 64
	t := &Texture{W: size, H: size, Pix: make([]uint32, size*size)}
	for y := 0; y < size; y++ {
		for x := 0; x < size; x++ {
			n := hash(x, y)
			var r, g, b int
			switch kind {
			case "stone":
				mortar := x%32 < 2 || y%16 < 2
				r, g, b = 95+n%26, 101+n%23, 102+n%23
				if mortar {
					r, g, b = 39, 51, 56
				}
			case "dark":
				seam := x%32 < 2 || y%32 < 2
				r, g, b = 51+n%18, 65+n%21, 72+n%23
				if seam {
					r, g, b = 26, 37, 43
				}
			case "gold":
				r, g, b = 130+n%44, 104+n%39, 65+n%28
				if x%16 < 2 || y%16 < 2 {
					r, g, b = 197, 161, 91
				}
			case "rune":
				dx, dy := float64(x)-31.5, float64(y)-31.5
				d := math.Hypot(dx, dy)
				glyph := math.Abs(d-23) < 2 || math.Abs(d-13) < 1.5 || (math.Abs(dx) < 2 && d < 24) || (math.Abs(dy) < 2 && d < 24)
				r, g, b = 47+n%18, 56+n%20, 65+n%18
				if glyph {
					r, g, b = 211, 164, 82
				}
			case "water":
				wave := int(12 * math.Sin(float64(x)*0.22+float64(y)*0.45))
				r, g, b = 23+n%11+wave/3, 57+n%20+wave, 72+n%23+wave
			}
			t.Pix[y*size+x] = uint32(clamp8(r))<<16 | uint32(clamp8(g))<<8 | uint32(clamp8(b))
		}
	}
	return t
}

func hash(x, y int) int {
	n := uint32(x*374761393 + y*668265263)
	n = (n ^ (n >> 13)) * 1274126177
	return int((n ^ (n >> 16)) & 255)
}

func clamp8(v int) byte {
	if v < 0 {
		return 0
	}
	if v > 255 {
		return 255
	}
	return byte(v)
}

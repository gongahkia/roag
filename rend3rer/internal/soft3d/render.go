// Package soft3d contains the CPU side of the shrine renderer. It has no
// dependency on the windowing library, which also makes frames reproducible.
package soft3d

import (
	"math"
	"sort"
)

const (
	Width  = 320
	Height = 224
	near   = 0.08
)

type Vec3 struct{ X, Y, Z float64 }
type Camera struct {
	Position Vec3
	Yaw      float64
	Pitch    float64
	FOV      float64
}

// Look applies mouse-like movement in logical pixels, with vertical rotation
// clamped before the view can flip upside down.
func (c *Camera) Look(dx, dy float64) {
	c.Rotate(dx*0.004, -dy*0.004)
}

// Rotate applies angles in radians. Positive pitch looks up.
func (c *Camera) Rotate(yaw, pitch float64) {
	c.Yaw = math.Remainder(c.Yaw+yaw, 2*math.Pi)
	c.Pitch += pitch
	if c.Pitch > 1.45 {
		c.Pitch = 1.45
	}
	if c.Pitch < -1.45 {
		c.Pitch = -1.45
	}
}

type Vertex struct {
	Pos  Vec3
	U, V float64
}

type Quad struct {
	V        [4]Vertex
	Texture  *Texture
	Tint     [3]float64
	TwoSided bool
}

// Texture stores opaque colors as 0xRRGGBB. Coordinates wrap at its edges.
type Texture struct {
	W, H int
	Pix  []uint32
}

func (t *Texture) sample(u, v float64) uint32 {
	if t == nil || t.W == 0 || t.H == 0 {
		return 0xffffff
	}
	x := int(math.Floor(u*float64(t.W))) % t.W
	y := int(math.Floor(v*float64(t.H))) % t.H
	if x < 0 {
		x += t.W
	}
	if y < 0 {
		y += t.H
	}
	return t.Pix[y*t.W+x]
}

type Stats struct {
	Quads, Pixels int
}

type Renderer struct {
	Pixels []byte
	depth  []float32
	Stats  Stats
}

func NewRenderer() *Renderer {
	return &Renderer{Pixels: make([]byte, Width*Height*4), depth: make([]float32, Width*Height)}
}

type projected struct{ x, y, z, u, v float64 }
type cameraVertex struct{ x, y, z, u, v float64 }
type drawQuad struct {
	vertices []projected
	texture  *Texture
	tint     [3]float64
	depth    float64
}

func (r *Renderer) Render(cam Camera, quads []Quad, tick float64) {
	r.Stats = Stats{}
	fov := cam.FOV
	if fov == 0 {
		fov = 75
	}
	focal := float64(Width) / 2 / math.Tan(fov*math.Pi/360)
	r.drawBackground(cam, tick, focal)
	for i := range r.depth {
		r.depth[i] = math.MaxFloat32
	}
	cy, sy := math.Cos(cam.Yaw), math.Sin(cam.Yaw)
	cp, sp := math.Cos(cam.Pitch), math.Sin(cam.Pitch)
	queue := make([]drawQuad, 0, len(quads))
	for _, q := range quads {
		poly := make([]cameraVertex, 4)
		for i, v := range q.V {
			dx, dy, dz := v.Pos.X-cam.Position.X, v.Pos.Y-cam.Position.Y, v.Pos.Z-cam.Position.Z
			x := dx*cy - dz*sy
			forward := dx*sy + dz*cy
			poly[i] = cameraVertex{x, dy*cp - forward*sp, dy*sp + forward*cp, v.U, v.V}
		}
		poly = clipNear(poly)
		if len(poly) < 3 {
			continue
		}
		dq := drawQuad{vertices: make([]projected, len(poly)), texture: q.Texture, tint: q.Tint}
		for i, v := range poly {
			dq.vertices[i] = projected{Width/2 + focal*v.x/v.z, Height/2 - focal*v.y/v.z, v.z, v.u, v.v}
			dq.depth += v.z
		}
		if !q.TwoSided && signedArea(dq.vertices) <= 0 {
			continue
		}
		dq.depth /= float64(len(poly))
		queue = append(queue, dq)
	}
	// Saturn-style painter order is useful for the intentional layering. The
	// pixel depth test below prevents nearby geometry from popping through.
	sort.SliceStable(queue, func(i, j int) bool { return queue[i].depth > queue[j].depth })
	for _, q := range queue {
		r.rasterPolygon(q.vertices, q.texture, q.tint)
		r.Stats.Quads++
	}
}

func signedArea(poly []projected) float64 {
	area := 0.0
	for i, a := range poly {
		b := poly[(i+1)%len(poly)]
		area += a.x*b.y - b.x*a.y
	}
	return area
}

func clipNear(poly []cameraVertex) []cameraVertex {
	out := make([]cameraVertex, 0, len(poly)+2)
	prev := poly[len(poly)-1]
	prevInside := prev.z >= near
	for _, cur := range poly {
		inside := cur.z >= near
		if inside != prevInside {
			t := (near - prev.z) / (cur.z - prev.z)
			out = append(out, cameraVertex{
				prev.x + t*(cur.x-prev.x), prev.y + t*(cur.y-prev.y), near,
				prev.u + t*(cur.u-prev.u), prev.v + t*(cur.v-prev.v),
			})
		}
		if inside {
			out = append(out, cur)
		}
		prev, prevInside = cur, inside
	}
	return out
}

func (r *Renderer) rasterPolygon(poly []projected, tex *Texture, tint [3]float64) {
	minY, maxY := math.Inf(1), math.Inf(-1)
	for _, p := range poly {
		minY = math.Min(minY, p.y)
		maxY = math.Max(maxY, p.y)
	}
	ystart := max(0, int(math.Floor(minY)))
	yend := min(Height-1, int(math.Ceil(maxY)))
	for y := ystart; y <= yend; y++ {
		scanY := float64(y) + 0.5
		var left, right projected
		found := false
		for i, a := range poly {
			b := poly[(i+1)%len(poly)]
			if (scanY < a.y && scanY < b.y) || (scanY >= a.y && scanY >= b.y) || a.y == b.y {
				continue
			}
			t := (scanY - a.y) / (b.y - a.y)
			p := projected{
				x: a.x + (b.x-a.x)*t, z: a.z + (b.z-a.z)*t,
				u: a.u + (b.u-a.u)*t, v: a.v + (b.v-a.v)*t,
			}
			if !found || p.x < left.x {
				left = p
			}
			if !found || p.x > right.x {
				right = p
			}
			found = true
		}
		if !found || right.x <= left.x {
			continue
		}
		xstart := max(0, int(math.Floor(left.x)))
		xend := min(Width-1, int(math.Ceil(right.x)))
		for x := xstart; x <= xend; x++ {
			scanX := float64(x) + 0.5
			if scanX < left.x || scanX >= right.x {
				continue
			}
			t := (scanX - left.x) / (right.x - left.x)
			z := left.z + (right.z-left.z)*t
			idx := y*Width + x
			if z >= float64(r.depth[idx]) {
				continue
			}
			r.depth[idx] = float32(z)
			c := tex.sample(left.u+(right.u-left.u)*t, left.v+(right.v-left.v)*t)
			fog := (z - 8) / 35
			if fog < 0 {
				fog = 0
			} else if fog > 0.62 {
				fog = 0.62
			}
			dither := float64(bayer4[(y&3)*4+(x&3)]) - 7.5
			j := idx * 4
			r.Pixels[j] = quantize(float64((c>>16)&255)*tint[0]*(1-fog) + 35*fog + dither*0.7)
			r.Pixels[j+1] = quantize(float64((c>>8)&255)*tint[1]*(1-fog) + 45*fog + dither*0.7)
			r.Pixels[j+2] = quantize(float64(c&255)*tint[2]*(1-fog) + 62*fog + dither*0.7)
			r.Pixels[j+3] = 255
			r.Stats.Pixels++
		}
	}
}

var bayer4 = [16]int{0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 5, 13}

func quantize(x float64) byte {
	v := int(x + 0.5)
	if v < 0 {
		v = 0
	} else if v > 255 {
		v = 255
	}
	return byte((v >> 3) * 255 / 31)
}

func (r *Renderer) drawBackground(cam Camera, tick, focal float64) {
	horizon := float64(Height)/2 + cam.Pitch*focal
	var mountain, foreground, cloud [Width]float64
	for x := 0; x < Width; x++ {
		px := float64(x) + cam.Yaw*95
		mountain[x] = horizon + 21 + 8*math.Sin(px*0.023) + 4*math.Sin(px*0.059)
		foreground[x] = horizon + 44 + 10*math.Sin(px*0.038+1.1)
		cloud[x] = 7 * math.Sin(px*0.015+tick*0.1)
	}
	for y := 0; y < Height; y++ {
		fade := float64(y) / Height
		for x := 0; x < Width; x++ {
			var red, green, blue float64
			switch {
			case float64(y) > foreground[x]:
				red, green, blue = 22, 35, 48
			case float64(y) > mountain[x]:
				red, green, blue = 36, 53, 69
			default:
				red, green, blue = 21+40*fade+cloud[x], 32+47*fade+cloud[x], 56+47*fade+cloud[x]
			}
			j := (y*Width + x) * 4
			r.Pixels[j], r.Pixels[j+1], r.Pixels[j+2], r.Pixels[j+3] = quantize(red), quantize(green), quantize(blue), 255
		}
	}
}

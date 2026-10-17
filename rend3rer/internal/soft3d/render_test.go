package soft3d

import (
	"math"
	"testing"

	"rend3rer/levels"
)

func TestClipNearInterpolatesUV(t *testing.T) {
	poly := []cameraVertex{
		{x: -1, z: -1, u: 0}, {x: 1, z: 1, u: 1},
		{x: 1, y: 1, z: 1, u: 1, v: 1}, {x: -1, y: 1, z: -1, v: 1},
	}
	got := clipNear(poly)
	if len(got) != 4 {
		t.Fatalf("clipped vertex count = %d, want 4", len(got))
	}
	for _, v := range got {
		if v.z < near-1e-9 || math.IsNaN(v.u) || math.IsNaN(v.v) {
			t.Fatalf("invalid clipped vertex: %+v", v)
		}
	}
}

func TestDepthIsIndependentOfSubmissionOrder(t *testing.T) {
	red := &Texture{W: 1, H: 1, Pix: []uint32{0xff0000}}
	green := &Texture{W: 1, H: 1, Pix: []uint32{0x00ff00}}
	far := makeQuad(Vec3{-2, -2, 4}, Vec3{2, -2, 4}, Vec3{2, 2, 4}, Vec3{-2, 2, 4}, red, 1, 1)
	close := makeQuad(Vec3{-1, -1, 2}, Vec3{1, -1, 2}, Vec3{1, 1, 2}, Vec3{-1, 1, 2}, green, 1, 1)
	far.TwoSided, close.TwoSided = true, true
	r := NewRenderer()
	cam := Camera{}
	r.Render(cam, []Quad{far, close}, 0)
	a := append([]byte(nil), r.Pixels...)
	r.Render(cam, []Quad{close, far}, 0)
	for i := range a {
		if a[i] != r.Pixels[i] {
			t.Fatalf("pixel %d differs by submission order", i)
		}
	}
	center := (Height/2*Width + Width/2) * 4
	if r.Pixels[center+1] <= r.Pixels[center] {
		t.Fatalf("near green surface was hidden: RGB=%v", r.Pixels[center:center+3])
	}
}

func TestBackfaceCulling(t *testing.T) {
	tex := &Texture{W: 1, H: 1, Pix: []uint32{0xffffff}}
	front := makeQuad(Vec3{-1, 1, 2}, Vec3{1, 1, 2}, Vec3{1, -1, 2}, Vec3{-1, -1, 2}, tex, 1, 1)
	back := makeQuad(Vec3{-1, -1, 2}, Vec3{1, -1, 2}, Vec3{1, 1, 2}, Vec3{-1, 1, 2}, tex, 1, 1)
	r := NewRenderer()
	r.Render(Camera{}, []Quad{back}, 0)
	if r.Stats.Quads != 0 {
		t.Fatalf("back face was drawn: %+v", r.Stats)
	}
	r.Render(Camera{}, []Quad{front}, 0)
	if r.Stats.Quads != 1 || r.Stats.Pixels == 0 {
		t.Fatalf("front face was culled: %+v", r.Stats)
	}
}

func TestShrineFrameAndNearPlane(t *testing.T) {
	r := NewRenderer()
	l, err := DecodeLevel(levels.Default)
	if err != nil {
		t.Fatal(err)
	}
	s, err := l.BuildScene()
	if err != nil {
		t.Fatal(err)
	}
	r.Render(l.SpawnCamera(), s.Quads(0), 0)
	if r.Stats.Quads < 30 || r.Stats.Pixels < 1000 {
		t.Fatalf("scene appears empty: %+v", r.Stats)
	}
	for i := 3; i < len(r.Pixels); i += 4 {
		if r.Pixels[i] != 255 {
			t.Fatalf("pixel %d is transparent", i/4)
		}
	}
	// Walk the camera into a pillar and ensure clipped quads stay in bounds.
	r.Render(Camera{Position: Vec3{X: -3.3, Y: 1.55, Z: 1.5}, Yaw: 0.3}, s.Quads(1), 1)
}

func BenchmarkShrineFrame(b *testing.B) {
	r := NewRenderer()
	l, err := DecodeLevel(levels.Default)
	if err != nil {
		b.Fatal(err)
	}
	s, err := l.BuildScene()
	if err != nil {
		b.Fatal(err)
	}
	q := s.Quads(1)
	cam := l.SpawnCamera()
	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		r.Render(cam, q, 1)
	}
}

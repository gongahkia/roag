package controls

import (
	"math"
	"testing"

	"rend3rer/internal/soft3d"
)

func TestMouseUsesRelativeMotionAndStopsImmediately(t *testing.T) {
	m := MouseLook{Multiplier: 1}
	c := soft3d.Camera{}
	m.Apply(&c, 500, 300) // position at capture is not movement
	if c.Yaw != 0 || c.Pitch != 0 {
		t.Fatal("capture moved camera")
	}
	m.Apply(&c, 540, 280)
	if math.Abs(c.Yaw-.01) > 1e-9 || math.Abs(c.Pitch-.005) > 1e-9 {
		t.Fatalf("unexpected sensitivity or direction: %+v", c)
	}
	before := c
	for i := 0; i < 180; i++ {
		m.Apply(&c, 540, 280)
	}
	if c != before {
		t.Fatal("stationary mouse kept rotating camera")
	}
}

func TestCaptureMenuAndResizeRebaseWithoutApplyingAccumulatedMotion(t *testing.T) {
	m := MouseLook{Multiplier: 1}
	c := soft3d.Camera{}
	m.Apply(&c, 500, 300)
	m.Apply(&c, 510, 300)
	for _, p := range [][2]float64{{0, 0}, {1900, -250}, {960, 672}, {-1250, 300}} {
		before := c
		m.Reset()
		m.Apply(&c, p[0], p[1])
		if c != before {
			t.Fatal("inactive cursor movement changed aim")
		}
		m.Apply(&c, p[0]+10, p[1])
		if math.Abs(c.Yaw-before.Yaw-.0025) > 1e-9 {
			t.Fatal("normal aiming did not resume after reset")
		}
	}
}

func TestSensitivityMultiplierAndRapidSwipes(t *testing.T) {
	for _, multiplier := range []float64{.25, .5, 1, 2} {
		m := MouseLook{Multiplier: multiplier}
		c := soft3d.Camera{}
		m.Apply(&c, 0, 0)
		m.Apply(&c, 300, 0)
		if math.Abs(c.Yaw-.075*multiplier) > 1e-9 {
			t.Fatalf("sensitivity %v: fast swipe was scaled incorrectly or dropped", multiplier)
		}
	}
}

func TestLargeMotionIsPreservedAndInvalidSamplesDoNotContaminateAim(t *testing.T) {
	m := MouseLook{Multiplier: 1}
	c := soft3d.Camera{}
	m.Apply(&c, 0, 0)
	m.Apply(&c, 5000, 5000)
	if math.Abs(c.Yaw-1.25) > 1e-9 || math.Abs(c.Pitch+1.25) > 1e-9 {
		t.Fatal("large finite movement was discarded")
	}
	yaw := c.Yaw
	m.Apply(&c, 5010, 5000)
	if math.Abs(c.Yaw-yaw-.0025) > 1e-9 {
		t.Fatal("normal motion after a large swipe was not preserved")
	}
	before := c
	m.Apply(&c, math.NaN(), 0)
	m.Apply(&c, 100, 100)
	m.Apply(&c, 100, 100)
	if c != before {
		t.Fatal("invalid sample contaminated aim")
	}
	for i := 0; i < 100; i++ {
		m.Apply(&c, 100+float64(i)*100, 100+float64(i)*100)
	}
	if math.Abs(c.Yaw) > math.Pi || math.Abs(c.Pitch) > 1.45 {
		t.Fatal("camera angles exceeded their bounds")
	}
}

func TestMouseDistanceDoesNotDependOnSampleBatching(t *testing.T) {
	var results []soft3d.Camera
	for _, samples := range []int{1, 8, 100, 1000} {
		m := MouseLook{Multiplier: 1}
		c := soft3d.Camera{}
		m.Apply(&c, 0, 0)
		for i := 1; i <= samples; i++ {
			m.Apply(&c, 8000*float64(i)/float64(samples), 400*float64(i)/float64(samples))
		}
		results = append(results, c)
	}
	for _, c := range results {
		if math.Abs(c.Yaw-2) > 1e-9 || math.Abs(c.Pitch+.1) > 1e-9 {
			t.Fatalf("aim depends on event batching: %+v", results)
		}
	}
}

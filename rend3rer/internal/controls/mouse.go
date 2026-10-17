// Package controls translates local input into camera movement independently of
// Ebitengine, so cursor capture and menu transitions can be regression tested.
package controls

import (
	"math"

	"rend3rer/internal/soft3d"
)

// DefaultMouseSensitivity is radians per window pixel at multiplier 1.
// Mouse input is independent of keyboard turning and the 320x224 framebuffer.
const DefaultMouseSensitivity = .00025

type MouseLook struct {
	Multiplier   float64
	lastX, lastY float64
	ready        bool
}

// Reset discards motion while input is inactive. Seed the baseline from the
// next input sample, after the OS has had a chance to apply cursor capture.
func (m *MouseLook) Reset() { m.ready = false }

func (m *MouseLook) Apply(camera *soft3d.Camera, x, y float64) (float64, float64) {
	if math.IsNaN(x) || math.IsNaN(y) || math.IsInf(x, 0) || math.IsInf(y, 0) {
		m.Reset()
		return 0, 0
	}
	dx, dy := x-m.lastX, y-m.lastY
	m.lastX, m.lastY = x, y
	if !m.ready {
		m.ready = true
		return 0, 0
	}
	// Preserve finite motion regardless of frame rate or mouse DPI. Cursor
	// capture belongs to the native backend, not a guessed pixel cutoff.
	if math.IsInf(dx, 0) || math.IsInf(dy, 0) {
		return 0, 0
	}
	if m.Multiplier <= 0 || math.IsNaN(m.Multiplier) || math.IsInf(m.Multiplier, 0) {
		return 0, 0
	}
	camera.Rotate(dx*DefaultMouseSensitivity*m.Multiplier, -dy*DefaultMouseSensitivity*m.Multiplier)
	return dx, dy
}

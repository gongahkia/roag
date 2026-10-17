package controls

import "math"

// Viewport shares the exact display transform with menu pointer hit testing.
type Viewport struct{ Scale, OffsetX, OffsetY float64 }

func FitViewport(windowWidth, windowHeight, width, height int) Viewport {
	scale := math.Min(float64(max(1, windowWidth))/float64(width), float64(max(1, windowHeight))/float64(height))
	if scale >= 1 {
		scale = math.Floor(scale)
	}
	return Viewport{scale, (float64(windowWidth) - scale*float64(width)) / 2, (float64(windowHeight) - scale*float64(height)) / 2}
}
func (v Viewport) ToGame(x, y float64) (float64, float64) {
	return (x - v.OffsetX) / v.Scale, (y - v.OffsetY) / v.Scale
}

package controls

import (
	"math"
	"testing"
)

func TestMenuPointerMatchesScaledAndLetterboxedImage(t *testing.T) {
	for _, size := range [][2]int{{960, 672}, {1920, 1080}, {400, 900}, {160, 112}} {
		v := FitViewport(size[0], size[1], 320, 224)
		for _, point := range [][2]float64{{52, 105}, {160, 105}, {268, 105}, {160, 174}} {
			x, y := v.ToGame(point[0]*v.Scale+v.OffsetX, point[1]*v.Scale+v.OffsetY)
			if math.Abs(x-point[0]) > 1e-9 || math.Abs(y-point[1]) > 1e-9 {
				t.Fatalf("slider pointer mismatch at %v", size)
			}
		}
	}
}

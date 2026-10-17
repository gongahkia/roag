package arena

import (
	"fmt"
	"image"
	"math"

	"rend3rer/internal/settings"
)

var (
	SensitivityTrack = image.Rect(52, 105, 268, 109)
	SensitivityHit   = image.Rect(44, 95, 276, 120)
	SettingsBack     = image.Rect(108, 164, 212, 183)
	TitleSettings    = image.Rect(110, 186, 210, 198)
	PauseSettings    = image.Rect(104, 118, 216, 136)
)

func (v *View) Settings(pixels []byte, sensitivity float64, status string) {
	c := canvas{pixels}
	c.dim()
	c.panel(24, 27, 272, 172)
	c.center(42, 2, "SETTINGS", gold)
	c.center(69, 1, "MOUSE SENSITIVITY", white)
	c.center(82, 2, fmt.Sprintf("%.3FX", sensitivity), cyan)
	x := SensitivityTrack.Min.X + int(math.Round(settings.SliderPosition(sensitivity)*float64(SensitivityTrack.Dx())))
	c.rect(SensitivityTrack.Min.X, SensitivityTrack.Min.Y, SensitivityTrack.Dx(), SensitivityTrack.Dy(), 0x344b58)
	c.rect(SensitivityTrack.Min.X, SensitivityTrack.Min.Y, x-SensitivityTrack.Min.X, SensitivityTrack.Dy(), 0xc89c61)
	c.rect(x-3, 100, 7, 14, 0x10202c)
	c.rect(x-2, 101, 5, 12, cyan)
	c.text(52, 120, 1, "SLOW", muted)
	c.text(253, 120, 1, "FAST", muted)
	c.center(135, 1, "DRAG SLIDER OR USE LEFT / RIGHT", white)
	if status != "" {
		c.center(149, 1, status, cyan)
	} else {
		c.center(149, 1, "CHANGES APPLY NOW AND SAVE ON RELEASE", muted)
	}
	c.panel(SettingsBack.Min.X, SettingsBack.Min.Y, SettingsBack.Dx(), SettingsBack.Dy())
	c.center(171, 1, "BACK [ESC / TAB]", gold)
}

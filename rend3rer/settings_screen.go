package main

import (
	"image"
	"log"

	"github.com/hajimehoshi/ebiten/v2"
	"rend3rer/internal/arena"
	"rend3rer/internal/controls"
	"rend3rer/internal/gameplay"
	"rend3rer/internal/settings"
	"rend3rer/internal/soft3d"
)

func (g *runGame) menuPointer() (float64, float64) {
	x, y := ebiten.CursorPositionF()
	return controls.FitViewport(g.width, g.height, soft3d.Width, soft3d.Height).ToGame(x, y)
}
func (g *runGame) pointerIn(box image.Rectangle) bool {
	x, y := g.menuPointer()
	return x >= float64(box.Min.X) && x < float64(box.Max.X) && y >= float64(box.Min.Y) && y < float64(box.Max.Y)
}

func (g *runGame) openSettings() {
	g.settingsReturnPaused = g.paused
	g.settingsOpen = true
	g.settingsDragging = false
	g.settingsStatus = ""
	if g.started && g.run.Phase == gameplay.Playing {
		g.paused = true
	}
	g.release()
}
func (g *runGame) setSensitivity(value float64) {
	g.mouse.Multiplier = value
	g.preferences.MouseSensitivity = value
	g.settingsDirty = true
	g.settingsStatus = ""
}
func (g *runGame) saveSettings() bool {
	if !g.settingsDirty {
		return true
	}
	if g.settingsPath == "" {
		g.settingsStatus = "COULD NOT SAVE SETTINGS"
		return false
	}
	if err := g.preferences.Save(g.settingsPath); err != nil {
		g.settingsStatus = "COULD NOT SAVE SETTINGS"
		log.Printf("Could not save settings; the change applies for this session: %v", err)
		return false
	}
	g.settingsDirty = false
	g.settingsStatus = "SAVED"
	return true
}
func (g *runGame) closeSettings() {
	g.saveSettings()
	g.settingsOpen = false
	g.settingsDragging = false
	g.paused = g.settingsReturnPaused
	if g.started && !g.paused && g.run.Phase == gameplay.Playing {
		g.capture()
	}
}
func (g *runGame) updateSettings(click bool) {
	g.clock.Reset()
	g.actions.Reset()
	g.mouse.Reset()
	if pressed(ebiten.KeyEscape) || pressed(ebiten.KeyTab) || pressed(ebiten.KeyEnter) || (click && g.pointerIn(arena.SettingsBack)) {
		g.closeSettings()
		return
	}
	if click && g.pointerIn(arena.SensitivityHit) {
		g.settingsDragging = true
	}
	if g.settingsDragging {
		if ebiten.IsMouseButtonPressed(ebiten.MouseButtonLeft) {
			x, _ := g.menuPointer()
			position := (x - float64(arena.SensitivityTrack.Min.X)) / float64(arena.SensitivityTrack.Dx())
			g.setSensitivity(settings.SliderValue(position))
		} else {
			g.settingsDragging = false
			g.saveSettings()
		}
	}
	change := 0.0
	if pressed(ebiten.KeyArrowLeft) {
		change -= .015
	}
	if pressed(ebiten.KeyArrowRight) {
		change += .015
	}
	if change != 0 {
		g.setSensitivity(settings.SliderValue(settings.SliderPosition(g.mouse.Multiplier) + change))
		g.saveSettings()
	}
}

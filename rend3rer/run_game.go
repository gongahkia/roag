package main

import (
	"fmt"
	"image/color"
	"time"

	"github.com/hajimehoshi/ebiten/v2"
	"github.com/hajimehoshi/ebiten/v2/ebitenutil"
	"github.com/hajimehoshi/ebiten/v2/inpututil"
	"rend3rer/internal/arena"
	"rend3rer/internal/controls"
	"rend3rer/internal/gameplay"
	"rend3rer/internal/settings"
	"rend3rer/internal/soft3d"
)

type runGame struct {
	run                                                                 *gameplay.Run
	view                                                                *arena.View
	renderer                                                            *soft3d.Renderer
	frame                                                               *ebiten.Image
	look                                                                soft3d.Camera
	width, height                                                       int
	started, paused, captured, showStats                                bool
	mouse                                                               controls.MouseLook
	frameMS                                                             float64
	sound                                                               *soundBank
	muted                                                               bool
	clock                                                               controls.TickClock
	actions                                                             controls.Actions
	lastFrame                                                           time.Time
	trace                                                               *inputTrace
	preferences                                                         settings.Preferences
	settingsPath, settingsStatus                                        string
	settingsOpen, settingsDragging, settingsDirty, settingsReturnPaused bool
}

func newRunGame(seed int64, muted bool, sensitivity float64) *runGame {
	g := &runGame{renderer: soft3d.NewRenderer(), frame: ebiten.NewImage(soft3d.Width, soft3d.Height), width: 960, height: 672, muted: muted, mouse: controls.MouseLook{Multiplier: sensitivity}, preferences: settings.Preferences{MouseSensitivity: sensitivity}}
	g.reset(seed)
	return g
}
func (g *runGame) reset(seed int64) {
	g.run = gameplay.New(seed)
	g.view = arena.New(g.run.World)
	g.look = soft3d.Camera{FOV: 75}
	g.mouse.Reset()
	g.paused = false
	g.clock.Reset()
	g.actions.Reset()
}
func (g *runGame) capture() {
	g.captured = true
	ebiten.SetCursorMode(ebiten.CursorModeCaptured)
	g.mouse.Reset()
	g.clock.Reset()
	g.actions.Reset()
	g.lastFrame = time.Now()
}
func (g *runGame) release() {
	g.captured = false
	g.mouse.Reset()
	g.clock.Reset()
	g.actions.Reset()
	ebiten.SetCursorMode(ebiten.CursorModeVisible)
}
func pressed(key ebiten.Key) bool { return inpututil.IsKeyJustPressed(key) }

func (g *runGame) Update() (err error) {
	now := time.Now()
	elapsed := time.Duration(0)
	if !g.lastFrame.IsZero() {
		elapsed = now.Sub(g.lastFrame)
	}
	g.lastFrame = now
	dx, dy := 0.0, 0.0
	defer func() {
		if err == nil {
			err = g.trace.record(elapsed, dx, dy, g.look, g.captured, ebiten.IsFocused(), g.run.Tick)
		}
	}()
	click := inpututil.IsMouseButtonJustPressed(ebiten.MouseButtonLeft)
	if pressed(ebiten.KeyF1) {
		g.showStats = !g.showStats
	}
	if pressed(ebiten.KeyM) {
		g.muted = !g.muted
		if g.sound != nil {
			g.sound.mute(g.muted)
		}
	}
	if g.settingsOpen {
		g.updateSettings(click)
		return nil
	}
	if pressed(ebiten.KeyTab) || (!g.started && click && g.pointerIn(arena.TitleSettings)) || (g.paused && click && g.pointerIn(arena.PauseSettings)) {
		g.openSettings()
		return nil
	}
	if g.started && (pressed(ebiten.KeyR) || pressed(ebiten.KeyN) || ((g.run.Phase == gameplay.Dead || g.run.Phase == gameplay.Won) && pressed(ebiten.KeyEnter))) {
		seed := g.run.Seed
		if pressed(ebiten.KeyN) {
			seed++
		}
		g.reset(seed)
		g.capture()
		return nil
	}
	if !g.started {
		g.view.Update(nil)
		if pressed(ebiten.KeyEnter) || click {
			g.started = true
			g.capture()
		}
		return nil
	}
	if (!ebiten.IsFocused() || pressed(ebiten.KeyEscape)) && g.run.Phase == gameplay.Playing {
		g.paused = true
		g.release()
	}
	if g.paused {
		g.view.Update(nil)
		if ebiten.IsFocused() && (pressed(ebiten.KeyEnter) || click) {
			g.paused = false
			g.capture()
		}
		return nil
	}
	if g.run.Phase == gameplay.Dead || g.run.Phase == gameplay.Won {
		g.release()
		g.view.Update(nil)
		return nil
	}
	if g.run.Phase == gameplay.Choosing {
		g.mouse.Reset()
		g.clock.Reset()
		g.actions.Reset()
		c := gameplay.Command{}
		for i, k := range []ebiten.Key{ebiten.KeyDigit1, ebiten.KeyDigit2, ebiten.KeyDigit3} {
			if pressed(k) {
				c.Choice = i + 1
			}
		}
		if pressed(ebiten.KeyEscape) {
			g.release()
		}
		g.advance(c)
		return nil
	}
	justCaptured := false
	if !g.captured && click {
		g.capture()
		justCaptured = true
	}
	if g.captured {
		x, y := ebiten.CursorPositionF()
		dx, dy = g.mouse.Apply(&g.look, x, y)
	}
	for _, k := range []struct {
		key    ebiten.Key
		dx, dy float64
	}{{ebiten.KeyArrowLeft, -6, 0}, {ebiten.KeyArrowRight, 6, 0}, {ebiten.KeyArrowUp, 0, -6}, {ebiten.KeyArrowDown, 0, 6}} {
		if ebiten.IsKeyPressed(k.key) {
			// Arrow keys are a turn rate; mouse deltas are already displacement.
			g.look.Look(k.dx*elapsed.Seconds()*gameplay.TickRate, k.dy*elapsed.Seconds()*gameplay.TickRate)
		}
	}
	c := gameplay.Command{Yaw: g.look.Yaw, Pitch: g.look.Pitch, Fire: (!justCaptured && g.captured && ebiten.IsMouseButtonPressed(ebiten.MouseButtonLeft)) || ebiten.IsKeyPressed(ebiten.KeyF), Sprint: ebiten.IsKeyPressed(ebiten.KeyShift), Jump: ebiten.IsKeyPressed(ebiten.KeySpace)}
	if ebiten.IsKeyPressed(ebiten.KeyW) {
		c.Forward++
	}
	if ebiten.IsKeyPressed(ebiten.KeyS) {
		c.Forward--
	}
	if ebiten.IsKeyPressed(ebiten.KeyD) {
		c.Strafe++
	}
	if ebiten.IsKeyPressed(ebiten.KeyA) {
		c.Strafe--
	}
	g.actions.Remember(gameplay.Command{Fire: (click && !justCaptured) || pressed(ebiten.KeyF), Dash: pressed(ebiten.KeyControl), Interact: pressed(ebiten.KeyE), Jump: pressed(ebiten.KeySpace)})
	for ticks := g.clock.Advance(elapsed); ticks > 0; ticks-- {
		g.advance(g.actions.Consume(c))
		if g.run.Phase != gameplay.Playing {
			g.clock.Reset()
			g.actions.Reset()
			break
		}
	}
	return nil
}
func (g *runGame) advance(c gameplay.Command) {
	g.run.Step(c)
	g.view.Update(g.run.Events)
	if !g.muted && len(g.run.Events) > 0 {
		if g.sound == nil {
			g.sound = newSoundBank()
		}
		g.sound.play(g.run.Events)
	}
}

func (g *runGame) Draw(screen *ebiten.Image) {
	start := time.Now()
	p := g.run.Player
	// Show current local aim even when this display frame has no simulation tick.
	// The next command carries these same angles into movement and hit detection.
	cam := soft3d.Camera{Position: soft3d.Vec3{X: p.Position.X, Y: p.Position.Y + gameplay.EyeHeight, Z: p.Position.Z}, Yaw: g.look.Yaw, Pitch: g.look.Pitch, FOV: 75}
	g.renderer.Render(cam, g.view.Quads(g.run, cam), float64(g.run.Tick)/gameplay.TickRate)
	g.view.HUD(g.renderer.Pixels, g.run, g.started, g.paused)
	if g.settingsOpen {
		g.view.Settings(g.renderer.Pixels, g.mouse.Multiplier, g.settingsStatus)
	}
	g.frame.WritePixels(g.renderer.Pixels)
	g.frameMS = float64(time.Since(start).Microseconds()) / 1000
	screen.Fill(color.RGBA{R: 9, G: 16, B: 24, A: 255})
	viewport := controls.FitViewport(g.width, g.height, soft3d.Width, soft3d.Height)
	op := &ebiten.DrawImageOptions{}
	op.GeoM.Scale(viewport.Scale, viewport.Scale)
	op.GeoM.Translate(viewport.OffsetX, viewport.OffsetY)
	op.Filter = ebiten.FilterNearest
	screen.DrawImage(g.frame, op)
	if g.showStats {
		ebitenutil.DebugPrintAt(screen, fmt.Sprintf("FPS %.0f  CPU %.1f ms  QUADS %d\nSEED %d  TICK %d  SOUND %t", ebiten.ActualFPS(), g.frameMS, g.renderer.Stats.Quads, g.run.Seed, g.run.Tick, !g.muted), 10, g.height-36)
	}
}
func (g *runGame) Layout(width, height int) (int, int) {
	width, height = max(1, width), max(1, height)
	if width != g.width || height != g.height {
		g.mouse.Reset()
	}
	g.width, g.height = width, height
	return g.width, g.height
}

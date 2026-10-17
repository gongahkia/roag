package main

import (
	"flag"
	"fmt"
	"image/color"
	"log"
	"math"
	"os"
	"runtime"
	"time"

	"github.com/hajimehoshi/ebiten/v2"
	"github.com/hajimehoshi/ebiten/v2/ebitenutil"
	"github.com/hajimehoshi/ebiten/v2/inpututil"
	"rend3rer/internal/controls"
	"rend3rer/internal/launch"
	"rend3rer/internal/settings"
	"rend3rer/internal/soft3d"
	"rend3rer/levels"
)

type demoGame struct {
	renderer  *soft3d.Renderer
	scene     *soft3d.Scene
	level     *soft3d.Level
	image     *ebiten.Image
	camera    soft3d.Camera
	tick      float64
	windowW   int
	windowH   int
	captured  bool
	dragging  bool
	mouse     controls.MouseLook
	showHUD   bool
	frameMS   float64
	trace     *inputTrace
	lastFrame time.Time
}

func newDemoGame(level *soft3d.Level, sensitivity float64) (*demoGame, error) {
	scene, err := level.BuildScene()
	if err != nil {
		return nil, err
	}
	return &demoGame{
		renderer: soft3d.NewRenderer(), scene: scene, level: level,
		image:   ebiten.NewImage(soft3d.Width, soft3d.Height),
		camera:  level.SpawnCamera(),
		windowW: 960, windowH: 672,
		mouse: controls.MouseLook{Multiplier: sensitivity},
	}, nil
}

func (g *demoGame) Update() (err error) {
	now := time.Now()
	elapsed := time.Duration(0)
	if !g.lastFrame.IsZero() {
		elapsed = now.Sub(g.lastFrame)
	}
	g.lastFrame = now
	dx, dy := 0.0, 0.0
	defer func() {
		if err == nil {
			err = g.trace.record(elapsed, dx, dy, g.camera, g.captured, ebiten.IsFocused(), int(g.tick*60))
		}
	}()
	if inpututil.IsKeyJustPressed(ebiten.KeyF1) {
		g.showHUD = !g.showHUD
	}
	if inpututil.IsKeyJustPressed(ebiten.KeyR) {
		g.camera = g.level.SpawnCamera()
		g.mouse.Reset()
	}
	if inpututil.IsKeyJustPressed(ebiten.KeyEscape) || !ebiten.IsFocused() {
		g.captured = false
		g.dragging = false
		g.mouse.Reset()
		ebiten.SetCursorMode(ebiten.CursorModeVisible)
	}
	if inpututil.IsMouseButtonJustPressed(ebiten.MouseButtonLeft) && !g.captured {
		g.captured = true
		g.dragging = false
		ebiten.SetCursorMode(ebiten.CursorModeCaptured)
		g.mouse.Reset()
	}
	if g.captured {
		x, y := ebiten.CursorPositionF()
		dx, dy = g.mouse.Apply(&g.camera, x, y)
	} else {
		if inpututil.IsMouseButtonJustPressed(ebiten.MouseButtonRight) {
			g.dragging = true
			g.mouse.Reset()
		}
		if g.dragging && ebiten.IsMouseButtonPressed(ebiten.MouseButtonRight) {
			x, y := ebiten.CursorPositionF()
			dx, dy = g.mouse.Apply(&g.camera, x, y)
		} else {
			g.dragging = false
			g.mouse.Reset()
		}
	}
	// Arrow keys provide a reliable way to look vertically even on systems
	// where captured-mouse motion is unavailable.
	if ebiten.IsKeyPressed(ebiten.KeyArrowUp) {
		g.camera.Look(0, -6)
	}
	if ebiten.IsKeyPressed(ebiten.KeyArrowDown) {
		g.camera.Look(0, 6)
	}
	if ebiten.IsKeyPressed(ebiten.KeyArrowLeft) {
		g.camera.Look(-6, 0)
	}
	if ebiten.IsKeyPressed(ebiten.KeyArrowRight) {
		g.camera.Look(6, 0)
	}
	forward, strafe := 0.0, 0.0
	if ebiten.IsKeyPressed(ebiten.KeyW) {
		forward++
	}
	if ebiten.IsKeyPressed(ebiten.KeyS) {
		forward--
	}
	if ebiten.IsKeyPressed(ebiten.KeyD) {
		strafe++
	}
	if ebiten.IsKeyPressed(ebiten.KeyA) {
		strafe--
	}
	if forward != 0 || strafe != 0 {
		length := math.Hypot(forward, strafe)
		forward, strafe = forward/length, strafe/length
		speed := 0.065
		if ebiten.IsKeyPressed(ebiten.KeyShift) {
			speed *= 1.7
		}
		dx := (math.Sin(g.camera.Yaw)*forward + math.Cos(g.camera.Yaw)*strafe) * speed
		dz := (math.Cos(g.camera.Yaw)*forward - math.Sin(g.camera.Yaw)*strafe) * speed
		if g.level.Walkable(g.camera.Position.X+dx, g.camera.Position.Z) {
			g.camera.Position.X += dx
		}
		if g.level.Walkable(g.camera.Position.X, g.camera.Position.Z+dz) {
			g.camera.Position.Z += dz
		}
	}
	g.tick += 1.0 / 60.0
	return nil
}

func (g *demoGame) Draw(screen *ebiten.Image) {
	start := time.Now()
	g.renderer.Render(g.camera, g.scene.Quads(g.tick), g.tick)
	g.frameMS = float64(time.Since(start).Microseconds()) / 1000
	g.image.WritePixels(g.renderer.Pixels)
	screen.Fill(color.RGBA{16, 24, 39, 255})
	scale := math.Min(float64(g.windowW)/soft3d.Width, float64(g.windowH)/soft3d.Height)
	if scale >= 1 {
		scale = math.Floor(scale)
	}
	op := &ebiten.DrawImageOptions{}
	op.GeoM.Scale(scale, scale)
	op.GeoM.Translate((float64(g.windowW)-scale*soft3d.Width)/2, (float64(g.windowH)-scale*soft3d.Height)/2)
	op.Filter = ebiten.FilterNearest
	screen.DrawImage(g.image, op)
	if !g.captured {
		ebitenutil.DebugPrintAt(screen, "CLICK, RIGHT-DRAG, OR ARROWS TO LOOK  |  WASD MOVE", 12, 12)
		ebitenutil.DebugPrintAt(screen, "SHIFT RUN  |  ESC RELEASE  |  R RESET  |  F1 STATS", 12, 28)
	}
	if g.showHUD {
		info := fmt.Sprintf("FPS %.0f  CPU %.1f ms  QUADS %d  PIXELS %d\nPOS %.1f %.1f  YAW %.2f  PITCH %.2f", ebiten.ActualFPS(), g.frameMS, g.renderer.Stats.Quads, g.renderer.Stats.Pixels, g.camera.Position.X, g.camera.Position.Z, g.camera.Yaw, g.camera.Pitch)
		ebitenutil.DebugPrintAt(screen, info, 12, g.windowH-38)
	}
}

func (g *demoGame) Layout(outsideWidth, outsideHeight int) (int, int) {
	outsideWidth, outsideHeight = max(1, outsideWidth), max(1, outsideHeight)
	if outsideWidth != g.windowW || outsideHeight != g.windowH {
		g.mouse.Reset()
	}
	g.windowW, g.windowH = outsideWidth, outsideHeight
	return g.windowW, g.windowH
}

func main() {
	path := flag.String("level", "", "load a JSON level file instead of the embedded shrine")
	demo := flag.Bool("demo", false, "explore the original shrine renderer demo")
	seed := flag.Int64("seed", 1, "seed for the five-chamber run")
	mute := flag.Bool("mute", false, "start without sound (M toggles sound)")
	sensitivity := flag.Float64("sensitivity", 1, "mouse sensitivity multiplier; use 0.5 for slower aiming")
	wslg := flag.Bool("wslg", false, "explicitly use the Linux WSLg window instead of native Windows (FPS mouse capture can be unreliable)")
	tracePath := flag.String("input-log", "", "write optional mouse motion and frame timing to a local CSV file")
	flag.Parse()
	if math.IsNaN(*sensitivity) || math.IsInf(*sensitivity, 0) || *sensitivity <= 0 || *sensitivity > 10 {
		log.Fatal("sensitivity must be a finite number greater than 0 and at most 10")
	}
	if !*wslg {
		handled, err := launch.NativeWindows(os.Args[1:])
		if err != nil {
			log.Fatal(err)
		}
		if handled {
			return
		}
	}
	preferences := settings.Default()
	settingsPath, settingsErr := settings.Path()
	if settingsErr == nil {
		preferences, settingsErr = settings.Load(settingsPath)
	}
	if settingsErr != nil {
		log.Printf("Could not load settings; using defaults: %v", settingsErr)
	}
	overrideSensitivity := false
	flag.Visit(func(f *flag.Flag) {
		if f.Name == "sensitivity" {
			overrideSensitivity = true
		}
	})
	if !overrideSensitivity {
		*sensitivity = preferences.MouseSensitivity
	}
	trace, err := openInputTrace(*tracePath)
	if err != nil {
		log.Fatal(err)
	}
	defer func() {
		if err := trace.Close(); err != nil {
			log.Print(err)
		}
	}()
	var game ebiten.Game
	title := "Rend3rer - Tidal Descent"
	if *demo || *path != "" {
		data := levels.Default
		if *path != "" {
			var err error
			data, err = os.ReadFile(*path)
			if err != nil {
				log.Fatal(err)
			}
		}
		level, err := soft3d.DecodeLevel(data)
		if err != nil {
			log.Fatal(err)
		}
		demo, err := newDemoGame(level, *sensitivity)
		if err != nil {
			log.Fatal(err)
		}
		demo.trace = trace
		game = demo
		title = "Rend3rer - " + level.Name
	} else {
		run := newRunGame(*seed, *mute, *sensitivity)
		run.trace = trace
		run.settingsPath = settingsPath
		game = run
		// Sample input and update the local view every display frame. The combat
		// simulation keeps its own fixed 60 Hz clock in runGame.
		ebiten.SetTPS(ebiten.SyncWithFPS)
	}
	ebiten.SetWindowTitle(title)
	ebiten.SetWindowSize(960, 672)
	ebiten.SetWindowResizingMode(ebiten.WindowResizingModeEnabled)
	options := &ebiten.RunGameOptions{}
	if runtime.GOOS == "darwin" && os.Getenv("EBITENGINE_GRAPHICS_LIBRARY") == "" {
		// Metal can repeatedly fail to allocate a drawable in some macOS
		// sessions. OpenGL only presents the CPU-rendered framebuffer.
		options.GraphicsLibrary = ebiten.GraphicsLibraryOpenGL
	}
	if err := ebiten.RunGameWithOptions(game, options); err != nil {
		panic(err)
	}
}

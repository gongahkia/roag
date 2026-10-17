// Command snapshot renders a deterministic frame without opening a window.
package main

import (
	"flag"
	"image"
	"image/png"
	"math"
	"os"

	"rend3rer/internal/arena"
	"rend3rer/internal/gameplay"
	"rend3rer/internal/soft3d"
	"rend3rer/levels"
)

func main() {
	levelPath := flag.String("level", "", "JSON level file; defaults to the embedded shrine")
	game := flag.Bool("game", false, "render the playable Tidal Descent run")
	seed := flag.Int64("seed", 1, "run seed (with -game)")
	ticks := flag.Int("ticks", 0, "simulate this many idle ticks before rendering (with -game)")
	title := flag.Bool("title", false, "show the title screen (with -game)")
	x := flag.Float64("x", math.NaN(), "camera X override")
	z := flag.Float64("z", math.NaN(), "camera Z override")
	yaw := flag.Float64("yaw", math.NaN(), "camera yaw override in radians")
	pitch := flag.Float64("pitch", math.NaN(), "camera pitch override in radians")
	tick := flag.Float64("time", 0, "scene time in seconds")
	flag.Parse()
	path := "shrine.png"
	if flag.NArg() > 0 {
		path = flag.Arg(0)
	}
	if *game {
		if *levelPath != "" || *ticks < 0 || *ticks > gameplay.RunTicks {
			panic("-game cannot use -level; -ticks must be within the five-minute run")
		}
		run := gameplay.New(*seed)
		view := arena.New(run.World)
		for i := 0; i < *ticks; i++ {
			run.Step(gameplay.Command{})
			view.Update(run.Events)
		}
		cam := soft3d.Camera{Position: soft3d.Vec3{X: run.Player.Position.X, Y: run.Player.Position.Y + gameplay.EyeHeight, Z: run.Player.Position.Z}, FOV: 75}
		overrideCamera(&cam, *x, *z, *yaw, *pitch)
		r := soft3d.NewRenderer()
		r.Render(cam, view.Quads(run, cam), float64(run.Tick)/gameplay.TickRate)
		view.HUD(r.Pixels, run, !*title, false)
		writePNG(path, r.Pixels)
		return
	}
	data := levels.Default
	if *levelPath != "" {
		var err error
		data, err = os.ReadFile(*levelPath)
		if err != nil {
			panic(err)
		}
	}
	level, err := soft3d.DecodeLevel(data)
	if err != nil {
		panic(err)
	}
	scene, err := level.BuildScene()
	if err != nil {
		panic(err)
	}
	cam := level.SpawnCamera()
	overrideCamera(&cam, *x, *z, *yaw, *pitch)
	r := soft3d.NewRenderer()
	r.Render(cam, scene.Quads(*tick), *tick)
	writePNG(path, r.Pixels)
}

func overrideCamera(cam *soft3d.Camera, x, z, yaw, pitch float64) {
	if !math.IsNaN(x) {
		cam.Position.X = x
	}
	if !math.IsNaN(z) {
		cam.Position.Z = z
	}
	if !math.IsNaN(yaw) {
		cam.Yaw = yaw
	}
	if !math.IsNaN(pitch) {
		cam.Pitch = pitch
	}
}

func writePNG(path string, pixels []byte) {
	f, err := os.Create(path)
	if err != nil {
		panic(err)
	}
	defer f.Close()
	img := &image.RGBA{Pix: pixels, Stride: soft3d.Width * 4, Rect: image.Rect(0, 0, soft3d.Width, soft3d.Height)}
	if err := png.Encode(f, img); err != nil {
		panic(err)
	}
}

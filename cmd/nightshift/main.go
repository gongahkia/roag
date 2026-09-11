package main

import (
	"flag"
	"fmt"
	"os"
	"os/signal"
	"syscall"
	"time"

	"nightshift/internal/controller"
	"nightshift/internal/game"
	"nightshift/internal/render"
	"nightshift/internal/terminal"
)

func main() {
	controllerPath := flag.String("controller", "", "Linux evdev path, e.g. /dev/input/event12 (auto-detected when omitted)")
	noController := flag.Bool("no-controller", false, "disable controller discovery")
	flag.Parse()
	s, err := terminal.Open()
	if err != nil {
		fmt.Fprintln(os.Stderr, "nightshift:", err)
		os.Exit(1)
	}
	defer s.Close()
	sig := make(chan os.Signal, 1)
	signal.Notify(sig, os.Interrupt, syscall.SIGTERM)
	defer signal.Stop(sig)
	var pad *controller.Device
	padName := "none"
	if !*noController {
		if d, e := controller.Open(*controllerPath); e == nil {
			pad = d
			padName = d.Name()
			defer pad.Close()
		} else if *controllerPath != "" {
			s.Close()
			fmt.Fprintln(os.Stderr, "nightshift:", e)
			os.Exit(1)
		}
	}
	g := game.New()
	showHelp := true
	heldUntil := map[game.Action]time.Time{}
	lastMouse := -1
	prevPad := controller.State{}
	tick := time.NewTicker(time.Second / 60)
	defer tick.Stop()
	last := time.Now()
	for {
		select {
		case <-sig:
			return
		case now := <-tick.C:
			dt := now.Sub(last).Seconds()
			last = now
			in := game.NewInput()
			for _, ev := range s.Poll() {
				if ev.Kind == terminal.Key {
					k := ev.Key
					if k == 'q' || k == 'Q' {
						return
					}
					if k == 'h' || k == 'H' {
						showHelp = !showHelp
						continue
					}
					if k == 10 && (g.Dead || g.Won) {
						g = game.New()
						showHelp = false
						heldUntil = map[game.Action]time.Time{}
						continue
					}
					if k == 27 {
						in.Pressed[game.Pause] = true
						continue
					}
					switch k {
					case 'w', 'W':
						heldUntil[game.MoveForward] = now.Add(180 * time.Millisecond)
					case 's', 'S':
						heldUntil[game.MoveBackward] = now.Add(180 * time.Millisecond)
					case 'a', 'A':
						heldUntil[game.StrafeLeft] = now.Add(180 * time.Millisecond)
					case 'd', 'D':
						heldUntil[game.StrafeRight] = now.Add(180 * time.Millisecond)
					case 'c', 'C':
						in.Pressed[game.ToggleCrouch] = true
					case 'r', 'R':
						in.Pressed[game.ToggleSprint] = true
					case 'e', 'E':
						in.Pressed[game.Interact] = true
					case 'f', 'F':
						in.Pressed[game.ToggleFlashlight] = true
					case 'g', 'G':
						in.Pressed[game.ThrowDecoy] = true
					case 'b', 'B':
						in.Pressed[game.UseFlare] = true
					case 't', 'T':
						in.Pressed[game.ToggleTracker] = true
					case ' ':
						in.Pressed[game.Fire] = true
					}
					if k == terminal.KeyLeft() {
						heldUntil[game.TurnLeft] = now.Add(180 * time.Millisecond)
					}
					if k == terminal.KeyRight() {
						heldUntil[game.TurnRight] = now.Add(180 * time.Millisecond)
					}
					if k == terminal.KeyUp() {
						heldUntil[game.MoveForward] = now.Add(180 * time.Millisecond)
					}
					if k == terminal.KeyDown() {
						heldUntil[game.MoveBackward] = now.Add(180 * time.Millisecond)
					}
				} else if ev.Kind == terminal.Mouse {
					if lastMouse >= 0 {
						delta := ev.X - lastMouse
						if delta > -20 && delta < 20 {
							in.LookX += float64(delta) * 2.1
						}
					}
					lastMouse = ev.X
					if ev.Buttons&terminal.Button1() != 0 {
						in.Pressed[game.Fire] = true
					}
					if ev.Buttons&terminal.Button2() != 0 {
						in.Pressed[game.Interact] = true
					}
					if ev.Buttons&terminal.Button3() != 0 {
						in.Pressed[game.ThrowDecoy] = true
					}
					if ev.Buttons&terminal.Button4() != 0 {
						in.Held[game.TurnLeft] = true
					}
					if ev.Buttons&terminal.Button5() != 0 {
						in.Held[game.TurnRight] = true
					}
				}
			}
			for action, until := range heldUntil {
				if now.Before(until) {
					in.Held[action] = true
				} else {
					delete(heldUntil, action)
				}
			}
			if pad != nil {
				p := pad.Poll()
				in.MoveX = p.LX
				in.MoveY = p.LY
				in.LookX += p.RX * 1.7
				in.Held[game.MoveForward] = in.Held[game.MoveForward] || p.LY < -.2
				in.Held[game.MoveBackward] = in.Held[game.MoveBackward] || p.LY > .2
				edge := func(v, old bool, a game.Action) {
					if v && !old {
						in.Pressed[a] = true
					}
				}
				edge(p.South, prevPad.South, game.Interact)
				edge(p.East, prevPad.East, game.ToggleCrouch)
				edge(p.West, prevPad.West, game.ThrowDecoy)
				edge(p.North, prevPad.North, game.UseFlare)
				edge(p.TL, prevPad.TL, game.ToggleSprint)
				edge(p.TR, prevPad.TR, game.Fire)
				edge(p.Select, prevPad.Select, game.ToggleTracker)
				edge(p.Start, prevPad.Start, game.Pause)
				prevPad = p
			}
			if !showHelp {
				g.Update(dt, in)
			}
			render.Frame(s, g, showHelp, padName)
		}
	}
}

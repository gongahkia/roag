package render

import (
	"fmt"
	"math"
	"strings"

	"nightshift/internal/game"
	"nightshift/internal/terminal"
)

const fov = math.Pi / 3

// The scene is rendered as color samples, then packed two-at-a-time into Unicode
// upper-half blocks. This doubles the useful vertical resolution of a terminal.
const (
	pxBlack = iota
	pxNavy
	pxCharcoal
	pxGray
	pxWhite
	pxRed
	pxAmber
)

type framebuffer struct {
	w, h int
	pix  []int
}

func newFramebuffer(w, h int) *framebuffer {
	return &framebuffer{w: w, h: h, pix: make([]int, w*h)}
}

func (f *framebuffer) set(x, y, color int) {
	if x >= 0 && x < f.w && y >= 0 && y < f.h {
		f.pix[y*f.w+x] = color
	}
}

func (f *framebuffer) at(x, y int) int { return f.pix[y*f.w+x] }

func (f *framebuffer) draw(s *terminal.Screen) {
	for y := 0; y < f.h/2; y++ {
		for x := 0; x < f.w; x++ {
			s.Pixel(x, y, f.at(x, y*2), f.at(x, y*2+1))
		}
	}
}

type sprite struct {
	at    game.Vec
	ch    rune
	color int
	scale float64
}

func Frame(s *terminal.Screen, g *game.Game, showHelp bool, controller string) {
	w, h := s.Size()
	s.Clear()
	if w < 72 || h < 24 {
		center(s, w, h/2, "NIGHTSHIFT requires at least 72 x 24", 2, true)
		center(s, w, h/2+2, fmt.Sprintf("current terminal: %d x %d", w, h), 1, false)
		s.Refresh()
		return
	}
	viewH := h - 7
	pixelH := viewH * 2
	frame := newFramebuffer(w, pixelH)
	depth := make([]float64, w)
	for x := 0; x < w; x++ {
		a := g.Facing + (float64(x)/float64(w)-.5)*fov
		d, hit := g.World.Ray(g.Player, a, 18)
		d *= math.Cos(a - g.Facing)
		depth[x] = d
		ceiling := int(float64(pixelH)/2 - float64(pixelH)/d*.52)
		floor := pixelH - ceiling
		if ceiling < 0 {
			ceiling = 0
		}
		if floor >= pixelH {
			floor = pixelH - 1
		}
		wallColor := surfaceColor(g, hit, d, x, w)
		for y := 0; y < pixelH; y++ {
			color := pxBlack
			switch {
			case y < ceiling:
				// A cold horizon and sparse pin lights give the ship depth without noise.
				if y > pixelH/3 {
					color = pxNavy
				}
				if (x*37+y*17)%379 == 0 {
					color = pxGray
				}
			case y <= floor:
				color = wallColor
				// Thin seams keep nearby bulkheads legible while moving.
				if d < 9 && ((y-ceiling)%8 == 0 || x%23 == 0) && color > pxCharcoal {
					color--
				}
			default:
				shade := float64(y-pixelH/2) / float64(pixelH/2)
				color = pxCharcoal
				if shade > .72 {
					color = pxBlack
				} else if shade < .28 {
					color = pxGray
				}
				cone := 1 - math.Abs(float64(x-w/2))/float64(w/2)
				if g.Flashlight && cone > .62 && shade < .62 {
					color = pxGray
				}
				if (x/6+y/4)%2 == 0 && color == pxGray {
					color = pxCharcoal
				}
			}
			frame.set(x, y, color)
		}
	}
	frame.draw(s)
	sp := []sprite{{at: g.Monster.Pos, ch: 'W', color: 2, scale: 1.8}, {at: game.Vec{X: float64(g.World.Generator.X) + .5, Y: float64(g.World.Generator.Y) + .5}, ch: 'G', color: 3, scale: 1.2}, {at: game.Vec{X: float64(g.World.Exit.X) + .5, Y: float64(g.World.Exit.Y) + .5}, ch: 'X', color: 5, scale: 1.4}}
	for _, it := range g.World.Items {
		if !it.Taken {
			sp = append(sp, sprite{at: it.At, ch: it.Kind.Rune(), color: 4, scale: .75})
		}
	}
	for p := range g.World.Hides {
		sp = append(sp, sprite{at: game.Vec{X: float64(p.X) + .5, Y: float64(p.Y) + .5}, ch: '[', color: 6, scale: .85})
	}
	for _, s0 := range sp {
		drawSprite(s, g, s0, depth, w, viewH)
	}
	if !g.Hidden {
		s.Put(w/2, viewH/2, '┼', 4, true)
	} else {
		for y := 0; y < viewH; y++ {
			if y < viewH/2-2 || y > viewH/2+2 {
				for x := 0; x < w; x++ {
					s.Put(x, y, ' ', 1, false)
				}
			}
		}
		s.Text(2, 2, "HIDDEN — [E] exit", 6, true)
	}
	for x := 0; x < w; x++ {
		s.Put(x, viewH, '─', 6, false)
	}
	status := fmt.Sprintf(" %s  CELL %d/3  DECOY %d  FLARE %d  STUN %d  LIGHT %3.0f%% ", g.Objective(), g.Fuses, g.Decoys, g.Flares, g.Charges, g.Battery)
	s.Text(0, viewH+1, trim(status, w), 3, true)
	stance := "WALK"
	if g.Crouched {
		stance = "CROUCH"
	}
	if g.Sprinting {
		stance = "SPRINT"
	}
	if g.Hidden {
		stance = "HIDDEN"
	}
	threat := threatText(g)
	line := fmt.Sprintf(" %s | %s | HUNTER: %s | controller: %s", stance, map[bool]string{true: "FLASHLIGHT ON", false: "dark"}[g.Flashlight], threat, controller)
	s.Text(0, viewH+2, trim(line, w), 1, false)
	if g.Tracker {
		tracker(s, g, w, viewH+3)
	} else {
		s.Text(0, viewH+3, trim(" [T] motion tracker   [H] controls", w), 6, false)
	}
	msg := g.Message
	if g.MessageTime <= 0 {
		msg = ""
	}
	s.Text(0, viewH+4, trim(" "+msg, w), map[bool]int{true: 2, false: 3}[g.Dead], true)
	if g.Dead {
		overlay(s, w, h, "YOU WERE FOUND", g.DeathReason, "[ENTER] begin another shift   [Q] quit", 2)
	} else if g.Won {
		overlay(s, w, h, "AIRLOCK SEALED", "You escaped the Sable Wake.", "[ENTER] play again   [Q] quit", 5)
	} else if g.Paused {
		overlay(s, w, h, "PAUSED", "The deck waits in silence.", "[ESC] resume   [H] controls", 3)
	}
	if showHelp {
		help(s, w, h)
	}
	s.Refresh()
}

func surfaceColor(g *game.Game, hit game.Point, distance float64, x, width int) int {
	if _, door := g.World.Doors[hit]; door {
		if distance < 10 || g.Flashlight {
			return pxAmber
		}
		return pxGray
	}
	color := pxCharcoal
	if distance < 11 {
		color = pxGray
	}
	if distance < 4 {
		color = pxWhite
	}
	cone := 1 - math.Abs(float64(x-width/2))/float64(width/2)
	if g.Flashlight && cone > .5 {
		reach := 5.0 + 10.0*(cone-.5)*2
		if distance < reach {
			color = pxWhite
		}
	}
	for _, flare := range g.FlaresActive {
		if flare.At.Distance(g.Player) < 7 && distance < 9 {
			return pxAmber
		}
	}
	return color
}

func drawSprite(scr *terminal.Screen, g *game.Game, sp sprite, depth []float64, w, h int) {
	delta := sp.at.Sub(g.Player)
	dist := delta.Len()
	if dist < .15 || dist > 18 || !g.World.LineClear(g.Player, sp.at) {
		return
	}
	a := math.Atan2(delta.Y, delta.X)
	rel := math.Mod(a-g.Facing+math.Pi, 2*math.Pi) - math.Pi
	if math.Abs(rel) > fov*.65 {
		return
	}
	sx := int((.5 + rel/fov) * float64(w))
	size := int(float64(h) / dist * sp.scale)
	if size < 1 {
		size = 1
	}
	if size > h {
		size = h
	}
	for dx := -size / 3; dx <= size/3; dx++ {
		x := sx + dx
		if x < 0 || x >= w || dist >= depth[x] {
			continue
		}
		for dy := -size / 2; dy <= size/2; dy++ {
			y := h/2 + dy
			if y >= 0 && y < h {
				ch := sp.ch
				if sp.ch == 'W' {
					if dy < -size/4 {
						ch = '^'
					} else if dx == 0 {
						ch = 'W'
					} else {
						ch = '|'
					}
				}
				scr.Put(x, y, ch, sp.color, true)
			}
		}
	}
}
func threatText(g *game.Game) string {
	d := g.MonsterDistance()
	if d < 2 {
		return "CONTACT"
	}
	if d < 5 {
		return "VERY CLOSE"
	}
	if d < 9 {
		return "NEARBY"
	}
	return "QUIET"
}
func tracker(s *terminal.Screen, g *game.Game, w, y int) {
	d := g.Monster.Pos.Sub(g.Player)
	a := math.Mod(math.Atan2(d.Y, d.X)-g.Facing+math.Pi, 2*math.Pi) - math.Pi
	dir := "AHEAD"
	if a > .45 {
		dir = "RIGHT"
	}
	if a < -.45 {
		dir = "LEFT"
	}
	if math.Abs(a) > 2.4 {
		dir = "BEHIND"
	}
	strength := int(math.Max(0, 10-d.Len()) / 2)
	bar := strings.Repeat("|", strength) + strings.Repeat(".", 5-strength)
	s.Text(0, y, trim(fmt.Sprintf(" TRACKER [%s] %s  range %.0fm", bar, dir, d.Len()), w), 5, true)
}
func trim(v string, n int) string {
	r := []rune(v)
	if len(r) > n {
		return string(r[:n])
	}
	return v
}
func center(s *terminal.Screen, w, y int, text string, color int, bold bool) {
	x := (w - len([]rune(text))) / 2
	if x < 0 {
		x = 0
	}
	s.Text(x, y, trim(text, w), color, bold)
}
func overlay(s *terminal.Screen, w, h int, title, sub, foot string, color int) {
	y := h/2 - 2
	for yy := y - 1; yy <= y+4; yy++ {
		for x := 0; x < w; x++ {
			s.Put(x, yy, ' ', 1, false)
		}
	}
	center(s, w, y, title, color, true)
	center(s, w, y+2, sub, 1, false)
	center(s, w, y+4, foot, 3, true)
}
func help(s *terminal.Screen, w, h int) {
	lines := []string{"CONTROLS / SURVIVAL", "W/S move  A/D strafe  arrows or mouse move turn", "R sprint toggle  C crouch  E interact / hide / door", "F flashlight  G throw noisemaker  B flare  T tracker", "SPACE or left mouse stunner  right mouse noisemaker", "controller: left stick move, right look, A interact, B crouch", "LB sprint, RB stunner, X noisemaker, Y flare, BACK tracker", "", "Sound travels. Light extends its vision. Doors slow it down.", "Break sight, vary hiding places, and do not repeat the same trick.", "", "[H] close"}
	top := (h - len(lines) - 2) / 2
	max := 0
	for _, l := range lines {
		if len(l) > max {
			max = len(l)
		}
	}
	left := (w - max - 4) / 2
	if left < 0 {
		left = 0
	}
	for y := top; y < top+len(lines)+2; y++ {
		for x := left; x < left+max+4 && x < w; x++ {
			s.Put(x, y, ' ', 1, false)
		}
	}
	for i, l := range lines {
		center(s, w, top+1+i, l, map[bool]int{true: 3, false: 1}[i == 0], i == 0)
	}
}

package arena

import (
	"fmt"
	"math"

	"rend3rer/internal/gameplay"
	"rend3rer/internal/soft3d"
)

// A tiny original bitmap font keeps every UI pixel at the game's resolution.
var glyphs = map[rune]string{
	'A': "010101111101101", 'B': "110101110101110", 'C': "011100100100011", 'D': "110101101101110", 'E': "111100110100111", 'F': "111100110100100", 'G': "011100101101011", 'H': "101101111101101", 'I': "111010010010111", 'J': "001001001101010", 'K': "101101110101101", 'L': "100100100100111", 'M': "101111111101101", 'N': "101111111111101", 'O': "010101101101010", 'P': "110101110100100", 'Q': "010101101111011", 'R': "110101110101101", 'S': "011100010001110", 'T': "111010010010010", 'U': "101101101101111", 'V': "101101101101010", 'W': "101101111111101", 'X': "101101010101101", 'Y': "101101010010010", 'Z': "111001010100111",
	'0': "111101101101111", '1': "010110010010111", '2': "110001010100111", '3': "110001010001110", '4': "101101111001001", '5': "111100110001110", '6': "011100111101111", '7': "111001010010010", '8': "111101111101111", '9': "111101111001110",
	':': "000010000010000", '.': "000000000000010", ',': "000000000010100", '-': "000000111000000", '/': "001001010100100", '+': "000010111010000", '!': "010010010000010", '?': "110001010000010", '[': "110100100100110", ']': "011001001001011", '=': "000111000111000", '(': "010100100100010", ')': "010001001001010", '%': "101001010100101", '\'': "010010000000000",
}

type canvas struct{ pixels []byte }

func (c canvas) rect(x, y, w, h int, col uint32) {
	for yy := max(0, y); yy < min(soft3d.Height, y+h); yy++ {
		for xx := max(0, x); xx < min(soft3d.Width, x+w); xx++ {
			i := (yy*soft3d.Width + xx) * 4
			c.pixels[i] = byte(col >> 16)
			c.pixels[i+1] = byte(col >> 8)
			c.pixels[i+2] = byte(col)
			c.pixels[i+3] = 255
		}
	}
}
func (c canvas) text(x, y, scale int, s string, col uint32) {
	start := x
	for _, r := range s {
		if r == '\n' {
			y += 7 * scale
			x = start
			continue
		}
		for i, b := range glyphs[r] {
			if b == '1' {
				c.rect(x+(i%3)*scale, y+(i/3)*scale, scale, scale, col)
			}
		}
		x += 4 * scale
	}
}
func (c canvas) center(y, scale int, s string, col uint32) {
	c.text((soft3d.Width-(len(s)*4-1)*scale)/2, y, scale, s, col)
}
func (c canvas) panel(x, y, w, h int) {
	c.rect(x, y, w, h, 0x101e29)
	c.rect(x, y, w, 1, 0x52777d)
	c.rect(x, y+h-1, w, 1, 0x2c4654)
}
func (c canvas) dim() {
	for i := 0; i < len(c.pixels); i += 4 {
		c.pixels[i] /= 3
		c.pixels[i+1] /= 3
		c.pixels[i+2] /= 3
	}
}

const gold uint32 = 0xe8bc79
const cyan uint32 = 0x86e6df
const white uint32 = 0xe3e9dd
const muted uint32 = 0x839ba1

func (v *View) HUD(pixels []byte, r *gameplay.Run, started, paused bool) {
	c := canvas{pixels}
	if started {
		v.weapon(c, r)
	}
	if v.HurtTicks > 0 {
		color := uint32(0x7e3339)
		for _, a := range [][4]int{{0, 0, 320, 3}, {0, 221, 320, 3}, {0, 0, 3, 224}, {317, 0, 3, 224}} {
			c.rect(a[0], a[1], a[2], a[3], color)
		}
	}
	c.panel(6, 6, 118, 21)
	c.text(12, 11, 1, fmt.Sprintf("CHAMBER %d / %d", r.Room+1, len(r.Cleared)), gold)
	status := fmt.Sprintf("%d HOSTILES", r.Remaining())
	if r.Cleared[r.Room] {
		status = "GATE OPEN - MOVE FORWARD"
	}
	c.text(12, 19, 1, status, muted)
	c.panel(244, 6, 70, 21)
	left := max(0, (gameplay.RunTicks-r.Elapsed+59)/60)
	c.text(251, 11, 1, "TIDE RISES", muted)
	c.text(251, 19, 1, fmt.Sprintf("%02d:%02d", left/60, left%60), cyan)
	c.panel(6, 195, 124, 23)
	c.text(12, 201, 1, fmt.Sprintf("VITALS %03d / %03d", r.Player.Health, r.Player.MaxHealth), white)
	c.rect(12, 210, 110, 3, 0x2e3b47)
	color := cyan
	if r.Player.Health < 30 {
		color = 0xe87767
	}
	c.rect(12, 210, 110*r.Player.Health/r.Player.MaxHealth, 3, color)
	c.panel(240, 195, 74, 23)
	c.text(247, 201, 1, "TIDECASTER", gold)
	status = "DASH READY"
	if r.Player.DashCooldown > 0 {
		status = "DASH CHARGING"
	}
	c.text(247, 210, 1, status, muted)
	c.text(143, 201, 1, fmt.Sprintf("SPEED %04.1f", r.Player.Velocity.Flat().Length()), white)
	status = "GROUNDED"
	if !r.Player.Grounded {
		status = "AIRBORNE"
	}
	c.text(143, 210, 1, status, muted)
	if r.Player.Rapid+r.Player.Fork+r.Player.Leech > 0 {
		c.text(9, 185, 1, fmt.Sprintf("HASTE %d  FORK %d  BLOOD %d", r.Player.Rapid, r.Player.Fork, r.Player.Leech), gold)
	}
	if v.MessageTicks > 0 && r.Phase == gameplay.Playing {
		c.panel(10, 33, 300, 13)
		c.center(37, 1, v.Message, cyan)
	}
	if r.Cleared[len(r.Cleared)-1] && r.Phase == gameplay.Playing {
		c.center(171, 1, "REACH THE BLUE RELIC. PRESS E TO ESCAPE.", cyan)
	}
	if started && !paused && r.Phase == gameplay.Playing {
		x, y := 160, 112
		cross := white
		if v.HitTicks > 0 {
			cross = gold
		}
		c.rect(x-7, y, 4, 1, cross)
		c.rect(x+4, y, 4, 1, cross)
		c.rect(x, y-7, 1, 4, cross)
		c.rect(x, y+4, 1, 4, cross)
		if v.HitTicks > 0 {
			c.rect(x-3, y-3, 1, 1, gold)
			c.rect(x+3, y-3, 1, 1, gold)
			c.rect(x-3, y+3, 1, 1, gold)
			c.rect(x+3, y+3, 1, 1, gold)
		}
	}
	if !started {
		v.title(c, r)
		return
	}
	if r.Phase == gameplay.Choosing {
		v.upgrades(c, r)
		return
	}
	if r.Phase == gameplay.Dead || r.Phase == gameplay.Won {
		v.ending(c, r)
		return
	}
	if paused {
		c.dim()
		c.panel(49, 68, 222, 106)
		c.center(82, 2, "TIDE PAUSED", gold)
		c.center(106, 1, "CLICK OR ENTER TO RESUME", white)
		c.panel(PauseSettings.Min.X, PauseSettings.Min.Y, PauseSettings.Dx(), PauseSettings.Dy())
		c.center(124, 1, "SETTINGS [TAB]", gold)
		c.center(145, 1, "M TO TOGGLE SOUND", muted)
		c.center(159, 1, "R RESTART SEED  /  N NEW SEED", muted)
	}
}

func (v *View) title(c canvas, r *gameplay.Run) {
	c.dim()
	c.panel(26, 18, 268, 188)
	c.text(44, 30, 1, "REND3RER / A SHRINE BELOW THE TIDE", muted)
	c.text(43, 48, 4, "TIDAL", gold)
	c.text(43, 73, 4, "DESCENT", white)
	c.text(44, 106, 1, "FIVE CHAMBERS. ONE LIFE. FIVE MINUTES.", cyan)
	c.text(44, 119, 1, "CLEAR EACH ROOM. BIND A RELIC. ESCAPE.", muted)
	c.text(44, 137, 1, "WASD MOVE  SPACE JUMP  CTRL DASH", white)
	c.text(44, 148, 1, "MOUSE / ARROWS LOOK  CLICK / F FIRE", white)
	c.text(44, 159, 1, "1-3 RELIC  E ESCAPE  ESC PAUSE  M SOUND", white)
	c.center(171, 2, "ENTER THE SHRINE", gold)
	c.panel(TitleSettings.Min.X, TitleSettings.Min.Y, TitleSettings.Dx(), TitleSettings.Dy())
	c.center(190, 1, "SETTINGS [TAB]", gold)
	c.center(201, 1, fmt.Sprintf("ENTER OR CLICK  /  SEED %d", r.Seed), muted)
}
func (v *View) upgrades(c canvas, r *gameplay.Run) {
	c.dim()
	c.panel(12, 26, 296, 174)
	c.center(38, 2, "CHAMBER PURIFIED", gold)
	c.center(55, 1, "CHOOSE A RELIC. EFFECTS STACK EACH CHAMBER.", muted)
	choices := [][3]string{{"1  STORM HEART", "FASTER FIRE. FASTER MOVEMENT.", fmt.Sprintf("HASTE %d -> %d", r.Player.Rapid, r.Player.Rapid+1)},
		{"2  FORKED TIDE", "ADD TWO SHOTS IN A WIDE FAN.", fmt.Sprintf("PELLETS %d -> %d", 1+2*r.Player.Fork, 3+2*r.Player.Fork)},
		{"3  BLOOD PACT", "+20 MAX HEALTH. HEAL ON EVERY KILL.", fmt.Sprintf("LIFE STEAL %d -> %d", r.Player.Leech*4, (r.Player.Leech+1)*4)}}
	for i, a := range choices {
		y := 71 + i*38
		c.rect(23, y, 274, 33, 0x1b303c)
		c.text(31, y+5, 1, a[0], cyan)
		c.text(31, y+15, 1, a[1], white)
		c.text(31, y+24, 1, a[2], muted)
	}
}
func (v *View) ending(c canvas, r *gameplay.Run) {
	c.dim()
	c.panel(30, 43, 260, 138)
	title := "CLAIMED BY THE TIDE"
	if r.Phase == gameplay.Won {
		title = "THE SHRINE RELEASES YOU"
	}
	c.center(58, 2, title, gold)
	line := "YOUR RELICS RETURN TO THE DEEP."
	if r.Phase == gameplay.Won {
		line = "THE RELIC IS YOURS. FOR NOW."
	} else if r.Elapsed >= gameplay.RunTicks {
		line = "TIME RAN OUT. THE TIDE TAKES ALL."
	}
	c.center(81, 1, line, muted)
	c.center(100, 1, fmt.Sprintf("%d KILLS  /  %d SHOTS  /  %02d:%02d", r.Kills, r.Shots, r.Elapsed/60/60, r.Elapsed/60%60), white)
	c.center(114, 1, fmt.Sprintf("SEED %d", r.Seed), cyan)
	c.center(138, 1, "R OR ENTER - RETRY THIS SEED", gold)
	c.center(153, 1, "N - DESCEND INTO A NEW SEED", white)
	c.center(167, 1, "ESC - RELEASE MOUSE", muted)
}

func (v *View) weapon(c canvas, r *gameplay.Run) {
	bob := 0
	if r.Phase == gameplay.Playing {
		bob = int(math.Sin(float64(v.Ticks)*.12) * 1.5)
	}
	kick := v.ShotTicks * 2
	x, y := 222, 154+bob+kick
	// Chunky iron barrel, brass receiver, turquoise coils, and a gloved hand.
	c.rect(x-12, y+37, 47, 40, 0x131e2c)
	c.rect(x-10, y+39, 42, 35, 0x455361)
	c.rect(x-5, y+32, 32, 21, 0x1b2935)
	c.rect(x-9, y+17, 35, 26, 0x172330)
	c.rect(x-6, y+19, 29, 21, 0xb79360)
	c.rect(x-2, y+20, 6, 19, 0xdec58b)
	c.rect(x+5, y-6, 16, 36, 0x0e1e2a)
	c.rect(x+8, y-7, 10, 34, 0x536d79)
	c.rect(x+10, y-8, 5, 8, 0x192935)
	for i := 0; i < 3; i++ {
		c.rect(x+6, y+4+i*6, 15, 3, cyan)
	}
	c.rect(x-3, y+43, 20, 19, 0x847364)
	c.rect(x+4, y+46, 21, 21, 0x344653)
	if v.ShotTicks >= 4 {
		c.rect(x+5, y-20, 18, 10, 0xe6a761)
		c.rect(x+9, y-26, 10, 20, 0xffe5aa)
		c.rect(x+12, y-29, 4, 26, 0xffffdd)
	}
}

package game

import (
	"fmt"
	"math"
)

type Monster struct {
	Pos, Target, LastSeen                Vec
	State                                AIState
	StateTime, DoorDelay, StunResistance float64
	Path                                 []Point
	PathAge                              float64
	Facing                               float64
	DecoyTrust                           float64
	Searched                             map[Point]int
}

type Game struct {
	World                                            *World
	Player                                           Vec
	Facing                                           float64
	Velocity                                         Vec
	Monster                                          Monster
	Crouched, Sprinting, Flashlight, Tracker, Hidden bool
	HideAt                                           Point
	Fuses, Decoys, Flares, Charges                   int
	Battery                                          float64
	GeneratorOn, Won, Dead, Paused                   bool
	Time, StepClock, MessageTime                     float64
	Message, DeathReason                             string
	Sounds                                           []Sound
	FlaresActive                                     []Sound
	HideUses                                         map[Point]int
	Alert                                            float64
}

func New() *Game {
	g := &Game{World: NewWorld(), Player: Vec{3.5, 12.5}, Facing: 0, Decoys: 2, Flares: 1, Charges: 2, Battery: 100, Message: "Find three power cells. Restore the generator. Reach the airlock.", MessageTime: 9, HideUses: map[Point]int{}}
	g.Monster = Monster{Pos: Vec{37.5, 22.5}, Target: Vec{37.5, 22.5}, State: Dormant, DecoyTrust: 1, Searched: map[Point]int{}}
	return g
}

func (g *Game) AddSound(at Vec, intensity float64, kind SoundKind) {
	g.Sounds = append(g.Sounds, Sound{At: at, Intensity: intensity, Kind: kind})
}
func (g *Game) Say(s string) { g.Message = s; g.MessageTime = 4 }

func (g *Game) Update(dt float64, in Input) {
	if in.Pressed[Pause] {
		g.Paused = !g.Paused
	}
	if g.Paused || g.Dead || g.Won {
		return
	}
	dt = clamp(dt, 0, .08)
	g.Time += dt
	g.MessageTime -= dt
	for i := range g.Sounds {
		g.Sounds[i].Age += dt
	}
	kept := g.Sounds[:0]
	for _, s := range g.Sounds {
		if s.Age < 4 {
			kept = append(kept, s)
		}
	}
	g.Sounds = kept
	flareKept := g.FlaresActive[:0]
	for _, f := range g.FlaresActive {
		f.Age += dt
		if f.Age < 9 {
			flareKept = append(flareKept, f)
		}
	}
	g.FlaresActive = flareKept
	if in.Pressed[ToggleCrouch] && !g.Hidden {
		g.Crouched = !g.Crouched
		if g.Crouched {
			g.Sprinting = false
		}
	}
	if in.Pressed[ToggleSprint] && !g.Hidden {
		g.Sprinting = !g.Sprinting
		if g.Sprinting {
			g.Crouched = false
		}
	}
	if in.Pressed[ToggleFlashlight] {
		if g.Battery > 0 {
			g.Flashlight = !g.Flashlight
		}
	}
	if in.Pressed[ToggleTracker] {
		g.Tracker = !g.Tracker
	}
	if in.Pressed[Interact] {
		g.interact()
	}
	if in.Pressed[ThrowDecoy] {
		g.throwDecoy()
	}
	if in.Pressed[UseFlare] {
		g.useFlare()
	}
	if in.Pressed[Fire] {
		g.fire()
	}
	if g.Hidden {
		g.updateMonster(dt)
		return
	}
	turn := 0.0
	if in.Held[TurnLeft] {
		turn--
	}
	if in.Held[TurnRight] {
		turn++
	}
	turn += in.LookX
	g.Facing += turn * 2.3 * dt
	forward, right := 0.0, 0.0
	if in.Held[MoveForward] {
		forward++
	}
	if in.Held[MoveBackward] {
		forward--
	}
	if in.Held[StrafeRight] {
		right++
	}
	if in.Held[StrafeLeft] {
		right--
	}
	forward += -in.MoveY
	right += in.MoveX
	forward = clamp(forward, -1, 1)
	right = clamp(right, -1, 1)
	dir := Vec{math.Cos(g.Facing)*forward - math.Sin(g.Facing)*right, math.Sin(g.Facing)*forward + math.Cos(g.Facing)*right}
	if dir.Len() > 1 {
		dir = dir.Normalized()
	}
	speed := 2.25
	noise := 2.1
	if g.Crouched {
		speed = 1.25
		noise = .7
	}
	if g.Sprinting {
		speed = 3.75
		noise = 6
	}
	g.Velocity = dir.Mul(speed)
	move := g.Velocity.Mul(dt)
	next := g.Player
	if p := g.Player.Add(Vec{move.X, 0}); g.World.Passable(p) {
		next.X = p.X
	}
	if p := (Vec{next.X, g.Player.Y + move.Y}); g.World.Passable(p) {
		next.Y = p.Y
	}
	g.Player = next
	if dir.Len() > .1 {
		g.StepClock -= dt
		if g.StepClock <= 0 {
			g.AddSound(g.Player, noise, SoundStep)
			g.StepClock = map[bool]float64{true: .27, false: .48}[g.Sprinting]
			if g.Crouched {
				g.StepClock = .72
			}
		}
	}
	if g.Flashlight {
		g.Battery -= dt * 1.2
		if g.Battery <= 0 {
			g.Battery = 0
			g.Flashlight = false
			g.Say("The flashlight battery died.")
		}
	}
	g.pickups()
	g.updateMonster(dt)
}

func (g *Game) nearestDoor() (Point, bool) {
	for p := range g.World.Doors {
		if g.Player.Distance(Vec{float64(p.X) + .5, float64(p.Y) + .5}) < 1.35 {
			return p, true
		}
	}
	return Point{}, false
}
func (g *Game) nearestHide() (Point, bool) {
	for p := range g.World.Hides {
		if g.Player.Distance(Vec{float64(p.X) + .5, float64(p.Y) + .5}) < 1.05 {
			return p, true
		}
	}
	return Point{}, false
}

func (g *Game) interact() {
	if g.Hidden {
		g.Hidden = false
		g.Player = Vec{float64(g.HideAt.X) + .5, float64(g.HideAt.Y) + .5}
		g.AddSound(g.Player, .5, SoundStep)
		g.Say("You slip out of hiding.")
		return
	}
	if g.Player.Distance(Vec{float64(g.World.Generator.X) + .5, float64(g.World.Generator.Y) + .5}) < 1.4 {
		if g.Fuses < 3 {
			g.Say(fmt.Sprintf("Generator needs 3 cells. You have %d.", g.Fuses))
			return
		}
		if !g.GeneratorOn {
			g.GeneratorOn = true
			g.AddSound(g.Player, 12, SoundGenerator)
			g.Alert = 1
			g.Monster.State = Investigate
			g.Monster.Target = g.Player
			g.Say("POWER RESTORED. The airlock is live—but the whole deck heard it.")
		}
		return
	}
	if g.Player.Distance(Vec{float64(g.World.Exit.X) + .5, float64(g.World.Exit.Y) + .5}) < 1.4 {
		if g.GeneratorOn {
			g.Won = true
			g.Say("AIRLOCK SEALED. You escaped the Sable Wake.")
		} else {
			g.Say("The airlock has no power.")
		}
		return
	}
	if p, ok := g.nearestDoor(); ok {
		g.World.Doors[p] = !g.World.Doors[p]
		g.AddSound(g.Player, 2.4, SoundDoor)
		if g.World.Doors[p] {
			g.Say("Door opened.")
		} else {
			g.Say("Door sealed. It may buy you seconds.")
		}
		return
	}
	if p, ok := g.nearestHide(); ok {
		g.Hidden = true
		g.HideAt = p
		g.HideUses[p]++
		g.Crouched = false
		g.Sprinting = false
		g.Say("Hidden. Stay still and listen.")
		return
	}
	g.Say("Nothing to use here.")
}

func (g *Game) throwDecoy() {
	if g.Decoys <= 0 {
		g.Say("No noisemakers left.")
		return
	}
	g.Decoys--
	at := g.Player.Add(Vec{math.Cos(g.Facing), math.Sin(g.Facing)}.Mul(5))
	if g.World.Blocked(at.Point()) {
		at = g.Player.Add(Vec{math.Cos(g.Facing), math.Sin(g.Facing)}.Mul(2))
	}
	g.AddSound(at, 10, SoundDecoy)
	g.Say("Noisemaker thrown. Repeating tricks makes it suspicious.")
}
func (g *Game) useFlare() {
	if g.Flares <= 0 {
		g.Say("No flares left.")
		return
	}
	g.Flares--
	g.FlaresActive = append(g.FlaresActive, Sound{At: g.Player, Intensity: 7})
	g.AddSound(g.Player, 4, SoundStep)
	g.Say("Flare burning: bright, loud, and briefly repellent.")
}
func (g *Game) fire() {
	if g.Charges <= 0 {
		g.Say("Stunner empty.")
		return
	}
	g.Charges--
	g.AddSound(g.Player, 11, SoundGun)
	d := g.Player.Distance(g.Monster.Pos)
	aim := math.Abs(angleDelta(math.Atan2(g.Monster.Pos.Y-g.Player.Y, g.Monster.Pos.X-g.Player.X), g.Facing))
	if d < 9 && aim < .12 && g.World.LineClear(g.Player, g.Monster.Pos) {
		duration := 4.5 - g.Monster.StunResistance*1.25
		if duration < 1.5 {
			duration = 1.5
		}
		g.Monster.State = Stunned
		g.Monster.StateTime = duration
		g.Monster.StunResistance++
		g.Say("Direct hit. It is learning to shake off the charge.")
	} else {
		g.Say("The shot cracks through the deck—and misses.")
	}
}
func (g *Game) pickups() {
	for i := range g.World.Items {
		it := &g.World.Items[i]
		if it.Taken || g.Player.Distance(it.At) > .7 {
			continue
		}
		it.Taken = true
		switch it.Kind {
		case Fuse:
			g.Fuses++
			g.Say(fmt.Sprintf("Power cell recovered (%d/3).", g.Fuses))
		case Decoy:
			g.Decoys++
			g.Say("Noisemaker recovered.")
		case Flare:
			g.Flares++
			g.Say("Emergency flare recovered.")
		case Battery:
			g.Battery = math.Min(100, g.Battery+55)
			g.Say("Flashlight battery recovered.")
		case StunCharge:
			g.Charges += 2
			g.Say("Two stunner charges recovered.")
		}
	}
}

func (g *Game) MonsterDistance() float64 { return g.Player.Distance(g.Monster.Pos) }
func (g *Game) Objective() string {
	if g.Fuses < 3 {
		return fmt.Sprintf("POWER CELLS %d/3", g.Fuses)
	}
	if !g.GeneratorOn {
		return "RETURN TO GENERATOR"
	}
	return "REACH AIRLOCK"
}

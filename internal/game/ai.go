package game

import (
	"math"
	"math/rand/v2"
)

func (g *Game) monsterSeesPlayer() bool {
	if g.Hidden {
		return false
	}
	d := g.Monster.Pos.Distance(g.Player)
	limit := 7.0
	if g.Flashlight {
		limit = 13
	}
	if d > limit || !g.World.LineClear(g.Monster.Pos, g.Player) {
		return false
	}
	to := math.Atan2(g.Player.Y-g.Monster.Pos.Y, g.Player.X-g.Monster.Pos.X)
	return g.Monster.State == Hunt || math.Abs(angleDelta(to, g.Monster.Facing)) < .82
}

func (g *Game) loudestSound() (Sound, bool) {
	bestScore := 0.0
	var best Sound
	for _, s := range g.Sounds {
		trust := 1.0
		if s.Kind == SoundDecoy {
			trust = g.Monster.DecoyTrust
		}
		d := g.Monster.Pos.Distance(s.At)
		score := (s.Intensity*trust - s.Age*.8) / (1 + d*.16)
		if s.Intensity*trust > d*.72 && score > bestScore {
			bestScore = score
			best = s
		}
	}
	return best, bestScore > 0
}

func (g *Game) flareRepels() (Vec, bool) {
	for _, f := range g.FlaresActive {
		if g.Monster.Pos.Distance(f.At) < 5.5 {
			return f.At, true
		}
	}
	return Vec{}, false
}

func (g *Game) updateMonster(dt float64) {
	m := &g.Monster
	m.StateTime -= dt
	m.PathAge -= dt
	if m.State == Dormant {
		if g.Time < 7 && g.Fuses == 0 {
			return
		}
		m.State = Stalk
		m.Target = Vec{20.5, 12.5}
		g.Say("Something heavy moved inside the vents.")
	}
	if m.State == Stunned {
		if m.StateTime > 0 {
			return
		}
		m.State = Hunt
		m.Target = g.Player
	}
	if f, ok := g.flareRepels(); ok {
		m.State = Retreat
		m.StateTime = 2
		m.Target = m.Pos.Add(m.Pos.Sub(f).Normalized().Mul(7))
	}
	seen := g.monsterSeesPlayer()
	if seen {
		m.State = Hunt
		m.StateTime = 6
		m.LastSeen = g.Player
		m.Target = g.Player.Add(g.Velocity.Normalized().Mul(2.5))
		g.Alert = math.Min(1, g.Alert+dt*.5)
	} else if s, ok := g.loudestSound(); ok && m.State != Retreat {
		if m.State != Hunt || m.StateTime < 2 {
			m.State = Investigate
			m.StateTime = 5
			m.Target = s.At
		}
	}
	switch m.State {
	case Hunt:
		if !seen && m.StateTime <= 0 {
			m.State = Search
			m.StateTime = 9
			m.Target = m.LastSeen
		}
	case Investigate:
		if m.Pos.Distance(m.Target) < .8 {
			decoyHere := false
			for _, s := range g.Sounds {
				if s.Kind == SoundDecoy && s.At.Distance(m.Target) < 1 {
					decoyHere = true
				}
			}
			if decoyHere {
				m.DecoyTrust = math.Max(.25, m.DecoyTrust-.22)
			}
			m.State = Search
			m.StateTime = 7
			m.Target = g.searchTarget()
		}
	case Search:
		if m.StateTime <= 0 {
			m.State = Stalk
			m.Target = g.predictObjective()
		} else if m.Pos.Distance(m.Target) < .8 {
			m.Target = g.searchTarget()
		}
	case Retreat:
		if m.StateTime <= 0 {
			m.State = Stalk
			m.Target = g.predictObjective()
		}
	case Stalk:
		if m.Pos.Distance(m.Target) < 1 {
			m.Target = g.predictObjective()
		}
	}
	// It opens doors, but each one costs time and announces its approach.
	nextPath := g.World.Path(m.Pos.Point(), m.Target.Point(), true)
	if len(nextPath) > 0 {
		n := nextPath[0]
		if open, door := g.World.Doors[n]; door && !open {
			m.DoorDelay += dt
			if m.DoorDelay > .85 {
				g.World.Doors[n] = true
				m.DoorDelay = 0
				g.AddSound(m.Pos, 2, SoundDoor)
			}
			return
		}
	}
	if m.PathAge <= 0 {
		m.Path = nextPath
		m.PathAge = .3
	}
	if len(m.Path) > 0 {
		target := Vec{float64(m.Path[0].X) + .5, float64(m.Path[0].Y) + .5}
		delta := target.Sub(m.Pos)
		speed := 1.45
		if m.State == Hunt {
			speed = 2.55
		}
		if m.State == Retreat {
			speed = 2.15
		}
		if m.State == Search {
			speed = 1.75
		}
		step := delta.Normalized().Mul(speed * dt)
		m.Facing = math.Atan2(step.Y, step.X)
		m.Pos = m.Pos.Add(step)
		if m.Pos.Distance(target) < .18 {
			m.Path = m.Path[1:]
		}
	}
	if g.Hidden && m.Pos.Distance(Vec{float64(g.HideAt.X) + .5, float64(g.HideAt.Y) + .5}) < .85 {
		uses := g.HideUses[g.HideAt]
		searched := m.Searched[g.HideAt]
		m.Searched[g.HideAt]++
		if m.State == Hunt || uses+searched >= 3 {
			g.kill("It learned your hiding habit and tore the locker open.")
		} else {
			m.Target = g.searchTarget()
		}
	} else if !g.Hidden && m.Pos.Distance(g.Player) < .62 {
		g.kill("The creature found you in the dark.")
	}
	g.Alert = math.Max(0, g.Alert-dt*.035)
}

func (g *Game) searchTarget() Vec {
	// Search nearby hiding places first only after observed/repeated use; otherwise sweep adjacent rooms.
	best := Point{}
	bestScore := -1
	for p := range g.World.Hides {
		d := abs(p.X-g.Monster.Pos.Point().X) + abs(p.Y-g.Monster.Pos.Point().Y)
		score := g.HideUses[p]*5 + g.Monster.Searched[p]*2 - d
		if score > bestScore {
			bestScore = score
			best = p
		}
	}
	if bestScore > 0 {
		return Vec{float64(best.X) + .5, float64(best.Y) + .5}
	}
	for attempts := 0; attempts < 12; attempts++ {
		p := Point{g.Monster.Target.Point().X + rand.IntN(11) - 5, g.Monster.Target.Point().Y + rand.IntN(9) - 4}
		if g.World.Inside(p) && !g.World.Blocked(p) {
			return Vec{float64(p.X) + .5, float64(p.Y) + .5}
		}
	}
	return g.Monster.LastSeen
}

func (g *Game) predictObjective() Vec {
	// The creature patrols likely task locations instead of knowing the player's position.
	if g.Fuses >= 3 && !g.GeneratorOn {
		return Vec{float64(g.World.Generator.X) + .5, float64(g.World.Generator.Y) + .5}
	}
	if g.GeneratorOn {
		return Vec{float64(g.World.Exit.X) + .5, float64(g.World.Exit.Y) + .5}
	}
	remaining := []Vec{}
	for _, it := range g.World.Items {
		if it.Kind == Fuse && !it.Taken {
			remaining = append(remaining, it.At)
		}
	}
	if len(remaining) > 0 {
		return remaining[rand.IntN(len(remaining))]
	}
	return Vec{20.5, 12.5}
}
func (g *Game) kill(reason string) { g.Dead = true; g.DeathReason = reason; g.Say(reason) }

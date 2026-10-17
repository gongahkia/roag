package gameplay

import "math"

type Phase uint8

const (
	Playing Phase = iota
	Choosing
	Dead
	Won
)

type EnemyKind uint8

const (
	Charger EnemyKind = iota
	Caster
)

type EventKind uint8

const (
	Shot EventKind = iota
	Hit
	Kill
	Hurt
	Warning
	BoltFired
	RoomClear
	UpgradeTaken
	Healed
	Victory
	Defeat
	Dashed
	Jumped
	Landed
)

// Command is intent, never a client-supplied position, damage value, or hit.
// Step validates it and advances exactly one tick. A future server can consume
// these commands and publish State snapshots using the same simulation.
type Command struct {
	Forward, Strafe, Yaw, Pitch        float64
	Fire, Sprint, Dash, Interact, Jump bool
	Choice                             int // 1..3, only accepted while Choosing
}

type Player struct {
	Position                                        Vec3
	Velocity                                        Vec3
	Grounded                                        bool
	JumpBuffer                                      int
	Yaw, Pitch                                      float64
	Health, MaxHealth                               int
	Cooldown, DashCooldown, DashTicks, Invulnerable int
	DashDirection                                   Vec3
	Rapid, Fork, Leech                              int
}
type Enemy struct {
	ID                                    uint32
	Room                                  int
	Kind                                  EnemyKind
	Position                              Vec3
	Health, Cooldown, Windup, Rush, Flash int
	Aim                                   Vec3
}
type Projectile struct {
	ID                 uint32
	Position, Velocity Vec3
	Life               int
}
type Pickup struct {
	ID       uint32
	Position Vec3
	Taken    bool
}
type Event struct {
	Kind             EventKind
	Position, Target Vec3
}

type State struct {
	Seed               int64
	Tick, Elapsed      int
	Phase              Phase
	Player             Player
	Enemies            []Enemy
	Projectiles        []Projectile
	Pickups            []Pickup
	Cleared            []bool
	Room, Kills, Shots int
}

type Run struct {
	World *World
	State
	Events       []Event // replaced each Step; presentation consumes these once
	nextID       uint32
	flow         []int
	flowW, flowH int
}

func New(seed int64) *Run {
	w := Generate(seed)
	r := &Run{World: w, State: State{Seed: seed, Player: Player{Position: w.Spawn, Health: 100, MaxHealth: 100, Grounded: true}, Cleared: make([]bool, len(w.Rooms))}}
	for room, rr := range w.Rooms {
		for _, s := range rr.Spawns {
			hp := 48
			if s.Kind == Caster {
				hp = 40
			}
			r.Enemies = append(r.Enemies, Enemy{ID: r.id(), Room: room, Kind: s.Kind, Position: s.Position, Health: hp, Cooldown: 45 + len(r.Enemies)*7%50})
		}
	}
	return r
}
func (r *Run) id() uint32 { r.nextID++; return r.nextID }
func (r *Run) event(kind EventKind, pos, target Vec3) {
	r.Events = append(r.Events, Event{kind, pos, target})
}

// Snapshot copies variable-length state, so retaining or serializing it cannot
// mutate the live run. It is a data boundary, not a network protocol yet.
func (r *Run) Snapshot() State {
	s := r.State
	s.Enemies = append([]Enemy(nil), s.Enemies...)
	s.Projectiles = append([]Projectile(nil), s.Projectiles...)
	s.Pickups = append([]Pickup(nil), s.Pickups...)
	s.Cleared = append([]bool(nil), s.Cleared...)
	return s
}

func clean(v, lo, hi float64) float64 {
	if math.IsNaN(v) || math.IsInf(v, 0) {
		return 0
	}
	return math.Max(lo, math.Min(v, hi))
}

func (r *Run) Step(c Command) {
	r.Events = r.Events[:0]
	if r.Phase == Dead || r.Phase == Won {
		return
	}
	if r.Phase == Choosing {
		if c.Choice >= 1 && c.Choice <= 3 {
			r.choose(c.Choice)
		}
		return
	}
	r.Tick++
	r.Elapsed++
	if r.Elapsed >= RunTicks {
		r.Player.Health = 0
		r.Phase = Dead
		r.event(Defeat, r.Player.Position, Vec3{})
		return
	}
	p := &r.Player
	p.Yaw = math.Remainder(clean(c.Yaw, -1e6, 1e6), 2*math.Pi)
	p.Pitch = clean(c.Pitch, -1.45, 1.45)
	p.Cooldown = max(0, p.Cooldown-1)
	p.DashCooldown = max(0, p.DashCooldown-1)
	p.Invulnerable = max(0, p.Invulnerable-1)
	r.movePlayer(c)
	for i, room := range r.World.Rooms {
		if room.Bounds.Contains(p.Position.X, p.Position.Z) && i > r.Room {
			r.Room = i
		}
	}
	if c.Fire && p.Cooldown == 0 {
		r.fire()
	}
	if r.Tick%15 == 1 {
		r.buildFlow()
	}
	r.updateEnemies()
	r.updateProjectiles()
	if r.Phase == Dead {
		return
	}
	for i := range r.Pickups {
		x := &r.Pickups[i]
		if !x.Taken && p.Health < p.MaxHealth && distance(p.Position, x.Position) < .8 {
			x.Taken = true
			p.Health = min(p.MaxHealth, p.Health+22)
			r.event(Healed, x.Position, Vec3{})
		}
	}
	if !r.Cleared[r.Room] && r.Remaining() == 0 {
		r.Cleared[r.Room] = true
		p.Health = min(p.MaxHealth, p.Health+12)
		r.Projectiles = r.Projectiles[:0]
		r.event(RoomClear, p.Position, Vec3{})
		if r.Room < len(r.World.Rooms)-1 {
			r.Phase = Choosing
		}
	}
	if c.Interact && r.Cleared[len(r.Cleared)-1] && distance(p.Position, r.World.Exit) < 1.8 {
		r.Phase = Won
		r.event(Victory, p.Position, Vec3{})
	}
}

func (r *Run) Remaining() int {
	n := 0
	for _, e := range r.Enemies {
		if e.Room == r.Room && e.Health > 0 {
			n++
		}
	}
	return n
}

func (r *Run) choose(choice int) {
	p := &r.Player
	switch choice {
	case 1:
		p.Rapid++
	case 2:
		p.Fork++
	case 3:
		p.Leech++
		p.MaxHealth += 20
		p.Health = min(p.MaxHealth, p.Health+30)
	}
	r.Phase = Playing
	r.event(UpgradeTaken, p.Position, Vec3{})
}

func (r *Run) fire() {
	p := &r.Player
	p.Cooldown = max(7, 18-p.Rapid*3)
	r.Shots++
	origin := p.Position.Add(Vec3{Y: EyeHeight})
	for n := 0; n < 1+2*p.Fork; n++ {
		yaw := p.Yaw + (float64(n)-float64(p.Fork))*.055
		dir := Vec3{math.Sin(yaw) * math.Cos(p.Pitch), math.Sin(p.Pitch), math.Cos(yaw) * math.Cos(p.Pitch)}
		limit := r.World.Raycast(origin, dir, 45, r.Cleared)
		index := -1
		for i, e := range r.Enemies {
			if e.Health <= 0 {
				continue
			}
			lo := e.Position.Add(Vec3{X: -.45, Z: -.45})
			hi := e.Position.Add(Vec3{X: .45, Y: 1.95, Z: .45})
			if d := RayBox(origin, dir, lo, hi); d < limit {
				limit = d
				index = i
			}
		}
		end := origin.Add(dir.Scale(limit))
		if n == 0 {
			r.event(Shot, origin, end)
		}
		if index >= 0 {
			e := &r.Enemies[index]
			e.Health -= 25
			e.Flash = 7
			r.event(Hit, end, Vec3{})
			if e.Health <= 0 {
				r.Kills++
				r.event(Kill, e.Position.Add(Vec3{Y: 1}), Vec3{})
				p.Health = min(p.MaxHealth, p.Health+p.Leech*4)
				if r.Kills%3 == 0 {
					r.Pickups = append(r.Pickups, Pickup{ID: r.id(), Position: e.Position})
				}
			}
		}
	}
}

func (r *Run) hurt(damage int) {
	p := &r.Player
	if p.Invulnerable > 0 || r.Phase != Playing {
		return
	}
	p.Health = max(0, p.Health-damage)
	p.Invulnerable = 24
	r.event(Hurt, p.Position, Vec3{})
	if p.Health == 0 {
		r.Phase = Dead
		r.event(Defeat, p.Position, Vec3{})
	}
}

func (r *Run) updateEnemies() {
	for i := range r.Enemies {
		e := &r.Enemies[i]
		if e.Health <= 0 || e.Room != r.Room {
			continue
		}
		e.Flash = max(0, e.Flash-1)
		eye := e.Position.Add(Vec3{Y: 1.35})
		target := r.Player.Position.Add(Vec3{Y: 1.2})
		d := distance(e.Position.Flat(), r.Player.Position.Flat())
		if e.Windup > 0 {
			e.Windup--
			if e.Windup == 0 {
				if e.Kind == Charger {
					e.Rush = 28
				} else {
					r.Projectiles = append(r.Projectiles, Projectile{r.id(), eye, e.Aim.Scale(7), 180})
					r.event(BoltFired, eye, Vec3{})
				}
			}
			continue
		}
		if e.Rush > 0 {
			e.Rush--
			e.Position = r.move(e.Position, e.Aim.Flat().Scale(9.5/TickRate), .38, e.ID)
			if distance(e.Position.Flat(), r.Player.Position.Flat()) < .9 && r.Player.Position.Y < 1.95 {
				r.hurt(18)
				e.Rush = 0
			}
			if e.Rush == 0 {
				e.Cooldown = 75
			}
			continue
		}
		e.Cooldown = max(0, e.Cooldown-1)
		see := r.World.visible(eye, target, r.Cleared)
		if e.Cooldown == 0 && see && (e.Kind == Caster || d < 6.5) {
			e.Windup = 40
			if e.Kind == Caster {
				e.Windup = 48
				e.Cooldown = 110
			}
			e.Aim = target.Sub(eye).Unit()
			r.event(Warning, eye, Vec3{})
			continue
		}
		if d < .8 && r.Player.Position.Y < 1.95 {
			r.hurt(10)
		}
		if e.Kind == Caster && see && d < 8 {
			continue
		}
		dir := r.chaseDirection(e.Position)
		if see {
			dir = r.Player.Position.Sub(e.Position).Flat().Unit()
		}
		// Deterministic separation keeps multiple enemies from occupying one point.
		for j := range r.Enemies {
			o := &r.Enemies[j]
			if i == j || o.Health <= 0 || o.Room != e.Room {
				continue
			}
			delta := e.Position.Sub(o.Position).Flat()
			n := delta.Length()
			if n > 0 && n < .95 {
				dir = dir.Add(delta.Scale((.95 - n) / n))
			}
		}
		speed := 2.0
		if e.Kind == Caster {
			speed = 1.5
		}
		e.Position = r.move(e.Position, dir.Unit().Scale(speed/TickRate), .38, e.ID)
	}
}

func (r *Run) updateProjectiles() {
	alive := r.Projectiles[:0]
	for _, b := range r.Projectiles {
		b.Life--
		delta := b.Velocity.Scale(1.0 / TickRate)
		n := delta.Length()
		wall := r.World.Raycast(b.Position, delta.Unit(), n, r.Cleared)
		p := r.Player.Position
		hit := RayBox(b.Position, delta.Unit(), p.Add(Vec3{X: -PlayerRadius, Z: -PlayerRadius}), p.Add(Vec3{X: PlayerRadius, Y: 1.8, Z: PlayerRadius}))
		if hit <= wall && hit <= n {
			r.hurt(12)
			continue
		}
		if wall < n || b.Life <= 0 {
			continue
		}
		b.Position = b.Position.Add(delta)
		alive = append(alive, b)
	}
	r.Projectiles = alive
}

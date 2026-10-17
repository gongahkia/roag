package gameplay

import "math"

const (
	GroundSpeed        = 7.5
	SprintSpeed        = 10.0
	GroundAcceleration = 12.0
	GroundFriction     = 8.0
	AirAcceleration    = 6.0
	AirWishSpeed       = 5.0
	MaxHorizontalSpeed = 18.0
	JumpSpeed          = 7.6
	Gravity            = 24.0
)

// accelerate limits speed projected onto the requested direction, preserving
// momentum in other directions. This enables air strafing and strafe jumps.
func accelerate(velocity, wish Vec3, wishSpeed, acceleration, baseSpeed float64) Vec3 {
	current := velocity.X*wish.X + velocity.Z*wish.Z
	add := wishSpeed - current
	if add <= 0 {
		return velocity
	}
	push := math.Min(add, acceleration*baseSpeed/TickRate)
	velocity.X += wish.X * push
	velocity.Z += wish.Z * push
	return velocity
}

func (r *Run) movePlayer(c Command) {
	p := &r.Player
	if p.Grounded && math.Abs(r.World.supportHeight(p.Position, p.Position.Y+.01, r.Cleared)-p.Position.Y) > .02 {
		p.Grounded = false
	}
	if c.Jump {
		p.JumpBuffer = 6
	} else {
		p.JumpBuffer = max(0, p.JumpBuffer-1)
	}
	if p.Grounded && p.JumpBuffer > 0 {
		p.Velocity.Y = JumpSpeed
		p.Grounded = false
		p.JumpBuffer = 0
		r.event(Jumped, p.Position, Vec3{})
	}
	f, s := clean(c.Forward, -1, 1), clean(c.Strafe, -1, 1)
	magnitude := math.Hypot(f, s)
	if magnitude > 1 {
		f /= magnitude
		s /= magnitude
		magnitude = 1
	}
	wish := Vec3{X: math.Sin(p.Yaw)*f + math.Cos(p.Yaw)*s, Z: math.Cos(p.Yaw)*f - math.Sin(p.Yaw)*s}.Unit()
	speed := GroundSpeed
	if c.Sprint {
		speed = SprintSpeed
	}
	speed *= 1 + float64(p.Rapid)*.05
	if c.Dash && p.DashCooldown == 0 {
		p.DashDirection = wish
		if magnitude == 0 {
			p.DashDirection = Vec3{X: math.Sin(p.Yaw), Z: math.Cos(p.Yaw)}
		}
		p.DashTicks = 10
		p.DashCooldown = 75
		p.Invulnerable = max(p.Invulnerable, 10)
		r.event(Dashed, p.Position, Vec3{})
	}
	if p.DashTicks > 0 {
		p.Velocity.X = p.DashDirection.X * 15
		p.Velocity.Z = p.DashDirection.Z * 15
		p.DashTicks--
	} else if p.Grounded {
		flat := p.Velocity.Flat().Length()
		if flat > 0 {
			next := math.Max(0, flat-math.Max(flat, 2)*GroundFriction/TickRate)
			p.Velocity.X *= next / flat
			p.Velocity.Z *= next / flat
		}
		p.Velocity = accelerate(p.Velocity, wish, speed*magnitude, GroundAcceleration, speed*magnitude)
	} else {
		p.Velocity = accelerate(p.Velocity, wish, AirWishSpeed*magnitude, AirAcceleration, speed*magnitude)
	}
	if flat := p.Velocity.Flat().Length(); flat > MaxHorizontalSpeed {
		p.Velocity.X *= MaxHorizontalSpeed / flat
		p.Velocity.Z *= MaxHorizontalSpeed / flat
	}
	if !p.Grounded {
		p.Velocity.Y -= Gravity / TickRate
	}
	before := p.Position
	delta := p.Velocity.Flat().Scale(1.0 / TickRate)
	p.Position = r.move(p.Position, delta, PlayerRadius, 0)
	// Clip only the component blocked by a wall, preserving a slide along it.
	if math.Abs(p.Position.X-before.X-delta.X) > 1e-6 {
		p.Velocity.X = 0
	}
	if math.Abs(p.Position.Z-before.Z-delta.Z) > 1e-6 {
		p.Velocity.Z = 0
	}
	oldY := p.Position.Y
	p.Position.Y += p.Velocity.Y / TickRate
	if p.Velocity.Y <= 0 {
		floor := r.World.supportHeight(p.Position, oldY+.01, r.Cleared)
		if p.Position.Y <= floor {
			if !p.Grounded {
				r.event(Landed, p.Position, Vec3{})
			}
			p.Position.Y = floor
			p.Velocity.Y = 0
			p.Grounded = true
		} else {
			p.Grounded = false
		}
	}
}

func (w *World) supportHeight(pos Vec3, maxY float64, cleared []bool) float64 {
	height := 0.0
	check := func(s Solid) {
		if s.Height > maxY {
			return
		}
		b := s.Bounds
		x, z := math.Max(b.MinX, math.Min(pos.X, b.MaxX)), math.Max(b.MinZ, math.Min(pos.Z, b.MaxZ))
		if math.Hypot(pos.X-x, pos.Z-z) < PlayerRadius {
			height = math.Max(height, s.Height)
		}
	}
	for _, s := range w.Solids {
		check(s)
	}
	for i, room := range w.Rooms {
		if !cleared[i] {
			check(room.Gate)
		}
	}
	return height
}

package sandbox

import "core:math"
import "core:time"

PLAYER_RADIUS :: 0.30
PLAYER_HEIGHT :: 1.80
PLAYER_EYE :: 1.62
COLLISION_EPS :: 0.0000001
PLAYER_TICK :: 1.0 / 120.0
WALK_SPEED_SCALE :: 4.5 / 14.0
PLAYER_AIR_DRAG :: 0.98 // Vertical velocity retained per 50 ms reference tick.
PLAYER_TERMINAL_SPEED :: 78.4
// Calibrated for 120 Hz collision substeps: roughly 1.25 blocks at the apex.
PLAYER_JUMP_SPEED :: 9.4

Player :: struct {
	velocity:                          V3,
	accumulator, last_ms:              f64,
	spectator, grounded, jump_pending: bool,
}
Player_Input :: struct {
	move:                          V3,
	speed:                         f64,
	jump, ascend, descend, sprint: bool,
}

// Saves and the renderer keep eye coordinates. The body extends from the feet
// to the head, with a small horizontal radius, independent of camera pitch.
player_bounds :: proc(eye: V3) -> (minimum, maximum: V3) {
	minimum = eye - V3{PLAYER_RADIUS, PLAYER_EYE, PLAYER_RADIUS}
	maximum = minimum + V3{2 * PLAYER_RADIUS, PLAYER_HEIGHT, 2 * PLAYER_RADIUS}
	return
}

player_overlaps_block :: proc(eye: V3, block: I3) -> bool {
	lo, hi := player_bounds(eye)
	for axis in 0 ..< 3 {
		if hi[axis] <= f64(block[axis]) + COLLISION_EPS ||
		   lo[axis] >= f64(block[axis]) + 1 - COLLISION_EPS {return false}
	}
	return true
}

player_collides :: proc(w: ^World, eye: V3) -> bool {
	lo, hi := player_bounds(eye)
	for z := i32(math.floor(lo.z + COLLISION_EPS));
	    z <= i32(math.floor(hi.z - COLLISION_EPS));
	    z += 1 {
		for y := i32(math.floor(lo.y + COLLISION_EPS));
		    y <= i32(math.floor(hi.y - COLLISION_EPS));
		    y += 1 {
			for x := i32(math.floor(lo.x + COLLISION_EPS));
			    x <= i32(math.floor(hi.x - COLLISION_EPS));
			    x += 1 {
				if is_solid(world_get(w, {x, y, z})) {return true}
			}
		}
	}
	return false
}

// Scan upward only when necessary. This handles old noclip saves, leaving
// spectator underground, and a fluid turning into a solid inside the body.
player_unstuck :: proc(w: ^World, eye: ^V3) {
	if eye.y >= PLAYER_EYE + 1 && !player_collides(w, eye^) {return}
	feet := max(1, math.floor(eye.y - PLAYER_EYE))
	for i in 0 ..= HEIGHT + 2 {
		candidate := eye^
		candidate.y = feet + f64(i) + PLAYER_EYE
		if !player_collides(w, candidate) {eye^ = candidate; return}
	}
}

player_set_spectator :: proc(p: ^Player, w: ^World, eye: ^V3, enabled: bool) {
	p.spectator = enabled
	p.velocity = {}; p.accumulator = 0; p.jump_pending = false; p.grounded = false
	if !enabled {
		player_unstuck(w, eye)
		probe := eye^; probe.y -= 0.001
		p.grounded = player_collides(w, probe)
	}
}

// Sweep the full body across every crossed voxel plane. Collision cannot tunnel
// through a one-block wall at high speed or during a long frame. Other axes stay
// unchanged, letting the remaining motion slide along walls and corners.
player_sweep_axis :: proc(w: ^World, eye: ^V3, axis: int, delta: f64) -> bool {
	if abs(delta) < 1e-12 {return false}
	lo, hi := player_bounds(eye^)
	sweep_lo, sweep_hi := lo, hi
	if delta < 0 {sweep_lo[axis] += delta} else {sweep_hi[axis] += delta}
	allowed := delta
	for z := i32(math.floor(sweep_lo.z + COLLISION_EPS));
	    z <= i32(math.floor(sweep_hi.z - COLLISION_EPS));
	    z += 1 {
		for y := i32(math.floor(sweep_lo.y + COLLISION_EPS));
		    y <= i32(math.floor(sweep_hi.y - COLLISION_EPS));
		    y += 1 {
			for x := i32(math.floor(sweep_lo.x + COLLISION_EPS));
			    x <= i32(math.floor(sweep_hi.x - COLLISION_EPS));
			    x += 1 {
				q := I3{x, y, z}
				if !is_solid(world_get(w, q)) {continue}
				gap := delta > 0 ? f64(q[axis]) - hi[axis] : f64(q[axis]) + 1 - lo[axis]
				if delta > 0 && gap >= -COLLISION_EPS {allowed = min(allowed, max(0, gap))}
				if delta < 0 && gap <= COLLISION_EPS {allowed = max(allowed, min(0, gap))}
			}
		}
	}
	eye[axis] += allowed
	return abs(allowed - delta) > COLLISION_EPS
}

player_fluid :: proc(w: ^World, eye: V3) -> Block {
	waist := eye - V3{0, PLAYER_EYE - 0.85, 0}
	q := I3{i32(math.floor(waist.x)), i32(math.floor(waist.y)), i32(math.floor(waist.z))}
	b := world_get(w, q)
	kind := fluid_kind(b)
	if kind != .Air &&
	   waist.y - math.floor(waist.y) <
		   f64(fluid_height(b, world_get(w, q + I3{0, 1, 0}))) / 8 {return kind}
	return .Air
}

Water_Contact :: struct {
	immersion: f64,
	current:   V3,
}

// Volume-weight the actual water surfaces touching the body. This catches
// ankle-deep streams and bank edges without pushing a player standing above it.
player_water_contact :: proc(w: ^World, eye: V3) -> Water_Contact {
	lo, hi := player_bounds(eye)
	result: Water_Contact
	for z := i32(math.floor(lo.z + COLLISION_EPS)); z <= i32(math.floor(hi.z - COLLISION_EPS)); z += 1 {
		for y := i32(math.floor(lo.y + COLLISION_EPS)); y <= i32(math.floor(hi.y - COLLISION_EPS)); y += 1 {
			for x := i32(math.floor(lo.x + COLLISION_EPS)); x <= i32(math.floor(hi.x - COLLISION_EPS)); x += 1 {
				q := I3{x, y, z}
				b := world_get(w, q)
				if fluid_kind(b) != .Water {continue}
				surface := f64(y) + f64(fluid_height(b, world_get(w, q + I3{0, 1, 0}))) / 8
				depth := max(0, min(hi.y, surface) - max(lo.y, f64(y)))
				if depth == 0 {continue}
				volume := depth * (min(hi.x, f64(x + 1)) - max(lo.x, f64(x))) * (min(hi.z, f64(z + 1)) - max(lo.z, f64(z)))
				result.immersion += volume
				result.current += water_current(w, q) * volume
			}
		}
	}
	if result.immersion > 0 {
		result.current /= result.immersion
		result.immersion = clamp(result.immersion / (4 * PLAYER_RADIUS * PLAYER_RADIUS * PLAYER_HEIGHT), 0, 1)
	}
	return result
}

player_update :: proc(p: ^Player, w: ^World, eye: ^V3, input: Player_Input, dt: f64) {
	start := time.tick_now()
	defer p.last_ms = time.duration_milliseconds(time.tick_since(start))
	if p.spectator {
		move := input.move
		if input.ascend {move.y += 1}
		if input.descend {move.y -= 1}
		p.velocity = normalize(move) * input.speed * (input.sprint ? 4 : 1)
		eye^ += p.velocity * clamp(dt, 0, 0.1)
		return
	}
	if player_collides(w, eye^) || eye.y < PLAYER_EYE + 1 {
		player_unstuck(w, eye); p.velocity = {}; p.grounded = false
	}
	p.jump_pending = p.jump_pending || input.jump
	p.accumulator = min(p.accumulator + max(0, dt), 0.1)
	for _ in 0 ..< 12 {
		if p.accumulator + 1e-12 < PLAYER_TICK {break}
		p.accumulator = max(0, p.accumulator - PLAYER_TICK)
		wish := input.move; wish.y = 0; wish = normalize(wish)
		fluid := player_fluid(w, eye^)
		water := player_water_contact(w, eye^)
		strength := clamp(water.immersion * 4, 0, 1)
		speed := input.speed * WALK_SPEED_SCALE * (input.sprint ? 1.6 : 1)
		if fluid == .Lava {speed *= 0.30} else {speed *= 1 - 0.55 * clamp(water.immersion * 2, 0, 1)}
		target := wish * speed + water.current * strength
		if water.immersion > 0 {
			drag := 1 - math.exp(-8 * strength * PLAYER_TICK)
			p.velocity.x += (target.x - p.velocity.x) * drag
			p.velocity.z += (target.z - p.velocity.z) * drag
		} else {p.velocity.x = target.x; p.velocity.z = target.z}
		if fluid != .Air {
			current_y := water.current.y * strength
			p.velocity.y = max(-2 + current_y, (-1 + current_y) + (p.velocity.y - (-1 + current_y)) * math.exp(-4 * PLAYER_TICK))
			if input.ascend {p.velocity.y = 3.5 + current_y}
			if input.descend {p.velocity.y = -3.5 + current_y}
		} else {
			if p.jump_pending && p.grounded {p.velocity.y = PLAYER_JUMP_SPEED}
			retained := math.pow(PLAYER_AIR_DRAG, PLAYER_TICK * 20)
			p.velocity.y = max(-PLAYER_TERMINAL_SPEED, (p.velocity.y + PLAYER_TERMINAL_SPEED) * retained - PLAYER_TERMINAL_SPEED)
		}
		p.jump_pending = false
		if player_sweep_axis(w, eye, 0, p.velocity.x * PLAYER_TICK) {p.velocity.x = 0}
		if player_sweep_axis(w, eye, 2, p.velocity.z * PLAYER_TICK) {p.velocity.z = 0}
		p.grounded = false
		if player_sweep_axis(w, eye, 1, p.velocity.y * PLAYER_TICK) {
			p.grounded = p.velocity.y < 0; p.velocity.y = 0
		}
	}
}

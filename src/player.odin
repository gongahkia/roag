package sandbox

import "core:math"
import "core:time"

PLAYER_RADIUS :: 0.30
PLAYER_HEIGHT :: 1.80
PLAYER_EYE :: 1.62
COLLISION_EPS :: 0.0000001
PLAYER_TICK :: 1.0 / 120.0
WALK_SPEED_SCALE :: 4.5 / 14.0

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
		speed := input.speed * WALK_SPEED_SCALE * (input.sprint ? 1.6 : 1)
		if fluid != .Air {speed *= fluid == .Water ? 0.45 : 0.30}
		p.velocity.x = wish.x * speed; p.velocity.z = wish.z * speed
		if fluid != .Air {
			p.velocity.y = max(-2, p.velocity.y * math.exp(-4 * PLAYER_TICK) - 4 * PLAYER_TICK)
			if input.ascend {p.velocity.y = 3.5}
			if input.descend {p.velocity.y = -3.5}
		} else {
			if p.jump_pending && p.grounded {p.velocity.y = 8}
			p.velocity.y = max(-50, p.velocity.y - 24 * PLAYER_TICK)
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

extends Node2D

# World-space pixel particles. Sampling happens once per rendered frame, never
# during prediction replay; no particles, randomness or RPCs affect gameplay.
const MAX_PARTICLES := 80
var enabled := DisplayServer.get_name() != "headless"
var tint := Color("77edc4")
var particles: Array[Dictionary] = []
var rng := RandomNumberGenerator.new()
var initialized := false
var previous_position := Vector2.ZERO
var previous_velocity := Vector2.ZERO
var previous_floor := false
var trail_clock := 0.0
var burst_clock := 0.0

func _ready() -> void:
	top_level = true
	global_transform = Transform2D.IDENTITY
	physics_interpolation_mode = Node.PHYSICS_INTERPOLATION_MODE_OFF
	show_behind_parent = true
	rng.randomize()
	add_to_group("movement_effects")
	enabled = enabled and bool(get_tree().get_meta("movement_particles", true))

func reset_tracking(clear := false) -> void:
	initialized = false
	trail_clock = 0.0
	burst_clock = 0.0
	if clear: particles.clear(); queue_redraw()

func sample(delta: float, at: Vector2, motion: Vector2, grounded: bool, wall_slide := false, wall_normal := 0.0, active := true) -> void:
	if not enabled:
		reset_tracking(true)
		return
	_advance(delta)
	if not active:
		reset_tracking()
		return
	if not initialized or at.distance_to(previous_position) > 80:
		initialized = true
		_remember(at, motion, grounded)
		return
	trail_clock = maxf(0, trail_clock - delta)
	burst_clock = maxf(0, burst_clock - delta)
	var speed := absf(motion.x)
	var direction := signf(motion.x)
	var feet := at + Vector2(0, 10)
	if burst_clock <= 0:
		if grounded and not previous_floor and previous_velocity.y > 80:
			_burst(feet, 12, 110, tint.lightened(0.3))
			burst_clock = 0.12
		elif motion.y < -140 and previous_velocity.y >= -60:
			_burst(feet, 8, 75, tint)
			burst_clock = 0.12
		elif grounded and previous_floor and absf(previous_velocity.x) > 160 and speed < absf(previous_velocity.x) - 30:
			_burst(feet, 5, 70, tint.lightened(0.45))
			burst_clock = 0.10
	if trail_clock <= 0:
		if wall_slide and motion.y > 0:
			for i in 2:
				_emit(at + Vector2(-wall_normal * 7, 4), Vector2(wall_normal * rng.randf_range(12, 45), -rng.randf_range(15, 50)), tint, 1.5, 0.28)
			trail_clock = 0.07
		elif grounded and speed > 65:
			var sprinting := speed > 330
			for i in (3 if sprinting else 2):
				var color := tint if sprinting else tint.lerp(Color("c5c9bf"), 0.5)
				_emit(feet + Vector2(-direction * 5, -1), Vector2(-direction * rng.randf_range(25, 70), -rng.randf_range(12, 42)), color, 2.5 if sprinting else 2.0, 0.32)
			trail_clock = 0.045 if sprinting else 0.085
	_remember(at, motion, grounded)

func _remember(at: Vector2, motion: Vector2, grounded: bool) -> void:
	previous_position = at
	previous_velocity = motion
	previous_floor = grounded

func _burst(at: Vector2, count: int, speed: float, color: Color) -> void:
	for i in count:
		_emit(at + Vector2(rng.randf_range(-4, 4), -1), Vector2(rng.randf_range(-speed, speed), -rng.randf_range(15, speed * 0.7)), color, rng.randf_range(1.5, 3), rng.randf_range(0.22, 0.42))

func _emit(at: Vector2, motion: Vector2, color: Color, width: float, lifetime: float) -> void:
	if particles.size() >= MAX_PARTICLES: return
	particles.append({"at": at, "motion": motion, "color": color, "size": width, "life": lifetime, "duration": lifetime})
	queue_redraw()

func _advance(delta: float) -> void:
	for i in range(particles.size() - 1, -1, -1):
		var particle: Dictionary = particles[i]
		particle.life -= delta
		if particle.life <= 0:
			particles.remove_at(i)
			continue
		particle.motion.y += 110 * delta
		particle.at += Vector2(particle.motion) * delta
	queue_redraw()

func _draw() -> void:
	for particle: Dictionary in particles:
		var fraction: float = particle.life / particle.duration
		var color: Color = particle.color
		color.a *= minf(0.8, fraction * 1.6)
		var width := maxf(1, roundf(float(particle.size) * fraction))
		draw_rect(Rect2(Vector2(particle.at).floor(), Vector2.ONE * width), color)

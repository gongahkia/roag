class_name Runner
extends CharacterBody2D

signal jumped(wall_jump: bool)

const Style = preload("res://scripts/avatar_style.gd")
const Particles = preload("res://scripts/movement_particles.gd")

@export var movement: MovementSettings = preload("res://scenes/movement.tres")
var controls_enabled := true
var test_input: Dictionary = {}
var facing := 1
var sliding := false
var frame := 0
var coyote := 0.0
var jump_buffer := 0.0
var wall_grace := 0.0
var wall_normal := 0.0
var wall_lock := 0.0
var age := 0.0
var sprite: Sprite2D
var identity_label: Label
var effects: Node2D
var effects_active := true
var manual_simulation := false
var accept_local_input := true
var environment: Dictionary = {}
var contact_override: Dictionary = {}
var external_launch := false

func _ready() -> void:
	name = "Player"
	safe_margin = 0.02
	floor_snap_length = 2.0
	var collision := CollisionShape2D.new()
	var shape := RectangleShape2D.new()
	shape.size = Vector2(12, 20)
	collision.shape = shape
	add_child(collision)
	sprite = Sprite2D.new()
	sprite.texture = load("res://assets/sprites.png")
	sprite.hframes = int(sprite.texture.get_width()) / 32
	sprite.vframes = int(sprite.texture.get_height()) / 32
	sprite.position = Vector2(0, 5)
	sprite.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	add_child(sprite)
	effects = Particles.new()
	add_child(effects)

func set_identity(display_name: String, tint: Color) -> void:
	Style.apply(sprite, tint)
	effects.tint = tint
	if identity_label == null: identity_label = Style.nameplate(self)
	identity_label.text = display_name
	identity_label.add_theme_color_override("font_color", tint)

func _process(delta: float) -> void:
	if DisplayServer.get_name() != "headless":
		effects.sample(delta, global_position, velocity, is_on_floor(), sliding, wall_normal, controls_enabled and effects_active)

func _unhandled_input(event: InputEvent) -> void:
	if accept_local_input and controls_enabled and event.is_action_pressed("jump") and not event.is_echo():
		queue_jump()

func queue_jump() -> void:
	jump_buffer = movement.jump_buffer_time

func respawn(at: Vector2) -> void:
	position = at
	velocity = Vector2.ZERO
	coyote = 0.0
	jump_buffer = 0.0
	wall_grace = 0.0
	wall_lock = 0.0
	wall_normal = 0.0
	sliding = false
	external_launch = false
	reset_physics_interpolation()
	if effects: effects.reset_tracking(true)

func _physics_process(delta: float) -> void:
	if not manual_simulation:
		step_movement(delta)

func step_movement(delta: float) -> void:
	if not controls_enabled:
		return
	age += delta
	var axis := 0.0
	var jump_held := false
	var sprinting := false
	var climb := 0.0
	if test_input.is_empty():
		axis = Input.get_axis("left", "right")
		jump_held = Input.is_action_pressed("jump")
		sprinting = Input.is_action_pressed("sprint")
	else:
		axis = float(test_input.get("axis", 0.0))
		jump_held = bool(test_input.get("jump", false))
		sprinting = bool(test_input.get("sprint", true))
		climb = float(test_input.get("climb", 0.0))
		if test_input.get("press", false):
			queue_jump()
			test_input["press"] = false
	var floor_before := is_on_floor()
	var wall_before := is_on_wall_only()
	var normal_before := get_wall_normal().x
	if not contact_override.is_empty():
		floor_before = contact_override["floor"]
		wall_before = contact_override["wall"]
		normal_before = contact_override["normal"]
		contact_override.clear()
	coyote = movement.coyote_time if floor_before else maxf(0.0, coyote - delta)
	jump_buffer = maxf(0.0, jump_buffer - delta)
	wall_lock = maxf(0.0, wall_lock - delta)
	wall_grace = maxf(0.0, wall_grace - delta)
	if wall_before:
		wall_normal = normal_before
		wall_grace = movement.wall_coyote_time
	var target_speed := movement.sprint_speed if sprinting else movement.run_speed
	target_speed *= float(environment.get("speed", 1.0))
	if absf(axis) > 0.1:
		facing = 1 if axis > 0 else -1
		var acceleration := movement.ground_acceleration if floor_before else movement.air_acceleration
		if floor_before: acceleration *= float(environment.get("traction", 1.0))
		if floor_before and signf(axis) != signf(velocity.x):
			acceleration = movement.turn_acceleration
			acceleration *= float(environment.get("traction", 1.0))
		if wall_lock <= 0.0:
			velocity.x = move_toward(velocity.x, axis * target_speed, acceleration * delta)
	elif wall_lock <= 0.0:
		var friction := movement.ground_friction * float(environment.get("friction", 1.0)) if floor_before else movement.air_friction
		velocity.x = move_toward(velocity.x, 0.0, friction * delta)
	if jump_buffer > 0.0:
		if coyote > 0.0 or environment.get("water", false) or environment.get("ladder", false):
			velocity.y = -movement.jump_speed * (0.6 if environment.get("water", false) else 1.0)
			coyote = 0.0
			jump_buffer = 0.0
			jumped.emit(false)
		elif wall_grace > 0.0:
			velocity = Vector2(wall_normal * movement.wall_jump_x, -movement.wall_jump_y)
			wall_lock = movement.wall_jump_lock
			wall_grace = 0.0
			jump_buffer = 0.0
			jumped.emit(true)
	if not jump_held and not external_launch and velocity.y < -movement.jump_cut_speed:
		velocity.y = -movement.jump_cut_speed
	var gravity_scale := movement.fall_gravity_scale if velocity.y > 0.0 else 1.0
	if jump_held and absf(velocity.y) < 65.0:
		gravity_scale = movement.apex_gravity_scale
	gravity_scale *= float(environment.get("gravity", 1.0))
	velocity += Vector2(environment.get("force", Vector2.ZERO)) * delta
	velocity.y = minf(260.0 if environment.get("water", false) else movement.max_fall_speed, velocity.y + movement.gravity * gravity_scale * delta)
	if environment.get("ladder", false) and absf(climb) > 0.1 and jump_buffer <= 0 and not jump_held:
		velocity.y = climb * 170.0
	sliding = wall_before and velocity.y > 0.0 and axis * wall_normal < -0.1
	if sliding:
		velocity.y = minf(velocity.y, movement.wall_slide_speed)
	var belt := float(environment.get("belt", 0.0)) if floor_before else 0.0
	velocity.x += belt
	move_and_slide()
	if belt != 0 and not is_on_wall(): velocity.x -= belt
	if velocity.y >= 0: external_launch = false
	frame = (0 if facing > 0 else 3)
	if not is_on_floor() or absf(velocity.x) > 10:
		frame += 1 + (int(age * (18.0 if sprinting else 12.0)) % 2)
	sprite.frame = frame
	# Subtle stretch/squash preserves the original bitmap while showing motion.
	sprite.scale = Vector2(0.92, 1.08) if not is_on_floor() else Vector2.ONE
	queue_redraw()

func capture_state() -> PackedFloat64Array:
	return PackedFloat64Array([position.x, position.y, velocity.x, velocity.y, coyote, jump_buffer,
		wall_grace, wall_normal, wall_lock, age, facing, frame, float(sliding), float(is_on_floor()), float(is_on_wall_only()), float(external_launch)])

func restore_state(state: PackedFloat64Array) -> void:
	if state.size() != 16: return
	position = Vector2(state[0], state[1])
	velocity = Vector2(state[2], state[3])
	coyote = state[4]; jump_buffer = state[5]; wall_grace = state[6]
	wall_normal = state[7]; wall_lock = state[8]; age = state[9]
	facing = int(state[10]); frame = int(state[11]); sliding = bool(state[12])
	contact_override = {"floor": bool(state[13]), "wall": bool(state[14]), "normal": wall_normal}
	external_launch = bool(state[15])
	sprite.frame = frame

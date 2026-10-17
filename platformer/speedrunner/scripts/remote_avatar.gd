extends Node2D

const Style = preload("res://scripts/avatar_style.gd")
const Particles = preload("res://scripts/movement_particles.gd")
var target := Vector2.ZERO
var sprite: Sprite2D
var label: Label
var effects: Node2D
var motion := Vector2.ZERO
var grounded := false
var sliding := false
var wall_normal := 0.0
var effects_active := false

func configure(display_name: String, tint: Color) -> void:
	physics_interpolation_mode = Node.PHYSICS_INTERPOLATION_MODE_OFF
	sprite = Sprite2D.new()
	sprite.texture = load("res://assets/sprites.png")
	sprite.hframes = int(sprite.texture.get_width()) / 32
	sprite.vframes = int(sprite.texture.get_height()) / 32
	sprite.position.y = 5
	sprite.texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	add_child(sprite)
	label = Style.nameplate(self)
	effects = Particles.new()
	add_child(effects)
	set_identity(display_name, tint)

func set_identity(display_name: String, tint: Color) -> void:
	Style.apply(sprite, tint)
	label.text = display_name
	label.add_theme_color_override("font_color", tint)
	effects.tint = tint

func update_pose(state: PackedFloat64Array, active := true) -> void:
	target = Vector2(state[0], state[1])
	if position.distance_to(target) > 160:
		position = target
		effects.reset_tracking(true)
	motion = Vector2(state[2], state[3])
	grounded = bool(state[13])
	sliding = bool(state[12])
	wall_normal = state[7]
	effects_active = active
	sprite.frame = int(state[11])
	sprite.scale = Vector2.ONE if grounded else Vector2(0.92, 1.08)

func _process(delta: float) -> void:
	position = position.lerp(target, 1.0 - exp(-delta * 22.0))
	if effects and DisplayServer.get_name() != "headless":
		effects.sample(delta, global_position, motion, grounded, sliding, wall_normal, effects_active)

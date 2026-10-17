class_name MovementSettings
extends Resource

## Pixel units per second. Tune these in the Inspector on Player/Movement.
@export var run_speed: float = 260.0
@export var sprint_speed: float = 420.0
@export var ground_acceleration: float = 6500.0
@export var air_acceleration: float = 2500.0
@export var turn_acceleration: float = 10000.0
@export var ground_friction: float = 9500.0
@export var air_friction: float = 1300.0
@export var gravity: float = 2000.0
@export var fall_gravity_scale: float = 1.3
@export var apex_gravity_scale: float = 1.0
@export var jump_speed: float = 600.0
@export var jump_cut_speed: float = 240.0
@export var max_fall_speed: float = 800.0
@export var coyote_time: float = 0.075
@export var jump_buffer_time: float = 0.10
@export var wall_slide_speed: float = 110.0
@export var wall_jump_x: float = 330.0
@export var wall_jump_y: float = 550.0
@export var wall_jump_lock: float = 0.075
@export var wall_coyote_time: float = 0.08

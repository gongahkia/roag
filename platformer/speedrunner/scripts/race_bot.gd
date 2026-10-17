extends RefCounted

# Receding-horizon control: evaluate actual CharacterBody2D movement against
# the actual course, then execute only a short prefix and observe again.
# The hidden probe has no collision layer and never triggers race interactions.
const Player = preload("res://scripts/player.gd")
const Catalog = preload("res://scripts/tile_catalog.gd")
const TIERS := ["Easy", "Normal", "Hard", "Expert"]
const PROFILES := [
	{"reaction": 22, "horizon": 40, "holds": [2, 10, 60], "sprint": false, "start": 66, "landing": 10, "risk": 2.2, "recovery": 180},
	{"reaction": 16, "horizon": 48, "holds": [2, 14, 60], "sprint": true, "start": 32, "landing": 5, "risk": 1.5, "recovery": 140},
	{"reaction": 12, "horizon": 54, "holds": [2, 8, 14, 22, 60], "sprint": true, "start": 12, "landing": 0, "risk": 0.8, "recovery": 110},
	{"reaction": 8, "horizon": 60, "holds": [2, 6, 14, 22, 60], "sprint": true, "start": 2, "landing": 0, "risk": 0.3, "recovery": 90},
]
var tier := 1
var profile: Dictionary
var world: TileWorld
var probe: Runner
var hazard_test: Callable
var request_respawn := false
var state := "Lining up"
var decisions := 0
var jumps := 0
var recoveries := 0
var ticks := 0
var next_think := 0
var wait_until := 0
var held_until := 0
var planned_jump := -1
var planned_hold := 0
var last_jump := -100
var was_grounded := true
var anchor := Vector2.ZERO
var stuck_ticks := 0
var last_deaths := 0
var goal := Vector2.ZERO

func _init(course_world: TileWorld, difficulty: int, hazards: Callable = Callable()) -> void:
	world = course_world
	tier = clampi(difficulty, 0, 3)
	profile = PROFILES[tier]
	hazard_test = hazards
	goal = Vector2(world.document.find(Catalog.Tile.FINISH) * Catalog.SIZE) + Vector2(12, 14)
	probe = Player.new()
	probe.manual_simulation = true
	probe.accept_local_input = false
	probe.collision_layer = 0
	probe.collision_mask = 1
	world.get_parent().add_child(probe)
	probe.name = "BotTrajectoryProbe"
	probe.hide()
	probe.set_process(false)
	probe.effects.enabled = false
	reset()

func dispose() -> void:
	if is_instance_valid(probe):
		probe.get_parent().remove_child(probe)
		probe.queue_free()

func reset(deaths := 0) -> void:
	ticks = 0; next_think = 0; held_until = 0; planned_jump = -1
	wait_until = int(profile.start) if deaths == 0 else maxi(4, int(profile.start) / 2)
	last_jump = -100; was_grounded = true; stuck_ticks = 0
	anchor = Vector2.ZERO; request_respawn = false; last_deaths = deaths
	state = "Recovering" if deaths else "Lining up"

func next_command(actor: Runner) -> Dictionary:
	ticks += 1
	var command := {"axis": 1.0, "jump": ticks < held_until, "press": false, "sprint": bool(profile.sprint), "climb": 0}
	if actor.position.distance_to(anchor) > 12:
		anchor = actor.position; stuck_ticks = 0
	else: stuck_ticks += 1
	if stuck_ticks > int(profile.recovery):
		request_respawn = true; recoveries += 1
	if ticks <= wait_until:
		command.axis = 0.0; command.sprint = false
		return command
	if actor.position.x > goal.x - 85:
		var distance := goal.x - actor.position.x
		command.axis = clampf(distance * 0.025 - actor.velocity.x * 0.008, -1, 1)
		command.jump = false
		state = "Finish approach"
		return command
	var grounded := actor.is_on_floor()
	if grounded and not was_grounded:
		wait_until = ticks + int(profile.landing)
		next_think = ticks
		planned_jump = -1
		if int(profile.landing) > 0:
			command.axis = 0.0
			was_grounded = true
			return command
	was_grounded = grounded
	if grounded and ticks >= next_think and ticks - last_jump > 10:
		_choose(actor)
	if planned_jump >= 0 and ticks >= planned_jump and (grounded or actor.coyote > 0):
		command.press = true
		held_until = ticks + planned_hold
		command.jump = true
		planned_jump = -1
		last_jump = ticks; jumps += 1
		state = "Jumping"
	if actor.is_on_wall_only() and ticks - last_jump > 12 and actor.velocity.y > 20:
		command.axis = actor.get_wall_normal().x
		command.press = true; command.jump = true
		held_until = ticks + 48; last_jump = ticks; jumps += 1
		state = "Wall kick"
	elif not grounded: state = "Air control"
	return command

func _choose(actor: Runner) -> void:
	decisions += 1
	next_think = ticks + int(profile.reaction)
	var snapshot := actor.capture_state()
	var best := _evaluate(snapshot, -1, 0)
	var delay := -1
	var hold := 0
	# A clear, grounded run at full chosen pace cannot be improved by a jump.
	# Avoid spending a full trajectory search on unbroken safe ground.
	var cruise := actor.movement.sprint_speed if profile.sprint else actor.movement.run_speed
	if best >= 0 and probe.is_on_floor() and probe.velocity.x >= cruise * 0.9:
		planned_jump = -1
		state = "Sprinting" if profile.sprint else "Running carefully"
		return
	for duration: int in profile.holds:
		for launch in [0, 6]:
			var score := _evaluate(snapshot, launch, duration)
			if score > best:
				best = score; delay = launch; hold = duration
	if delay >= 0:
		planned_jump = ticks + delay
		planned_hold = hold
		state = "Reading jump"
	else:
		planned_jump = -1
		state = "Sprinting" if profile.sprint else "Running carefully"

func _evaluate(snapshot: PackedFloat64Array, delay: int, hold: int) -> float:
	probe.restore_state(snapshot)
	var origin := probe.position
	var horizon := int(profile.horizon) * 2
	var clearance := 0.0
	var airborne := false
	var preview_last_jump := -100
	var preview_hold_until := 0
	for step in horizon:
		var tick := step
		probe.test_input = {"axis": 1.0, "jump": delay >= 0 and tick >= delay and tick < delay + hold,
			"press": delay >= 0 and tick == delay, "sprint": bool(profile.sprint), "climb": 0}
		if bool(probe.test_input.press): preview_last_jump = tick
		if probe.is_on_wall_only() and tick - preview_last_jump > 12 and probe.velocity.y > 20:
			probe.test_input.axis = probe.get_wall_normal().x
			probe.test_input.press = true
			preview_last_jump = tick; preview_hold_until = tick + 48
		if tick < preview_hold_until: probe.test_input.jump = true
		probe.environment = world.environment_at(probe)
		probe.step_movement(1.0 / 120)
		world.apply_spring(probe)
		if absf(probe.position.x - goal.x) < 15 and absf(probe.position.y - goal.y) < 20: return 10000.0 + (horizon - step) * 10
		if _dead(probe, (step + 1) / 120.0): return -10000.0 + step * 10 + (probe.position.x - origin.x)
		if not probe.is_on_floor(): airborne = true
		if probe.is_on_floor():
			clearance += 1
			if airborne and probe.position.x - origin.x > 24: break
	var progress := probe.position.x - origin.x
	var fall_risk := maxf(0, probe.position.y - origin.y - 48) * float(profile.risk)
	# Surviving a rollout is worth more than immediate horizontal progress.
	# Penalize unnecessary jumps and unstable end states, especially for Easy.
	return progress - fall_risk + clearance * 0.12 - (4.0 if delay >= 0 else 0.0) - (float(profile.risk) * 12 if not probe.is_on_floor() else 0.0)

func _dead(actor: Runner, future: float) -> bool:
	if actor.position.y > world.document.height * Catalog.SIZE + 48: return true
	for hit: Array in world.touches(actor):
		if hit[1] in [Catalog.Tile.SPIKES, Catalog.Tile.LAVA, Catalog.Tile.SAW]: return true
	return hazard_test.is_valid() and bool(hazard_test.call(actor, future))

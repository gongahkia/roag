extends SceneTree

const PlayerScript = preload("res://scripts/player.gd")
const SessionScript = preload("res://scripts/race_session.gd")
var failures := 0
var checks := 0
var stage: Node2D
var runner: Runner

func _initialize() -> void:
	call_deferred("run")

func check(condition: bool, message: String) -> void:
	checks += 1
	if not condition:
		failures += 1
		push_error(message)

func advance(count: int) -> void:
	for i in count:
		await physics_frame
		await process_frame

func solid(rect: Rect2) -> void:
	var body := StaticBody2D.new()
	body.position = rect.get_center()
	var collision := CollisionShape2D.new()
	var shape := RectangleShape2D.new()
	shape.size = rect.size
	collision.shape = shape
	body.add_child(collision)
	stage.add_child(body)

func fixture() -> void:
	if is_instance_valid(stage):
		root.remove_child(stage)
		stage.queue_free()
	stage = Node2D.new()
	root.add_child(stage)
	solid(Rect2(-100, 200, 2000, 32))
	runner = PlayerScript.new()
	stage.add_child(runner)
	runner.respawn(Vector2(40, 190))
	runner.test_input = {"axis": 0.0, "jump": false, "sprint": true}
	await advance(4)

func jump_apex(hold: int) -> float:
	await fixture()
	runner.test_input["jump"] = true
	runner.test_input["press"] = true
	var top := runner.position.y
	for i in 110:
		if i == hold:
			runner.test_input["jump"] = false
		await advance(1)
		top = minf(top, runner.position.y)
	return top

func run() -> void:
	await fixture()
	check(runner.is_on_floor(), "controller settles on real static geometry")
	runner.test_input["axis"] = 1.0
	await advance(20)
	check(absf(runner.velocity.x - runner.movement.sprint_speed) < 1.0, "sprint accelerates to its configured speed")
	var before_stop := runner.position.x
	runner.test_input["axis"] = 0.0
	await advance(8)
	check(runner.velocity.x == 0.0 and runner.position.x - before_stop < 12.0, "Mario-style ground braking stops a sprint within twelve pixels")
	runner.test_input["axis"] = 1.0
	await advance(12)
	runner.test_input["axis"] = -1.0
	await advance(12)
	check(runner.velocity.x < 0.0, "ground braking allows a prompt turn")
	var short_top := await jump_apex(3)
	var full_top := await jump_apex(90)
	check(short_top - full_top > 45.0, "holding jump produces substantially more height than tapping")

	# Walk past a ledge, then jump after losing floor contact.
	await fixture()
	for child in stage.get_children():
		if child is StaticBody2D:
			stage.remove_child(child)
			child.queue_free()
	solid(Rect2(0, 200, 100, 32))
	runner.respawn(Vector2(80, 190))
	runner.test_input["axis"] = 1.0
	await advance(4)
	for i in 40:
		await advance(1)
		if not runner.is_on_floor():
			break
	await advance(3)
	runner.test_input["jump"] = true
	runner.test_input["press"] = true
	await advance(1)
	check(runner.velocity.y < -400.0, "coyote time accepts a jump after leaving a ledge")

	# Buffer a press just before landing; never press again on the floor.
	await fixture()
	runner.respawn(Vector2(40, 160))
	runner.velocity.y = 250.0
	runner.test_input = {"axis": 0.0, "jump": true, "press": true}
	var buffered_jump := false
	for i in 20:
		await advance(1)
		if runner.velocity.y < -400.0:
			buffered_jump = true
	check(buffered_jump, "a buffered jump fires on landing")

	await fixture()
	solid(Rect2(100, -200, 20, 400))
	runner.respawn(Vector2(70, 30))
	runner.velocity.y = 400
	runner.test_input = {"axis": 1.0, "jump": false}
	await advance(24)
	check(runner.is_on_wall() and runner.velocity.y <= runner.movement.wall_slide_speed + 1, "holding into a wall limits falling speed")
	runner.test_input["jump"] = true
	runner.test_input["press"] = true
	await advance(2)
	check(runner.velocity.x < -300 and runner.velocity.y < -400, "wall jump kicks up and away while input points into the wall")
	check(runner.position.x + 6 <= 100.1, "collision prevents penetrating the wall at speed")
	runner.respawn(Vector2(40, 190))
	check(runner.jump_buffer == 0 and runner.wall_lock == 0 and runner.velocity == Vector2.ZERO, "respawn clears buffered actions and momentum")

	var session := SessionScript.new()
	session.record_path = "user://test-records-v2.json"
	session.begin("test")
	session.started = true
	for i in 120:
		session.tick(Vector2(i, 190), i % 6)
	session.finish()
	check(session.new_best and not session.save_failed, "a valid finish saves a PB")
	check(RaceSession.format_time(session.ticks) == "00:01.000", "120 physics ticks equal exactly one second")
	var loaded := SessionScript.new()
	loaded.record_path = session.record_path
	loaded.load_records()
	loaded.begin("test")
	check(loaded.best_ticks() == 120 and loaded.ghost_frame(119) == [11900, 19000, 5], "PB time and ghost survive a save/load round trip")
	loaded.practice = true
	loaded.started = true
	loaded.tick(Vector2.ZERO, 0)
	loaded.finish()
	check(loaded.best_ticks() == 120 and not loaded.new_best, "practice cannot overwrite a record")
	loaded.begin("test")
	loaded.started = true
	loaded.valid = false
	loaded.tick(Vector2.ZERO, 0)
	loaded.finish()
	check(loaded.best_ticks() == 120 and not loaded.new_best, "paused or invalidated attempts cannot overwrite a record")
	loaded.retry()
	check(loaded.ticks == 0 and loaded.frames.is_empty() and not loaded.started, "retry clears time and the recording")
	FileAccess.open(loaded.record_path, FileAccess.WRITE).store_string('{"ruleset":%d,"records":{"bad":{"ticks":"oops"},"ghost":{"ticks":1,"frames":[["oops",0,0]]}}}' % RaceSession.RULESET)
	loaded.load_records()
	check(not loaded.records.has("bad") and loaded.records["ghost"]["frames"].is_empty(), "malformed records and ghost samples are ignored safely")
	DirAccess.remove_absolute(loaded.record_path)
	root.remove_child(stage)
	stage.queue_free()
	print("REDLINE: %d checks, %d failures" % [checks, failures])
	quit(1 if failures else 0)

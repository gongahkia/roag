extends SceneTree

var checks := 0
var failures := 0

func _initialize() -> void:
	call_deferred("run")

func check(value: bool, description: String) -> void:
	checks += 1
	if not value: failures += 1; push_error(description)

func frames(count: int) -> void:
	for i in count:
		await physics_frame
		await process_frame

func run() -> void:
	var game = load("res://scenes/solo.tscn").instantiate()
	root.add_child(game)
	await frames(2)
	check(not game.racing and game.overlay.visible, "single player opens a course and difficulty lobby")
	check(not game.room.network_active and game.room.people.size() == 1, "solo mode opens without a network connection")
	game.count_choice.select(4)
	game.difficulty_choice.select(0)
	game.start_race()
	check(game.room.people.size() == 5 and game.room.bot_controllers.size() == 4, "mixed field spawns four actual AI opponents")
	var tiers: Array = []
	var colors: Array = []
	for id: int in game.room.people:
		colors.append(game.room.people[id].color)
		if id == 1: continue
		var brain = game.room.bot_controllers[id]
		tiers.append(brain.tier)
		check(game.room.actors[id].movement == game.room.actors[1].movement, "bots share the human movement resource")
		check(brain.probe.collision_layer == 0 and not brain.probe.visible, "trajectory probes are hidden and cannot collide with racers")
	check(tiers == [0, 1, 2, 3] and colors == [0, 1, 2, 3, 4], "mixed rivals have separate tiers and assigned colors")
	await frames(30)
	check(game.room.bot_controllers[-2].ticks == 0 and game.room.actors[-2].position == game.room.document.spawn(), "countdown keeps bots stationary")
	check(game.room.actors[1].position == game.room.document.spawn(), "countdown also keeps the human stationary")
	var tick: int = game.room.server_tick
	game._set_paused(true)
	await frames(10)
	check(game.room.server_tick == tick and game.overlay.visible, "pausing freezes the whole offline race")
	game._set_paused(false)
	await frames(5)
	check(game.room.server_tick > tick, "resuming restores the race clock")
	game.difficulty_choice.select(4)
	game.count_choice.select(2)
	game.track_choice.select(1)
	game.start_race()
	check(game.room.people.size() == 3 and game.room.bot_controllers.size() == 2, "new race replaces the old rivals without stale players")
	check(game.room.bot_controllers[-2].tier == 3 and game.room.bot_controllers[-3].tier == 3, "a selected tier applies to every rival")
	check(game.room.round_results.is_empty() and game.room.document.title == "Skyline Steps", "changing course resets results and loads its actual terrain")
	game.count_choice.select(0)
	game.start_race()
	check(game.room.bot_controllers.is_empty() and game.room.people.size() == 1, "zero opponents starts a real time trial")
	game.room.start_tick = game.room.server_tick
	await frames(10)
	var before: int = game.room.server_tick
	game.room.request_restart()
	check(game.room.people[1].deaths == 1 and game.room.server_tick == before, "checkpoint respawn adds a death without resetting the round clock")
	var goal: Vector2i = game.room.document.find(TileCatalog.Tile.FINISH)
	game.room.actors[1].position = Vector2(goal * 24) + Vector2(12, 14)
	game.room._interact(1)
	await frames(3)
	var finish: int = game.room.people[1].finish
	check(finish >= 0 and game.caption.text.begins_with("FINISHED"), "human finish is validated and displayed in solo mode")
	await frames(10)
	check(game.timer_label.text == RaceSession.format_time(finish), "the player's timer freezes on their finish")
	game._set_paused(false)
	root.remove_child(game)
	game.queue_free()
	await process_frame
	print("Solo mode: %d checks, %d failures" % [checks, failures])
	quit(1 if failures else 0)

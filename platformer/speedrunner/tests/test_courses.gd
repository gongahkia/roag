extends SceneTree

# Recorded player strategies exercise the real controller, geometry, hazards,
# timer and finish trigger together. They never teleport or bypass collision.
var failures := 0

func _initialize() -> void:
	call_deferred("run")

func run() -> void:
	var game = load("res://scenes/main.tscn").instantiate()
	root.add_child(game)
	game.session.record_path = "user://test-courses-v2.json"
	game.session.records.clear()
	for index in 3:
		game.load_course(index)
		await process_frame
		var player: Runner = game.player
		player.test_input = {"axis": 1.0, "jump": false, "sprint": true}
		var marks: Array = []
		if index == 0:
			marks = [[210, 2], [362, 60], [646, 60], [966, 60], [1290, 60]]
		elif index == 1:
			marks = [[706, 80], [976, 80]]
		else:
			marks = [[180, 2], [392, 70], [628, 2], [746, 70], [980, 2], [1112, 8], [1326, 70], [1590, 70], [1860, 2]]
		var mark := 0
		var hold := 0
		var axis := 1.0
		var since_jump := 100
		for step in 1800:
			var press := false
			if index == 1 and player.position.x < 410:
				if player.is_on_floor() and player.position.x > 330:
					press = true
					hold = 100
					since_jump = 0
				if player.is_on_wall_only() and since_jump > 12:
					press = true
					hold = 100
					since_jump = 0
					axis = player.get_wall_normal().x
				if player.position.y < 128:
					axis = 1.0
			else:
				axis = 1.0
				if mark < marks.size() and player.position.x >= marks[mark][0] and (player.is_on_floor() or player.coyote > 0):
					press = true
					hold = marks[mark][1]
					mark += 1
			player.test_input = {"axis": axis, "jump": hold > 0, "sprint": true, "press": press}
			hold -= 1
			since_jump += 1
			await physics_frame
			await process_frame
			if game.session.deaths or game.session.finished:
				break
		if not game.session.finished or game.session.deaths != 0 or not game.session.new_best:
			push_error("Course %d has no clean finish on its reference route" % index)
			failures += 1
		else:
			print("Course %d: clean finish %s" % [index + 1, RaceSession.format_time(game.session.ticks)])
		# Verify the entire recorded ghost survives actual disk persistence.
		var loaded := RaceSession.new()
		loaded.record_path = game.session.record_path
		loaded.load_records()
		loaded.begin(game.course["id"])
		if loaded.best_ticks() != game.session.ticks or loaded.records.get(game.course["id"], {}).get("frames", []) != game.session.frames:
			push_error("Course %d ghost did not reload exactly" % index)
			failures += 1
	DirAccess.remove_absolute(game.session.record_path)
	root.remove_child(game)
	game.queue_free()
	print("REDLINE course integration: %d failures" % failures)
	quit(1 if failures else 0)

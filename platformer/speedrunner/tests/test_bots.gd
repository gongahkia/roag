extends SceneTree

const Room = preload("res://scripts/room_network.gd")
const Courses = preload("res://scripts/solo_courses.gd")
const Brain = preload("res://scripts/race_bot.gd")
const Hazards = preload("res://scripts/race_hazards.gd")
var failures := 0
var checks := 0

func check(value: bool, description: String) -> void:
	checks += 1
	if not value: failures += 1; push_error(description)

func _initialize() -> void:
	call_deferred("run")

func run() -> void:
	for track in 3:
		var room := Room.new()
		root.add_child(room)
		room.set_physics_process(false)
		var course := Courses.course(track)
		room.install_document(course.document)
		var hazards := Hazards.new()
		hazards.definitions = course.saws
		hazards.clock = func(): return maxf(0, room.server_tick - room.start_tick) / 120.0
		room.add_child(hazards)
		room.hazard_probe = hazards.hits
		for tier in 4:
			var brain := Brain.new(room.world, tier, hazards.hits)
			room.add_local_bot("Bot " + Brain.TIERS[tier], brain)
		room.request_phase("race")
		room.start_tick = 1
		var world_before := room.document.pack()
		var started := Time.get_ticks_msec()
		for step in 120 * 45:
			await physics_frame
			room._server_step(1.0 / 120)
			await process_frame
			if room.round_results.size() == 4: break
		for id: int in room.bot_controllers:
			var person: Dictionary = room.people[id]
			var brain = room.bot_controllers[id]
			var actor: Runner = room.actors[id]
			print("Track %d / %s: finish=%s deaths=%d pos=%s state=%s decisions=%d jumps=%d" % [track + 1, person.name, person.finish, person.deaths, actor.position, brain.state, brain.decisions, brain.jumps])
			check(int(person.finish) > 0, "Bot completes through real physics: " + person.name + " on " + str(track))
			check(brain.decisions > 1 and brain.jumps > 1, "Bot makes multiple live decisions and jumps")
			check(actor.velocity.length() <= 900, "Bot has no inflated movement speed")
		check(room.document.pack() == world_before, "Prediction does not edit the live course")
		check(room.people[-5].finish < room.people[-2].finish, "Expert outpaces Easy on each course")
		print("Track benchmark: %d ms" % (Time.get_ticks_msec() - started))
		root.remove_child(room)
		room.queue_free()
		await process_frame
	print("Bot race integration: %d checks, %d failures" % [checks, failures])
	quit(1 if failures else 0)

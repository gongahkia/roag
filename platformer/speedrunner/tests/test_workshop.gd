extends SceneTree

const Room = preload("res://scripts/room_network.gd")
const Doc = preload("res://scripts/world_document.gd")
const Catalog = preload("res://scripts/tile_catalog.gd")
var room: WorkshopRoom
var failures := 0
var checks := 0

func _initialize() -> void:
	call_deferred("run")

func check(value: bool, description: String) -> void:
	checks += 1
	if not value: failures += 1; push_error(description)

func advance(count: int, command: Dictionary = {"axis": 0.0, "jump": false, "sprint": true, "press": false, "climb": 0}) -> void:
	for i in count:
		await physics_frame
		room._step_actor(room.local_actor(), command, 1.0 / 120)
		await process_frame

func fixture(floor_tile: int = Catalog.Tile.GRASS) -> void:
	var document := Doc.new(32, 16)
	for x in 32: document.set_raw(Vector2i(x, 12), floor_tile)
	document.set_raw(Vector2i(2, 11), Catalog.Tile.SPAWN)
	document.set_raw(Vector2i(28, 11), Catalog.Tile.FINISH)
	room.install_document(document)
	await advance(4)

func run() -> void:
	var document := Doc.example()
	check(Catalog.NAMES.size() == 36, "catalog includes erase plus 35 distinct tiles")
	check(Doc.unpack(document.pack()) != null, "the demonstration world validates")
	var a := Vector2i(5, 5)
	var b := Vector2i(6, 5)
	check(not document.commit(a, Catalog.Tile.BRICK, 0, 0).has("error"), "first cell write is accepted")
	check(not document.commit(b, Catalog.Tile.ICE, 0, 0).has("error"), "an unrelated cell can use its original version")
	check(document.commit(a, Catalog.Tile.WOOD, 0, 0).has("error"), "stale same-cell write is rejected")
	check(document.commit(a, Catalog.Tile.BG_BRICK, 0, document.version(a)).has("error"), "background tiles cannot enter the collision layer")
	check(document.commit(Vector2i(-1, 5), 3, 0, 0).has("error"), "out-of-bounds edits are rejected")
	var old_spawn := document.find(Catalog.Tile.SPAWN)
	document.commit(Vector2i(8, 8), Catalog.Tile.SPAWN, 0, 0)
	check(document.tile(old_spawn) == 0 and document.layers[0].count(Catalog.Tile.SPAWN) == 1, "moving start keeps one spawn")
	check(document.commit(document.find(Catalog.Tile.FINISH), 0, 0, 0).has("error"), "finish cannot be accidentally erased")
	var path := "user://test-workshop-world.json"
	check(document.save_to(path), "world saves atomically")
	var restored := Doc.load_from(path)
	check(restored != null and restored.pack() == document.pack(), "world layers and cell revisions survive disk reload")
	DirAccess.remove_absolute(path)
	var malformed := document.pack()
	malformed.front[0] = 9999
	check(Doc.unpack(malformed) == null, "unknown imported tiles are rejected")

	room = Room.new()
	root.add_child(room)
	room.set_physics_process(false)
	room.world_path = "user://test-workshop-autosave.json"
	await fixture()
	var actor := room.local_actor()
	actor.velocity.x = 420
	await advance(3)
	var normal_speed := actor.velocity.x
	await fixture(Catalog.Tile.ICE)
	actor.velocity.x = 420
	await advance(3)
	check(actor.velocity.x > 400 and actor.velocity.x - normal_speed > 200, "ice preserves momentum when normal ground brakes")
	await fixture(Catalog.Tile.BELT_RIGHT)
	var start_x := actor.position.x
	await advance(12)
	check(actor.position.x - start_x > 10, "conveyor carries a stationary player")

	await fixture()
	room.document.set_raw(Vector2i(4, 11), Catalog.Tile.SPRING)
	actor.respawn(Vector2(108, 278))
	await advance(1)
	check(actor.velocity.y < -800, "spring launches on contact without a jump press")
	await advance(20)
	check(actor.position.y < 190, "spring launch is not cut short by an unheld jump button")
	var snapshot := actor.capture_state()
	actor.external_launch = false
	actor.restore_state(snapshot)
	check(actor.external_launch, "network snapshots preserve spring launch state")

	await fixture()
	for y in range(8, 12): room.document.set_raw(Vector2i(4, y), Catalog.Tile.LADDER)
	actor.respawn(Vector2(108, 278))
	await advance(20, {"axis": 0.0, "jump": false, "climb": -1})
	check(actor.position.y < 252 and actor.velocity.y < -160, "ladder input climbs continuously across tile boundaries")
	await fixture()
	for y in range(8, 12):
		for x in range(4, 10): room.document.set_raw(Vector2i(x, y), Catalog.Tile.WATER)
	actor.respawn(Vector2(108, 254))
	actor.velocity.y = 600
	await advance(20, {"axis": 1.0, "jump": false, "sprint": true})
	check(actor.velocity.y <= 260 and actor.velocity.x <= 232, "water slows falling and horizontal movement")

	await fixture()
	room.document.set_raw(Vector2i(4, 10), Catalog.Tile.PLATFORM)
	room.world.rebuild(Vector2i(4, 10))
	actor.respawn(Vector2(108, 272))
	actor.velocity.y = -600
	var top := actor.position.y
	for i in 90:
		await advance(1, {"axis": 0.0, "jump": true})
		top = minf(top, actor.position.y)
	check(top < 210 and absf(actor.position.y - 230) < 1, "one-way platform permits upward passage and catches the landing")

	await fixture()
	room.document.set_raw(Vector2i(4, 11), Catalog.Tile.WIND_UP)
	actor.respawn(Vector2(108, 278))
	await advance(10)
	check(actor.velocity.y < -80 and actor.position.y < 275, "updraft pushes against normal gravity")
	await fixture()
	room.document.set_raw(Vector2i(4, 11), Catalog.Tile.KEY_RED)
	room.document.set_raw(Vector2i(8, 11), Catalog.Tile.DOOR_RED)
	room.world.rebuild(Vector2i(8, 11))
	actor.respawn(Vector2(108, 278))
	room._server_step(1.0 / 120)
	check(room.world.doors.red and not room.world.bodies.has(Vector2i(8, 11)), "red key opens shared red-door collision")
	room.key_until.red = 0
	actor.respawn(Vector2(60, 278))
	room._server_step(1.0 / 120)
	check(not room.world.doors.red, "door closes when the key timer expires")
	room.world.set_doors("red", true)
	actor.respawn(Vector2(204, 278))
	room._server_step(1.0 / 120)
	check(room.world.doors.red, "door remains open while a player occupies its cell")

	await fixture()
	room.document.set_raw(Vector2i(4, 11), Catalog.Tile.COIN)
	actor.respawn(Vector2(108, 278))
	room._interact(1); room._interact(1)
	check(room.people[1].coins.size() == 1, "a coin counts once per player per round")
	room.document.set_raw(Vector2i(4, 11), Catalog.Tile.CHECKPOINT)
	room._interact(1)
	actor.position.y = 1000
	room._interact(1)
	check(actor.position.distance_to(Vector2(108, 278)) < 1 and room.people[1].deaths == 1, "falling respawns at the personal checkpoint")
	room.document.set_raw(Vector2i(4, 11), Catalog.Tile.SPIKES)
	room._interact(1)
	check(room.people[1].deaths == 2, "spike collision is validated by the host")
	room._set_phase(1, "race")
	var revision := room.document.revision
	room.request_edit(Vector2i(5, 5), Catalog.Tile.BRICK, 0)
	check(room.document.revision == revision, "race mode freezes the editable world")
	room._set_phase(1, "build")
	root.remove_child(room)
	room.queue_free()
	print("Workshop: %d checks, %d failures" % [checks, failures])
	quit(1 if failures else 0)

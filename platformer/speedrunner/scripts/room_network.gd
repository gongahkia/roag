class_name WorkshopRoom
extends Node2D

signal notice(message: String)
signal document_changed(transaction: Dictionary)
signal roster_changed
signal phase_changed
signal ready_to_play

const PROTOCOL := 2
const MAX_PLAYERS := 16
const DEFAULT_PORT := 24567
const HZ := 120
const Catalog = preload("res://scripts/tile_catalog.gd")
const Document = preload("res://scripts/world_document.gd")
const World = preload("res://scripts/tile_world.gd")
const Player = preload("res://scripts/player.gd")
const Avatar = preload("res://scripts/remote_avatar.gd")
const Style = preload("res://scripts/avatar_style.gd")
const COLORS = Style.COLORS

var document: WorldDocument
var world: TileWorld
var people: Dictionary = {}
var actors: Dictionary = {}
var avatars: Dictionary = {}
var inputs: Dictionary = {}
var histories: Dictionary = {}
var redos: Dictionary = {}
var edit_budgets: Dictionary = {}
var name_changes: Dictionary = {}
var bot_controllers: Dictionary = {}
var hazard_probe: Callable
var network_active := false
var dedicated := false
var owner_id := 1
var guest_editing := true
var phase := "build"
var epoch := 0
var server_tick := 0
var start_tick := 0
var estimated_tick := 0.0
var round_results: Dictionary = {}
var key_until := {"red": 0, "blue": 0}
var world_path := "user://workshop-world.json"
var dirty := false
var display_name := "Runner"
var local_command: Dictionary = {"axis": 0.0, "jump": false, "sprint": false, "press": false, "climb": 0}
var test_command: Dictionary = {}
var local_sequence := 0
var last_ack := 0
var pending_state := PackedFloat64Array()
var command_history: Dictionary = {}
var latest_snapshot: Dictionary = {}
var room_ready := true
var port := DEFAULT_PORT
var connection_started := 0

func _ready() -> void:
	name = "Room"
	world = World.new()
	world.name = "World"
	add_child(world)
	document = Document.example()
	world.install(document)
	people[1] = _person("Runner", 0)
	_spawn_actor(1)
	multiplayer.peer_connected.connect(_peer_connected)
	multiplayer.peer_disconnected.connect(_peer_disconnected)
	multiplayer.connected_to_server.connect(_connected)
	multiplayer.connection_failed.connect(_connection_failed)
	multiplayer.server_disconnected.connect(_server_disconnected)

func authoritative() -> bool:
	return not network_active or multiplayer.is_server()

func local_id() -> int:
	return multiplayer.get_unique_id() if network_active else 1

func local_actor() -> Runner:
	return actors.get(local_id())

func is_owner() -> bool:
	return local_id() == owner_id

func can_edit(id := -1) -> bool:
	if id < 0: id = local_id()
	return phase == "build" and people.has(id) and (guest_editing or id == owner_id)

func _person(player_name: String, color_index: int) -> Dictionary:
	return {"name": player_name, "color": color_index, "editor": false, "spectator": false,
		"finish": -1, "deaths": 0, "coins": [], "checkpoint": document.spawn(), "ack": 0, "generation": 0}

func _spawn_actor(id: int) -> void:
	if actors.has(id): return
	var actor: Runner = Player.new()
	actor.manual_simulation = true
	actor.accept_local_input = false
	actor.collision_layer = 2
	actor.collision_mask = 1
	add_child(actor)
	actor.name = "Runner_%d" % id
	actor.respawn(document.spawn())
	actor.set_identity(people[id].name, COLORS[int(people[id].color)])
	actors[id] = actor
	inputs[id] = {}

func add_local_bot(player_name: String, controller: RefCounted) -> int:
	if network_active or people.size() >= MAX_PLAYERS: return 0
	var id := -2
	while people.has(id): id -= 1
	people[id] = _person(unique_name(player_name), available_color())
	people[id]["bot"] = true
	_spawn_actor(id)
	bot_controllers[id] = controller
	roster_changed.emit()
	return id

func clear_local_bots() -> void:
	for id: int in bot_controllers.keys(): _remove_person(id)
	roster_changed.emit()

func _spawn_avatar(id: int) -> void:
	if avatars.has(id) or id == local_id(): return
	var avatar = Avatar.new()
	add_child(avatar)
	avatar.configure(people[id].name, COLORS[int(people[id].color) % COLORS.size()])
	avatar.position = document.spawn()
	avatar.target = avatar.position
	avatars[id] = avatar

func _clear_players() -> void:
	for controller in bot_controllers.values(): controller.dispose()
	bot_controllers.clear()
	for collection in [actors, avatars]:
		for value in collection.values():
			remove_child(value)
			value.queue_free()
		collection.clear()
	people.clear(); inputs.clear(); histories.clear(); redos.clear(); edit_budgets.clear(); name_changes.clear()
	_reset_prediction()

func host(listen_port: int, player_name: String, headless := false) -> Error:
	close_connection(false)
	var peer := ENetMultiplayerPeer.new()
	var result := peer.create_server(listen_port, MAX_PLAYERS if headless else MAX_PLAYERS - 1, 4)
	if result != OK:
		close_connection()
		notice.emit("Cannot host on UDP %d: %s" % [listen_port, error_string(result)])
		return result
	multiplayer.multiplayer_peer = peer
	network_active = true
	dedicated = headless
	port = listen_port
	display_name = clean_name(player_name)
	_clear_players()
	owner_id = 0 if dedicated else 1
	if not dedicated:
		people[1] = _person(display_name, 0)
		_spawn_actor(1)
	room_ready = true
	phase = "build"
	server_tick = 0
	start_tick = 0
	epoch += 1
	estimated_tick = 0
	round_results.clear()
	key_until = {"red": 0, "blue": 0}
	world.set_doors("red", false); world.set_doors("blue", false)
	guest_editing = true
	phase_changed.emit(); roster_changed.emit(); ready_to_play.emit()
	notice.emit("Hosting on UDP %d / up to %d players" % [port, MAX_PLAYERS])
	return OK

func join(address: String, listen_port: int, player_name: String) -> Error:
	close_connection(false)
	var peer := ENetMultiplayerPeer.new()
	var result := peer.create_client(address.strip_edges(), listen_port, 4)
	if result != OK:
		close_connection()
		notice.emit("Cannot connect: " + error_string(result))
		return result
	_clear_players()
	multiplayer.multiplayer_peer = peer
	network_active = true
	dedicated = false
	display_name = clean_name(player_name)
	port = listen_port
	room_ready = false
	connection_started = Time.get_ticks_msec()
	notice.emit("Connecting to %s:%d…" % [address, port])
	return OK

func close_connection(make_local := true) -> void:
	if network_active:
		multiplayer.multiplayer_peer.close()
		multiplayer.multiplayer_peer = OfflineMultiplayerPeer.new()
	network_active = false
	if make_local:
		_clear_players()
		dedicated = false
		owner_id = 1
		phase = "build"
		room_ready = true
		people[1] = _person(display_name, 0)
		_spawn_actor(1)
		phase_changed.emit(); roster_changed.emit(); ready_to_play.emit()

static func clean_name(value: String) -> String:
	var result := ""
	for character: String in value.substr(0, 128):
		var code := character.unicode_at(0)
		if code <= 32 or (code >= 127 and code <= 159):
			result += " "
		elif code not in range(0x200b, 0x2010) and code not in range(0x202a, 0x202f) and code not in range(0x2066, 0x206a) and code != 0xfeff:
			result += character
	return " ".join(result.split(" ", false)).substr(0, 20) if not result.strip_edges().is_empty() else "Runner"

func available_color() -> int:
	var used: Array = []
	for person: Dictionary in people.values(): used.append(int(person.color))
	for index in COLORS.size():
		if not used.has(index): return index
	return -1

func unique_name(requested: String, except_id := -1) -> String:
	var base := clean_name(requested)
	var candidate := base
	var suffix := 2
	while true:
		var taken := false
		for id: int in people:
			if id != except_id and str(people[id].name).nocasecmp_to(candidate) == 0: taken = true; break
		if not taken: return candidate
		var ending := " %d" % suffix
		candidate = base.substr(0, 20 - ending.length()) + ending
		suffix += 1
	return candidate

func request_name(requested: String) -> void:
	if authoritative(): _rename(local_id(), requested)
	elif room_ready: rename_player.rpc_id(1, requested)

@rpc("any_peer", "call_remote", "reliable", 0)
func rename_player(requested: String) -> void:
	if authoritative(): _rename(multiplayer.get_remote_sender_id(), requested)

func _rename(id: int, requested: String) -> void:
	if not people.has(id): return
	var accepted := unique_name(requested, id)
	if accepted == people[id].name: return
	var now := Time.get_ticks_msec()
	if now - int(name_changes.get(id, -1000)) < 1000: _reject(id, "Wait a moment before changing your username again"); return
	name_changes[id] = now
	people[id].name = accepted
	if round_results.has(id): round_results[id].name = accepted
	_apply_identity(id)
	if network_active: identity_applied.rpc(id, accepted)
	roster_changed.emit()

func _apply_identity(id: int) -> void:
	var person: Dictionary = people[id]
	var tint: Color = COLORS[int(person.color)]
	if actors.has(id): actors[id].set_identity(person.name, tint)
	if avatars.has(id): avatars[id].set_identity(person.name, tint)
	if id == local_id(): display_name = person.name

@rpc("authority", "call_remote", "reliable", 0)
func identity_applied(id: int, accepted: String) -> void:
	if authoritative() or not people.has(id): return
	people[id].name = accepted
	if round_results.has(id): round_results[id].name = accepted
	_apply_identity(id)
	roster_changed.emit()

func _peer_connected(_id: int) -> void:
	pass # A peer is not admitted until its protocol/name handshake is validated.

func _connected() -> void:
	register.rpc_id(1, PROTOCOL, display_name)

func _connection_failed() -> void:
	close_connection()
	notice.emit("Connection failed. Check the address and UDP port.")

func _server_disconnected() -> void:
	close_connection()
	notice.emit("Host disconnected. You are now in a local copy.")

func _peer_disconnected(id: int) -> void:
	if not authoritative(): return
	_remove_person(id)
	if id == owner_id:
		owner_id = int(people.keys()[0]) if not people.is_empty() else 0
	if network_active: remove_person.rpc(id, owner_id)
	roster_changed.emit()

func _remove_person(id: int) -> void:
	if bot_controllers.has(id):
		bot_controllers[id].dispose()
		bot_controllers.erase(id)
	people.erase(id); inputs.erase(id); histories.erase(id); redos.erase(id); edit_budgets.erase(id); name_changes.erase(id)
	for collection in [actors, avatars]:
		if collection.has(id):
			var actor: Node = collection[id]
			remove_child(actor); actor.queue_free(); collection.erase(id)

@rpc("any_peer", "call_remote", "reliable", 0)
func register(protocol: int, player_name: String) -> void:
	if not authoritative(): return
	var sender := multiplayer.get_remote_sender_id()
	if protocol != PROTOCOL or people.size() >= MAX_PLAYERS or people.has(sender):
		multiplayer.multiplayer_peer.disconnect_peer(sender)
		return
	people[sender] = _person(unique_name(player_name), available_color())
	people[sender].spectator = phase == "race"
	_spawn_actor(sender)
	if owner_id == 0: owner_id = sender
	welcome.rpc_id(sender, document.pack(), people, owner_id, phase, epoch, start_tick, server_tick, guest_editing)
	announce_person.rpc(sender, people[sender], owner_id)
	roster_changed.emit()

@rpc("authority", "call_remote", "reliable", 0)
func welcome(data: Dictionary, roster: Dictionary, owner: int, mode: String, round_id: int, start: int, tick: int, editing: bool) -> void:
	if authoritative(): return
	var incoming := Document.unpack(data)
	if incoming == null:
		close_connection(); notice.emit("The host sent an invalid world"); return
	document = incoming
	world.install(document)
	people = roster
	owner_id = owner
	phase = mode; epoch = round_id; start_tick = start; estimated_tick = tick
	guest_editing = editing
	_spawn_actor(local_id())
	display_name = people[local_id()].name
	for id: int in people:
		_spawn_avatar(id)
	room_ready = true
	phase_changed.emit(); roster_changed.emit(); ready_to_play.emit()
	notice.emit("Joined / %d players / world revision %d" % [people.size(), document.revision])

@rpc("authority", "call_remote", "reliable", 0)
func announce_person(id: int, data: Dictionary, owner: int) -> void:
	if authoritative() or not room_ready: return
	people[id] = data
	owner_id = owner
	_spawn_avatar(id)
	roster_changed.emit()

@rpc("authority", "call_remote", "reliable", 0)
func remove_person(id: int, owner: int) -> void:
	if authoritative(): return
	_remove_person(id)
	owner_id = owner
	roster_changed.emit()

func request_edit(cell: Vector2i, tile_id: int, layer: int) -> void:
	var expected := document.version(cell, layer)
	if authoritative(): _edit(local_id(), cell, tile_id, layer, expected)
	else: edit_cell.rpc_id(1, cell, tile_id, layer, expected)

@rpc("any_peer", "call_remote", "reliable", 1)
func edit_cell(cell: Vector2i, tile_id: int, layer: int, expected: int) -> void:
	if authoritative(): _edit(multiplayer.get_remote_sender_id(), cell, tile_id, layer, expected)

func _budget(id: int) -> bool:
	var now := Time.get_ticks_msec()
	var budget: Dictionary = edit_budgets.get(id, {"at": now, "tokens": 80.0})
	budget.tokens = minf(80, float(budget.tokens) + (now - int(budget.at)) * 0.06)
	budget.at = now
	if budget.tokens < 1:
		edit_budgets[id] = budget
		return false
	budget.tokens -= 1
	edit_budgets[id] = budget
	return true

func _edit(id: int, cell: Vector2i, tile_id: int, layer: int, expected: int) -> void:
	if not can_edit(id): _reject(id, "Editing is locked during a race or by the host"); return
	if not _budget(id): _reject(id, "Build a little slower"); return
	if layer == 0 and Catalog.solid(tile_id):
		var rect := Rect2(Vector2(cell * Catalog.SIZE), Vector2.ONE * Catalog.SIZE)
		for actor: Runner in actors.values():
			if rect.intersects(Rect2(actor.position - Vector2(6, 10), Vector2(12, 20))):
				_reject(id, "Cannot place a solid block inside a player"); return
	var transaction := document.commit(cell, tile_id, layer, expected)
	if transaction.has("error"): _reject(id, transaction.error); return
	var stack: Array = histories.get(id, [])
	transaction["stamps"] = []
	for edit: Array in transaction.edits: transaction.stamps.append(transaction.revision)
	stack.append(transaction)
	if stack.size() > 128: stack.pop_front()
	histories[id] = stack
	redos[id] = []
	_publish_transaction(transaction)

func _publish_transaction(transaction: Dictionary) -> void:
	transaction["epoch"] = epoch
	world.changed(transaction)
	dirty = true
	round_results.clear()
	for person: Dictionary in people.values():
		var at := Vector2i((Vector2(person.checkpoint) / Catalog.SIZE).floor())
		if document.tile(at) not in [Catalog.Tile.CHECKPOINT, Catalog.Tile.SPAWN]: person.checkpoint = document.spawn()
	if network_active: receive_edit.rpc(transaction)
	document_changed.emit(transaction)

@rpc("authority", "call_remote", "reliable", 1)
func receive_edit(transaction: Dictionary) -> void:
	if authoritative(): return
	if int(transaction.get("epoch", -1)) != epoch or int(transaction.revision) <= document.revision: return
	document.apply_remote(transaction)
	world.changed(transaction)
	round_results.clear()
	document_changed.emit(transaction)

func _reject(id: int, message: String) -> void:
	if id == local_id(): notice.emit(message)
	elif network_active: rejected.rpc_id(id, message)

@rpc("authority", "call_remote", "reliable", 0)
func rejected(message: String) -> void:
	notice.emit(message)

func request_undo(redo := false) -> void:
	if authoritative(): _undo(local_id(), redo)
	else: undo_edit.rpc_id(1, redo)

@rpc("any_peer", "call_remote", "reliable", 1)
func undo_edit(redo: bool) -> void:
	if authoritative(): _undo(multiplayer.get_remote_sender_id(), redo)

func _undo(id: int, redo: bool) -> void:
	if not can_edit(id) or not _budget(id): _reject(id, "Editing is locked"); return
	var source: Dictionary = redos if redo else histories
	var target: Dictionary = histories if redo else redos
	var stack: Array = source.get(id, [])
	if stack.is_empty(): _reject(id, "Nothing to redo" if redo else "Nothing to undo"); return
	var previous: Dictionary = stack.back()
	var edits: Array = []
	for index in previous.edits.size():
		var edit: Array = previous.edits[index]
		var cell := Vector2i(edit[0], edit[1])
		if document.version(cell, int(edit[2])) != int(previous.stamps[index]):
			_reject(id, "Another builder changed this cell; undo would overwrite them"); return
		if Catalog.solid(int(edit[3])):
			for actor: Runner in actors.values():
				if Rect2(Vector2(cell * Catalog.SIZE), Vector2.ONE * Catalog.SIZE).intersects(Rect2(actor.position - Vector2(6, 10), Vector2(12, 20))):
					_reject(id, "Undo would place a block inside a player"); return
		edits.append([edit[0], edit[1], edit[2], edit[4], edit[3]])
	stack.pop_back()
	source[id] = stack
	var transaction := document.apply_edits(edits)
	transaction["stamps"] = []
	for edit: Array in edits:
		transaction.stamps.append(transaction.revision)
		# Retarget only the nearest older action on this cell. This allows
		# repeated undo of one's own strokes without undoing another builder.
		for older_index in range(stack.size() - 1, -1, -1):
			var found := false
			var older: Dictionary = stack[older_index]
			for cell_index in older.edits.size():
				var old_edit: Array = older.edits[cell_index]
				if old_edit[0] == edit[0] and old_edit[1] == edit[1] and old_edit[2] == edit[2]:
					if old_edit[4] == edit[4]: older.stamps[cell_index] = transaction.revision
					found = true
					break
			if found: break
	var opposite: Array = target.get(id, [])
	opposite.append(transaction)
	target[id] = opposite
	_publish_transaction(transaction)

func request_editor(enabled: bool) -> void:
	if authoritative(): _editor(local_id(), enabled)
	else: editor_state.rpc_id(1, enabled)

@rpc("any_peer", "call_remote", "reliable", 0)
func editor_state(enabled: bool) -> void:
	if authoritative(): _editor(multiplayer.get_remote_sender_id(), enabled)

func _editor(id: int, enabled: bool) -> void:
	if not people.has(id): return
	if enabled and not can_edit(id): _reject(id, "The host has locked editing"); return
	people[id].editor = enabled
	people[id].generation += 1
	people[id].ack = 0
	inputs[id] = {}
	if actors.has(id): actors[id].velocity = Vector2.ZERO
	if network_active: editor_applied.rpc(id, enabled, people[id].generation, actors[id].capture_state())
	roster_changed.emit()

@rpc("authority", "call_remote", "reliable", 0)
func editor_applied(id: int, enabled: bool, generation: int, state: PackedFloat64Array) -> void:
	if people.has(id):
		people[id].editor = enabled
		people[id].generation = generation
	if id == local_id():
		_reset_prediction()
		if local_actor(): local_actor().restore_state(state)
	roster_changed.emit()

func request_phase(mode: String) -> void:
	if authoritative(): _set_phase(local_id(), mode)
	else: change_phase.rpc_id(1, mode)

@rpc("any_peer", "call_remote", "reliable", 0)
func change_phase(mode: String) -> void:
	if authoritative(): _set_phase(multiplayer.get_remote_sender_id(), mode)

func _set_phase(id: int, mode: String) -> void:
	if id != owner_id or mode not in ["build", "race"]: _reject(id, "Only the host can start or end a race"); return
	epoch += 1
	phase = mode
	start_tick = server_tick + HZ * 3 if mode == "race" else 0
	round_results.clear()
	key_until = {"red": 0, "blue": 0}
	world.set_doors("red", false); world.set_doors("blue", false)
	for peer: int in people:
		people[peer].editor = false; people[peer].spectator = false
		people[peer].finish = -1; people[peer].deaths = 0; people[peer].coins = []
		people[peer].checkpoint = document.spawn(); people[peer].ack = 0
		people[peer].generation += 1
		inputs[peer] = {}
		actors[peer].respawn(document.spawn())
		if bot_controllers.has(peer): bot_controllers[peer].reset()
	_reset_prediction()
	if network_active: phase_applied.rpc(phase, epoch, start_tick, server_tick, people, document.pack())
	phase_changed.emit(); roster_changed.emit()

@rpc("authority", "call_remote", "reliable", 0)
func phase_applied(mode: String, round_id: int, start: int, tick: int, roster: Dictionary, data: Dictionary) -> void:
	if authoritative(): return
	var frozen_world := Document.unpack(data)
	if frozen_world == null: return
	document = frozen_world
	world.install(document)
	phase = mode; epoch = round_id; start_tick = start; estimated_tick = tick
	people = roster
	round_results.clear()
	_reset_prediction()
	if local_actor(): local_actor().respawn(document.spawn())
	phase_changed.emit(); roster_changed.emit()

func _reset_prediction() -> void:
	local_sequence = 0; last_ack = 0; command_history.clear(); pending_state.clear()
	latest_snapshot.clear()

func request_lock(locked: bool) -> void:
	if authoritative(): _lock(local_id(), locked)
	else: edit_lock.rpc_id(1, locked)

func request_kick(id: int) -> void:
	if authoritative(): _kick(local_id(), id)
	else: kick_player.rpc_id(1, id)

@rpc("any_peer", "call_remote", "reliable", 0)
func kick_player(id: int) -> void:
	if authoritative(): _kick(multiplayer.get_remote_sender_id(), id)

func _kick(sender: int, target: int) -> void:
	if sender != owner_id or target == owner_id or not people.has(target):
		_reject(sender, "Only the host can remove a guest"); return
	if network_active: multiplayer.multiplayer_peer.disconnect_peer(target)

@rpc("any_peer", "call_remote", "reliable", 0)
func edit_lock(locked: bool) -> void:
	if authoritative(): _lock(multiplayer.get_remote_sender_id(), locked)

func _lock(id: int, locked: bool) -> void:
	if id != owner_id: _reject(id, "Only the host can lock editing"); return
	guest_editing = not locked
	if locked:
		for peer: int in people:
			if peer != owner_id: _editor(peer, false)
	if network_active: lock_applied.rpc(guest_editing)
	roster_changed.emit()

@rpc("authority", "call_remote", "reliable", 0)
func lock_applied(allowed: bool) -> void:
	guest_editing = allowed
	roster_changed.emit()

func request_restart() -> void:
	if authoritative(): _restart(local_id())
	else: restart_player.rpc_id(1)

@rpc("any_peer", "call_remote", "reliable", 0)
func restart_player() -> void:
	if authoritative(): _restart(multiplayer.get_remote_sender_id())

func _restart(id: int) -> void:
	if not people.has(id): return
	if phase == "race" and int(people[id].finish) >= 0: return
	actors[id].respawn(people[id].checkpoint)
	people[id].deaths += 1
	if bot_controllers.has(id): bot_controllers[id].reset(int(people[id].deaths))
	people[id].generation += 1
	people[id].ack = 0
	inputs[id] = {}
	if network_active: respawn_player.rpc(id, people[id].checkpoint, people[id].generation)

@rpc("authority", "call_remote", "reliable", 0)
func respawn_player(id: int, at: Vector2, generation: int) -> void:
	if people.has(id): people[id].generation = generation
	if id == local_id() and local_actor():
		local_actor().respawn(at)
		# Snapshot acknowledgement will discard old input; discard predicted
		# motion immediately to avoid replaying the pre-death trajectory.
		_reset_prediction()

func request_save() -> void:
	if not authoritative(): save_requested.rpc_id(1); return
	_save(local_id())

@rpc("any_peer", "call_remote", "reliable", 0)
func save_requested() -> void:
	if authoritative(): _save(multiplayer.get_remote_sender_id())

func _save(id: int) -> void:
	if id != owner_id: _reject(id, "Only the host saves the shared world"); return
	if document.save_to(world_path): dirty = false; _reject(id, "World saved on the host")
	else: _reject(id, "Could not save the world")

func request_load() -> void:
	if not authoritative(): load_requested.rpc_id(1); return
	_load(local_id())

@rpc("any_peer", "call_remote", "reliable", 0)
func load_requested() -> void:
	if authoritative(): _load(multiplayer.get_remote_sender_id())

func _load(id: int) -> void:
	if id != owner_id or phase != "build": _reject(id, "Host can load a world in Build mode"); return
	var incoming := Document.load_from(world_path)
	if incoming == null: _reject(id, "No valid saved world found"); return
	install_document(incoming)
	_reject(id, "Loaded the saved world")

func install_document(incoming: WorldDocument) -> void:
	if not authoritative(): return
	document = incoming
	world.install(document)
	histories.clear(); redos.clear()
	_set_phase(owner_id, "build")
	if network_active: replace_document.rpc(document.pack(), epoch)
	document_changed.emit({})

@rpc("authority", "call_remote", "reliable", 0)
func replace_document(data: Dictionary, round_id: int) -> void:
	var incoming := Document.unpack(data)
	if authoritative() or incoming == null: return
	document = incoming
	epoch = round_id
	world.install(document)
	if local_actor(): local_actor().respawn(document.spawn())
	_reset_prediction()
	document_changed.emit({})

static func encode_command(sequence: int, command: Dictionary) -> PackedInt32Array:
	var flags := int(bool(command.get("jump", false))) | (int(bool(command.get("sprint", false))) << 1) | (int(bool(command.get("press", false))) << 2)
	return PackedInt32Array([sequence, roundi(clampf(float(command.get("axis", 0)), -1, 1) * 1000), flags, clampi(int(command.get("climb", 0)), -1, 1)])

static func decode_command(values: PackedInt32Array, offset := 0) -> Dictionary:
	var flags := values[offset + 2]
	return {"axis": values[offset + 1] / 1000.0, "jump": bool(flags & 1), "sprint": bool(flags & 2), "press": bool(flags & 4), "climb": values[offset + 3]}

@rpc("any_peer", "call_remote", "unreliable_ordered", 2)
func submit_inputs(round_id: int, generation: int, batch: PackedInt32Array) -> void:
	if not authoritative() or round_id != epoch or batch.size() > 256 or batch.size() % 4 != 0: return
	var sender := multiplayer.get_remote_sender_id()
	if not people.has(sender) or generation != int(people[sender].generation) or (phase == "race" and server_tick < start_tick): return
	var ack := int(people[sender].ack)
	var queue: Dictionary = inputs[sender]
	for offset in range(0, batch.size(), 4):
		var sequence := batch[offset]
		if sequence <= ack or sequence > ack + 256 or absi(batch[offset + 1]) > 1000 or batch[offset + 2] < 0 or batch[offset + 2] > 7 or absi(batch[offset + 3]) > 1: continue
		queue[sequence] = decode_command(batch, offset)

func _physics_process(delta: float) -> void:
	if not room_ready:
		if connection_started and Time.get_ticks_msec() - connection_started > 10000:
			_connection_failed()
		return
	if authoritative(): _server_step(delta)
	else: _client_step(delta)

func _step_actor(actor: Runner, command: Dictionary, delta: float) -> void:
	actor.controls_enabled = true
	actor.test_input = command.duplicate()
	actor.environment = world.environment_at(actor)
	actor.step_movement(delta)
	world.apply_spring(actor)
	actor.position.x = clampf(actor.position.x, 6, document.width * Catalog.SIZE - 6)

func _server_step(delta: float) -> void:
	server_tick += 1
	estimated_tick = server_tick
	for id: int in people:
		var person: Dictionary = people[id]
		var actor: Runner = actors[id]
		actor.effects_active = not (person.editor or person.spectator or int(person.finish) >= 0 or (phase == "race" and server_tick < start_tick))
		if not actor.effects_active: continue
		var command: Dictionary = {}
		if bot_controllers.has(id):
			command = bot_controllers[id].next_command(actor)
			if bot_controllers[id].request_respawn:
				_restart(id)
				continue
		elif id == 1 and not dedicated:
			command = test_command if not test_command.is_empty() else local_command
		else:
			var next := int(person.ack) + 1
			var queue: Dictionary = inputs[id]
			if not queue.has(next): continue
			command = queue[next]
			queue.erase(next)
			person.ack = next
		_step_actor(actor, command, delta)
		_interact(id)
	for color_name: String in key_until:
		var opened: bool = int(key_until[color_name]) > server_tick
		if not opened and world.doors[color_name]:
			var door := Catalog.Tile.DOOR_RED if color_name == "red" else Catalog.Tile.DOOR_BLUE
			for actor: Runner in actors.values():
				for hit: Array in world.touches(actor):
					if hit[1] == door: opened = true
		world.set_doors(color_name, opened)
	if network_active and server_tick % 4 == 0:
		var entries: Array = []
		for id: int in people:
			entries.append({"id": id, "state": actors[id].capture_state(), "ack": people[id].ack, "generation": people[id].generation, "finish": people[id].finish, "deaths": people[id].deaths, "coins": people[id].coins.size(), "editor": people[id].editor})
		state_snapshot.rpc(epoch, server_tick, entries, round_results, key_until, world.doors)
	if dirty and server_tick % (HZ * 5) == 0:
		if document.save_to(world_path): dirty = false

func _interact(id: int) -> void:
	var actor: Runner = actors[id]
	var person: Dictionary = people[id]
	if actor.position.y > document.height * Catalog.SIZE + 48 or actor.position.y < -160:
		_restart(id); return
	if hazard_probe.is_valid() and bool(hazard_probe.call(actor, 0.0)):
		_restart(id); return
	for hit: Array in world.touches(actor):
		var cell: Vector2i = hit[0]
		var tile_id := int(hit[1])
		if tile_id in [Catalog.Tile.SPIKES, Catalog.Tile.LAVA, Catalog.Tile.SAW]: _restart(id); return
		if tile_id == Catalog.Tile.CHECKPOINT:
			person.checkpoint = Vector2(cell * Catalog.SIZE) + Vector2(12, 14)
		if tile_id == Catalog.Tile.COIN:
			var key := "%d:%d" % [cell.x, cell.y]
			if not person.coins.has(key): person.coins.append(key)
		if tile_id in [Catalog.Tile.KEY_RED, Catalog.Tile.KEY_BLUE]:
			key_until["red" if tile_id == Catalog.Tile.KEY_RED else "blue"] = server_tick + HZ * 8
		if tile_id == Catalog.Tile.FINISH and phase == "race" and server_tick >= start_tick:
			person.finish = server_tick - start_tick
			round_results[id] = {"name": person.name, "ticks": person.finish, "deaths": person.deaths, "coins": person.coins.size(), "revision": document.revision}
			roster_changed.emit()

func _client_step(delta: float) -> void:
	estimated_tick += 1
	var actor := local_actor()
	if actor == null: return
	var person: Dictionary = people.get(local_id(), {})
	actor.effects_active = not (person.is_empty() or person.editor or person.spectator or int(person.finish) >= 0 or (phase == "race" and estimated_tick < start_tick))
	if not actor.effects_active: return
	if not pending_state.is_empty():
		var before := actor.position
		actor.restore_state(pending_state)
		pending_state.clear()
		for sequence: int in command_history:
			_step_actor(actor, command_history[sequence], delta)
		if actor.position.distance_to(before) > 100: actor.reset_physics_interpolation()
	local_sequence += 1
	var command: Dictionary = test_command if not test_command.is_empty() else local_command
	var encoded := encode_command(local_sequence, command)
	command_history[local_sequence] = decode_command(encoded)
	_step_actor(actor, command_history[local_sequence], delta)
	if command_history.size() > 240:
		close_connection(); notice.emit("Connection fell too far behind; reconnect to the host"); return
	if local_sequence % 2 == 0:
		var batch := PackedInt32Array()
		for sequence: int in command_history:
			batch.append_array(encode_command(sequence, command_history[sequence]))
			if batch.size() >= 256: break
		submit_inputs.rpc_id(1, epoch, int(person.generation), batch)

@rpc("authority", "call_remote", "unreliable_ordered", 3)
func state_snapshot(round_id: int, tick: int, entries: Array, results: Dictionary, keys: Dictionary, doors: Dictionary) -> void:
	if authoritative() or not room_ready or round_id != epoch: return
	if tick <= int(latest_snapshot.get("tick", -1)): return
	latest_snapshot = {"tick": tick}
	estimated_tick = tick
	round_results = results
	for id: int in round_results:
		if people.has(id): round_results[id].name = people[id].name
	key_until = keys
	world.set_doors("red", doors.red); world.set_doors("blue", doors.blue)
	for entry: Dictionary in entries:
		var id := int(entry.id)
		if not people.has(id) or int(entry.generation) != int(people[id].generation): continue
		people[id].finish = entry.finish; people[id].deaths = entry.deaths; people[id].editor = entry.editor
		people[id]["coin_count"] = entry.coins
		if id == local_id():
			if int(entry.generation) != int(people[id].generation): continue
			last_ack = int(entry.ack)
			for sequence: int in command_history.keys():
				if sequence <= last_ack: command_history.erase(sequence)
			pending_state = entry.state
			if entry.editor or int(entry.finish) >= 0: local_actor().restore_state(entry.state)
		elif avatars.has(id): avatars[id].update_pose(entry.state, not entry.editor and int(entry.finish) < 0 and not people[id].spectator)

func _exit_tree() -> void:
	for controller in bot_controllers.values(): controller.dispose()
	bot_controllers.clear()
	if network_active: multiplayer.multiplayer_peer.close()

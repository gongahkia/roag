extends SceneTree

const Room = preload("res://scripts/room_network.gd")
const Document = preload("res://scripts/world_document.gd")
const Catalog = preload("res://scripts/tile_catalog.gd")
var room: WorkshopRoom
var role := "client"
var tag := "client"
var directory := "/tmp/redline-network"
var port := 24569
var last_command := -1
var last_report := 0
var notices: Array[String] = []

func _initialize() -> void:
	call_deferred("run")

func run() -> void:
	for arg: String in OS.get_cmdline_user_args():
		var parts := arg.trim_prefix("--").split("=", true, 1)
		if parts.size() != 2: continue
		match parts[0]:
			"role": role = parts[1]
			"tag": tag = parts[1]
			"directory": directory = parts[1]
			"port": port = int(parts[1])
	var workshop := Node2D.new()
	workshop.name = "Workshop"
	root.add_child(workshop)
	room = Room.new()
	room.name = "Room"
	workshop.add_child(room)
	room.world_path = directory.path_join("saved-world.json")
	room.notice.connect(func(message):
		notices.append(message)
		if notices.size() > 32: notices.pop_front())
	room.test_command = {"axis": 0.0, "jump": false, "sprint": true, "press": false, "climb": 0}
	if role in ["server", "listen"]:
		if room.host(port, tag, role == "server") != OK: quit(1); return
		var document := Document.new(32, 16)
		for x in document.width:
			for y in range(12, 16): document.set_raw(Vector2i(x, y), Catalog.Tile.GRASS)
		document.set_raw(Vector2i(2, 11), Catalog.Tile.SPAWN)
		document.set_raw(Vector2i(12, 11), Catalog.Tile.FINISH)
		room.install_document(document)
	else:
		if room.join("127.0.0.1", port, tag) != OK: quit(1); return
	while true:
		await process_frame
		var now := Time.get_ticks_msec()
		if now - last_report < 100: continue
		last_report = now
		poll_command()
		report()

func poll_command() -> void:
	var path := directory.path_join(tag + "-command.json")
	if not FileAccess.file_exists(path): return
	var command = JSON.parse_string(FileAccess.get_file_as_string(path))
	if not command is Dictionary or int(command.get("sequence", -1)) <= last_command: return
	last_command = int(command.sequence)
	var cell := Vector2i(command.get("x", 5), command.get("y", 5))
	match str(command.get("action", "")):
		"edit": room.request_edit(cell, int(command.get("tile", 4)), int(command.get("layer", 0)))
		"raw_edit": room.edit_cell.rpc_id(1, cell, int(command.get("tile", 4)), int(command.get("layer", 0)), int(command.get("expected", 0)))
		"undo": room.request_undo()
		"redo": room.request_undo(true)
		"editor": room.request_editor(bool(command.get("enabled", true)))
		"phase": room.request_phase(str(command.get("mode", "race")))
		"lock": room.request_lock(bool(command.get("locked", true)))
		"kick":
			for id: int in room.people:
				if room.people[id].name == str(command.get("target", "")): room.request_kick(id)
		"restart": room.request_restart()
		"move": room.test_command = {"axis": float(command.get("axis", 0)), "jump": bool(command.get("jump", false)), "sprint": true, "press": false, "climb": 0}
		"rename": room.request_name(str(command.get("username", "Runner")))
		"save": room.request_save()
		"load": room.request_load()
		"disconnect": room.close_connection()
		"reconnect": room.join("127.0.0.1", port, tag)
		"bad_input":
			var generation := int(room.people.get(room.local_id(), {}).get("generation", 0))
			room.submit_inputs.rpc_id(1, room.epoch, generation, PackedInt32Array([1, 999999, 2, 0]))
		"quit": quit()

func report() -> void:
	var people := {}
	var identities := {}
	for id: int in room.people:
		var person: Dictionary = room.people[id]
		var position := Vector2.ZERO
		if room.actors.has(id): position = room.actors[id].position
		elif room.avatars.has(id): position = room.avatars[id].target
		var avatar = room.actors.get(id, room.avatars.get(id))
		var label: Label = avatar.identity_label if avatar is Runner else avatar.label
		identities[str(id)] = {"name": person.name, "color": person.color, "label": label.text,
			"tint": Color(avatar.sprite.material.get_shader_parameter("tint")).to_html(), "texture": avatar.sprite.texture.resource_path}
		people[person.name] = {"id": id, "color": person.color, "x": position.x, "y": position.y, "ack": person.ack, "generation": person.generation,
			"editor": person.editor, "spectator": person.spectator, "finish": person.finish, "deaths": person.deaths}
	var cells := {}
	for cell in [Vector2i(5, 5), Vector2i(6, 5), Vector2i(7, 5), Vector2i(8, 5), Vector2i(9, 5), Vector2i(10, 5), Vector2i(11, 5)]:
		cells["%d:%d" % [cell.x, cell.y]] = room.document.tile(cell)
	var data := {"ready": room.room_ready, "active": room.network_active, "local_id": room.local_id(), "owner": room.owner_id,
		"count": room.people.size(), "people": people, "identities": identities, "revision": room.document.revision, "cells": cells,
		"back": room.document.tile(Vector2i(5, 5), 1), "phase": room.phase, "epoch": room.epoch, "start": room.start_tick,
		"tick": int(room.estimated_tick), "results": room.round_results, "editing": room.guest_editing, "notices": notices,
		"hash": JSON.stringify([Array(room.document.layers[0]), Array(room.document.layers[1]), Array(room.document.versions[0]), Array(room.document.versions[1])]).sha256_text()}
	var path := directory.path_join(tag + ".json")
	var file := FileAccess.open(path + ".tmp", FileAccess.WRITE)
	file.store_string(JSON.stringify(data)); file.close()
	DirAccess.rename_absolute(path + ".tmp", path)

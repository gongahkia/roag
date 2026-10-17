extends Node2D

const RoomScript = preload("res://scripts/room_network.gd")
const Catalog = preload("res://scripts/tile_catalog.gd")
const Document = preload("res://scripts/world_document.gd")
const TileButton = preload("res://scripts/tile_button.gd")
var room: WorkshopRoom
var camera: Camera2D
var jump_pressed := false
var brush := Catalog.Tile.GRASS
var layer := 0
var last_paint := Vector2i(-999, -999)
var last_paint_id := -1
var paint_clock := 0.0
var pointer_cell := Vector2i.ZERO
var editing := false
var panning := false
var popup: PanelContainer
var palette: PanelContainer
var palette_grid: GridContainer
var palette_category: OptionButton
var tile_buttons: Dictionary = {}
var timer_label: Label
var clock_caption: Label
var world_label: Label
var room_label: Label
var info_label: Label
var status_label: Label
var brush_label: Label
var roster: PanelContainer
var roster_text: Label
var kick_choice: OptionButton
var kick_button: Button
var roster_signature := ""
var name_field: LineEdit
var address_field: LineEdit
var port_field: SpinBox
var edit_button: Button
var race_button: Button
var build_button: Button
var lock_button: Button
var layer_button: Button
var save_button: Button
var load_button: Button
var message := "Local world / Host to invite friends / B to build"
var message_until := 0
var testing := false
var particles_enabled := true
var profile := ConfigFile.new()
var color_swatch: ColorRect
var accepted_name := ""
var profile_ready := false

func _ready() -> void:
	if "--solo" in OS.get_cmdline_user_args() and not get_tree().has_meta("solo_launch_consumed"):
		get_tree().set_meta("solo_launch_consumed", true)
		get_tree().change_scene_to_file.call_deferred("res://scenes/solo.tscn")
		return
	process_physics_priority = -100
	testing = "--testing" in OS.get_cmdline_user_args()
	_setup_input()
	room = RoomScript.new()
	room.name = "Room"
	add_child(room)
	room.notice.connect(_notice)
	room.ready_to_play.connect(_room_ready)
	room.roster_changed.connect(_refresh_permissions)
	room.phase_changed.connect(_phase_changed)
	room.document_changed.connect(func(_transaction): queue_redraw())
	camera = Camera2D.new()
	camera.process_callback = Camera2D.CAMERA2D_PROCESS_PHYSICS
	camera.position_smoothing_enabled = false
	add_child(camera)
	_create_ui()
	var options := _options()
	if not testing and not options.has("server"): profile.load("user://player-profile.cfg")
	profile_ready = true
	_set_particles_enabled(bool(profile.get_value("player", "particles", true)))
	if options.has("world"): room.world_path = str(options.world)
	if not testing and not options.has("fresh-world"):
		var saved := Document.load_from(room.world_path)
		if saved:
			room.document = saved
			room.world.install(saved)
			room.local_actor().respawn(saved.spawn())
	var player_name := str(options.get("name", profile.get_value("player", "username", "Runner")))
	name_field.text = player_name
	room.request_name(player_name)
	var port := clampi(int(options.get("port", RoomScript.DEFAULT_PORT)), 1024, 65535)
	port_field.value = port
	if options.has("server"):
		var error := room.host(port, player_name, true)
		if error != OK: get_tree().quit(1)
	elif options.has("host"):
		room.host(port, player_name)
	elif options.has("join"):
		address_field.text = str(options.join)
		room.join(str(options.join), port, player_name)
	_room_ready()
	if options.has("server"): print("REDLINE_SERVER_READY port=%d world=%s" % [port, room.world_path])

func _options() -> Dictionary:
	var result := {}
	for arg: String in OS.get_cmdline_user_args():
		if not arg.begins_with("--"): continue
		var parts := arg.substr(2).split("=", true, 1)
		result[parts[0]] = parts[1] if parts.size() == 2 else true
	return result

func _setup_input() -> void:
	var actions := {"left": [KEY_A, KEY_LEFT], "right": [KEY_D, KEY_RIGHT], "jump": [KEY_SPACE, KEY_Z], "sprint": [KEY_SHIFT, KEY_X], "climb_up": [KEY_W, KEY_UP], "climb_down": [KEY_S, KEY_DOWN]}
	for action: String in actions:
		if not InputMap.has_action(action): InputMap.add_action(action)
		InputMap.action_erase_events(action)
		for code: int in actions[action]:
			var event := InputEventKey.new()
			event.physical_keycode = code
			InputMap.action_add_event(action, event)
	var buttons := {"jump": JOY_BUTTON_A, "sprint": JOY_BUTTON_X, "left": JOY_BUTTON_DPAD_LEFT, "right": JOY_BUTTON_DPAD_RIGHT, "climb_up": JOY_BUTTON_DPAD_UP, "climb_down": JOY_BUTTON_DPAD_DOWN}
	for action: String in buttons:
		var event := InputEventJoypadButton.new()
		event.button_index = buttons[action]
		InputMap.action_add_event(action, event)

func _save_profile() -> void:
	if not profile_ready or testing or "--server" in OS.get_cmdline_user_args(): return
	profile.set_value("player", "username", room.display_name)
	profile.set_value("player", "particles", particles_enabled)
	profile.save("user://player-profile.cfg")

func _set_particles_enabled(value: bool) -> void:
	particles_enabled = value
	get_tree().set_meta("movement_particles", value)
	get_tree().set_group("movement_effects", "enabled", value and DisplayServer.get_name() != "headless")

func _apply_username() -> void:
	name_field.text = RoomScript.clean_name(name_field.text)
	room.request_name(name_field.text)
	name_field.release_focus()
	_save_profile()

func _room_ready() -> void:
	if camera == null: return
	camera.limit_left = 0; camera.limit_top = 0
	camera.limit_right = room.document.width * Catalog.SIZE
	camera.limit_bottom = room.document.height * Catalog.SIZE
	camera.position = room.document.spawn() + Vector2(64, -80)
	camera.reset_smoothing()
	_refresh_permissions()
	_save_profile()

func _phase_changed() -> void:
	editing = false
	if palette: palette.hide()
	last_paint = Vector2i(-999, -999)
	_refresh_permissions()
	_room_ready()

func _notice(text: String) -> void:
	message = text
	message_until = Time.get_ticks_msec() + 6500

func _physics_process(delta: float) -> void:
	if room == null or not room.room_ready: return
	var focused := get_viewport().gui_get_focus_owner() != null
	var person: Dictionary = room.people.get(room.local_id(), {})
	var was_editing := editing
	editing = bool(person.get("editor", false)) and room.can_edit()
	if editing != was_editing:
		palette.visible = editing
		if editing: camera.position.y += 64
		_refresh_permissions()
	var blocked: bool = focused or popup.visible or editing
	room.local_command = {"axis": 0.0 if blocked else Input.get_axis("left", "right"),
		"jump": not blocked and Input.is_action_pressed("jump"), "sprint": not blocked and Input.is_action_pressed("sprint"),
		"press": not blocked and jump_pressed, "climb": 0 if blocked else roundi(Input.get_axis("climb_up", "climb_down"))}
	jump_pressed = false
	if editing and not focused and not popup.visible:
		var pan := Vector2(Input.get_axis("left", "right"), Input.get_axis("climb_up", "climb_down"))
		camera.position += pan * delta * 480
		camera.position.x = clampf(camera.position.x, 0, room.document.width * Catalog.SIZE)
		camera.position.y = clampf(camera.position.y, 0, room.document.height * Catalog.SIZE)
	elif room.local_actor():
		camera.position = room.local_actor().position + Vector2(68, -64)
	paint_clock -= delta
	if editing and not popup.visible and not _over_ui() and not Input.is_physical_key_pressed(KEY_ALT) and paint_clock <= 0:
		if Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT): _paint(brush)
		elif Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT): _paint(0)

func _over_ui() -> bool:
	var mouse := get_viewport().get_mouse_position()
	return mouse.y < 108 or mouse.y > (226 if palette.visible else 328) or (roster.visible and roster.get_global_rect().has_point(mouse))

func _paint(tile_id: int) -> void:
	if not room.document.inside(pointer_cell): return
	if pointer_cell == last_paint and tile_id == last_paint_id: return
	room.request_edit(pointer_cell, tile_id, layer)
	last_paint = pointer_cell
	last_paint_id = tile_id
	paint_clock = 1.0 / 35

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo:
		if event.physical_keycode in [KEY_SPACE, KEY_Z]: jump_pressed = true
		if event.ctrl_pressed:
			match event.physical_keycode:
				KEY_Z: room.request_undo(event.shift_pressed)
				KEY_Y: room.request_undo(true)
				KEY_S: room.request_save()
				KEY_O: room.request_load()
		else:
			match event.physical_keycode:
				KEY_B: room.request_editor(not editing)
				KEY_R: room.request_restart()
				KEY_TAB: roster.visible = not roster.visible
				KEY_ESCAPE: popup.visible = not popup.visible
				KEY_F4: _solo()
				KEY_F7:
					_set_particles_enabled(not particles_enabled)
					_save_profile()
					_notice("Movement particles on" if particles_enabled else "Movement particles off")
				KEY_F6: get_viewport().get_texture().get_image().save_png("user://workshop-screenshot.png")
				KEY_F11: DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED if DisplayServer.window_get_mode() == DisplayServer.WINDOW_MODE_FULLSCREEN else DisplayServer.WINDOW_MODE_FULLSCREEN)
				KEY_Q:
					if popup.visible: get_tree().quit()
				_:
					var slot: int = event.physical_keycode - KEY_1
					var shortcuts := [Catalog.Tile.GRASS, Catalog.Tile.BRICK, Catalog.Tile.GLASS, Catalog.Tile.ICE, Catalog.Tile.SPIKES, Catalog.Tile.SPRING, Catalog.Tile.COIN, Catalog.Tile.CHECKPOINT, Catalog.Tile.FINISH]
					if slot >= 0 and slot < shortcuts.size(): _select_tile(shortcuts[slot])
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_MIDDLE: panning = event.pressed
		if event.pressed and event.button_index in [MOUSE_BUTTON_LEFT, MOUSE_BUTTON_RIGHT]:
			last_paint = Vector2i(-999, -999)
			if event.alt_pressed and editing:
				_select_tile(room.document.tile(pointer_cell, layer))
		if event.pressed and event.button_index in [MOUSE_BUTTON_WHEEL_UP, MOUSE_BUTTON_WHEEL_DOWN]:
			_select_tile(posmod(brush + (-1 if event.button_index == MOUSE_BUTTON_WHEEL_UP else 1), Catalog.NAMES.size()))
	if event is InputEventMouseMotion and editing and panning:
		camera.position -= event.relative / camera.zoom

func _notification(what: int) -> void:
	if what == NOTIFICATION_APPLICATION_FOCUS_OUT and room:
		jump_pressed = false
		room.local_command = {"axis": 0.0, "jump": false, "sprint": false, "press": false, "climb": 0}

func _select_tile(id: int) -> void:
	brush = id
	if id: layer = 1 if Catalog.background(id) else 0
	for key: int in tile_buttons: tile_buttons[key].button_pressed = key == brush
	layer_button.text = "Layer: " + ("BACK" if layer else "FRONT")
	last_paint = Vector2i(-999, -999)
	_refresh_palette()

func _refresh_palette() -> void:
	if not palette_category: return
	var selected := palette_category.get_item_text(palette_category.selected)
	for id: int in tile_buttons:
		tile_buttons[id].visible = selected == "All tiles" or Catalog.category(id) == selected or id == 0

func _refresh_permissions() -> void:
	if not race_button: return
	var person: Dictionary = room.people.get(room.local_id(), {})
	if name_field and not person.is_empty():
		color_swatch.color = RoomScript.COLORS[int(person.color)]
		if DisplayServer.get_name() != "headless": DisplayServer.window_set_title("REDLINE / " + str(person.name))
		if accepted_name != str(person.name):
			accepted_name = person.name
			name_field.text = accepted_name
			_save_profile()
	race_button.disabled = not room.is_owner()
	build_button.disabled = not room.is_owner()
	lock_button.disabled = not room.is_owner()
	save_button.disabled = not room.is_owner()
	load_button.disabled = not room.is_owner() or room.phase != "build"
	edit_button.disabled = not room.can_edit()
	edit_button.text = "B: Play" if editing else "B: Build"
	lock_button.text = "Unlock edits" if not room.guest_editing else "Lock edits"
	if not room.can_edit():
		editing = false
		if palette: palette.hide()
	if kick_button:
		kick_button.disabled = not room.is_owner()
		var signature := str(room.people)
		if signature != roster_signature:
			roster_signature = signature
			kick_choice.clear()
			for id: int in room.people:
				if id != room.owner_id: kick_choice.add_item(room.people[id].name, id)

func _process(_delta: float) -> void:
	if room == null or not timer_label: return
	pointer_cell = Vector2i((get_global_mouse_position() / Catalog.SIZE).floor())
	var elapsed := maxi(0, int(room.estimated_tick) - room.start_tick) if room.phase == "race" else 0
	timer_label.text = RaceSession.format_time(elapsed)
	var person: Dictionary = room.people.get(room.local_id(), {})
	if int(person.get("finish", -1)) >= 0: timer_label.text = RaceSession.format_time(int(person.finish))
	var countdown := room.start_tick - int(room.estimated_tick)
	clock_caption.text = "SERVER RACE CLOCK / map v%d" % room.document.revision if room.phase == "race" else "RACE TIMER / waiting for the host"
	if room.phase == "race" and countdown > 0: clock_caption.text = "STARTING IN %d / EDITS LOCKED" % ceili(countdown / 120.0)
	world_label.text = room.document.title
	room_label.text = "%s / %d OF %d PLAYERS / %s" % ["HOST" if room.authoritative() and room.network_active else "CONNECTED" if room.network_active else "LOCAL", room.people.size(), RoomScript.MAX_PLAYERS, "RACE" if room.phase == "race" else "BUILD TOGETHER"]
	info_label.text = "LATE JOIN / watch until the next round" if person.get("spectator", false) else "FINISHED / host can start another round" if int(person.get("finish", -1)) >= 0 else "W/S: pan  |  drag: paint  |  right-click: erase  |  Ctrl Z/Y: undo/redo" if editing else "A/D move  SHIFT sprint  SPACE jump  W/S ladders  B build  TAB players"
	status_label.text = message if Time.get_ticks_msec() < message_until or message_until == 0 else "Direct IP / UDP %d / %s" % [room.port, "edits open" if room.can_edit() else "edits locked"]
	brush_label.text = "%s / %s / cell %d,%d" % [Catalog.NAMES[brush], "background" if layer else "foreground", pointer_cell.x, pointer_cell.y]
	var lines := "ROOM / %d PLAYERS\n" % room.people.size()
	for id: int in room.people:
		var data: Dictionary = room.people[id]
		lines += "%s%s%s\n" % ["* " if id == room.owner_id else "  ", str(data.name).substr(0, 15), " [B]" if data.editor else ""]
	if not room.round_results.is_empty():
		lines += "\nROUND RESULTS\n"
		var results: Array = room.round_results.values()
		results.sort_custom(func(a, b): return int(a.ticks) < int(b.ticks))
		for result: Dictionary in results:
			lines += "%s  %s\n" % [str(result.name).substr(0, 8), RaceSession.format_time(int(result.ticks))]
	roster_text.text = lines
	queue_redraw()

func _draw() -> void:
	if not editing or not room or not room.document.inside(pointer_cell): return
	var rect := Rect2(Vector2(pointer_cell * Catalog.SIZE), Vector2.ONE * Catalog.SIZE)
	Catalog.draw(self, brush, rect, 0, true)
	draw_rect(rect, Color("e5f8d2"), false, 1.5)

func _button(parent: Node, text: String, callback: Callable, width := 64) -> Button:
	var button := Button.new()
	button.text = text
	button.custom_minimum_size = Vector2(width, 24)
	button.add_theme_font_size_override("font_size", 10)
	button.focus_mode = Control.FOCUS_NONE
	button.pressed.connect(callback)
	parent.add_child(button)
	return button

func _label(parent: Node, at: Vector2, text: String, size := 11, color := Color("b6c9d9")) -> Label:
	var label := Label.new()
	label.position = at
	label.text = text
	label.add_theme_font_size_override("font_size", size)
	label.add_theme_color_override("font_color", color)
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	parent.add_child(label)
	return label

func _panel(parent: Node, at: Vector2, size: Vector2) -> PanelContainer:
	var panel := PanelContainer.new()
	panel.position = at
	panel.size = size
	var style := StyleBoxFlat.new()
	style.bg_color = Color("101d2c")
	style.border_color = Color("3f6276")
	style.set_border_width_all(1)
	style.set_content_margin_all(8)
	panel.add_theme_stylebox_override("panel", style)
	parent.add_child(panel)
	return panel

func _create_ui() -> void:
	var canvas := CanvasLayer.new()
	add_child(canvas)
	var hud := Control.new()
	hud.mouse_filter = Control.MOUSE_FILTER_IGNORE
	canvas.add_child(hud)
	var top := ColorRect.new()
	top.color = Color("09131f")
	top.size = Vector2(640, 108)
	hud.add_child(top)
	_label(hud, Vector2(14, 4), "REDLINE / SHARED WORLDS", 10, Color("81efc4"))
	world_label = _label(hud, Vector2(14, 20), "Shared world", 17, Color("edf5fa"))
	world_label.size.x = 330
	world_label.clip_text = true
	world_label.text_overrun_behavior = TextServer.OVERRUN_TRIM_ELLIPSIS
	room_label = _label(hud, Vector2(14, 44), "", 9)
	timer_label = _label(hud, Vector2(394, -5), "00:00.000", 46, Color("edfff7"))
	clock_caption = _label(hud, Vector2(397, 51), "RACE TIMER", 9, Color("79e6c0"))
	var bar := HBoxContainer.new()
	bar.position = Vector2(12, 76)
	bar.add_theme_constant_override("separation", 4)
	hud.add_child(bar)
	_button(bar, "Host / Join", func(): popup.visible = not popup.visible, 80)
	edit_button = _button(bar, "B: Build", func(): room.request_editor(not editing), 65)
	race_button = _button(bar, "Start race", func(): room.request_phase("race"), 76)
	build_button = _button(bar, "Build mode", func(): room.request_phase("build"), 76)
	save_button = _button(bar, "Save", room.request_save, 42)
	load_button = _button(bar, "Load", room.request_load, 42)
	lock_button = _button(bar, "Lock edits", func(): room.request_lock(room.guest_editing), 76)
	_button(bar, "F4: Solo", _solo, 65)
	info_label = _label(hud, Vector2(14, 111), "", 10, Color("94b9cc"))
	var footer := ColorRect.new()
	footer.position = Vector2(0, 330)
	footer.size = Vector2(640, 30)
	footer.color = Color("09131f")
	hud.add_child(footer)
	status_label = _label(hud, Vector2(12, 333), "", 10)
	_label(hud, Vector2(12, 348), "Avatar placeholders: VVVVVV / Terry Cavanagh. Tile artwork is original.", 8, Color("627c91"))
	_create_palette(hud)
	roster = _panel(hud, Vector2(448, 131), Vector2(180, 184))
	var roster_box := VBoxContainer.new()
	roster.add_child(roster_box)
	var scroll := ScrollContainer.new()
	scroll.custom_minimum_size = Vector2(160, 134)
	roster_box.add_child(scroll)
	roster_text = _label(scroll, Vector2.ZERO, "", 10)
	var kick_row := HBoxContainer.new()
	roster_box.add_child(kick_row)
	kick_choice = OptionButton.new()
	kick_choice.custom_minimum_size.x = 106
	kick_choice.add_theme_font_size_override("font_size", 10)
	kick_row.add_child(kick_choice)
	kick_button = _button(kick_row, "Kick", func():
		if kick_choice.selected >= 0: room.request_kick(kick_choice.get_selected_id()), 42)
	roster.hide()
	_create_connection_popup(hud)
	_refresh_permissions()

func _create_palette(parent: Node) -> void:
	palette = _panel(parent, Vector2(8, 229), Vector2(624, 96))
	var row := HBoxContainer.new()
	palette.add_child(row)
	var tools := VBoxContainer.new()
	tools.custom_minimum_size.x = 134
	row.add_child(tools)
	palette_category = OptionButton.new()
	for title in ["All tiles", "Blocks", "Movement", "Objects", "Backdrop"]: palette_category.add_item(title)
	palette_category.add_theme_font_size_override("font_size", 10)
	palette_category.item_selected.connect(func(_index): _refresh_palette())
	tools.add_child(palette_category)
	layer_button = _button(tools, "Layer: FRONT", func(): layer = 1 - layer; layer_button.text = "Layer: BACK" if layer else "Layer: FRONT", 126)
	brush_label = _label(tools, Vector2.ZERO, "", 8)
	brush_label.custom_minimum_size = Vector2(134, 24)
	brush_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	var scroll := ScrollContainer.new()
	scroll.custom_minimum_size = Vector2(466, 78)
	row.add_child(scroll)
	palette_grid = GridContainer.new()
	palette_grid.columns = 18
	palette_grid.add_theme_constant_override("h_separation", 2)
	palette_grid.add_theme_constant_override("v_separation", 2)
	scroll.add_child(palette_grid)
	for id in Catalog.NAMES.size():
		var button := TileButton.new()
		button.tile_id = id
		button.pressed.connect(_select_tile.bind(id))
		palette_grid.add_child(button)
		tile_buttons[id] = button
	_select_tile(brush)
	palette.hide()

func _create_connection_popup(parent: Node) -> void:
	popup = _panel(parent, Vector2(142, 112), Vector2(356, 205))
	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 7)
	popup.add_child(box)
	_label(box, Vector2.ZERO, "PLAY & BUILD TOGETHER / 16 PLAYERS", 13, Color("80e9bf"))
	var identity_row := HBoxContainer.new()
	box.add_child(identity_row)
	color_swatch = ColorRect.new()
	color_swatch.custom_minimum_size = Vector2(18, 18)
	color_swatch.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	color_swatch.tooltip_text = "Your color is assigned automatically by the room"
	identity_row.add_child(color_swatch)
	name_field = LineEdit.new()
	name_field.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	name_field.add_theme_font_size_override("font_size", 14)
	name_field.placeholder_text = "Username"
	name_field.tooltip_text = "Choose a username (up to 20 characters)"
	name_field.text_submitted.connect(func(_text): _apply_username())
	name_field.text = "Runner"
	name_field.max_length = 20
	identity_row.add_child(name_field)
	_button(identity_row, "Set name", _apply_username, 74)
	var fields := HBoxContainer.new()
	box.add_child(fields)
	address_field = LineEdit.new()
	address_field.text = "127.0.0.1"
	address_field.placeholder_text = "Host IP or hostname"
	address_field.custom_minimum_size.x = 224
	fields.add_child(address_field)
	port_field = SpinBox.new()
	port_field.min_value = 1024; port_field.max_value = 65535
	port_field.value = RoomScript.DEFAULT_PORT
	port_field.custom_minimum_size.x = 100
	fields.add_child(port_field)
	var buttons := HBoxContainer.new()
	box.add_child(buttons)
	_button(buttons, "Host room", func():
		if room.host(int(port_field.value), name_field.text) == OK: popup.hide(), 100)
	_button(buttons, "Join room", func():
		if room.join(address_field.text, int(port_field.value), name_field.text) == OK: popup.hide(), 100)
	_button(buttons, "Close", func(): popup.hide(), 70)
	var exit_row := HBoxContainer.new()
	box.add_child(exit_row)
	_button(exit_row, "Disconnect", func(): room.close_connection(); popup.hide(); _notice("Local copy / disconnected"), 100)
	_button(exit_row, "Quit game", func(): get_tree().quit(), 90)
	var hint := _label(box, Vector2.ZERO, "LAN: use the host’s local IP. Internet: forward UDP 24567.\nSet name: update your username. F7: toggle movement particles.", 9, Color("7892a7"))
	hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	popup.hide()

func _solo() -> void:
	room.close_connection(false)
	get_tree().change_scene_to_file("res://scenes/solo.tscn")

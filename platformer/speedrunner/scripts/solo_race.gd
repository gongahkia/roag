extends Node2D

const Room = preload("res://scripts/room_network.gd")
const Courses = preload("res://scripts/solo_courses.gd")
const Brain = preload("res://scripts/race_bot.gd")
const Hazards = preload("res://scripts/race_hazards.gd")
const NAMES := ["Sprout", "Circuit", "Blaze", "Vex"]
var room: WorkshopRoom
var hazards: Node2D
var camera: Camera2D
var track_choice: OptionButton
var difficulty_choice: OptionButton
var count_choice: OptionButton
var timer_label: Label
var caption: Label
var standings: Label
var progress: Control
var briefing: Label
var overlay: PanelContainer
var overlay_title: Label
var overlay_detail: Label
var start_button: Button
var jump_pressed := false
var racing := false
var paused := false
var testing := false
var particles_enabled := true
var profile := ConfigFile.new()
var current_track := 0

func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	process_physics_priority = -100
	testing = "--testing" in OS.get_cmdline_user_args()
	if not testing: profile.load("user://player-profile.cfg")
	_setup_input()
	room = Room.new()
	room.process_mode = Node.PROCESS_MODE_PAUSABLE
	add_child(room)
	room.set_physics_process(false)
	room.world_path = "user://solo-unused-world.json"
	room.request_name(str(profile.get_value("player", "username", "Runner")))
	hazards = Hazards.new()
	hazards.clock = race_time
	room.add_child(hazards)
	room.hazard_probe = hazards.hits
	camera = Camera2D.new()
	camera.process_callback = Camera2D.CAMERA2D_PROCESS_PHYSICS
	add_child(camera)
	_create_ui()
	_load_preview()
	particles_enabled = bool(profile.get_value("player", "particles", true))
	_apply_particles()
	DisplayServer.window_set_title("REDLINE / Solo racing")
	for argument: String in OS.get_cmdline_user_args():
		if argument.begins_with("--track="): track_choice.select(clampi(int(argument.get_slice("=", 1)) - 1, 0, 2))
		elif argument.begins_with("--bots="): count_choice.select(clampi(int(argument.get_slice("=", 1)), 0, 4))
		elif argument.begins_with("--difficulty="):
			var value := argument.get_slice("=", 1).to_lower()
			var index := ["mixed", "easy", "normal", "hard", "expert"].find(value)
			if index >= 0: difficulty_choice.select(index)
	_load_preview()
	if "--autostart" in OS.get_cmdline_user_args(): start_race()

func _setup_input() -> void:
	var keys := {"left": [KEY_A, KEY_LEFT], "right": [KEY_D, KEY_RIGHT], "jump": [KEY_SPACE, KEY_Z, KEY_UP], "sprint": [KEY_SHIFT, KEY_X]}
	for action: String in keys:
		if not InputMap.has_action(action): InputMap.add_action(action)
		InputMap.action_erase_events(action)
		for code: int in keys[action]:
			var event := InputEventKey.new()
			event.physical_keycode = code
			InputMap.action_add_event(action, event)
	for action: String in {"jump": JOY_BUTTON_A, "sprint": JOY_BUTTON_X, "left": JOY_BUTTON_DPAD_LEFT, "right": JOY_BUTTON_DPAD_RIGHT}:
		var event := InputEventJoypadButton.new()
		event.button_index = {"jump": JOY_BUTTON_A, "sprint": JOY_BUTTON_X, "left": JOY_BUTTON_DPAD_LEFT, "right": JOY_BUTTON_DPAD_RIGHT}[action]
		InputMap.action_add_event(action, event)

func _load_preview() -> void:
	if racing: return
	var course := Courses.course(track_choice.selected)
	room.install_document(course.document)
	hazards.definitions = course.saws
	briefing.text = course.description
	_center_camera()

func start_race() -> void:
	_set_paused(false)
	room.set_physics_process(false)
	room.clear_local_bots()
	current_track = track_choice.selected
	var course := Courses.course(current_track)
	room.install_document(course.document)
	hazards.definitions = course.saws
	var count := count_choice.selected
	for i in count:
		var tier := i % 4 if difficulty_choice.selected == 0 else difficulty_choice.selected - 1
		var brain := Brain.new(room.world, tier, hazards.hits)
		room.add_local_bot(NAMES[tier] + " / " + Brain.TIERS[tier], brain)
	room.request_phase("race")
	room.set_physics_process(true)
	racing = true
	jump_pressed = false
	overlay.hide()
	start_button.text = "New race"
	briefing.text = "A/D move  SPACE jump  SHIFT sprint  R respawn  ESC pause  TAB standings"
	_center_camera()
	if not testing:
		profile.set_value("solo", "track", current_track)
		profile.set_value("solo", "difficulty", difficulty_choice.selected)
		profile.set_value("solo", "opponents", count)
		profile.save("user://player-profile.cfg")

func race_time() -> float:
	return maxf(0, room.server_tick - room.start_tick) / 120.0 if racing else 0.0

func _set_paused(value: bool) -> void:
	paused = value
	get_tree().paused = value
	if overlay:
		overlay.visible = paused or not racing
		overlay_title.text = "PAUSED" if paused else "SOLO / RACE THE RIVALS"
		overlay_detail.text = "Escape to resume. Enter starts a fresh race.\nF3: shared worlds   F5: solo training" if paused else "Choose a course, rival tier and opponent count above.\nMixed gives you all four skill levels. Zero bots is a time trial.\nPress Enter or Start race."

func _physics_process(_delta: float) -> void:
	if room == null: return
	var blocked := paused or not racing or get_viewport().gui_get_focus_owner() != null
	room.local_command = {"axis": 0.0 if blocked else Input.get_axis("left", "right"),
		"jump": not blocked and Input.is_action_pressed("jump"), "sprint": not blocked and Input.is_action_pressed("sprint"),
		"press": not blocked and jump_pressed, "climb": 0}
	jump_pressed = false
	if room.local_actor(): camera.position = room.local_actor().position + Vector2(70, -70)

func _center_camera() -> void:
	camera.limit_left = 0; camera.limit_top = 0
	camera.limit_right = room.document.width * 24
	camera.limit_bottom = room.document.height * 24
	camera.position = room.document.spawn() + Vector2(70, -70)
	camera.reset_smoothing()

func _unhandled_input(event: InputEvent) -> void:
	if not event is InputEventKey or not event.pressed or event.echo: return
	match event.physical_keycode:
		KEY_SPACE, KEY_Z, KEY_UP: jump_pressed = true
		KEY_ENTER: start_race()
		KEY_R:
			if racing and not paused: room.request_restart()
		KEY_ESCAPE, KEY_P:
			if racing: _set_paused(not paused)
		KEY_TAB: standings.visible = not standings.visible
		KEY_F3: _leave("res://scenes/workshop.tscn")
		KEY_F5: _leave("res://scenes/main.tscn")
		KEY_F6: get_viewport().get_texture().get_image().save_png("user://solo-screenshot.png")
		KEY_F7:
			particles_enabled = not particles_enabled
			_apply_particles()
			if not testing:
				profile.set_value("player", "particles", particles_enabled)
				profile.save("user://player-profile.cfg")
		KEY_F11: DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED if DisplayServer.window_get_mode() == DisplayServer.WINDOW_MODE_FULLSCREEN else DisplayServer.WINDOW_MODE_FULLSCREEN)
		KEY_Q:
			if paused: get_tree().quit()

func _apply_particles() -> void:
	get_tree().set_meta("movement_particles", particles_enabled)
	get_tree().set_group("movement_effects", "enabled", particles_enabled and DisplayServer.get_name() != "headless")

func _notification(what: int) -> void:
	if what == NOTIFICATION_APPLICATION_FOCUS_OUT and racing and not testing: _set_paused(true)

func _leave(scene: String) -> void:
	get_tree().paused = false
	get_tree().change_scene_to_file(scene)

func _process(_delta: float) -> void:
	if room == null or timer_label == null: return
	var local: Dictionary = room.people[1]
	var elapsed := maxi(0, room.server_tick - room.start_tick) if racing else 0
	timer_label.text = RaceSession.format_time(local.finish if int(local.finish) >= 0 else elapsed)
	var sorted := ordered_racers()
	var place := sorted.find(1) + 1
	var countdown := room.start_tick - room.server_tick
	caption.text = "SOLO / READY TO RACE"
	if racing:
		caption.text = "STARTING IN %d" % ceili(countdown / 120.0) if countdown > 0 else "PLACE %d / %d  •  DEATHS %d" % [place, sorted.size(), local.deaths]
		if int(local.finish) >= 0: caption.text = "FINISHED %d / %d  •  ENTER: RACE AGAIN" % [place, sorted.size()]
	var lines := "RACE STANDINGS"
	for rank in sorted.size():
		var id: int = sorted[rank]
		var person: Dictionary = room.people[id]
		lines += "\n%d  %s%s\n" % [rank + 1, person.name, " / YOU" if id == 1 else ""]
		if int(person.finish) >= 0: lines += "    " + RaceSession.format_time(person.finish)
		elif room.bot_controllers.has(id): lines += "    " + room.bot_controllers[id].state
		else: lines += "    Racing" if racing else "    Ready"
	standings.text = lines
	progress.visible = racing

func ordered_racers() -> Array:
	var ids: Array = room.people.keys()
	ids.sort_custom(func(a, b):
		var a_finish := int(room.people[a].finish)
		var b_finish := int(room.people[b].finish)
		if a_finish >= 0 or b_finish >= 0:
			if a_finish < 0: return false
			if b_finish < 0: return true
			return a_finish < b_finish if a_finish != b_finish else int(a) > int(b)
		return room.actors[a].position.x > room.actors[b].position.x if room.actors[a].position.x != room.actors[b].position.x else int(a) > int(b))
	return ids

func _label(parent: Node, text: String, at: Vector2, size := 11, color := Color("b6c9d9")) -> Label:
	var label := Label.new()
	label.text = text; label.position = at
	label.add_theme_font_size_override("font_size", size)
	label.add_theme_color_override("font_color", color)
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	parent.add_child(label)
	return label

func _button(parent: Node, text: String, callback: Callable, width := 64) -> Button:
	var button := Button.new()
	button.text = text
	button.custom_minimum_size = Vector2(width, 24)
	button.add_theme_font_size_override("font_size", 10)
	button.focus_mode = Control.FOCUS_NONE
	button.pressed.connect(callback)
	parent.add_child(button)
	return button

func _choice(parent: Node, values: Array, width: int) -> OptionButton:
	var choice := OptionButton.new()
	choice.custom_minimum_size = Vector2(width, 24)
	choice.add_theme_font_size_override("font_size", 10)
	choice.focus_mode = Control.FOCUS_NONE
	for value: String in values: choice.add_item(value)
	parent.add_child(choice)
	return choice

func _create_ui() -> void:
	var canvas := CanvasLayer.new()
	add_child(canvas)
	var hud := Control.new()
	hud.mouse_filter = Control.MOUSE_FILTER_IGNORE
	canvas.add_child(hud)
	var top := ColorRect.new()
	top.color = Color("09131f"); top.size = Vector2(640, 108)
	hud.add_child(top)
	_label(hud, "REDLINE / SINGLE PLAYER", Vector2(14, 4), 10, Color("81efc4"))
	_label(hud, "RACE THE RIVALS", Vector2(14, 22), 20, Color("edf5fa"))
	_label(hud, "OFFLINE / SAME PHYSICS / FOUR AI TIERS", Vector2(14, 50), 9)
	timer_label = _label(hud, "00:00.000", Vector2(394, -5), 46, Color("edfff7"))
	caption = _label(hud, "", Vector2(395, 51), 9, Color("79e6c0"))
	var toolbar := HBoxContainer.new()
	toolbar.position = Vector2(12, 76)
	toolbar.add_theme_constant_override("separation", 4)
	hud.add_child(toolbar)
	track_choice = _choice(toolbar, Courses.titles(), 132)
	difficulty_choice = _choice(toolbar, ["Mixed rivals", "Easy", "Normal", "Hard", "Expert"], 100)
	count_choice = _choice(toolbar, ["0 bots", "1 bot", "2 bots", "3 bots", "4 bots"], 64)
	track_choice.select(clampi(int(profile.get_value("solo", "track", 0)), 0, 2))
	difficulty_choice.select(clampi(int(profile.get_value("solo", "difficulty", 0)), 0, 4))
	count_choice.select(clampi(int(profile.get_value("solo", "opponents", 4)), 0, 4))
	track_choice.item_selected.connect(func(_index): _load_preview())
	start_button = _button(toolbar, "Start race", start_race, 74)
	_button(toolbar, "F5: Train", func(): _leave("res://scenes/main.tscn"), 68)
	_button(toolbar, "F3: Worlds", func(): _leave("res://scenes/workshop.tscn"), 76)
	briefing = _label(hud, "", Vector2(14, 111), 9)
	standings = _label(hud, "", Vector2(444, 137), 9)
	standings.add_theme_constant_override("line_spacing", -1)
	standings.add_theme_color_override("font_shadow_color", Color("09131f"))
	standings.add_theme_constant_override("shadow_offset_x", 1)
	standings.add_theme_constant_override("shadow_offset_y", 1)
	progress = preload("res://scripts/race_progress.gd").new()
	progress.room = room
	progress.position = Vector2(16, 322)
	progress.visible = false
	hud.add_child(progress)
	var footer := ColorRect.new()
	footer.position = Vector2(0, 332); footer.size = Vector2(640, 28); footer.color = Color("09131f")
	hud.add_child(footer)
	_label(hud, "R: checkpoint respawn / race clock continues. Enter: new race. F7: particles.", Vector2(12, 335), 10)
	_label(hud, "Avatar placeholders: VVVVVV / Terry Cavanagh. Solo races do not write training PBs.", Vector2(12, 349), 8, Color("627c91"))
	overlay = PanelContainer.new()
	overlay.position = Vector2(96, 170); overlay.size = Vector2(448, 130)
	var style := StyleBoxFlat.new()
	style.bg_color = Color("101d2c"); style.border_color = Color("3f6276")
	style.set_border_width_all(1); style.set_content_margin_all(12)
	overlay.add_theme_stylebox_override("panel", style)
	hud.add_child(overlay)
	var box := VBoxContainer.new()
	overlay.add_child(box)
	overlay_title = _label(box, "SOLO / RACE THE RIVALS", Vector2.ZERO, 18, Color("81efc4"))
	overlay_detail = _label(box, "", Vector2.ZERO, 11)
	_set_paused(false)

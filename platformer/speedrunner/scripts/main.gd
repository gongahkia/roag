extends Node2D

const PlayerScript = preload("res://scripts/player.gd")
const SessionScript = preload("res://scripts/race_session.gd")
const CoursesScript = preload("res://scripts/course_data.gd")
const SoundScript = preload("res://scripts/sound.gd")
var courses: Array[Dictionary] = CoursesScript.courses()
var course_index := 0
var course: Dictionary
var player: Runner
var session: RaceSession = SessionScript.new()
var geometry: Node2D
var camera: Camera2D
var ghost: Sprite2D
var paused := false
var show_ghost := true
var checkpoint := Vector2.ZERO
var checkpoint_index := -1
var death_flash := 0.0
var world_time := 0.0
var testing := false
var hud: Control
var heading: Label
var timer_label: Label
var pb_label: Label
var status_label: Label
var speed_label: Label
var message_label: Label
var result: PanelContainer
var result_title: Label
var result_time: Label
var result_detail: Label
var credit_label: Label
var flash: ColorRect
var sounds: Dictionary = {}
var muted := false

func _ready() -> void:
	process_physics_priority = 100
	testing = "--testing" in OS.get_cmdline_user_args()
	_setup_input()
	if not testing:
		session.load_records()
	_create_hud()
	_create_sounds()
	load_course(0)

func _setup_input() -> void:
	var keys := {
		"left": [KEY_A, KEY_LEFT], "right": [KEY_D, KEY_RIGHT],
		"jump": [KEY_SPACE, KEY_Z, KEY_UP], "sprint": [KEY_SHIFT, KEY_X],
		"retry": [KEY_R], "pause": [KEY_ESCAPE], "practice": [KEY_P],
		"next": [KEY_TAB, KEY_BRACKETRIGHT], "previous": [KEY_BRACKETLEFT],
		"ghost": [KEY_G], "confirm": [KEY_ENTER], "quit_game": [KEY_Q],
	}
	for action: String in keys:
		if not InputMap.has_action(action):
			InputMap.add_action(action)
		for code in keys[action]:
			var event := InputEventKey.new()
			event.physical_keycode = code
			InputMap.action_add_event(action, event)
	var buttons := {"jump": JOY_BUTTON_A, "sprint": JOY_BUTTON_X, "retry": JOY_BUTTON_BACK, "pause": JOY_BUTTON_START, "left": JOY_BUTTON_DPAD_LEFT, "right": JOY_BUTTON_DPAD_RIGHT}
	for action: String in buttons:
		var button := InputEventJoypadButton.new()
		button.button_index = buttons[action]
		InputMap.action_add_event(action, button)
	for direction in [-1, 1]:
		var axis := InputEventJoypadMotion.new()
		axis.axis = JOY_AXIS_LEFT_X
		axis.axis_value = direction
		InputMap.action_add_event("left" if direction < 0 else "right", axis)

func load_course(index: int) -> void:
	course_index = posmod(index, courses.size())
	course = courses[course_index]
	if is_instance_valid(player):
		remove_child(player)
		player.queue_free()
	if is_instance_valid(geometry):
		remove_child(geometry)
		geometry.queue_free()
	geometry = Node2D.new()
	geometry.name = "CourseGeometry"
	add_child(geometry)
	for platform: Rect2 in course["platforms"]:
		var body := StaticBody2D.new()
		body.position = platform.get_center()
		var collision := CollisionShape2D.new()
		var shape := RectangleShape2D.new()
		shape.size = platform.size
		collision.shape = shape
		body.add_child(collision)
		geometry.add_child(body)
	player = PlayerScript.new()
	add_child(player)
	player.jumped.connect(func(wall: bool): _play_sound("wall" if wall else "jump"))
	player.respawn(course["spawn"])
	camera = Camera2D.new()
	camera.process_callback = Camera2D.CAMERA2D_PROCESS_PHYSICS
	camera.position = Vector2(76, -35)
	camera.position_smoothing_enabled = true
	camera.position_smoothing_speed = 12.0
	camera.limit_left = 0
	camera.limit_top = 0
	camera.limit_right = int(course["size"].x)
	camera.limit_bottom = int(course["size"].y)
	player.add_child(camera)
	camera.reset_smoothing()
	if is_instance_valid(ghost):
		ghost.queue_free()
	ghost = Sprite2D.new()
	ghost.texture = player.sprite.texture
	ghost.hframes = player.sprite.hframes
	ghost.vframes = player.sprite.vframes
	ghost.modulate = Color(0.7, 0.65, 1.0, 0.35)
	ghost.z_index = -1
	add_child(ghost)
	checkpoint = course["spawn"]
	checkpoint_index = -1
	paused = false
	world_time = 0.0
	session.begin(course["id"])
	result.hide()
	queue_redraw()

func retry() -> void:
	session.retry()
	player.respawn(checkpoint if session.practice else course["spawn"])
	paused = false
	player.controls_enabled = true
	# Hazards restart from the same phase on every attempt.
	world_time = 0.0
	result.hide()
	camera.reset_smoothing()

func _unhandled_input(event: InputEvent) -> void:
	if event.is_echo():
		return
	if event.is_action_pressed("retry") or (session.finished and event.is_action_pressed("confirm")):
		retry()
	elif event.is_action_pressed("next"):
		load_course(course_index + 1)
	elif event.is_action_pressed("previous"):
		load_course(course_index - 1)
	elif event.is_action_pressed("ghost"):
		show_ghost = not show_ghost
	elif event.is_action_pressed("practice"):
		session.practice = not session.practice
		checkpoint = course["spawn"]
		checkpoint_index = -1
		retry()
	elif event.is_action_pressed("pause"):
		_set_paused(not paused)
	elif paused and event.is_action_pressed("quit_game"):
		get_tree().quit()
	elif event is InputEventKey and event.pressed and event.keycode == KEY_F11:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED if DisplayServer.window_get_mode() == DisplayServer.WINDOW_MODE_FULLSCREEN else DisplayServer.WINDOW_MODE_FULLSCREEN)
	elif event is InputEventKey and event.pressed and event.keycode == KEY_M:
		muted = not muted
	elif event is InputEventKey and event.pressed and event.keycode == KEY_F6:
		get_viewport().get_texture().get_image().save_png("user://screenshot.png")
	elif event is InputEventKey and event.pressed and event.keycode == KEY_F3:
		get_tree().change_scene_to_file("res://scenes/workshop.tscn")

func _set_paused(value: bool) -> void:
	paused = value
	if session.started and not session.finished:
		session.valid = false
	player.controls_enabled = not paused and not session.finished

func _notification(what: int) -> void:
	if what == NOTIFICATION_APPLICATION_FOCUS_OUT and is_instance_valid(player) and session.started and not session.finished:
		_set_paused(true)

func saw_position(saw: Array) -> Vector2:
	return saw[0] + saw[1] * sin(world_time * TAU / float(saw[2]))

func _physics_process(delta: float) -> void:
	if not is_instance_valid(player) or paused or session.finished:
		return
	if not session.started and (absf(player.velocity.x) > 1 or player.velocity.y < -1):
		session.started = true
	if session.started:
		world_time += delta
	var bounds := Rect2(player.position - Vector2(6, 10), Vector2(12, 20))
	var dead: bool = player.position.y > course["size"].y + 48 or player.position.x < -16
	for spike: Rect2 in course["spikes"]:
		# Tips and edges have a small forgiving inset from the drawn hazard.
		if bounds.intersects(spike.grow(-2)):
			dead = true
	for saw: Array in course["saws"]:
		var nearest := saw_position(saw).clamp(bounds.position, bounds.end)
		if nearest.distance_to(saw_position(saw)) < float(saw[3]) - 2.0:
			dead = true
	if dead:
		session.deaths += 1
		death_flash = 0.14
		_play_sound("death")
		retry()
		return
	if session.practice:
		var flags: Array = course["checkpoints"]
		for i in flags.size():
			if i > checkpoint_index and player.position.distance_to(flags[i]) < 28:
				checkpoint_index = i
				checkpoint = flags[i]
	session.tick(player.position, player.frame)
	if bounds.intersects(course["goal"]):
		session.finish()
		_play_sound("clear")
		player.controls_enabled = false
		_show_result()

func _process(delta: float) -> void:
	death_flash = maxf(0.0, death_flash - delta)
	if is_instance_valid(flash):
		flash.color = Color(1.0, 0.25, 0.3, death_flash)
	_update_hud()
	if is_instance_valid(ghost):
		var sample := session.ghost_frame(session.ticks - 1)
		ghost.visible = show_ghost and not session.practice and not sample.is_empty() and session.started
		if ghost.visible:
			ghost.position = Vector2(sample[0], sample[1]) * 0.01 + Vector2(0, 5)
			ghost.frame = int(sample[2])
	queue_redraw()

func _draw() -> void:
	if course.is_empty():
		return
	var accent: Color = course["accent"]
	var extent: Vector2 = course["size"]
	for x in range(0, int(extent.x), 32):
		draw_line(Vector2(x, 0), Vector2(x, extent.y), Color("101b27"))
	for y in range(0, int(extent.y), 32):
		draw_line(Vector2(0, y), Vector2(extent.x, y), Color("101b27"))
	for platform: Rect2 in course["platforms"]:
		draw_rect(platform, Color("182838"))
		draw_rect(platform, Color("2a4053"), false, 1.0)
		draw_line(platform.position, platform.position + Vector2(platform.size.x, 0), accent, 2.0)
		for x in range(int(platform.position.x) + 8, int(platform.end.x) - 4, 16):
			draw_line(Vector2(x, platform.position.y + 8), Vector2(x + 4, platform.position.y + 8), Color("2d4658"))
	for spike: Rect2 in course["spikes"]:
		for x in range(int(spike.position.x), int(spike.end.x), 8):
			draw_colored_polygon(PackedVector2Array([Vector2(x, spike.end.y), Vector2(x + 4, spike.position.y), Vector2(x + 8, spike.end.y)]), Color("ff7289"))
	for saw: Array in course["saws"]:
		var center := saw_position(saw)
		var radius := float(saw[3])
		draw_line(saw[0] - saw[1], saw[0] + saw[1], Color("6d354b"), 1.0)
		var points := PackedVector2Array()
		for tooth in 24:
			var angle := tooth * TAU / 24.0 + world_time * 4.0
			points.append(center + Vector2.from_angle(angle) * (radius if tooth % 2 else radius - 4))
		draw_colored_polygon(points, Color("ff7289"))
		draw_circle(center, 4.0, Color("172333"))
	var goal: Rect2 = course["goal"]
	draw_rect(goal.grow(5), Color(accent, 0.08))
	draw_rect(goal, Color(accent, 0.14))
	draw_rect(goal, accent, false, 1.5)
	for y in range(int(goal.position.y) + 3, int(goal.end.y) - 2, 6):
		draw_line(Vector2(goal.position.x + 3, y), Vector2(goal.end.x - 3, y), Color(accent, 0.5))
	if session.practice:
		for flag: Vector2 in course["checkpoints"]:
			draw_line(flag + Vector2(0, -20), flag + Vector2(0, 10), accent, 1.5)
			draw_colored_polygon(PackedVector2Array([flag + Vector2(0, -20), flag + Vector2(15, -15), flag + Vector2(0, -10)]), accent)
	var font := ThemeDB.fallback_font
	for sign_data: Array in course["signs"]:
		draw_string(font, sign_data[0], sign_data[1], HORIZONTAL_ALIGNMENT_LEFT, -1, 10, Color(accent, 0.55))
	if player.velocity.length() > 300 and not session.finished:
		var start := player.position + Vector2(-player.facing * 12, 2)
		for i in 3:
			draw_line(start + Vector2(-player.facing * (5 + i * 3), i * 4 - 4), start + Vector2(-player.facing * (17 + i * 4), i * 4 - 4), Color(accent, 0.45 - i * 0.1), 1.0)

func _label(parent: Node, at: Vector2, size: int, color: Color) -> Label:
	var label := Label.new()
	label.position = at
	label.add_theme_font_size_override("font_size", size)
	label.add_theme_color_override("font_color", color)
	parent.add_child(label)
	return label

func _create_sounds() -> void:
	var cues := {"jump": [520.0, 800.0, 0.07], "wall": [760.0, 1150.0, 0.08], "death": [190.0, 55.0, 0.12], "clear": [650.0, 1300.0, 0.20]}
	for cue: String in cues:
		var sound := AudioStreamPlayer.new()
		var data: Array = cues[cue]
		sound.stream = SoundScript.tone(data[0], data[1], data[2])
		sound.volume_db = -12.0
		add_child(sound)
		sounds[cue] = sound

func _play_sound(cue: String) -> void:
	if not muted and not testing:
		sounds[cue].play()

func _create_hud() -> void:
	var canvas := CanvasLayer.new()
	add_child(canvas)
	hud = Control.new()
	hud.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	hud.mouse_filter = Control.MOUSE_FILTER_IGNORE
	canvas.add_child(hud)
	var top := ColorRect.new()
	top.color = Color(0.035, 0.047, 0.07, 0.94)
	top.size = Vector2(640, 76)
	hud.add_child(top)
	_label(hud, Vector2(18, 10), 11, Color("6cecc6")).text = "R E D L I N E   /   PRECISION PLATFORMER"
	heading = _label(hud, Vector2(18, 28), 17, Color("edf4f7"))
	timer_label = _label(hud, Vector2(414, -4), 42, Color("edf4f7"))
	pb_label = _label(hud, Vector2(422, 54), 11, Color("879eb1"))
	var workshop_button := Button.new()
	workshop_button.text = "F3 / MULTIPLAYER + BUILD"
	workshop_button.position = Vector2(18, 51)
	workshop_button.add_theme_font_size_override("font_size", 10)
	workshop_button.focus_mode = Control.FOCUS_NONE
	workshop_button.pressed.connect(func(): get_tree().change_scene_to_file("res://scenes/workshop.tscn"))
	hud.add_child(workshop_button)
	var bottom := ColorRect.new()
	bottom.color = Color(0.035, 0.047, 0.07, 0.94)
	bottom.position = Vector2(0, 330)
	bottom.size = Vector2(640, 30)
	hud.add_child(bottom)
	status_label = _label(hud, Vector2(18, 336), 10, Color("9bb0c1"))
	speed_label = _label(hud, Vector2(512, 336), 10, Color("6cecc6"))
	message_label = _label(hud, Vector2(18, 80), 12, Color("b4c8d6"))
	credit_label = _label(hud, Vector2(18, 98), 9, Color("607b90"))
	credit_label.text = "PLACEHOLDER SPRITES / VVVVVV / TERRY CAVANAGH"
	flash = ColorRect.new()
	flash.size = Vector2(640, 360)
	flash.mouse_filter = Control.MOUSE_FILTER_IGNORE
	flash.color = Color.TRANSPARENT
	hud.add_child(flash)
	result = PanelContainer.new()
	result.position = Vector2(150, 96)
	result.custom_minimum_size = Vector2(340, 178)
	var style := StyleBoxFlat.new()
	style.bg_color = Color("101b29")
	style.border_color = Color("6cecc6")
	style.set_border_width_all(1)
	style.set_content_margin_all(20)
	result.add_theme_stylebox_override("panel", style)
	hud.add_child(result)
	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 8)
	result.add_child(box)
	result_title = _label(box, Vector2.ZERO, 16, Color("6cecc6"))
	result_time = _label(box, Vector2.ZERO, 32, Color("edf4f7"))
	result_detail = _label(box, Vector2.ZERO, 11, Color("9bb0c1"))
	_label(box, Vector2.ZERO, 12, Color("edf4f7")).text = "R / ENTER retry     TAB next course"
	result.hide()

func _show_result() -> void:
	result_title.text = "PRACTICE CLEAR" if session.practice else "NEW PERSONAL BEST" if session.new_best else "COURSE CLEAR"
	result_time.text = RaceSession.format_time(session.ticks)
	result_detail.text = "Unranked: run paused" if not session.valid and not session.practice else "Practice: records disabled" if session.practice else "Save failed: PB is in memory" if session.save_failed else "Clean line. Take another tenth."
	result.show()

func _update_hud() -> void:
	if not is_instance_valid(player):
		return
	heading.text = course["title"]
	heading.modulate = course["accent"]
	timer_label.text = RaceSession.format_time(session.ticks)
	pb_label.text = "PB " + (RaceSession.format_time(session.best_ticks()) if session.best_ticks() else "--:--.---") + ("  / GHOST" if show_ghost else "")
	status_label.text = "R retry   TAB course   P practice   G ghost   ESC pause"
	speed_label.text = "%03d px/s  /  D:%d" % [absf(player.velocity.x), session.deaths]
	if paused:
		message_label.text = "PAUSED / ESC resume   R fresh attempt   Q quit"
	elif session.practice:
		message_label.text = "PRACTICE / checkpoints on / records off"
	elif not session.valid:
		message_label.text = "UNRANKED / R to start a valid attempt"
	elif not session.started:
		message_label.text = "A/D move   SHIFT sprint   SPACE jump / hold for height / wall jump"
	else:
		message_label.text = ""
	credit_label.visible = paused or not session.started

class_name TileWorld
extends Node2D

const Catalog = preload("res://scripts/tile_catalog.gd")
const Document = preload("res://scripts/world_document.gd")
var document: WorldDocument
var bodies: Dictionary = {}
var doors := {"red": false, "blue": false}
var time := 0.0

func install(value: WorldDocument) -> void:
	document = value
	for child in get_children():
		remove_child(child)
		child.queue_free()
	bodies.clear()
	for y in document.height:
		for x in document.width: rebuild(Vector2i(x, y))
	queue_redraw()

func rebuild(cell: Vector2i) -> void:
	if bodies.has(cell):
		var body: StaticBody2D = bodies[cell]
		remove_child(body)
		body.queue_free()
		bodies.erase(cell)
	var id := document.tile(cell)
	if (id == Catalog.Tile.DOOR_RED and doors.red) or (id == Catalog.Tile.DOOR_BLUE and doors.blue): return
	if not Catalog.solid(id) and id != Catalog.Tile.PLATFORM: return
	var body := StaticBody2D.new()
	body.position = Vector2(cell * Catalog.SIZE) + Vector2.ONE * Catalog.SIZE / 2
	var collision := CollisionShape2D.new()
	var shape := RectangleShape2D.new()
	shape.size = Vector2.ONE * Catalog.SIZE
	if id == Catalog.Tile.PLATFORM:
		shape.size.y = 5
		collision.position.y = -Catalog.SIZE / 2.0 + 2.5
		collision.one_way_collision = true
		collision.one_way_collision_margin = 1.0
	collision.shape = shape
	body.add_child(collision)
	add_child(body)
	bodies[cell] = body

func changed(transaction: Dictionary) -> void:
	for edit: Array in transaction.edits:
		if int(edit[2]) == 0: rebuild(Vector2i(edit[0], edit[1]))
	queue_redraw()

func set_doors(color_name: String, opened: bool) -> void:
	if doors.get(color_name) == opened: return
	doors[color_name] = opened
	var id := Catalog.Tile.DOOR_RED if color_name == "red" else Catalog.Tile.DOOR_BLUE
	for index in document.layers[0].size():
		if document.layers[0][index] == id: rebuild(Vector2i(index % document.width, index / document.width))
	queue_redraw()

func at(position_in_world: Vector2) -> int:
	return document.tile(Vector2i((position_in_world / Catalog.SIZE).floor()))

func environment_at(actor: Runner) -> Dictionary:
	var result: Dictionary = {}
	var body_tile := at(actor.position)
	var floor_tile := at(actor.position + Vector2(0, 12))
	if body_tile == Catalog.Tile.WATER: result.merge({"water": true, "gravity": 0.3, "speed": 0.55})
	if body_tile == Catalog.Tile.LADDER: result["ladder"] = true
	if floor_tile == Catalog.Tile.ICE: result.merge({"friction": 0.06, "traction": 0.3})
	if floor_tile == Catalog.Tile.BELT_RIGHT: result["belt"] = 135.0
	if floor_tile == Catalog.Tile.BELT_LEFT: result["belt"] = -135.0
	if body_tile == Catalog.Tile.WIND_UP: result["force"] = Vector2(0, -3200)
	if body_tile == Catalog.Tile.WIND_LEFT: result["force"] = Vector2(-1800, 0)
	if body_tile == Catalog.Tile.WIND_RIGHT: result["force"] = Vector2(1800, 0)
	return result

func touches(actor: Runner) -> Array:
	var result: Array = []
	var bounds := Rect2(actor.position - Vector2(6, 10), Vector2(12, 20))
	var start := Vector2i((bounds.position / Catalog.SIZE).floor())
	var end := Vector2i((bounds.end / Catalog.SIZE).floor())
	for y in range(start.y, end.y + 1):
		for x in range(start.x, end.x + 1):
			var cell := Vector2i(x, y)
			var id := document.tile(cell)
			if id == 0: continue
			var tile_bounds := Rect2(Vector2(cell * Catalog.SIZE), Vector2.ONE * Catalog.SIZE)
			if id == Catalog.Tile.SPIKES: tile_bounds = Rect2(tile_bounds.position + Vector2(2, 6), Vector2(20, 18))
			elif id == Catalog.Tile.SAW: tile_bounds = tile_bounds.grow(-4)
			if bounds.intersects(tile_bounds): result.append([cell, id])
	return result

func apply_spring(actor: Runner) -> void:
	for hit: Array in touches(actor):
		if int(hit[1]) == Catalog.Tile.SPRING and actor.velocity.y >= 0:
			actor.velocity.y = -840
			actor.external_launch = true

func _process(delta: float) -> void:
	time += delta
	queue_redraw()

func _draw() -> void:
	if document == null: return
	var inverse := get_canvas_transform().affine_inverse()
	var top := inverse * Vector2.ZERO
	var bottom := inverse * get_viewport_rect().size
	var start := Vector2i((top / Catalog.SIZE).floor()) - Vector2i.ONE
	var end := Vector2i((bottom / Catalog.SIZE).ceil()) + Vector2i.ONE
	for y in range(maxi(0, start.y), mini(document.height, end.y)):
		for x in range(maxi(0, start.x), mini(document.width, end.x)):
			var cell := Vector2i(x, y)
			var rect := Rect2(Vector2(cell * Catalog.SIZE), Vector2.ONE * Catalog.SIZE)
			var back := document.tile(cell, 1)
			if back: Catalog.draw(self, back, rect, time)
			var id := document.tile(cell)
			if id:
				var faded: bool = (id == Catalog.Tile.DOOR_RED and doors.red) or (id == Catalog.Tile.DOOR_BLUE and doors.blue)
				Catalog.draw(self, id, rect, time, faded)
			else: draw_rect(rect.grow(-0.5), Color(0.13, 0.22, 0.3, 0.18), false, 0.5)

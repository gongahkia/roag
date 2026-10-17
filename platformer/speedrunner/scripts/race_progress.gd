extends Control

var room: WorkshopRoom

func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE

func _process(_delta: float) -> void:
	queue_redraw()

func _draw() -> void:
	if room == null: return
	var start := room.document.spawn().x
	var finish := room.document.find(TileCatalog.Tile.FINISH).x * 24 + 12.0
	draw_line(Vector2.ZERO, Vector2(606, 0), Color("416078"), 2)
	for id: int in room.people:
		var fraction := clampf((room.actors[id].position.x - start) / maxf(1, finish - start), 0, 1)
		var point := Vector2(fraction * 606, 0)
		var tint: Color = WorkshopRoom.COLORS[int(room.people[id].color)]
		draw_circle(point, 3, tint)
		if id == 1: draw_arc(point, 5, 0, TAU, 16, Color.WHITE, 1)

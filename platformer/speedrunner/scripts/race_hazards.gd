extends Node2D

var definitions: Array = []
var clock: Callable

func at(saw: Array, offset := 0.0) -> Vector2:
	var time := float(clock.call()) if clock.is_valid() else 0.0
	return Vector2(saw[0]) + Vector2(saw[1]) * sin((time + offset) * TAU / float(saw[2]))

func hits(actor: Runner, offset := 0.0) -> bool:
	var bounds := Rect2(actor.position - Vector2(6, 10), Vector2(12, 20))
	for saw: Array in definitions:
		var center := at(saw, offset)
		if center.distance_to(center.clamp(bounds.position, bounds.end)) < float(saw[3]) - 2: return true
	return false

func _process(_delta: float) -> void:
	queue_redraw()

func _draw() -> void:
	for saw: Array in definitions:
		var center := at(saw)
		draw_line(Vector2(saw[0]) - Vector2(saw[1]), Vector2(saw[0]) + Vector2(saw[1]), Color("804455"))
		var points := PackedVector2Array()
		for i in 24:
			points.append(center + Vector2.from_angle(i * TAU / 24.0) * (float(saw[3]) if i % 2 else float(saw[3]) - 3))
		draw_colored_polygon(points, Color("ff8195"))
		draw_circle(center, 3, Color("182838"))

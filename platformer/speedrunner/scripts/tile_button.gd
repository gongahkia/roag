extends Button

const Catalog = preload("res://scripts/tile_catalog.gd")
var tile_id := 1

func _ready() -> void:
	custom_minimum_size = Vector2(24, 24)
	toggle_mode = true
	focus_mode = Control.FOCUS_NONE
	tooltip_text = Catalog.NAMES[tile_id]

func _draw() -> void:
	if tile_id: Catalog.draw(self, tile_id, Rect2(Vector2(3, 3), Vector2(18, 18)))
	else:
		draw_line(Vector2(6, 6), Vector2(18, 18), Color("ef9dae"), 2)
		draw_line(Vector2(18, 6), Vector2(6, 18), Color("ef9dae"), 2)
	if button_pressed: draw_rect(Rect2(Vector2.ONE, size - Vector2.ONE * 2), Color("7aebca"), false, 2)

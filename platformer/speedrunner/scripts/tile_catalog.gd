class_name TileCatalog
extends RefCounted

const SIZE := 24
enum Tile { AIR, GRASS, DIRT, STONE, BRICK, METAL, GLASS, ICE, SAND, WOOD,
	PLATFORM, SPIKES, LAVA, WATER, LADDER, SPRING, BELT_RIGHT, BELT_LEFT,
	CHECKPOINT, SPAWN, FINISH, COIN, KEY_RED, DOOR_RED, KEY_BLUE, DOOR_BLUE,
	WIND_UP, WIND_LEFT, WIND_RIGHT, BG_SKY, BG_BRICK, BG_LEAVES, FLOWER, LAMP, WINDOW, SAW }
const NAMES := ["Erase", "Grass", "Dirt", "Stone", "Brick", "Metal", "Glass", "Ice", "Sand", "Wood",
	"One-way", "Spikes", "Lava", "Water", "Ladder", "Spring", "Belt >", "Belt <", "Checkpoint", "Start", "Finish", "Coin",
	"Red key", "Red door", "Blue key", "Blue door", "Updraft", "Wind <", "Wind >", "Sky", "Wallpaper", "Leaves", "Flower", "Lamp", "Window", "Saw"]
const COLORS := ["152331", "72c866", "95634a", "8295a5", "c77b68", "657b91", "7cdae8", "a2d8ff", "e5cb82", "ba925f",
	"b2a3e8", "ff8195", "ff804d", "5c9ee7", "d9ae6f", "f593c7", "f5c56c", "f5c56c", "7de3bc", "71eacb", "f5d86b", "ffde74",
	"fb7f8c", "da657a", "82acff", "6a8bdd", "a5efda", "a5efda", "a5efda", "233e5c", "4d384c", "264c45", "f3a3cc", "f8e6a0", "7797bc", "f2849a"]

static func valid(id: int) -> bool:
	return id >= 0 and id < NAMES.size()

static func background(id: int) -> bool:
	return id >= Tile.BG_SKY and id <= Tile.WINDOW

static func solid(id: int) -> bool:
	return (id >= Tile.GRASS and id <= Tile.WOOD) or id in [Tile.DOOR_RED, Tile.DOOR_BLUE, Tile.BELT_RIGHT, Tile.BELT_LEFT]

static func category(id: int) -> String:
	if id == 0: return "Tools"
	if background(id): return "Backdrop"
	if id <= Tile.PLATFORM or id in [Tile.BELT_RIGHT, Tile.BELT_LEFT]: return "Blocks"
	if id in [Tile.WATER, Tile.LADDER, Tile.SPRING, Tile.WIND_UP, Tile.WIND_LEFT, Tile.WIND_RIGHT]: return "Movement"
	return "Objects"

static func color(id: int) -> Color:
	return Color(COLORS[clampi(id, 0, COLORS.size() - 1)])

# All tile artwork is drawn here; no Everybody Edits art is imported.
static func draw(canvas: CanvasItem, id: int, rect: Rect2, time: float = 0.0, faded := false) -> void:
	if not valid(id) or id == 0: return
	var c := color(id)
	if faded: c.a = 0.45
	var p := rect.position
	var s := rect.size.x
	var center := rect.get_center()
	var dark := c.darkened(0.55)
	var light := c.lightened(0.3)
	var font := ThemeDB.fallback_font
	if solid(id) or background(id):
		canvas.draw_rect(rect, Color(c, c.a * (0.4 if id == Tile.GLASS else 1.0)))
		canvas.draw_rect(rect.grow(-0.5), dark, false, 1)
		canvas.draw_line(p + Vector2(1, 1), p + Vector2(s - 2, 1), light)
	match id:
		Tile.GRASS:
			canvas.draw_rect(Rect2(p + Vector2(1, 6), Vector2(s - 2, s - 7)), Color("725345"))
			for x in range(2, int(s) - 2, 5): canvas.draw_line(p + Vector2(x, 1), p + Vector2(x, 7), light)
		Tile.DIRT, Tile.SAND, Tile.STONE:
			for dot in [Vector2(5, 6), Vector2(15, 4), Vector2(9, 16), Vector2(19, 19)]: canvas.draw_rect(Rect2(p + dot * s / SIZE, Vector2(2, 2)), dark)
		Tile.BRICK, Tile.BG_BRICK:
			canvas.draw_line(p + Vector2(0, s / 2), p + Vector2(s, s / 2), dark)
			canvas.draw_line(p + Vector2(s / 2, 0), p + Vector2(s / 2, s / 2), dark)
			canvas.draw_line(p + Vector2(s / 4, s / 2), p + Vector2(s / 4, s), dark)
		Tile.METAL:
			canvas.draw_rect(rect.grow(-4), dark, false)
			for offset in [Vector2(3, 3), Vector2(s - 4, s - 4)]: canvas.draw_circle(p + offset, 1, light)
		Tile.GLASS, Tile.ICE, Tile.WINDOW:
			canvas.draw_line(p + Vector2(4, s - 5), p + Vector2(s - 5, 4), light, 2)
			if id == Tile.WINDOW:
				canvas.draw_line(center - Vector2(s / 2, 0), center + Vector2(s / 2, 0), dark, 2)
				canvas.draw_line(center - Vector2(0, s / 2), center + Vector2(0, s / 2), dark, 2)
		Tile.WOOD:
			for x in [s / 3, s * 2 / 3]: canvas.draw_line(p + Vector2(x, 2), p + Vector2(x, s - 2), dark)
		Tile.PLATFORM:
			canvas.draw_rect(Rect2(p, Vector2(s, 5)), c)
			canvas.draw_line(p, p + Vector2(s, 0), light, 2)
		Tile.SPIKES:
			for i in 3:
				canvas.draw_colored_polygon(PackedVector2Array([p + Vector2(i * s / 3, s), p + Vector2((i + 0.5) * s / 3, 4), p + Vector2((i + 1) * s / 3, s)]), c)
		Tile.LAVA, Tile.WATER:
			canvas.draw_rect(rect, Color(c, 0.65))
			for i in 3:
				var y := 4.0 + sin(time * 3 + i) * 2
				canvas.draw_line(p + Vector2(i * s / 3, y), p + Vector2((i + 1) * s / 3, y), light, 2)
		Tile.LADDER:
			for x in [5, s - 5]: canvas.draw_line(p + Vector2(x, 0), p + Vector2(x, s), c, 3)
			for y in [4, 12, 20]: canvas.draw_line(p + Vector2(5, y * s / SIZE), p + Vector2(s - 5, y * s / SIZE), c, 2)
		Tile.SPRING:
			canvas.draw_rect(Rect2(p + Vector2(2, s - 5), Vector2(s - 4, 4)), dark)
			canvas.draw_polyline(PackedVector2Array([p + Vector2(5, s - 6), p + Vector2(s - 5, s - 10), p + Vector2(5, s - 14), p + Vector2(s - 5, s - 18)]), c, 2)
			canvas.draw_rect(Rect2(p + Vector2(1, 4), Vector2(s - 2, 4)), light)
		Tile.BELT_LEFT, Tile.BELT_RIGHT:
			var direction := -1 if id == Tile.BELT_LEFT else 1
			canvas.draw_string(font, p + Vector2(4, s - 5), "<<<" if direction < 0 else ">>>", HORIZONTAL_ALIGNMENT_LEFT, -1, int(s * 0.48), dark)
		Tile.CHECKPOINT, Tile.SPAWN, Tile.FINISH:
			canvas.draw_line(p + Vector2(5, 3), p + Vector2(5, s), c, 2)
			canvas.draw_colored_polygon(PackedVector2Array([p + Vector2(6, 3), p + Vector2(s - 2, 8), p + Vector2(6, 14)]), c)
			canvas.draw_string(font, p + Vector2(10, s - 2), "S" if id == Tile.SPAWN else "F" if id == Tile.FINISH else "+", HORIZONTAL_ALIGNMENT_LEFT, -1, int(s * 0.48), c)
		Tile.COIN:
			canvas.draw_circle(center, s * 0.3, c)
			canvas.draw_arc(center, s * 0.2, 0, TAU, 12, light, 1.5)
		Tile.KEY_RED, Tile.KEY_BLUE:
			canvas.draw_arc(p + Vector2(8, 8), 4, 0, TAU, 12, c, 2)
			canvas.draw_line(p + Vector2(10, 11), p + Vector2(19, 20), c, 3)
			canvas.draw_line(p + Vector2(16, 17), p + Vector2(20, 13), c, 2)
		Tile.DOOR_RED, Tile.DOOR_BLUE:
			canvas.draw_rect(rect.grow(-3), dark, false, 2)
			canvas.draw_circle(center, 2.5, light)
		Tile.WIND_UP, Tile.WIND_LEFT, Tile.WIND_RIGHT:
			var direction := Vector2.UP if id == Tile.WIND_UP else Vector2.LEFT if id == Tile.WIND_LEFT else Vector2.RIGHT
			var side := direction.rotated(PI / 2)
			canvas.draw_line(center - direction * 7, center + direction * 7, c, 2)
			canvas.draw_polyline(PackedVector2Array([center + direction * 2 + side * 5, center + direction * 7, center + direction * 2 - side * 5]), c, 2)
		Tile.BG_LEAVES:
			for offset in [Vector2(6, 7), Vector2(16, 14), Vector2(7, 19)]: canvas.draw_circle(p + offset * s / SIZE, 4, light.darkened(0.5))
		Tile.FLOWER:
			canvas.draw_line(center, p + Vector2(s / 2, s), Color("76b879"), 2)
			for angle in 5: canvas.draw_circle(center + Vector2.from_angle(angle * TAU / 5) * 4, 3, c)
			canvas.draw_circle(center, 2, Color("ffe5a0"))
		Tile.LAMP:
			canvas.draw_line(center, p + Vector2(s / 2, s), dark, 2)
			canvas.draw_circle(center, 5, c)
			canvas.draw_circle(center, 8, Color(c, 0.12))
		Tile.SAW:
			var points := PackedVector2Array()
			for i in 24: points.append(center + Vector2.from_angle(i * TAU / 24 + time * 4) * (s * 0.46 if i % 2 else s * 0.33))
			canvas.draw_colored_polygon(points, c)
			canvas.draw_circle(center, 3, dark)

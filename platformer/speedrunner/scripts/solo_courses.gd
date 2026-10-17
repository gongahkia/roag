extends RefCounted

const Document = preload("res://scripts/world_document.gd")
const T = preload("res://scripts/tile_catalog.gd").Tile

static func titles() -> Array[String]:
	return ["Split Second", "Skyline Steps", "Foundry Rush"]

static func course(index: int) -> Dictionary:
	var doc := Document.new(88 if index == 0 else 108, 24)
	doc.title = titles()[index]
	var saws: Array = []
	match index:
		0:
			_floor(doc, 0, 18, 18)
			_floor(doc, 21, 40, 18)
			_floor(doc, 43, 64, 18)
			_floor(doc, 67, 88, 18)
			for x in [10, 11, 30, 51, 52, 76]: doc.set_raw(Vector2i(x, 17), T.SPIKES)
			for x in range(7, 15): doc.set_raw(Vector2i(x, 14), T.BRICK)
			for x in range(28, 35): doc.set_raw(Vector2i(x, 15), T.PLATFORM)
			for x in [24, 46, 70]: doc.set_raw(Vector2i(x, 17), T.CHECKPOINT)
			doc.set_raw(Vector2i(3, 17), T.SPAWN)
			doc.set_raw(Vector2i(84, 17), T.FINISH)
		1:
			for section: Array in [[0, 18, 20], [21, 36, 18], [39, 53, 16], [56, 70, 18], [73, 88, 17], [91, 108, 20]]:
				_floor(doc, section[0], section[1], section[2])
			for cell in [Vector2i(10, 19), Vector2i(29, 17), Vector2i(47, 15), Vector2i(64, 17), Vector2i(81, 16), Vector2i(99, 19)]: doc.set_raw(cell, T.SPIKES)
			for cell in [Vector2i(24, 17), Vector2i(42, 15), Vector2i(60, 17), Vector2i(77, 16), Vector2i(94, 19)]: doc.set_raw(cell, T.CHECKPOINT)
			for x in range(26, 34): doc.set_raw(Vector2i(x, 15), T.PLATFORM)
			for x in range(59, 67): doc.set_raw(Vector2i(x, 15), T.PLATFORM)
			doc.set_raw(Vector2i(3, 19), T.SPAWN)
			doc.set_raw(Vector2i(104, 19), T.FINISH)
		2:
			for section: Array in [[0, 24, 19], [27, 49, 19], [52, 76, 18], [79, 108, 19]]:
				_floor(doc, section[0], section[1], section[2], T.METAL)
			for x in range(7, 13): doc.set_raw(Vector2i(x, 19), T.ICE)
			for x in range(33, 39): doc.set_raw(Vector2i(x, 19), T.BELT_RIGHT)
			for x in range(82, 88): doc.set_raw(Vector2i(x, 19), T.BELT_LEFT)
			for cell in [Vector2i(16, 18), Vector2i(43, 18), Vector2i(67, 17), Vector2i(93, 18)]: doc.set_raw(cell, T.SPIKES)
			_floor(doc, 57, 60, 16, T.BRICK)
			for cell in [Vector2i(30, 18), Vector2i(54, 17), Vector2i(80, 18)]: doc.set_raw(cell, T.CHECKPOINT)
			saws = [[Vector2(1080, 412), Vector2(0, 62), 2.4, 10.0], [Vector2(2220, 404), Vector2(50, 0), 2.7, 10.0]]
			doc.set_raw(Vector2i(3, 18), T.SPAWN)
			doc.set_raw(Vector2i(104, 18), T.FINISH)
	for x in doc.width:
		for y in range(8, 22): doc.set_raw(Vector2i(x, y), T.BG_SKY if index != 2 else T.BG_BRICK, 1)
	return {"document": doc, "saws": saws, "description": [
		"Low ceilings, spike hops and gaps. Pick the ground line or the raised platforms.",
		"Climb a chain of raised islands, then carry your speed through the descent.",
		"Read the moving saws, control your skid on ice and use the conveyor belts."][index]}

static func _floor(doc: WorldDocument, start: int, end: int, row: int, tile := T.GRASS) -> void:
	for x in range(start, end):
		for y in range(row, doc.height): doc.set_raw(Vector2i(x, y), tile if y == row else T.DIRT)

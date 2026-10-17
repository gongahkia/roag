class_name WorldDocument
extends RefCounted

const Catalog = preload("res://scripts/tile_catalog.gd")
const FORMAT := 1
var width := 96
var height := 28
var title := "Shared speedrun workshop"
var revision := 0
var layers: Array[PackedInt32Array] = []
var versions: Array[PackedInt32Array] = []

func _init(w: int = 96, h: int = 28) -> void:
	width = clampi(w, 16, 128)
	height = clampi(h, 12, 64)
	for i in 2:
		var cells := PackedInt32Array()
		cells.resize(width * height)
		layers.append(cells)
		versions.append(cells.duplicate())

func inside(cell: Vector2i) -> bool:
	return cell.x >= 0 and cell.y >= 0 and cell.x < width and cell.y < height

func tile(cell: Vector2i, layer := 0) -> int:
	return layers[layer][cell.y * width + cell.x] if inside(cell) and layer in [0, 1] else 0

func version(cell: Vector2i, layer := 0) -> int:
	return versions[layer][cell.y * width + cell.x] if inside(cell) and layer in [0, 1] else -1

func find(id: int) -> Vector2i:
	for index in layers[0].size():
		if layers[0][index] == id: return Vector2i(index % width, index / width)
	return Vector2i(-1, -1)

func spawn() -> Vector2:
	var cell := find(Catalog.Tile.SPAWN)
	if cell.x < 0: cell = Vector2i(3, height - 6)
	return Vector2(cell * Catalog.SIZE) + Vector2(Catalog.SIZE / 2, Catalog.SIZE - 10)

func set_raw(cell: Vector2i, id: int, layer := 0) -> void:
	if inside(cell): layers[layer][cell.y * width + cell.x] = id

# The server serializes a transaction. Optimistic per-cell versions allow
# simultaneous changes to different cells and reject stale same-cell writes.
func commit(cell: Vector2i, id: int, layer: int, expected: int) -> Dictionary:
	if not inside(cell) or layer not in [0, 1] or not Catalog.valid(id): return {"error": "Invalid tile or cell"}
	if id != 0 and Catalog.background(id) != (layer == 1): return {"error": "Wrong tile layer"}
	if expected != version(cell, layer): return {"error": "Cell changed; try again"}
	if tile(cell, layer) == id: return {"error": "No change"}
	if tile(cell, layer) in [Catalog.Tile.SPAWN, Catalog.Tile.FINISH] and id == 0:
		return {"error": "Move the start/finish by placing it elsewhere"}
	var edits: Array = []
	if id in [Catalog.Tile.SPAWN, Catalog.Tile.FINISH]:
		var old := find(id)
		if old.x >= 0 and old != cell: edits.append([old.x, old.y, 0, id, 0])
	# Never replace one required marker with the other.
	if tile(cell, layer) in [Catalog.Tile.SPAWN, Catalog.Tile.FINISH] and tile(cell, layer) != id:
		return {"error": "Keep the start and finish separate"}
	edits.append([cell.x, cell.y, layer, tile(cell, layer), id])
	return apply_edits(edits)

func apply_edits(edits: Array) -> Dictionary:
	revision += 1
	for edit: Array in edits:
		var index := int(edit[1]) * width + int(edit[0])
		layers[int(edit[2])][index] = int(edit[4])
		versions[int(edit[2])][index] = revision
	return {"revision": revision, "edits": edits}

func apply_remote(transaction: Dictionary) -> void:
	for edit: Array in transaction["edits"]:
		var index := int(edit[1]) * width + int(edit[0])
		layers[int(edit[2])][index] = int(edit[4])
		versions[int(edit[2])][index] = int(transaction["revision"])
	revision = int(transaction["revision"])

func pack() -> Dictionary:
	return {"format": FORMAT, "width": width, "height": height, "title": title, "revision": revision,
		"front": Array(layers[0]), "back": Array(layers[1]), "front_versions": Array(versions[0]), "back_versions": Array(versions[1])}

static func unpack(data: Dictionary) -> WorldDocument:
	if data.get("format") != FORMAT: return null
	var w := int(data.get("width", 0))
	var h := int(data.get("height", 0))
	if w < 16 or w > 128 or h < 12 or h > 64: return null
	var document := WorldDocument.new(w, h)
	document.title = str(data.get("title", "Shared world")).strip_edges().substr(0, 64)
	document.revision = clampi(int(data.get("revision", 0)), 0, 1000000000)
	for layer in 2:
		var values = data.get("front" if layer == 0 else "back", [])
		var stamps = data.get("front_versions" if layer == 0 else "back_versions", [])
		if not values is Array or values.size() != w * h or not stamps is Array or stamps.size() != w * h: return null
		for i in values.size():
			if not (values[i] is int or values[i] is float) or not (stamps[i] is int or stamps[i] is float): return null
			var id := int(values[i])
			if not Catalog.valid(id) or (id != 0 and Catalog.background(id) != (layer == 1)): return null
			if int(stamps[i]) < 0 or int(stamps[i]) > document.revision: return null
			document.layers[layer][i] = id
			document.versions[layer][i] = int(stamps[i])
	if document.layers[0].count(Catalog.Tile.SPAWN) != 1 or document.layers[0].count(Catalog.Tile.FINISH) != 1: return null
	return document

func save_to(path: String) -> bool:
	var file := FileAccess.open(path + ".tmp", FileAccess.WRITE)
	if file == null: return false
	file.store_string(JSON.stringify(pack()))
	file.flush()
	var error := file.get_error()
	file.close()
	return error == OK and DirAccess.rename_absolute(path + ".tmp", path) == OK

static func load_from(path: String) -> WorldDocument:
	if not FileAccess.file_exists(path) or FileAccess.get_file_as_bytes(path).size() > 2000000: return null
	var data = JSON.parse_string(FileAccess.get_file_as_string(path))
	return unpack(data) if data is Dictionary else null

static func example() -> WorldDocument:
	var document := WorldDocument.new()
	var T = Catalog.Tile
	for x in document.width:
		for y in range(23, 28):
			document.set_raw(Vector2i(x, y), T.GRASS if y == 23 else T.DIRT)
		for y in range(18, 23): document.set_raw(Vector2i(x, y), T.BG_SKY, 1)
	for x in range(7, 12): document.set_raw(Vector2i(x, 23), T.ICE)
	for x in range(20, 24): document.set_raw(Vector2i(x, 23), T.BELT_RIGHT)
	for x in range(28, 32):
		for y in range(23, 28): document.set_raw(Vector2i(x, y), T.WATER)
	for x in range(45, 49):
		for y in range(23, 28): document.set_raw(Vector2i(x, y), T.LAVA)
	for x in range(61, 66):
		for y in range(23, 28): document.set_raw(Vector2i(x, y), T.AIR)
	for x in range(34, 40): document.set_raw(Vector2i(x, 18), T.PLATFORM)
	for y in range(19, 23): document.set_raw(Vector2i(34, y), T.LADDER)
	for x in range(4, 92, 8): document.set_raw(Vector2i(x, 20), T.COIN)
	for x in [14, 15, 55, 56, 80]: document.set_raw(Vector2i(x, 22), T.SPIKES)
	for x in range(69, 74):
		document.set_raw(Vector2i(x, 23), T.BRICK)
		document.set_raw(Vector2i(x, 22), T.FLOWER, 1)
	document.set_raw(Vector2i(26, 22), T.CHECKPOINT)
	document.set_raw(Vector2i(59, 22), T.SPRING)
	document.set_raw(Vector2i(52, 22), T.KEY_RED)
	for y in range(20, 23): document.set_raw(Vector2i(76, y), T.DOOR_RED)
	document.set_raw(Vector2i(41, 22), T.KEY_BLUE)
	document.set_raw(Vector2i(84, 22), T.DOOR_BLUE)
	document.set_raw(Vector2i(3, 22), T.SPAWN)
	document.set_raw(Vector2i(90, 22), T.FINISH)
	return document

class_name RaceSession
extends RefCounted

const RULESET := 3
const PHYSICS_HZ := 120
const MAX_FRAMES := 14400
var record_path := "user://records-v3.json"
var records: Dictionary = {}
var course_id := ""
var ticks := 0
var deaths := 0
var attempts := 0
var started := false
var finished := false
var valid := true
var practice := false
var new_best := false
var save_failed := false
var frames: Array = []

func begin(id: String, training: bool = false) -> void:
	course_id = id
	practice = training
	deaths = 0
	attempts = 0
	retry()

func retry() -> void:
	attempts += 1
	ticks = 0
	started = false
	finished = false
	valid = not practice
	new_best = false
	save_failed = false
	frames.clear()

func tick(at: Vector2, frame: int) -> void:
	if not started or finished:
		return
	ticks += 1
	if frames.size() < MAX_FRAMES:
		# Integer hundredths of a pixel round-trip exactly through JSON.
		frames.append([roundi(at.x * 100.0), roundi(at.y * 100.0), frame])

func best_ticks() -> int:
	return int(records.get(course_id, {}).get("ticks", 0))

func ghost_frame(index: int) -> Array:
	var ghost: Array = records.get(course_id, {}).get("frames", [])
	return ghost[index] if index >= 0 and index < ghost.size() else []

func finish() -> void:
	if finished:
		return
	finished = true
	if ticks > 0 and valid and not practice and (best_ticks() == 0 or ticks < best_ticks()):
		new_best = true
		records[course_id] = {"ticks": ticks, "frames": frames.duplicate(true) if ticks <= MAX_FRAMES else []}
		save_failed = not save_records()

func save_records() -> bool:
	var temporary := record_path + ".tmp"
	var file := FileAccess.open(temporary, FileAccess.WRITE)
	if file == null:
		return false
	file.store_string(JSON.stringify({"ruleset": RULESET, "records": records}, "", true, true))
	file.flush()
	var write_error := file.get_error()
	file.close()
	if write_error != OK:
		return false
	return DirAccess.rename_absolute(temporary, record_path) == OK

func load_records() -> void:
	records.clear()
	if not FileAccess.file_exists(record_path):
		return
	var parsed = JSON.parse_string(FileAccess.get_file_as_string(record_path))
	if not parsed is Dictionary or parsed.get("ruleset") != RULESET or not parsed.get("records") is Dictionary:
		return
	for id: String in parsed["records"]:
		var record = parsed["records"][id]
		if not record is Dictionary or not (record.get("ticks") is float or record.get("ticks") is int):
			continue
		var count := int(record["ticks"])
		if count < 1 or count > 10000000:
			continue
		var ghost = record.get("frames", [])
		var usable: bool = ghost is Array and ghost.size() == count and count <= MAX_FRAMES
		if usable:
			for sample in ghost:
				if not sample is Array or sample.size() != 3:
					usable = false
					break
				for coordinate in sample:
					if not (coordinate is float or coordinate is int) or not is_finite(float(coordinate)) or absf(float(coordinate)) > 100000000:
						usable = false
				if not usable:
					break
				if int(sample[2]) < 0 or int(sample[2]) > 5:
					usable = false
					break
				sample[2] = int(sample[2])
				sample[0] = int(sample[0])
				sample[1] = int(sample[1])
		records[id] = {"ticks": count, "frames": ghost if usable else []}

static func format_time(count: int) -> String:
	var ms := int(count * 1000.0 / PHYSICS_HZ)
	return "%02d:%02d.%03d" % [ms / 60000, (ms / 1000) % 60, ms % 1000]

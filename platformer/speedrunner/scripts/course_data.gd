class_name CourseData
extends RefCounted

## Rectangles use top-left x/y, width/height in world pixels.
## Original layouts: no campaign geometry is imported from VVVVVV.
static func courses() -> Array[Dictionary]:
	return [
		{
			"id": "momentum", "title": "01 / RUNNING START", "subtitle": "Carry speed. Cut corners. Keep moving.",
			"size": Vector2(1728, 360), "spawn": Vector2(48, 294), "goal": Rect2(1656, 256, 24, 48),
			"accent": Color("6cecc6"), "par": 5.6,
			"platforms": [Rect2(0, 304, 384, 56), Rect2(464, 304, 208, 56), Rect2(768, 272, 224, 88), Rect2(1088, 240, 224, 120), Rect2(1408, 304, 320, 56)],
			"spikes": [Rect2(240, 292, 48, 12), Rect2(528, 292, 32, 12), Rect2(1112, 228, 32, 12)],
			"saws": [], "checkpoints": [Vector2(804, 262)],
			"signs": [[Vector2(32, 248), "SPACE / JUMP"], [Vector2(144, 216), "SHIFT / SPRINT"], [Vector2(784, 216), "FIND YOUR LINE"]],
		},
		{
			"id": "wallflow", "title": "02 / WALL FLOW", "subtitle": "Press into a wall to slide. Jump to kick away.",
			"size": Vector2(1472, 512), "spawn": Vector2(48, 438), "goal": Rect2(1384, 256, 24, 48),
			"accent": Color("b1a0ff"), "par": 10.5,
			"platforms": [Rect2(0, 448, 480, 64), Rect2(256, 96, 24, 240), Rect2(368, 160, 24, 288), Rect2(464, 224, 256, 24), Rect2(816, 272, 192, 24), Rect2(1104, 304, 368, 208)],
			"spikes": [Rect2(1152, 292, 48, 12)],
			"saws": [[Vector2(924, 220), Vector2(0, 36), 1.4, 13.0]],
			"checkpoints": [Vector2(496, 214)],
			"signs": [[Vector2(32, 388), "ENTER BELOW THE SHAFT"], [Vector2(288, 68), "WALL / JUMP"], [Vector2(480, 184), "NO SPEED LIMIT"]],
		},
		{
			"id": "redline", "title": "03 / REDLINE", "subtitle": "Short hops under the ceiling. Full jumps over the void.",
			"size": Vector2(2112, 432), "spawn": Vector2(48, 326), "goal": Rect2(2024, 240, 24, 48),
			"accent": Color("ff967d"), "par": 8.4,
			"platforms": [Rect2(0, 336, 416, 96), Rect2(112, 260, 256, 16), Rect2(528, 304, 224, 128), Rect2(848, 272, 256, 160), Rect2(960, 184, 144, 16), Rect2(1200, 336, 160, 96), Rect2(1488, 304, 208, 128), Rect2(1792, 288, 320, 144)],
			"spikes": [Rect2(208, 324, 32, 12), Rect2(656, 292, 48, 12), Rect2(1008, 260, 32, 12), Rect2(1888, 276, 48, 12)],
			"saws": [[Vector2(1352, 220), Vector2(42, 0), 1.3, 14.0], [Vector2(1664, 280), Vector2(0, 16), 1.7, 13.0]],
			"checkpoints": [Vector2(872, 262)],
			"signs": [[Vector2(24, 224), "TAP / LOW JUMP"], [Vector2(536, 244), "HOLD / FULL JUMP"], [Vector2(1808, 224), "MAKE IT COUNT"]],
		},
	]

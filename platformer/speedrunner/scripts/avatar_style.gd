extends RefCounted

const SHADER = preload("res://shaders/avatar_tint.gdshader")
const COLORS := [
	Color("77edc4"), Color("f6a2ce"), Color("ffc878"), Color("9cadff"),
	Color("b9ef8b"), Color("f19885"), Color("8dd9f7"), Color("d6b3ff"),
	Color("ffdf69"), Color("f56d9c"), Color("45c9b4"), Color("7f91ed"),
	Color("c9c0a6"), Color("da864c"), Color("6dc266"), Color("ecedf9"),
]

static func apply(sprite: Sprite2D, tint: Color) -> void:
	var material := ShaderMaterial.new()
	material.shader = SHADER
	material.set_shader_parameter("tint", tint)
	sprite.material = material

static func nameplate(parent: Node) -> Label:
	var label := Label.new()
	label.position = Vector2(-64, -29)
	label.size.x = 128
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.clip_text = true
	label.text_overrun_behavior = TextServer.OVERRUN_TRIM_ELLIPSIS
	label.add_theme_font_size_override("font_size", 10)
	label.add_theme_color_override("font_shadow_color", Color("09131f"))
	label.add_theme_constant_override("shadow_offset_x", 1)
	label.add_theme_constant_override("shadow_offset_y", 1)
	parent.add_child(label)
	return label

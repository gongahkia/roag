shader_type canvas_item;
render_mode unshaded;

uniform vec4 tint : source_color = vec4(0.47, 0.93, 0.77, 1.0);
varying vec4 vertex_tint;

void vertex() {
	vertex_tint = COLOR;
}

void fragment() {
	vec4 source = texture(TEXTURE, UV);
	float brightest = max(source.r, max(source.g, source.b));
	float darkest = min(source.r, min(source.g, source.b));
	// Recolor the cyan body; retain neutral eyes, outlines and transparency.
	vec3 body = tint.rgb * brightest;
	vec3 shaded = mix(source.rgb, body, step(0.12, brightest - darkest));
	COLOR = vec4(shaded, source.a * tint.a) * vertex_tint;
}

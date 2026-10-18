package sandbox

import "core:fmt"
import gl "vendor:OpenGL"
import glfw "vendor:glfw"

Menu_Layout :: struct {
	x, y, width, height, row_top, row_height: f32,
}
settings_layout :: proc(width, height: i32) -> Menu_Layout {
	w := min(432, f32(width) - 16)
	h := min(326, f32(height) - 16)
	compact := h < 300
	return {
		(f32(width) - w) / 2,
		(f32(height) - h) / 2,
		w,
		h,
		compact ? 40 : 64,
		compact ? 28 : 44,
	}
}

menu_contains :: proc(mx, my: f64, x, y, w, h: f32) -> bool {
	return mx >= f64(x) && mx < f64(x + w) && my >= f64(y) && my < f64(y + h)
}

// Return true to resume. All mouse clicks are consumed while the menu is open.
settings_menu_input :: proc(
	menu: ^Settings_Menu,
	s: ^Settings,
	down, old: ^[512]bool,
	clicked: bool,
	mx, my: f64,
	width, height: i32,
) -> bool {
	if down[glfw.KEY_UP] && !old[glfw.KEY_UP] {menu.selected = (menu.selected + 3) % 4}
	if down[glfw.KEY_DOWN] && !old[glfw.KEY_DOWN] {menu.selected = (menu.selected + 1) % 4}
	if down[glfw.KEY_LEFT] && !old[glfw.KEY_LEFT] {settings_adjust(s, menu.selected, -1)}
	if down[glfw.KEY_RIGHT] && !old[glfw.KEY_RIGHT] {settings_adjust(s, menu.selected, 1)}
	if down[glfw.KEY_R] && !old[glfw.KEY_R] {s^ = settings_defaults()}
	if down[glfw.KEY_ENTER] && !old[glfw.KEY_ENTER] {return true}
	if !clicked {return false}
	l := settings_layout(width, height)
	for row in 0 ..< 4 {
		y := l.y + l.row_top + f32(row) * l.row_height
		if menu_contains(mx, my, l.x + 8, y, l.width - 16, l.row_height) {
			menu.selected = row
			if menu_contains(
				mx,
				my,
				l.x + l.width - 94,
				y,
				32,
				l.row_height,
			) {settings_adjust(s, row, -1)}
			if menu_contains(
				mx,
				my,
				l.x + l.width - 48,
				y,
				32,
				l.row_height,
			) {settings_adjust(s, row, 1)}
		}
	}
	if menu_contains(mx, my, l.x + 16, l.y + l.height - 36, 90, 24) {s^ = settings_defaults()}
	return menu_contains(mx, my, l.x + l.width - 124, l.y + l.height - 36, 108, 24)
}

settings_button :: proc(r: ^Renderer, x, y, w, h: f32, label: string) {
	rect(r, x, y, w, h, {0.15, 0.28, 0.32, 1})
	text_draw(r, x + (w - f32(len(label)) * 6) / 2, y + (h - 7) / 2, label, 1)
}

settings_panel :: proc(
	r: ^Renderer,
	menu: ^Settings_Menu,
	s: Settings,
	p: ^Frame_Pacing,
	width, height: i32,
	ortho: Mat4,
) {
	l := settings_layout(width, height)
	compact := l.height < 300
	rect(r, 0, 0, f32(width), f32(height), {0.015, 0.025, 0.04, 0.64})
	rect(r, l.x, l.y, l.width, l.height, {0.035, 0.06, 0.08, 0.98})
	rect(r, l.x, l.y, l.width, 2, FRAME_COLOR)
	text_draw(r, l.x + 16, l.y + 14, "SETTINGS", 2, FRAME_COLOR)
	if !compact {text_draw(r, l.x + 16, l.y + 39, "CHANGES APPLY IMMEDIATELY", 1)}
	labels := [4]string{"KEYBOARD MOVEMENT", "MOUSE LOOK", "VSYNC", "FPS LIMIT"}
	values := [4]string {
		fmt.tprintf("WALK %.1F / FLY %.1F", s.move_speed * WALK_SPEED_SCALE, s.move_speed),
		fmt.tprintf("%.1FX SENSITIVITY", s.mouse_sensitivity),
		s.vsync ? "ON" : "OFF",
		fps_limit_label(s.fps_limit),
	}
	for row in 0 ..< 4 {
		y := l.y + l.row_top + f32(row) * l.row_height
		if menu.selected ==
		   row {rect(r, l.x + 8, y, l.width - 16, l.row_height - 2, {0.09, 0.16, 0.20, 1})}
		text_draw(r, l.x + 16, y + 3, labels[row], 1, {0.66, 0.76, 0.80, 1})
		text_draw(r, l.x + 16, y + (compact ? 15 : 21), values[row], 1, FRAME_COLOR)
		button_h: f32 = compact ? 22 : 30
		button_y := y + (l.row_height - button_h) / 2
		settings_button(r, l.x + l.width - 94, button_y, 32, button_h, "-")
		settings_button(r, l.x + l.width - 48, button_y, 32, button_h, "+")
		flat_flush(r, ortho, gl.TRIANGLES)
	}
	status_y := l.y + l.row_top + 4 * l.row_height + 8
	display := p.display_hz > 0 ? fmt.tprintf("DISPLAY %d HZ", p.display_hz) : "DISPLAY UNKNOWN"
	hud_line(
		r,
		l.x + 16,
		status_y,
		l.width - 32,
		fmt.tprintf("%s / %s", display, frame_limit_label(s, p)),
	)
	if !compact {
		hud_line(
			r,
			l.x + 16,
			status_y + 16,
			l.width - 32,
			menu.save_failed ? "SETTINGS SAVE FAILED" : "ARROWS ADJUST / ESC OR ENTER RESUME",
		)
	}
	settings_button(r, l.x + 16, l.y + l.height - 36, 90, 24, "RESET")
	settings_button(r, l.x + l.width - 124, l.y + l.height - 36, 108, 24, "RESUME")
	flat_flush(r, ortho, gl.TRIANGLES)
}

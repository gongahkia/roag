package sandbox

import "core:fmt"
import "core:strings"
import gl "vendor:OpenGL"
import glfw "vendor:glfw"

Palette_State :: struct {
	open: bool,
	query: [48]u8,
	length, selected, scroll: int,
}

palette_character :: proc "c" (window: glfw.WindowHandle, code: rune) {
	a := cast(^App)glfw.GetWindowUserPointer(window)
	p := &a.palette
	if !p.open || code < 32 || code > 126 || p.length == len(p.query) {return}
	c := u8(code)
	if c >= 'a' && c <= 'z' {c -= 'a' - 'A'}
	p.query[p.length] = c; p.length += 1; p.selected = 0; p.scroll = 0
}

palette_matches :: proc(p: ^Palette_State) -> (results: [len(CREATIVE_BLOCKS)]int, count: int) {
	for b, i in CREATIVE_BLOCKS {
		if strings.contains(BLOCK_NAMES[int(b)], string(p.query[:p.length])) {
			results[count] = i; count += 1
		}
	}
	return
}

palette_layout :: proc(width, height: i32) -> Menu_Layout {
	w, h := min(432, f32(width) - 16), min(430, f32(height) - 16)
	return {(f32(width) - w) / 2, (f32(height) - h) / 2, w, h, 70, 24, max(1, int((h - 110) / 24))}
}

palette_input :: proc(a: ^App) {
	p := &a.palette; down, old := &a.input.keys, &a.input.old_keys
	if down[glfw.KEY_BACKSPACE] && !old[glfw.KEY_BACKSPACE] && p.length > 0 {p.length -= 1; p.selected = 0}
	results, count := palette_matches(p)
	if down[glfw.KEY_DOWN] && !old[glfw.KEY_DOWN] && count > 0 {p.selected = (p.selected + 1) % count}
	if down[glfw.KEY_UP] && !old[glfw.KEY_UP] && count > 0 {p.selected = (p.selected + count - 1) % count}
	p.selected = clamp(p.selected, 0, max(0, count - 1))
	l := palette_layout(a.width, a.height)
	p.scroll = clamp(p.scroll, max(0, p.selected - l.rows + 1), p.selected)
	choose := down[glfw.KEY_ENTER] && !old[glfw.KEY_ENTER]
	if a.input.mouse[0] && !a.input.old_mouse[0] && a.input.menu_was_open {
		ww, wh := glfw.GetWindowSize(a.window)
		mx, my := a.input.x * f64(a.width) / f64(max(ww, 1)), a.input.y * f64(a.height) / f64(max(wh, 1))
		for slot in 0 ..< l.rows {
			row := slot + p.scroll
			if row < count && menu_contains(mx, my, l.x + 8, l.y + l.row_top + f32(slot) * l.row_height, l.width - 16, l.row_height) {p.selected = row; choose = true}
		}
	}
	if choose && count > 0 {a.selected = results[p.selected]; p.open = false}
}

palette_panel :: proc(a: ^App, ortho: Mat4) {
	r := &a.renderer; p := &a.palette
	l := palette_layout(a.width, a.height)
	results, count := palette_matches(p)
	rect(r, 0, 0, f32(a.width), f32(a.height), {0.015, 0.025, 0.04, 0.64})
	rect(r, l.x, l.y, l.width, l.height, {0.035, 0.06, 0.08, 0.98})
	text_draw(r, l.x + 16, l.y + 14, "BLOCK PALETTE", 2, FRAME_COLOR)
	text_draw(r, l.x + 16, l.y + 44, fmt.tprintf("SEARCH: %s_", string(p.query[:p.length])), 1)
	for slot in 0 ..< l.rows {
		row := slot + p.scroll
		if row >= count {break}
		y := l.y + l.row_top + f32(slot) * l.row_height
		if row == p.selected {rect(r, l.x + 8, y, l.width - 16, l.row_height - 2, {0.09, 0.22, 0.24, 1})}
		b := CREATIVE_BLOCKS[results[row]]
		rect(r, l.x + 16, y + 4, 14, 14, BLOCK_COLORS[int(b)])
		text_draw(r, l.x + 40, y + 8, BLOCK_NAMES[int(b)], 1)
	}
	text_draw(r, l.x + 16, l.y + l.height - 26, count == 0 ? "NO MATCHES / BACKSPACE TO EDIT" : fmt.tprintf("%d BLOCKS / ARROWS / ENTER / ESC", count), 1)
	flat_flush(r, ortho, gl.TRIANGLES)
}

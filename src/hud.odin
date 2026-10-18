package sandbox

import "core:fmt"
import gl "vendor:OpenGL"

hud_draw :: proc(a: ^App) {
	r := &a.renderer
	menu := &a.menu
	settings := a.settings
	pacing := &a.pacing
	player := &a.player
	width, height := a.width, a.height
	selected, show := a.selected, a.stats
	immersion := a.immersion

	gl.Disable(gl.DEPTH_TEST)
	gl.Disable(gl.CULL_FACE)
	gl.Enable(gl.BLEND)
	gl.BlendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA)
	ortho := identity()
	ortho[0] = 2 / f32(width)
	ortho[5] = -2 / f32(height)
	ortho[12] = -1
	ortho[13] = 1
	if immersion != .Air {
		tint: F4 = immersion == .Water ? F4{0.08, 0.27, 0.54, 0.23} : F4{0.9, 0.23, 0.02, 0.48}
		rect(r, 0, 0, f32(width), f32(height), tint)
		flat_flush(r, ortho, gl.TRIANGLES)
	}
	if menu.open {
		settings_panel(r, menu, settings, pacing, width, height, ortho)
		gl.Disable(gl.BLEND)
		return
	}
	if show {
		benchmark_panel(a, ortho)
	}
	flat_flush(r, ortho, gl.TRIANGLES)
	cx, cy := f32(width) / 2, f32(height) / 2
	rect(r, cx - 5, cy - 1, 10, 2, {1, 1, 1, 0.9})
	rect(r, cx - 1, cy - 5, 2, 10, {1, 1, 1, 0.9})
	page := selected / 9
	pitch := min(44, (f32(width) - 16) / 9)
	start := cx - pitch * 4.5
	for i in 0 ..< 9 {
		index := page * 9 + i
		x := start + f32(i) * pitch
		tint: F4 = {0.08, 0.12, 0.14, 0.9}
		if index == selected {
			tint = {0.28, 0.44, 0.35, 0.95}
		}
		rect(r, x, f32(height) - 64, pitch - 3, 43, tint)
		if index >= len(CREATIVE_BLOCKS) {continue}
		text_draw(r, x + 4, f32(height) - 59, fmt.tprintf("%d", i + 1), 1)
		b := CREATIVE_BLOCKS[index]
		rect(r, x + pitch / 2 - 7, f32(height) - 49, 15, 16, BLOCK_COLORS[int(b)])
		if b >= .Coal_Ore && b <= .Redstone_Ore {
			rect(r, x + pitch / 2 - 5, f32(height) - 47, 4, 5, BLOCK_COLORS[int(Block.Stone)])
		}
	}
	name := BLOCK_NAMES[int(CREATIVE_BLOCKS[selected])]
	text_draw(r, cx - f32(len(name)) * 4.5, f32(height) - 82, name, 1.5)
	text_draw(r, cx - 42, f32(height) - 12, fmt.tprintf("TAB: PAGE %d/2", page + 1), 1)
	text_draw(
		r,
		start + 4,
		f32(height) - 12,
		player.spectator ? "NOCLIP [F4]" : "WALK [F4]",
		1,
		FRAME_COLOR,
	)
	flat_flush(r, ortho, gl.TRIANGLES)
	gl.Disable(gl.BLEND)
}

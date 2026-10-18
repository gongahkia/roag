package sandbox

import glfw "vendor:glfw"

// GLFW snapshots and control history belong to the main thread.
Input_State :: struct {
	keys, old_keys:                 [512]bool,
	mouse, old_mouse:               [2]bool,
	x, y, previous_x, previous_y:   f64,
	menu_was_open, capture_changed: bool,
	focused, focus_changed:         bool,
}

app_window_focus :: proc "c" (window: glfw.WindowHandle, focused: i32) {
	a := cast(^App)glfw.GetWindowUserPointer(window)
	a.input.focused = focused != 0
	a.input.focus_changed = true
}

// Keep camera input, pointer visibility, and the native grab in agreement.
// Re-enter disabled mode after focus changes: setting the same GLFW mode again
// is a no-op and cannot repair a grab lost by the window system.
app_sync_mouse_capture :: proc(a: ^App) {
	capture := a.input.focused && !a.menu.open && !a.config.hidden
	if capture == a.captured && !a.input.focus_changed {return}
	a.captured = capture
	a.input.capture_changed = true
	if capture {
		cursor_visibility_set_hidden(&a.cursor_visibility, true)
		glfw.SetInputMode(a.window, glfw.CURSOR, glfw.CURSOR_HIDDEN)
		glfw.SetInputMode(a.window, glfw.CURSOR, glfw.CURSOR_DISABLED)
	} else {
		glfw.SetInputMode(a.window, glfw.CURSOR, glfw.CURSOR_NORMAL)
		cursor_visibility_set_hidden(&a.cursor_visibility, false)
	}
	a.input.x, a.input.y = glfw.GetCursorPos(a.window)
	a.input.previous_x, a.input.previous_y = a.input.x, a.input.y
	a.input.focus_changed = false
}

app_controls :: proc(a: ^App) -> bool {
	a.input.capture_changed = false
	app_sync_mouse_capture(a)
	a.input.keys = {}
	a.input.mouse = {}
	if a.input.focused {
		for k in 32 ..< 349 {
			a.input.keys[k] = glfw.GetKey(a.window, i32(k)) == glfw.PRESS
		}
		a.input.mouse = [2]bool {
			glfw.GetMouseButton(a.window, glfw.MOUSE_BUTTON_LEFT) == glfw.PRESS,
			glfw.GetMouseButton(a.window, glfw.MOUSE_BUTTON_RIGHT) == glfw.PRESS,
		}
	}
	if a.input.keys[glfw.KEY_Q] && !a.menu.open {
		return false
	}
	a.width, a.height = glfw.GetFramebufferSize(a.window)
	a.input.x, a.input.y = glfw.GetCursorPos(a.window)
	a.input.menu_was_open = a.menu.open
	if (a.input.keys[glfw.KEY_ESCAPE] && !a.input.old_keys[glfw.KEY_ESCAPE]) ||
	   (a.input.keys[glfw.KEY_F1] && !a.input.old_keys[glfw.KEY_F1]) {a.menu.open = !a.menu.open}
	if a.menu.open {
		ww, wh := glfw.GetWindowSize(a.window)
		ui_x := a.input.x * f64(a.width) / f64(max(ww, 1))
		ui_y := a.input.y * f64(a.height) / f64(max(wh, 1))
		if settings_menu_input(
			&a.menu,
			&a.settings,
			&a.input.keys,
			&a.input.old_keys,
			a.input.mouse[0] && !a.input.old_mouse[0] && a.input.menu_was_open,
			ui_x,
			ui_y,
			a.width,
			a.height,
		) {a.menu.open = false}
	}
	if a.menu.open != a.input.menu_was_open {
		app_sync_mouse_capture(a)
		if !a.menu.open {session_save_settings(a)}
	}
	if a.settings.vsync != a.config.vsync {
		a.config.vsync = a.settings.vsync
		glfw.SwapInterval(i32(int(a.settings.vsync)))
	}
	if a.input.keys[glfw.KEY_F3] && !a.input.old_keys[glfw.KEY_F3] {
		a.stats = !a.stats
	}
	if !a.menu.open && a.input.keys[glfw.KEY_EQUAL] && !a.input.old_keys[glfw.KEY_EQUAL] {
		a.settings.move_speed = min(a.settings.move_speed * 1.5, 512)
	}
	if !a.menu.open && a.input.keys[glfw.KEY_MINUS] && !a.input.old_keys[glfw.KEY_MINUS] {
		a.settings.move_speed = max(a.settings.move_speed / 1.5, 1)
	}
	if !a.menu.open && a.input.keys[glfw.KEY_TAB] && !a.input.old_keys[glfw.KEY_TAB] {
		page := (a.selected / 9 + 1) % 2
		a.selected = min(page * 9 + a.selected % 9, len(CREATIVE_BLOCKS) - 1)
	}
	for i in 1 ..= 9 {
		if !a.menu.open && a.input.keys[glfw.KEY_0 + i] {
			index := a.selected / 9 * 9 + i - 1
			if index < len(CREATIVE_BLOCKS) {a.selected = index}
		}
	}
	if a.captured && !a.input.capture_changed && a.frame > 0 && a.config.benchmark == "" {
		a.yaw += (a.input.x - a.input.previous_x) * 0.0025 * a.settings.mouse_sensitivity
		a.pitch = clamp(
			a.pitch - (a.input.y - a.input.previous_y) * 0.0025 * a.settings.mouse_sensitivity,
			-1.55,
			1.55,
		)
	}
	a.input.previous_x, a.input.previous_y = a.input.x, a.input.y
	a.forward = direction(a.yaw, a.pitch)
	if a.captured &&
	   !a.menu.open &&
	   a.config.benchmark == "" &&
	   a.input.keys[glfw.KEY_F4] &&
	   !a.input.old_keys[glfw.KEY_F4] {
		player_set_spectator(&a.player, &a.world, &a.position, !a.player.spectator)
	}

	return true
}

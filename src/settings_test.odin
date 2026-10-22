package sandbox

import "core:fmt"
import "core:math"
import "core:os"
import "core:testing"
import "core:time"
import glfw "vendor:glfw"

@(test)
test_settings_roundtrip_and_validation :: proc(t: ^testing.T) {
	dir := fmt.tprintf("build/settings-test-%d", time.tick_now())
	os.make_directory(dir)
	defer os.remove(dir)
	path := fmt.tprintf("%s/settings.cfg", dir)
	defer os.remove(path)
	s := Settings {
		move_speed        = 22,
		mouse_sensitivity = 1.7,
		vsync             = false,
		fps_limit         = 144,
		render_distance = 4,
	}
	testing.expect(t, settings_save(s, path))
	testing.expect(t, settings_save(s, path)) // Replacement must work on every OS.
	loaded, ok := settings_load(path)
	testing.expect(t, ok && loaded == s)
	bad := []string {
		"",
		"VOXSET2 move_speed 14 mouse_sensitivity 1 vsync 1 fps_limit 0",
		"VOXSET1 move_speed NaN mouse_sensitivity 1 vsync 1 fps_limit 0",
		"VOXSET1 move_speed 14 mouse_sensitivity Inf vsync 1 fps_limit 0",
		"VOXSET1 move_speed 0 mouse_sensitivity 1 vsync 1 fps_limit 0",
		"VOXSET1 move_speed 14 mouse_sensitivity 6 vsync 1 fps_limit 0",
		"VOXSET1 move_speed 14 mouse_sensitivity 1 vsync 2 fps_limit 0",
		"VOXSET1 move_speed 14 mouse_sensitivity 1 vsync 1 fps_limit 999",
		"VOXSET1 move_speed 14 mouse_sensitivity 1 vsync 1 fps_limit 60 extra",
	}
	for text in bad {_, valid := settings_decode(text); testing.expect(t, !valid)}
	s.move_speed = math.inf_f64(1)
	testing.expect(t, !settings_save(s, path))
	loaded, ok = settings_load(path)
	testing.expect(t, ok && loaded == Settings{22, 1.7, false, 144, 4, .Full, .Balanced})
}

@(test)
test_menu_keyboard_and_mouse_at_minimum_window_size :: proc(t: ^testing.T) {
	s := settings_defaults()
	menu := Settings_Menu {
		open = true,
	}
	down, old: [512]bool
	down[glfw.KEY_RIGHT] = true
	testing.expect(t, !settings_menu_input(&menu, &s, &down, &old, false, 0, 0, 320, 240))
	testing.expect(t, s.move_speed == 17.5)
	old = down
	settings_menu_input(&menu, &s, &down, &old, false, 0, 0, 320, 240)
	testing.expect(t, s.move_speed == 17.5) // Holding a key doesn't repeat per frame.
	down, old = {}, {}
	l := settings_layout(320, 240)
	settings_menu_input(
		&menu,
		&s,
		&down,
		&old,
		true,
		f64(l.x + l.width - 32),
		f64(l.y + l.row_top + l.row_height + 10),
		320,
		240,
	)
	testing.expect(t, menu.selected == 1 && abs(s.mouse_sensitivity - 1.1) < 0.0001)
	testing.expect(
		t,
		settings_menu_input(
			&menu,
			&s,
			&down,
			&old,
			true,
			f64(l.x + l.width - 64),
			f64(l.y + l.height - 24),
			320,
			240,
		),
	)
}

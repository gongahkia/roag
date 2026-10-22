package sandbox

import "core:fmt"
import "core:os"

// Runtime persistence policy stays here; the binary world codec lives in
// world_save.odin and settings serialization stays in settings.odin.
session_load :: proc(a: ^App) {
	if a.config.generator != 0 {a.world.generator = a.config.generator}
	loaded_world := false
	if !a.config.no_save && os.exists(a.config.save_path) {
		if !world_load(&a.world, &a.position, a.config.save_path) {
			fmt.eprintln("Cannot load world; existing save was preserved:", a.config.save_path)
			os.exit(1)
		}
		if a.config.generator != 0 && a.config.generator != a.world.generator {
			fmt.eprintln(
				"Requested generator conflicts with existing save; save preserved:",
				a.config.save_path,
			)
			os.exit(2)
		}
		loaded_world = true
		fmt.println("Loaded", a.config.save_path)
	}
	if !loaded_world {
		if a.world.generator >= 3 &&
		   a.config.benchmark ==
			   "" {a.position = world_spawn(&a.world)} else {a.position = {8, f64(terrain_height(a.world.seed, 8, 8, a.world.generator)) + 12, 8}}
	}
	fmt.printf("Terrain generator %d / seed %d\n", a.world.generator, a.world.seed)
	a.config.seed = a.world.seed
	player_set_spectator(
		&a.player,
		&a.world,
		&a.position,
		a.config.spectator || a.config.benchmark != "",
	)
	if a.world.generator ==
	   1 {fmt.println("Legacy terrain preserved; a new save generates ores and natural fluids.")}
	a.settings = settings_defaults()
	a.settings.render_distance = a.config.radius
	if !a.config.no_save && os.exists(a.config.settings_path) {
		loaded, ok := settings_load(a.config.settings_path)
		if ok {a.settings = loaded} else {fmt.eprintln("Invalid settings; using defaults:", a.config.settings_path)}
	}
	if a.config.radius_explicit || a.config.benchmark != "" {a.settings.render_distance = a.config.radius}
	if a.config.lighting_explicit {a.settings.lighting = a.config.lighting}
	expected := a.settings; settings_preset(&expected, a.settings.preset)
	if a.settings.render_distance != expected.render_distance || a.settings.lighting != expected.lighting {a.settings.preset = .Custom}
	a.config.radius = a.settings.render_distance
	if !a.config.vsync {a.settings.vsync = false; a.settings.fps_limit = -1}
	if a.config.frame_limit != -2 {a.settings.fps_limit = a.config.frame_limit}
	a.config.vsync = a.settings.vsync
}

session_save_settings :: proc(a: ^App) {
	if a.config.no_save {return}
	a.menu.save_failed = !settings_save(a.settings, a.config.settings_path)
	if a.menu.save_failed {fmt.eprintln("Settings save failed:", a.config.settings_path)}
}

session_save_world :: proc(a: ^App) {
	if a.config.no_save {return}
	a.save.requested = true
}

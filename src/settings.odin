package sandbox

import "core:fmt"
import "core:math"
import "core:os"
import "core:strconv"
import "core:strings"

// Zero follows the current display; -1 deliberately removes the software cap.
FPS_LIMITS := [?]int{0, -1, 30, 60, 90, 120, 144, 165, 240, 360}
Settings :: struct {
	move_speed, mouse_sensitivity: f64,
	vsync:                         bool,
	fps_limit:                     int,
}
Settings_Menu :: struct {
	open:        bool,
	selected:    int,
	save_failed: bool,
}

settings_defaults :: proc() -> Settings {
	return {move_speed = 14, mouse_sensitivity = 1, vsync = true, fps_limit = 0}
}

settings_valid :: proc(s: Settings) -> bool {
	if math.is_nan(s.move_speed) ||
	   math.is_inf(s.move_speed) ||
	   math.is_nan(s.mouse_sensitivity) ||
	   math.is_inf(s.mouse_sensitivity) ||
	   s.move_speed < 1 ||
	   s.move_speed > 512 ||
	   s.mouse_sensitivity < 0.1 ||
	   s.mouse_sensitivity > 5 {return false}
	for limit in FPS_LIMITS {if limit == s.fps_limit {return true}}
	return false
}

settings_decode :: proc(data: string) -> (Settings, bool) {
	parts := strings.fields(data)
	defer delete(parts)
	if len(parts) != 9 ||
	   parts[0] != "VOXSET1" ||
	   parts[1] != "move_speed" ||
	   parts[3] != "mouse_sensitivity" ||
	   parts[5] != "vsync" ||
	   parts[7] != "fps_limit" {
		return {}, false
	}
	s: Settings
	move, move_ok := strconv.parse_f64(parts[2])
	look, look_ok := strconv.parse_f64(parts[4])
	vsync, vsync_ok := strconv.parse_int(parts[6])
	limit, limit_ok := strconv.parse_int(parts[8])
	if !move_ok ||
	   !look_ok ||
	   !vsync_ok ||
	   !limit_ok ||
	   (vsync != 0 && vsync != 1) {return {}, false}
	s = {
		move_speed        = move,
		mouse_sensitivity = look,
		vsync             = vsync == 1,
		fps_limit         = limit,
	}
	return s, settings_valid(s)
}

settings_load :: proc(path: string) -> (Settings, bool) {
	file, err := os.open(path)
	if err != nil {return {}, false}
	defer os.close(file)
	length, size_err := os.file_size(file)
	if size_err != nil || length <= 0 || length > 1024 {return {}, false}
	data, read_err := os.read_entire_file(file, context.allocator)
	if read_err != nil {return {}, false}
	defer delete(data)
	return settings_decode(string(data))
}

settings_save :: proc(s: Settings, path: string) -> bool {
	if !settings_valid(s) {return false}
	dir := os.dir(path)
	if dir != "" {os.make_directory_all(dir)}
	data := fmt.tprintf(
		"VOXSET1\nmove_speed %.6f\nmouse_sensitivity %.6f\nvsync %d\nfps_limit %d\n",
		s.move_speed,
		s.mouse_sensitivity,
		int(s.vsync),
		s.fps_limit,
	)
	temporary := fmt.tprintf("%s.tmp", path)
	if os.write_entire_file(temporary, transmute([]u8)data) != nil {return false}
	return os.rename(temporary, path) == nil
}

settings_adjust :: proc(s: ^Settings, row, delta: int) {
	switch row {
	case 0:
		s.move_speed = clamp(s.move_speed * math.pow(1.25, f64(delta)), 1, 512)
	case 1:
		s.mouse_sensitivity = clamp(math.round(s.mouse_sensitivity * 10 + f64(delta)) / 10, 0.1, 5)
	case 2:
		s.vsync = !s.vsync
	case 3:
		for limit, i in FPS_LIMITS {
			if limit == s.fps_limit {
				s.fps_limit = FPS_LIMITS[(i + delta + len(FPS_LIMITS)) % len(FPS_LIMITS)]
				break
			}
		}
	}
}

fps_limit_label :: proc(limit: int) -> string {
	if limit == 0 {return "DISPLAY"}
	if limit < 0 {return "UNLIMITED"}
	return fmt.tprintf("%d FPS", limit)
}

package sandbox

import "core:fmt"
import "core:os"
import "core:strconv"

Config :: struct {
	profile:                                         string,
	width, height:                                   int,
	radius, workers:                                 int,
	gpu_budget, mesh_budget:                         int,
	cpu_budget, upload_budget:                       int,
	seed:                                            u64,
	generator:                                       u32,
	frames, seconds:                                 int,
	benchmark, save_path, screenshot, settings_path: string,
	frame_limit:                                     int,
	hidden, no_save, vsync, naive_mesh:              bool,
	spectator:                                       bool,
}

usage :: proc() {
	fmt.println(
		`VOXEL / creative sandbox
Usage: voxel [--profile tiny|low|standard] [--seed N] [--generator 1|2|3] [--radius N]
  --gpu-mb N --cpu-mb N --workers N --upload-kb N --width N --height N
  --save PATH --no-save --no-vsync --hidden --frames N --seconds N --screenshot PATH
  --settings PATH --fps-limit display|unlimited|30|60|90|120|144|165|240|360
  --spectator (start with free flight and noclip)
  --benchmark normal|flight|edit|dense|fluids|terrain|sky|caves --naive-mesh
WASD walk / SPACE jump or swim up / SHIFT sprint / F4 spectator
Spectator: SPACE up / CTRL down / SHIFT fast / +/- movement speed
Mouse look / ESC or F1 settings / Q quit
LMB break / RMB place / 1-9 select / TAB palette page / F3 stats / F5 save`,
	)
}

config_parse :: proc() -> (Config, bool) {
	c := Config {
		profile       = "tiny",
		seed          = 42,
		workers       = 2,
		upload_budget = 2 * MiB,
		save_path     = "saves/creative.vox",
		settings_path = "saves/settings.cfg",
		frame_limit   = -2,
		vsync         = true,
	}
	for i := 1; i < len(os.args); i += 1 {
		if os.args[i] == "--profile" {
			if i + 1 >= len(os.args) {
				return c, false
			}
			c.profile = os.args[i + 1]
		}
	}
	switch c.profile {
	case "tiny":
		c.width = 960
		c.height = 540
		c.radius = 4
		c.gpu_budget = 64 * MiB
		c.mesh_budget = 40 * MiB
		c.cpu_budget = 256 * MiB
	case "low":
		c.width = 1280
		c.height = 720
		c.radius = 8
		c.gpu_budget = 128 * MiB
		c.mesh_budget = 88 * MiB
		c.cpu_budget = 512 * MiB
	case "standard":
		c.width = 1920
		c.height = 1080
		c.radius = 12
		c.gpu_budget = 256 * MiB
		c.mesh_budget = 184 * MiB
		c.cpu_budget = 1024 * MiB
	case:
		return c, false
	}
	for i := 1; i < len(os.args); i += 1 {
		arg := os.args[i]
		switch arg {
		case "--help", "-h":
			usage()
			os.exit(0)
		case "--no-save":
			c.no_save = true
		case "--hidden":
			c.hidden = true
		case "--no-vsync":
			c.vsync = false
		case "--naive-mesh":
			c.naive_mesh = true
		case "--spectator":
			c.spectator = true
		case:
			if i + 1 >= len(os.args) {
				return c, false
			}
			i += 1
			v := os.args[i]
			if arg == "--profile" {
				continue
			}
			if arg == "--save" {
				c.save_path = v
				continue
			}
			if arg == "--settings" {c.settings_path = v; continue}
			if arg == "--fps-limit" {
				if v == "display" {c.frame_limit = 0; continue}
				if v == "unlimited" {c.frame_limit = -1; continue}
			}
			if arg == "--screenshot" {
				c.screenshot = v
				continue
			}
			if arg == "--benchmark" {
				c.benchmark = v
				continue
			}
			if arg == "--seed" {
				n, err := strconv.parse_u64(v)
				if !err {
					return c, false
				}
				c.seed = n
				continue
			}
			n, err := strconv.parse_int(v)
			if !err || n < 0 || n > 1000000 {
				return c, false
			}
			switch arg {
			case "--width":
				c.width = n
			case "--height":
				c.height = n
			case "--radius":
				c.radius = n
			case "--generator":
				if n < 1 || n > GENERATOR_VERSION {return c, false}
				c.generator = u32(n)
			case "--workers":
				c.workers = n
			case "--gpu-mb":
				c.gpu_budget = n * MiB
				c.mesh_budget = c.gpu_budget * 5 / 8
			case "--cpu-mb":
				c.cpu_budget = n * MiB
			case "--upload-kb":
				c.upload_budget = n * 1024
			case "--frames":
				c.frames = n
			case "--seconds":
				c.seconds = n
			case "--fps-limit":
				c.frame_limit = n
			case:
				return c, false
			}
		}
	}
	if c.frame_limit != -2 {
		test_settings := settings_defaults(); test_settings.fps_limit = c.frame_limit
		if !settings_valid(test_settings) {return c, false}
	}
	if c.benchmark != "" &&
	   c.benchmark != "normal" &&
	   c.benchmark != "flight" &&
	   c.benchmark != "edit" &&
	   c.benchmark != "fluids" &&
	   c.benchmark != "terrain" &&
	   c.benchmark != "sky" &&
	   c.benchmark != "caves" &&
	   c.benchmark != "dense" {
		return c, false
	}
	if c.width < 320 ||
	   c.width > 3840 ||
	   c.height < 240 ||
	   c.height > 2160 ||
	   c.radius < 1 ||
	   c.radius > 32 ||
	   c.workers < 1 ||
	   c.workers > 8 ||
	   c.gpu_budget < 16 * MiB ||
	   c.gpu_budget > 4096 * MiB ||
	   c.cpu_budget < 32 * MiB ||
	   c.cpu_budget > 8192 * MiB ||
	   c.upload_budget < 16 * 1024 ||
	   c.upload_budget > 64 * MiB ||
	   c.frames < 0 {
		return c, false
	}
	c.workers = min(
		c.workers,
		max(
			1,
			(c.cpu_budget - CPU_BACKGROUND_BYTES - WORLD_EDIT_HEADROOM_BYTES) /
			WORKER_MEMORY_BYTES,
		),
	)
	c.mesh_budget = min(
		c.mesh_budget,
		c.gpu_budget - graphics_reserved_bytes(0, c.width, c.height),
	)
	if c.mesh_budget < MiB {
		return c, false
	}
	if c.benchmark != "" {
		c.no_save = true
		c.vsync = false
		if c.frames == 0 && c.seconds == 0 {
			c.seconds = 30
		}
	}
	return c, true
}

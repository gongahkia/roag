package sandbox

import "core:fmt"
import "core:math"
import "core:os"

benchmark_valid :: proc(scene: string) -> bool {
	choices := []string {
		"",
		"normal",
		"generation",
		"lighting",
		"light-room",
		"ocean",
		"river",
		"lake",
		"flight",
		"edit",
		"dense",
		"fluids",
		"terrain",
		"sky",
		"caves",
		"plains",
		"forest",
		"desert",
		"coast",
		"arch",
		"overhang",
		"spire",
		"snowy-plains",
		"taiga",
		"savanna",
		"jungle",
		"swamp",
		"badlands",
		"birch",
		"cherry",
		"mushrooms",
		"snowy-peaks",
	}
	for choice in choices {if scene == choice {return true}}
	return false
}

terrain_find_form :: proc(
	seed: u64,
	kind: i32,
	generator: u32 = 3,
) -> (
	form: Ground_Form,
	found: bool,
) {
	for ring: i32 = 0; ring <= 32; ring += 1 {
		for z := -ring; z <= ring; z += 1 {for x := -ring; x <= ring; x += 1 {
				if max(abs(x), abs(z)) != ring {continue}
				f := ground_form(seed, x, z, generator)
				if f.active && f.kind == kind {return f, true}
			}}
	}
	return {}, false
}

terrain_find_landscape :: proc(seed: u64, biome: Biome, generator: u32 = 3) -> (p: I3, ok: bool) {
	for ring: i32 = 0; ring <= 64; ring += 1 {
		for z := -ring; z <= ring; z += 1 {for x := -ring; x <= ring; x += 1 {
				if max(abs(x), abs(z)) != ring {continue}
				c := terrain_base_for(seed, x * 64, z * 64, generator)
				if c.biome == biome &&
				   (biome != .Mountains ||
						   generator >= 4 ||
						   c.height >= 112) {return {x * 64, c.height, z * 64}, true}
			}}
	}
	return {}, false
}

// Previews of forest regions look at actual vegetation on gentle ground. The
// general landscape finder stays stable for compatibility/golden fixtures.
terrain_biome_view :: proc(seed: u64, biome: Biome, generator: u32) -> (p: I3, ok: bool) {
	if generator < 4 {return terrain_find_landscape(seed, biome, generator)}
	if generator >= 5 && (biome == .Mountains || biome == .Snowy_Peaks) {
		p, ok := terrain_find_landscape(seed, biome, generator)
		if !ok {return p, false}
		best := p
		for dz: i32 = -64; dz <= 64; dz += 16 {for dx: i32 = -64; dx <= 64; dx += 16 {
				c := geography_dry_column(seed, p.x + dx, p.z + dz)
				if c.height > best.y {best = {p.x + dx, c.height, p.z + dz}}
			}}
		return best, true
	}
	if generator >= 5 && biome == .Ocean {
		for ring: i32 = 0;
		    ring <= 48;
		    ring += 1 {for z := -ring; z <= ring; z += 1 {for x := -ring; x <= ring; x += 1 {
					if max(abs(x), abs(z)) != ring {continue}
					c := geography_dry_column(seed, x * 64, z * 64)
					if c.biome == .Ocean &&
					   c.height >= 36 &&
					   c.temperature > TQ * 40 / 100 {return {x * 64, c.height, z * 64}, true}
				}}}
	}
	vegetated :=
		biome == .Birch_Forest ||
		biome == .Cherry_Grove ||
		biome == .Forest ||
		biome == .Jungle ||
		biome == .Taiga ||
		biome == .Mushroom_Fields ||
		biome == .Savanna ||
		biome == .Desert ||
		biome == .Badlands ||
		biome == .Swamp
	if !vegetated {return terrain_find_landscape(seed, biome, generator)}
	for ring: i32 = 0; ring <= 64; ring += 1 {
		for z := -ring; z <= ring; z += 1 {for x := -ring; x <= ring; x += 1 {
				if max(abs(x), abs(z)) != ring {continue}
				c := terrain_base_for(seed, x * 64, z * 64, generator)
				if c.biome != biome || c.slope > TQ / 2 || c.height > 100 {continue}
				interior := true
				for dz: i32 = -1; dz <= 1; dz += 2 {for dx: i32 = -1; dx <= 1; dx += 2 {
						if terrain_base_for(seed, x * 64 + dx * 12, z * 64 + dz * 12, generator).biome !=
						   biome {interior = false}
					}}
				if !interior {continue}
				if generator >= 5 {
					form := ground_form(
						seed,
						floor_div(x * 64, 128),
						floor_div(z * 64, 128),
						generator,
					)
					if form.active &&
					   max(abs(form.x - x * 64), abs(form.z - z * 64)) < 64 {continue}
				}
				for dz: i32 = -2; dz <= 2; dz += 1 {for dx: i32 = -2; dx <= 2; dx += 1 {
						t := plant_at(seed, x * 8 + dx, z * 8 + dz, generator)
						if t.active &&
						   terrain_base_for(seed, t.x, t.z, generator).biome ==
							   biome {return {t.x, t.h, t.z}, true}
					}}
			}}
	}
	return terrain_find_landscape(seed, biome, generator)
}

terrain_find_island :: proc(seed: u64) -> (p: I3, ok: bool) {
	for ring: i32 = 0; ring <= 8; ring += 1 {
		for z := -ring; z <= ring; z += 1 {for x := -ring; x <= ring; x += 1 {
				if max(abs(x), abs(z)) != ring {continue}
				cx, cz, active := sky_region(seed, x, z)
				if !active {continue}
				for dz: i32 = -1; dz <= 1; dz += 1 {for dx: i32 = -1; dx <= 1; dx += 1 {
						ix, iz := floor_div(cx, 128) + dx, floor_div(cz, 128) + dz
						h := hash3(seed + 3101, ix, 0, iz)
						px, pz :=
							ix * 128 +
							64 +
							i32(h >> 8 & 15) -
							8,
							iz * 128 +
							64 +
							i32(h >> 16 & 15) -
							8
						bottom, top := island_span(seed, px, pz)
						if bottom >= 0 {return {px, top, pz}, true}
					}}
			}}
	}
	return {}, false
}

terrain_find_lake :: proc(seed: u64, generator: u32 = 3) -> (cell: Cave_Cell, ok: bool) {
	for ring: i32 = 0; ring <= 24; ring += 1 {
		for z := -ring; z <= ring; z += 1 {for x := -ring; x <= ring; x += 1 {
				if max(abs(x), abs(z)) != ring {continue}
				for y: i32 = 0; y <= 2; y += 1 {
					c := cave_cell_v3(seed, {x, y, z}, generator)
					if c.lake {return c, true}
				}
			}}
	}
	return {}, false
}

benchmark_prepare :: proc(a: ^App) {
	scene := a.config.benchmark
	if scene == "light-room" {benchmark_light_room(a); return}
	biome: Biome
	landscape := true
	switch scene {
	case "ocean":
		biome = .Ocean
	case "terrain":
		biome = .Mountains
	case "plains":
		biome = .Plains
	case "forest":
		biome = .Forest
	case "desert":
		biome = .Desert
	case "coast":
		biome = .Coast
	case "snowy-plains":
		biome = .Snowy_Plains
	case "taiga":
		biome = .Taiga
	case "savanna":
		biome = .Savanna
	case "jungle":
		biome = .Jungle
	case "swamp":
		biome = .Swamp
	case "badlands":
		biome = .Badlands
	case "birch":
		biome = .Birch_Forest
	case "cherry":
		biome = .Cherry_Grove
	case "mushrooms":
		biome = .Mushroom_Fields
	case "snowy-peaks":
		biome = .Snowy_Peaks

	case:
		landscape = false
	}
	if scene == "river" || scene == "lake" {
		p, ok := terrain_find_river(a.world.seed, scene == "lake")
		if !ok {fmt.eprintln("No drainage fixture for seed"); os.exit(2)}
		a.benchmark_target = {f64(p.x), f64(p.y), f64(p.z)}
		a.benchmark_camera = a.benchmark_target + V3{36, 50, 36}
	} else if landscape {
		p, ok := terrain_biome_view(a.world.seed, biome, a.world.generator)
		if !ok {fmt.eprintln("No landscape benchmark fixture for seed"); os.exit(2)}
		a.benchmark_target = {f64(p.x), f64(p.y), f64(p.z)}
		if a.world.generator >= 5 && biome == .Ocean {a.benchmark_target.y = SEA_LEVEL}
		a.benchmark_camera = a.benchmark_target + V3{24, 22, 24}
		if a.world.generator >= 5 &&
		   (biome == .Mountains ||
				   biome ==
					   .Snowy_Peaks) {a.benchmark_camera = a.benchmark_target + V3{46, 42, 46}}
		if a.world.generator < 5 {
			a.benchmark_camera.y = max(
				a.benchmark_camera.y,
				f64(
					max(
						terrain_height(
							a.world.seed,
							i32(a.benchmark_camera.x),
							i32(a.benchmark_camera.z),
							2,
						),
						terrain_height(
							a.world.seed,
							i32(a.benchmark_camera.x),
							i32(a.benchmark_camera.z),
							3,
						),
					),
				) +
				8,
			)
		}
		a.benchmark_camera.y = max(
			a.benchmark_camera.y,
			f64(
				terrain_height(
					a.world.seed,
					i32(a.benchmark_camera.x),
					i32(a.benchmark_camera.z),
					a.world.generator,
				),
			) +
			18,
		)
	} else if scene == "arch" || scene == "overhang" || scene == "spire" {
		kind: i32 = scene == "arch" ? 0 : (scene == "overhang" ? 1 : 2)
		f, ok := terrain_find_form(a.world.seed, kind, a.world.generator)
		if !ok {fmt.eprintln("No landmark benchmark fixture for seed"); os.exit(2)}
		a.benchmark_target = {f64(f.x), f64(f.base + f.height / 2), f64(f.z)}
		a.benchmark_camera = a.benchmark_target + V3{26, 12, 28}
	} else if scene == "sky" {
		p, ok := terrain_find_island(a.world.seed)
		if !ok {fmt.eprintln("No island benchmark fixture for seed"); os.exit(2)}
		a.benchmark_target = {f64(p.x), f64(p.y - 10), f64(p.z)}
		a.benchmark_camera = a.benchmark_target + V3{42, 20, 48}
	} else if scene == "caves" {
		cell, ok := terrain_find_lake(a.world.seed, a.world.generator)
		if !ok {fmt.eprintln("No lake benchmark fixture for seed"); os.exit(2)}
		a.benchmark_target = {
			f64(cell.center.x),
			f64(cell.waterline),
			f64(cell.center.z - cell.radius.z),
		}
		a.benchmark_camera = {
			f64(cell.center.x),
			f64(cell.center.y + 2),
			f64(cell.center.z + cell.radius.z / 3),
		}
	} else {return}
	a.position = a.benchmark_camera
	player_unstuck(&a.world, &a.position)
	a.benchmark_camera = a.position
	a.benchmark_view = true
	fmt.printf(
		"Terrain scene %s: camera %.1f %.1f %.1f / target %.1f %.1f %.1f\n",
		scene,
		a.position.x,
		a.position.y,
		a.position.z,
		a.benchmark_target.x,
		a.benchmark_target.y,
		a.benchmark_target.z,
	)
}

// Repeatable graphical fixture: elevated waterfall/lavafall and an ore wall.
fluid_demo :: proc(w: ^World) {
	for z in 0 ..= 24 {
		for x in 0 ..= 24 {
			world_apply(w, {w.sequence + 1, {i32(x), 84, i32(z)}, .Stone})
			if x == 0 || x == 24 || z == 0 || z == 24 {
				for y in 85 ..= 87 {world_apply(w, {w.sequence + 1, {i32(x), i32(y), i32(z)}, .Cobblestone})}
			}
		}
	}
	for i in 0 ..< 8 {
		for y in 85 ..= 88 {
			for x in 0 ..< 2 {world_apply(w, {w.sequence + 1, {2 + i32(i * 3 + x), i32(y), 2}, Block(int(Block.Coal_Ore) + i)})}
		}
	}
	world_apply(w, {w.sequence + 1, {6, 90, 8}, .Water})
	world_apply(w, {w.sequence + 1, {15, 90, 8}, .Lava})
}

benchmark_step :: proc(a: ^App) {
	if a.config.benchmark != "" {
		// Fixed-time path gives equivalent routes even at different frame rates.
		t := max(a.elapsed - (a.config.benchmark_ready ? a.benchmark_ready_at : 2), 0)
		if a.config.benchmark_ready && a.benchmark_ready_at == 0 {t = 0}
		if a.config.benchmark == "flight" {
			cycle := math.mod(t, 24)
			travel := cycle
			if cycle > 12 {
				travel = 24 - cycle
			}
			a.position.x = 8 + travel * 38
			a.position.z = 8 + math.sin(cycle * (2 * math.PI / 24)) * 80
			a.position.y =
				f64(
					terrain_height(
						a.world.seed,
						i32(a.position.x),
						i32(a.position.z),
						a.world.generator,
					),
				) +
				24
			a.yaw = 0.4
			a.pitch = -0.35
		} else {
			a.position = {8, 100, 8}
			a.yaw = -0.8
			a.pitch = -0.5
		}
		if a.config.benchmark ==
		   "fluids" {a.position = {26, 97, 26}; a.yaw = -2.25; a.pitch = -0.55}
		if a.benchmark_view {
			a.position = a.benchmark_camera
			d := normalize(a.benchmark_target - a.position)
			a.yaw = math.atan2(d.z, d.x)
			a.pitch = math.asin(d.y)
		}
		a.forward = direction(a.yaw, a.pitch)
		if a.config.benchmark == "edit" && int(t * 4) > a.benchmark_edit_step {
			a.benchmark_edit_step = int(t * 4)
			p := I3{12, 84, 12}
			b: Block = a.benchmark_edit_step % 2 == 0 ? .Lamp : .Air
			if world_get(&a.world, p) != b && world_apply(&a.world, {a.world.sequence + 1, p, b}) {stream_invalidate(&a.stream, p)}
		}
		if a.config.benchmark == "dense" && a.frame == 0 {
			for z in 0 ..< 32 {
				for y in 0 ..< 32 {
					for x in 0 ..< 32 {
						p := I3{i32(x), i32(y + 64), i32(z)}
						b: Block = .Air
						if (x + y + z) % 2 == 0 {
							b = .Stone
						}
						world_apply(&a.world, {a.world.sequence + 1, p, b})
					}
				}
			}
		}
	}
}

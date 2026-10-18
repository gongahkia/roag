package sandbox

import "core:fmt"
import "core:math"

terrain_find_landscape :: proc(seed: u64, biome: Biome) -> (p: I3, ok: bool) {
	for ring: i32 = 0; ring <= 32; ring += 1 {
		for z := -ring; z <= ring; z += 1 {for x := -ring; x <= ring; x += 1 {
			if max(abs(x), abs(z)) != ring {continue}
			c := terrain_base_column(seed, x * 64, z * 64)
			if c.biome == biome {return {x * 64, c.height, z * 64}, true}
		}}
	}
	return {}, false
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
				px, pz := ix * 128 + 64 + i32(h >> 8 & 15) - 8, iz * 128 + 64 + i32(h >> 16 & 15) - 8
				bottom, top := island_span(seed, px, pz)
				if bottom >= 0 {return {px, top, pz}, true}
			}}
		}}
	}
	return {}, false
}

terrain_find_lake :: proc(seed: u64) -> (cell: Cave_Cell, ok: bool) {
	for ring: i32 = 0; ring <= 24; ring += 1 {
		for z := -ring; z <= ring; z += 1 {for x := -ring; x <= ring; x += 1 {
			if max(abs(x), abs(z)) != ring {continue}
			for y: i32 = 0; y <= 2; y += 1 {
				c := cave_cell_v3(seed, {x, y, z})
				if c.lake {return c, true}
			}
		}}
	}
	return {}, false
}

benchmark_prepare :: proc(a: ^App) {
	scene := a.config.benchmark
	if scene == "terrain" {
		p, ok := terrain_find_landscape(a.world.seed, .Mountains)
		if !ok {p = {8, terrain_height(a.world.seed, 8, 8, a.world.generator), 8}}
		a.benchmark_target = {f64(p.x), f64(p.y), f64(p.z)}
		a.benchmark_camera = a.benchmark_target + V3{44, 32, 44}
	} else if scene == "sky" {
		p, ok := terrain_find_island(a.world.seed)
		if !ok {fmt.eprintln("No island benchmark fixture for seed"); return}
		a.benchmark_target = {f64(p.x), f64(p.y - 10), f64(p.z)}
		a.benchmark_camera = a.benchmark_target + V3{42, 20, 48}
	} else if scene == "caves" {
		cell, ok := terrain_find_lake(a.world.seed)
		if !ok {fmt.eprintln("No lake benchmark fixture for seed"); return}
		a.benchmark_target = {f64(cell.center.x), f64(cell.waterline), f64(cell.center.z - cell.radius.z)}
		a.benchmark_camera = {f64(cell.center.x), f64(cell.center.y + 2), f64(cell.center.z + cell.radius.z / 3)}
	} else {return}
	a.position = a.benchmark_camera
	fmt.printf("Terrain scene %s: camera %.1f %.1f %.1f / target %.1f %.1f %.1f\n", scene,
		a.position.x, a.position.y, a.position.z, a.benchmark_target.x, a.benchmark_target.y, a.benchmark_target.z)
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
		t := max(a.elapsed - 2, 0)
		if a.config.benchmark == "flight" {
			cycle := math.mod(t, 24)
			travel := cycle
			if cycle > 12 {
				travel = 24 - cycle
			}
			a.position.x = 8 + travel * 38
			a.position.z = 8 + math.sin(t * 0.3) * 80
			a.position.y =
				f64(terrain_height(a.world.seed, i32(a.position.x), i32(a.position.z), a.world.generator)) + 24
			a.yaw = 0.4
			a.pitch = -0.35
		} else {
			a.position = {8, 100, 8}
			a.yaw = -0.8
			a.pitch = -0.5
		}
		if a.config.benchmark ==
		   "fluids" {a.position = {26, 97, 26}; a.yaw = -2.25; a.pitch = -0.55}
		if a.config.benchmark == "terrain" || a.config.benchmark == "sky" || a.config.benchmark == "caves" {
			a.position = a.benchmark_camera
			d := normalize(a.benchmark_target - a.position)
			a.yaw = math.atan2(d.z, d.x)
			a.pitch = math.asin(d.y)
		}
		a.forward = direction(a.yaw, a.pitch)
		if a.config.benchmark == "edit" && a.frame % 8 == 0 {
			p := I3{12 + i32((a.frame / 8) % 16), 84, 12 + i32((a.frame / 128) % 16)}
			b: Block = .Stone
			if (a.frame / 2048) % 2 == 1 {
				b = .Air
			}
			if world_apply(&a.world, {a.world.sequence + 1, p, b}) {
				stream_invalidate(&a.stream, p)
			}
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

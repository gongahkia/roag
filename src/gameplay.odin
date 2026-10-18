package sandbox

import "core:math"
import glfw "vendor:glfw"

app_update_gameplay :: proc(a: ^App) {
	if a.captured && a.config.benchmark == "" {
		move: V3
		horizontal := V3{math.cos(a.yaw), 0, math.sin(a.yaw)}
		right := V3{-math.sin(a.yaw), 0, math.cos(a.yaw)}
		if a.input.keys[glfw.KEY_W] {
			move += horizontal
		}
		if a.input.keys[glfw.KEY_S] {
			move -= horizontal
		}
		if a.input.keys[glfw.KEY_D] {
			move += right
		}
		if a.input.keys[glfw.KEY_A] {
			move -= right
		}
		player_update(
			&a.player,
			&a.world,
			&a.position,
			{
				move = move,
				speed = a.settings.move_speed,
				jump = a.input.keys[glfw.KEY_SPACE] && !a.input.old_keys[glfw.KEY_SPACE],
				ascend = a.input.keys[glfw.KEY_SPACE],
				descend = a.input.keys[glfw.KEY_LEFT_CONTROL],
				sprint = a.input.keys[glfw.KEY_LEFT_SHIFT],
			},
			a.dt,
		)
	} else {
		a.player.accumulator = 0; a.player.jump_pending = false; a.player.last_ms = 0
	}

	benchmark_step(a)
	a.position.y = clamp(a.position.y, -16, 1024)
	a.position.x = clamp(a.position.x, -100000000, 100000000)
	a.position.z = clamp(a.position.z, -100000000, 100000000)
	a.hit = raycast(&a.world, a.position, a.forward, 8)
	for pressed, i in a.input.mouse {
		if pressed &&
		   !a.input.old_mouse[i] &&
		   a.captured &&
		   !a.menu.open &&
		   !a.input.menu_was_open &&
		   !a.input.capture_changed {
			if a.hit.hit {
				p := a.hit.block
				b: Block = .Air
				if i == 1 {
					p = a.hit.adjacent
					b = CREATIVE_BLOCKS[a.selected]
					if is_fluid(world_get(&a.world, a.hit.block)) {p = a.hit.block}
				}
				if !a.player.spectator &&
				   is_solid(b) &&
				   player_overlaps_block(a.position, p) {continue}
				previous := world_get(&a.world, p)
				if world_apply(&a.world, {a.world.sequence + 1, p, b}) {
					stream_invalidate(&a.stream, p)
					fluids_notify(&a.fluids, &a.world, p, previous)
				}
			}
		}
	}
	a.input.old_mouse = a.input.mouse
	fluids_reseed(&a.fluids, &a.world, a.position, a.now)
	fluids_step(&a.fluids, &a.world, u64(a.now * 10))
	for i in 0 ..< a.fluids.changed_count {stream_invalidate(&a.stream, a.fluids.changed[i])}
	if a.input.keys[glfw.KEY_F5] && !a.input.old_keys[glfw.KEY_F5] {session_save_world(a)}
	a.input.old_keys = a.input.keys
}

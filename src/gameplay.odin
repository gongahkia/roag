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
				descend = a.input.keys[glfw.KEY_LEFT_CONTROL] && !a.input.keys[glfw.KEY_Z] && !a.input.keys[glfw.KEY_Y],
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
	can_edit := a.captured && !a.input.menu_was_open && !a.input.capture_changed
	if can_edit {
		ctrl := a.input.keys[glfw.KEY_LEFT_CONTROL] || a.input.keys[glfw.KEY_RIGHT_CONTROL]
		if ctrl && a.input.keys[glfw.KEY_Z] && !a.input.old_keys[glfw.KEY_Z] {creative_history(a, a.input.keys[glfw.KEY_LEFT_SHIFT])}
		if ctrl && a.input.keys[glfw.KEY_Y] && !a.input.old_keys[glfw.KEY_Y] {creative_history(a, true)}
		if a.input.mouse[2] && !a.input.old_mouse[2] && a.hit.hit {
			index := creative_pick(world_get(&a.world, a.hit.block))
			if index >= 0 {a.selected = index}
		}
		for i in 0 ..< 2 {
			if !edit_repeat_due(&a.edit_repeat, i, a.input.mouse[i], a.input.old_mouse[i], a.now) || !a.hit.hit {continue}
			p := a.hit.block; b := Block.Air
			if i == 1 {
				p = a.hit.adjacent; b = CREATIVE_BLOCKS[a.selected]
				if is_fluid(world_get(&a.world, a.hit.block)) {p = a.hit.block}
			}
			creative_apply(a, p, b)
			// A second action in this frame must target the updated world.
			a.hit = raycast(&a.world, a.position, a.forward, 8)
		}
	} else {a.edit_repeat = {}}
	a.input.old_mouse = a.input.mouse
	fluids_reseed(&a.fluids, &a.world, a.position, a.now)
	fluids_step(&a.fluids, &a.world, u64(a.now * 10))
	stream_invalidate_many(&a.stream, a.fluids.changed[:a.fluids.changed_count])
	if a.input.keys[glfw.KEY_F5] && !a.input.old_keys[glfw.KEY_F5] {session_save_world(a)}
	a.input.old_keys = a.input.keys
}

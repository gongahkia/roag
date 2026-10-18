package sandbox

import "core:testing"

@(test)
test_frame_caps_follow_active_display_without_guessing :: proc(t: ^testing.T) {
	s := settings_defaults()
	testing.expect(t, frame_target(s, 144) == 144)
	testing.expect(t, frame_target(s, 0) == 0)
	s.fps_limit = 240
	testing.expect(t, frame_target(s, 60) == 60)
	s.fps_limit = 30
	testing.expect(t, frame_target(s, 144) == 30)
	s.fps_limit = -1
	testing.expect(t, frame_target(s, 165) == 165)
	s.vsync = false
	testing.expect(t, frame_target(s, 165) == 0)
	s.fps_limit = 240
	testing.expect(t, frame_target(s, 60) == 240)
	testing.expect(t, renderer_is_software("Mesa llvmpipe (LLVM)"))
	testing.expect(t, !renderer_is_software("Intel(R) UHD Graphics"))
}

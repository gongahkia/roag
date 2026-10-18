#+build linux, darwin
package sandbox

import "core:time"

Frame_Sleeper :: struct {}
frame_sleep_init :: proc() -> Frame_Sleeper {return {}}
frame_sleep_destroy :: proc(s: ^Frame_Sleeper) {}
frame_sleep :: proc(s: ^Frame_Sleeper, seconds: f64) {
	time.sleep(time.Duration(seconds * 1e9))
}

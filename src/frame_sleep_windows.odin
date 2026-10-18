#+build windows
package sandbox

import "core:math"
import win "core:sys/windows"

Frame_Sleeper :: struct {timer: win.HANDLE}
frame_sleep_init :: proc() -> Frame_Sleeper {
	timer := win.CreateWaitableTimerExW(nil, nil, win.CREATE_WAITABLE_TIMER_HIGH_RESOLUTION, win.TIMER_ALL_ACCESS)
	if timer == nil {timer = win.CreateWaitableTimerW(nil, false, nil)}
	return {timer}
}
frame_sleep_destroy :: proc(s: ^Frame_Sleeper) {
	if s.timer != nil {win.CloseHandle(s.timer); s.timer = nil}
}
frame_sleep :: proc(s: ^Frame_Sleeper, seconds: f64) {
	// Relative due times use negative 100-ns units. Avoid a busy wait or changing
	// the global Windows timer period, and retain sub-millisecond cap precision.
	if s.timer != nil {
		due := win.LARGE_INTEGER(-max(1, i64(math.ceil(seconds * 1e7))))
		if win.SetWaitableTimerEx(s.timer, &due, 0, nil, nil, nil, 0) {
			win.WaitForSingleObject(s.timer, win.INFINITE)
			return
		}
	}
	win.Sleep(win.DWORD(math.ceil(seconds * 1000)))
}

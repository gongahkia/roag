package sandbox

import win "core:sys/windows"

process_sample :: proc() -> Process_Sample {
	result: Process_Sample
	process := win.GetCurrentProcess()
	creation, exit_time, kernel, user: win.FILETIME
	if win.GetProcessTimes(process, &creation, &exit_time, &kernel, &user) {
		kernel_ticks := u64(kernel.dwLowDateTime) | (u64(kernel.dwHighDateTime) << 32)
		user_ticks := u64(user.dwLowDateTime) | (u64(user.dwHighDateTime) << 32)
		result.cpu_seconds = f64(kernel_ticks + user_ticks) * 1e-7; result.cpu_valid = true
	}
	memory: win.PROCESS_MEMORY_COUNTERS; memory.cb = size_of(memory)
	if win.GetProcessMemoryInfo(process, &memory, size_of(memory)) {
		result.rss_bytes = u64(
			memory.WorkingSetSize,
		); result.peak_rss_bytes = u64(memory.PeakWorkingSetSize)
		result.page_faults = u64(memory.PageFaultCount); result.ram_valid = true
	}
	return result
}

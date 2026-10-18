package sandbox

import darwin "core:sys/darwin"
import posix "core:sys/posix"

process_sample :: proc() -> Process_Sample {
	result := process_usage()
	info: darwin.proc_taskinfo
	if darwin.proc_pidinfo(posix.getpid(), .TASKINFO, 0, &info, size_of(info)) == size_of(info) {
		result.rss_bytes = info.pti_resident_size; result.ram_valid = true
	}
	result.peak_rss_bytes = max(result.peak_rss_bytes, result.rss_bytes)
	return result
}

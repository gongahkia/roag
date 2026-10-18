#+build linux, darwin
package sandbox

import posix "core:sys/posix"

process_usage :: proc() -> Process_Sample {
	result: Process_Sample
	usage: posix.rusage
	if posix.getrusage(.SELF, &usage) == .OK {
		result.cpu_seconds =
			f64(usage.ru_utime.tv_sec) +
			f64(usage.ru_stime.tv_sec) +
			(f64(usage.ru_utime.tv_usec) + f64(usage.ru_stime.tv_usec)) * 1e-6
		result.cpu_valid = true
		result.page_faults = u64(max(usage.ru_minflt, 0)) + u64(max(usage.ru_majflt, 0))
		when ODIN_OS == .Linux {result.peak_rss_bytes = u64(max(usage.ru_maxrss, 0)) * 1024} else {result.peak_rss_bytes = u64(max(usage.ru_maxrss, 0))}
	}
	return result
}

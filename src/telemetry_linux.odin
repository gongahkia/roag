package sandbox

import "core:os"
import posix "core:sys/posix"

process_sample :: proc() -> Process_Sample {
	result := process_usage()
	file, err := os.open("/proc/self/statm")
	if err == nil {
		buffer: [256]u8
		count, read_err := os.read(file, buffer[:]); os.close(file)
		if read_err == nil {
			at := 0
			for at < count && buffer[at] != ' ' {at += 1}
			for at < count && buffer[at] == ' ' {at += 1}
			pages: u64; start := at
			for at < count &&
			    buffer[at] >= '0' &&
			    buffer[at] <= '9' {pages = pages * 10 + u64(buffer[at] - '0'); at += 1}
			page_size := posix.sysconf(._PAGESIZE)
			if at > start &&
			   page_size > 0 {result.rss_bytes = pages * u64(page_size); result.ram_valid = true}
		}
	}
	result.peak_rss_bytes = max(result.peak_rss_bytes, result.rss_bytes)
	return result
}

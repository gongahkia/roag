package sandbox

import "core:fmt"
import "core:os"

main :: proc() {
	config, valid := config_parse()
	if !valid {
		usage()
		fmt.eprintln("Invalid arguments or insufficient budget for resolution")
		os.exit(2)
	}
	if config.benchmark == "generation" {benchmark_generation(config); return}
	app_run(config)
}

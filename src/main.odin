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
	app_run(config)
}

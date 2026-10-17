package levels

import _ "embed"

// Default is the editable shrine level embedded for binaries launched outside
// the repository. Pass -level to load a file at runtime instead.
//
//go:embed tidal_shrine.json
var Default []byte

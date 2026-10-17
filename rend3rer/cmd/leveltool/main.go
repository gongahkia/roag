// Command leveltool validates a level or writes its normalized JSON form.
package main

import (
	"flag"
	"fmt"
	"log"
	"os"

	"rend3rer/internal/soft3d"
)

func main() {
	in := flag.String("in", "levels/tidal_shrine.json", "level JSON to read")
	out := flag.String("out", "", "write the decoded level as formatted JSON")
	flag.Parse()
	data, err := os.ReadFile(*in)
	if err != nil {
		log.Fatal(err)
	}
	level, err := soft3d.DecodeLevel(data)
	if err != nil {
		log.Fatal(err)
	}
	scene, err := level.BuildScene()
	if err != nil {
		log.Fatal(err)
	}
	if *out != "" {
		encoded, err := level.Encode()
		if err != nil {
			log.Fatal(err)
		}
		if err := os.WriteFile(*out, append(encoded, '\n'), 0644); err != nil {
			log.Fatal(err)
		}
	}
	fmt.Printf("%s: %d geometry entries, %d static quads; valid\n", level.Name, len(level.Geometry), len(scene.Static))
}

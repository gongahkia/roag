package launch

import (
	"os"
	"path/filepath"
	"reflect"
	"strings"
	"testing"
)

func TestWindowsPathsPreserveArgumentsAndConvertFileFlags(t *testing.T) {
	args := []string{"-seed", "42", "-level", "levels/my shrine.json", "-input-log=/tmp/mouse trace.csv", "-sensitivity", "0.5", "-mute"}
	got, err := windowsPaths(args, func(s string) (string, error) { return "WIN:" + s, nil })
	if err != nil {
		t.Fatal(err)
	}
	want := []string{"-seed", "42", "-level", "WIN:levels/my shrine.json", "-input-log=WIN:/tmp/mouse trace.csv", "-sensitivity", "0.5", "-mute"}
	if !reflect.DeepEqual(got, want) {
		t.Fatalf("arguments changed: %#v", got)
	}
	if args[3] != "levels/my shrine.json" {
		t.Fatal("forwarding mutated original arguments")
	}
	got, err = windowsPaths([]string{"--level=x.json", "--input-log", "log.csv"}, func(s string) (string, error) { return "WIN:" + s, nil })
	if err != nil || !reflect.DeepEqual(got, []string{"--level=WIN:x.json", "--input-log", "WIN:log.csv"}) {
		t.Fatalf("long flag syntax failed: %v, %v", got, err)
	}
}

func TestCrossBuildCannotInheritLinuxTargetOrCgo(t *testing.T) {
	env := windowsEnvironment([]string{"GOOS=linux", "GOOS=darwin", "GOARCH=386", "CGO_ENABLED=1", "PATH=/example", "EXTRA=unchanged"}, "amd64")
	counts := map[string]int{}
	for _, entry := range env {
		key, _, _ := strings.Cut(entry, "=")
		counts[key]++
	}
	for _, key := range []string{"GOOS", "GOARCH", "CGO_ENABLED"} {
		if counts[key] != 1 {
			t.Fatalf("%s has duplicate or missing values", key)
		}
	}
	for _, entry := range []string{"GOOS=windows", "GOARCH=amd64", "CGO_ENABLED=0", "PATH=/example", "EXTRA=unchanged"} {
		found := false
		for _, got := range env {
			if got == entry {
				found = true
			}
		}
		if !found {
			t.Fatalf("missing %q", entry)
		}
	}
}

func TestModuleRootFindsCheckoutFromNestedDirectory(t *testing.T) {
	root := t.TempDir()
	if err := os.WriteFile(filepath.Join(root, "go.mod"), []byte("module rend3rer\n\ngo 1.25.0\n"), 0644); err != nil {
		t.Fatal(err)
	}
	nested := filepath.Join(root, "some", "nested", "folder")
	if err := os.MkdirAll(nested, 0755); err != nil {
		t.Fatal(err)
	}
	got, err := moduleRoot(nested)
	if err != nil || got != root {
		t.Fatalf("root=%q error=%v", got, err)
	}
}

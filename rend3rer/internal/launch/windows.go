// Package launch keeps WSL development on Linux while running the interactive
// game natively on Windows. WSLg's remoted cursor is unsuitable for FPS capture.
package launch

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"runtime"
	"strings"
)

func IsWSL() bool {
	if runtime.GOOS != "linux" {
		return false
	}
	if os.Getenv("WSL_DISTRO_NAME") != "" || os.Getenv("WSL_INTEROP") != "" {
		return true
	}
	kernel, _ := os.ReadFile("/proc/sys/kernel/osrelease")
	return strings.Contains(strings.ToLower(string(kernel)), "microsoft")
}

// NativeWindows returns false on other platforms. Go's build cache avoids a
// stale executable without requiring a second source-change detector.
func NativeWindows(args []string) (bool, error) {
	if !IsWSL() {
		return false, nil
	}
	if os.Getenv("WSL_INTEROP") == "" {
		return true, fmt.Errorf("WSL Windows interoperability is disabled; enable it to run the native game, or explicitly use -wslg")
	}
	cwd, err := os.Getwd()
	if err != nil {
		return true, err
	}
	root, err := moduleRoot(cwd)
	if err != nil {
		return true, err
	}
	forward, err := windowsPaths(args, func(path string) (string, error) {
		abs, e := filepath.Abs(path)
		if e != nil {
			return "", e
		}
		out, e := exec.Command("wslpath", "-w", abs).Output()
		if e != nil {
			return "", fmt.Errorf("translate Windows path %q: %w", path, e)
		}
		return strings.TrimSpace(string(out)), nil
	})
	if err != nil {
		return true, err
	}
	bin := filepath.Join(root, "bin")
	if err := os.MkdirAll(bin, 0755); err != nil {
		return true, err
	}
	target := filepath.Join(bin, "rend3rer-windows-"+runtime.GOARCH+".exe")
	fmt.Fprintln(os.Stderr, "WSL detected: building and launching native Windows to bypass WSLg mouse capture.")
	build := exec.Command("go", "build", "-o", target, ".")
	build.Dir = root
	build.Env = windowsEnvironment(os.Environ(), runtime.GOARCH)
	build.Stdout, build.Stderr = os.Stdout, os.Stderr
	if err := build.Run(); err != nil {
		return true, fmt.Errorf("build native Windows game: %w", err)
	}
	game := exec.Command(target, forward...)
	game.Dir = cwd
	game.Stdin, game.Stdout, game.Stderr = os.Stdin, os.Stdout, os.Stderr
	if err := game.Run(); err != nil {
		return true, fmt.Errorf("run native Windows game: %w", err)
	}
	return true, nil
}

func moduleRoot(dir string) (string, error) {
	for {
		data, err := os.ReadFile(filepath.Join(dir, "go.mod"))
		if err == nil && strings.HasPrefix(string(data), "module rend3rer\n") {
			return dir, nil
		}
		parent := filepath.Dir(dir)
		if parent == dir {
			return "", fmt.Errorf("run from the rend3rer source checkout to build for Windows; use -wslg to explicitly run the Linux binary")
		}
		dir = parent
	}
}

func windowsEnvironment(env []string, arch string) []string {
	out := make([]string, 0, len(env)+3)
	for _, value := range env {
		key, _, _ := strings.Cut(value, "=")
		if key != "GOOS" && key != "GOARCH" && key != "CGO_ENABLED" {
			out = append(out, value)
		}
	}
	return append(out, "GOOS=windows", "GOARCH="+arch, "CGO_ENABLED=0")
}

// The child uses Windows path syntax even though the parent runs in WSL.
// Pass arguments directly to exec, without shell quoting or interpolation.
func windowsPaths(args []string, translate func(string) (string, error)) ([]string, error) {
	out := append([]string(nil), args...)
	for i := 0; i < len(out); i++ {
		arg := out[i]
		if arg == "--" {
			break
		}
		for _, name := range []string{"-level", "-input-log"} {
			if arg == name || arg == "-"+name {
				if i+1 >= len(out) {
					return nil, fmt.Errorf("missing value for %s", arg)
				}
				i++
				if out[i] != "" {
					value, err := translate(out[i])
					if err != nil {
						return nil, err
					}
					out[i] = value
				}
			} else if value, ok := strings.CutPrefix(arg, name+"="); ok {
				if value != "" {
					value, err := translate(value)
					if err != nil {
						return nil, err
					}
					out[i] = name + "=" + value
				}
			} else if value, ok := strings.CutPrefix(arg, "-"+name+"="); ok {
				if value != "" {
					value, err := translate(value)
					if err != nil {
						return nil, err
					}
					out[i] = "-" + name + "=" + value
				}
			}
		}
	}
	return out, nil
}

package main

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"net/http"
	"net/url"
	"os"
	"os/exec"
	"path/filepath"
	"strings"
	"time"
)

const requestTimeout = 900 * time.Millisecond

type event struct {
	Type    string `json:"type"`
	Project string `json:"project,omitempty"`
}

type hookInput struct {
	CWD            string `json:"cwd"`
	StopHookActive bool   `json:"stop_hook_active"`
}

func main() {
	os.Exit(run(os.Args[1:]))
}

func run(args []string) int {
	if len(args) == 0 {
		usage()
		return 2
	}

	switch args[0] {
	case "started", "done", "idle":
		if len(args) > 2 {
			fmt.Fprintln(os.Stderr, "usage: codex-beacon", args[0], "[project]")
			return 2
		}
		project := ""
		if len(args) == 2 {
			project = args[1]
		} else if args[0] != "idle" {
			project = currentProject()
		}
		if err := notify(event{Type: args[0], Project: project}); err != nil {
			fmt.Fprintln(os.Stderr, err)
			return 1
		}
		return 0
	case "test":
		project := currentProject()
		if err := notify(event{Type: "started", Project: project}); err != nil {
			fmt.Fprintln(os.Stderr, err)
			return 1
		}
		if err := notify(event{Type: "done", Project: project}); err != nil {
			fmt.Fprintln(os.Stderr, err)
			return 1
		}
		fmt.Println("sent started and done")
		return 0
	case "run":
		return runCodex(args[1:])
	case "hook":
		return runHook()
	default:
		usage()
		return 2
	}
}

func usage() {
	fmt.Fprintln(os.Stderr, `usage:
  codex-beacon started [project]
  codex-beacon done [project]
  codex-beacon idle
  codex-beacon test
  codex-beacon run -- codex [args...]
  codex-beacon hook`)
}

func beaconURL() (string, error) {
	base := strings.TrimRight(strings.TrimSpace(os.Getenv("CODEX_BEACON_URL")), "/")
	if base == "" {
		return "", errors.New("CODEX_BEACON_URL is not set")
	}
	parsed, err := url.Parse(base)
	if err != nil || parsed.Scheme == "" || parsed.Host == "" ||
		(parsed.Scheme != "http" && parsed.Scheme != "https") {
		return "", fmt.Errorf("invalid CODEX_BEACON_URL %q", base)
	}
	return base, nil
}

func notify(e event) error {
	base, err := beaconURL()
	if err != nil {
		return err
	}
	payload, err := json.Marshal(e)
	if err != nil {
		return fmt.Errorf("encode event: %w", err)
	}

	ctx, cancel := context.WithTimeout(context.Background(), requestTimeout)
	defer cancel()
	req, err := http.NewRequestWithContext(ctx, http.MethodPost, base+"/event", strings.NewReader(string(payload)))
	if err != nil {
		return fmt.Errorf("create request: %w", err)
	}
	req.Header.Set("Content-Type", "application/json")

	resp, err := (&http.Client{Timeout: requestTimeout}).Do(req)
	if err != nil {
		return fmt.Errorf("notify %s: %w", base, err)
	}
	defer resp.Body.Close()
	_, _ = io.Copy(io.Discard, io.LimitReader(resp.Body, 4096))
	if resp.StatusCode < 200 || resp.StatusCode >= 300 {
		return fmt.Errorf("device returned HTTP %s", resp.Status)
	}
	return nil
}

func currentProject() string {
	cwd, err := os.Getwd()
	if err != nil {
		return "Codex"
	}
	name := filepath.Base(filepath.Clean(cwd))
	if name == "." || name == string(filepath.Separator) || name == "" {
		return "Codex"
	}
	return name
}

func runCodex(args []string) int {
	if len(args) > 0 && args[0] == "--" {
		args = args[1:]
	}
	if len(args) == 0 {
		fmt.Fprintln(os.Stderr, "usage: codex-beacon run -- codex [args...]")
		return 2
	}

	project := currentProject()
	if err := notify(event{Type: "started", Project: project}); err != nil {
		fmt.Fprintln(os.Stderr, "warning:", err)
	}

	command := exec.Command(args[0], args[1:]...)
	command.Stdin = os.Stdin
	command.Stdout = os.Stdout
	command.Stderr = os.Stderr
	err := command.Run()

	if notifyErr := notify(event{Type: "done", Project: project}); notifyErr != nil {
		fmt.Fprintln(os.Stderr, "warning:", notifyErr)
	}
	if err == nil {
		return 0
	}
	var exitErr *exec.ExitError
	if errors.As(err, &exitErr) {
		return exitErr.ExitCode()
	}
	fmt.Fprintln(os.Stderr, err)
	return 1
}

func runHook() int {
	inputBytes, _ := io.ReadAll(io.LimitReader(os.Stdin, 1<<20))
	var input hookInput
	_ = json.Unmarshal(inputBytes, &input)
	if input.StopHookActive {
		return 0
	}

	project := currentProject()
	if input.CWD != "" {
		project = filepath.Base(filepath.Clean(input.CWD))
	}
	if err := notify(event{Type: "done", Project: project}); err != nil {
		// Hook notifications are best-effort and must not affect Codex.
		fmt.Fprintln(os.Stderr, "warning:", err)
	}
	return 0
}

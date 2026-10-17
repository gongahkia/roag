package main

import (
	"encoding/csv"
	"os"
	"runtime"
	"strconv"
	"time"

	"rend3rer/internal/soft3d"
)

// Optional local diagnostics: received motion, camera output, and frame timing.
// Disabled by default; it records no keys, button contents, or desktop positions.
type inputTrace struct {
	file  *os.File
	csv   *csv.Writer
	start time.Time
	rows  int
}

func openInputTrace(path string) (*inputTrace, error) {
	if path == "" {
		return nil, nil
	}
	f, err := os.Create(path)
	if err != nil {
		return nil, err
	}
	t := &inputTrace{file: f, csv: csv.NewWriter(f), start: time.Now()}
	if err := t.csv.Write([]string{"platform", "elapsed_ms", "frame_ms", "mouse_dx", "mouse_dy", "yaw", "pitch", "captured", "focused", "simulation_tick"}); err != nil {
		f.Close()
		return nil, err
	}
	return t, nil
}
func (t *inputTrace) record(elapsed time.Duration, dx, dy float64, cam soft3d.Camera, captured, focused bool, tick int) error {
	if t == nil {
		return nil
	}
	float := func(n float64) string { return strconv.FormatFloat(n, 'f', 6, 64) }
	if err := t.csv.Write([]string{runtime.GOOS, float(float64(time.Since(t.start)) / float64(time.Millisecond)), float(float64(elapsed) / float64(time.Millisecond)), float(dx), float(dy), float(cam.Yaw), float(cam.Pitch), strconv.FormatBool(captured), strconv.FormatBool(focused), strconv.Itoa(tick)}); err != nil {
		return err
	}
	t.rows++
	if t.rows%120 == 0 {
		t.csv.Flush()
	}
	return t.csv.Error()
}
func (t *inputTrace) Close() error {
	if t == nil {
		return nil
	}
	t.csv.Flush()
	writeErr := t.csv.Error()
	closeErr := t.file.Close()
	if writeErr != nil {
		return writeErr
	}
	return closeErr
}

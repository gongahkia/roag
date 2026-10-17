// Package settings stores local player preferences, outside gameplay state.
package settings

import (
	"encoding/json"
	"errors"
	"fmt"
	"math"
	"os"
	"path/filepath"
)

const MinSensitivity = .05
const MaxSensitivity = 10.0

type Preferences struct {
	MouseSensitivity float64 `json:"mouse_sensitivity"`
}

func Default() Preferences { return Preferences{MouseSensitivity: 1} }
func Path() (string, error) {
	dir, err := os.UserConfigDir()
	if err != nil {
		return "", err
	}
	return filepath.Join(dir, "rend3rer", "settings.json"), nil
}
func valid(value float64) bool {
	return !math.IsNaN(value) && !math.IsInf(value, 0) && value > 0 && value <= MaxSensitivity
}
func Load(path string) (Preferences, error) {
	p := Default()
	data, err := os.ReadFile(path)
	if errors.Is(err, os.ErrNotExist) {
		return p, nil
	}
	if err != nil {
		return p, err
	}
	if err := json.Unmarshal(data, &p); err != nil {
		return Default(), err
	}
	if !valid(p.MouseSensitivity) {
		return Default(), fmt.Errorf("invalid saved mouse sensitivity")
	}
	return p, nil
}
func (p Preferences) Save(path string) error {
	if !valid(p.MouseSensitivity) {
		return fmt.Errorf("invalid mouse sensitivity")
	}
	data, err := json.MarshalIndent(p, "", "  ")
	if err != nil {
		return err
	}
	if err := os.MkdirAll(filepath.Dir(path), 0755); err != nil {
		return err
	}
	f, err := os.CreateTemp(filepath.Dir(path), "settings-*.tmp")
	if err != nil {
		return err
	}
	defer os.Remove(f.Name())
	if _, err = f.Write(append(data, '\n')); err != nil {
		f.Close()
		return err
	}
	if err := f.Close(); err != nil {
		return err
	}
	return os.Rename(f.Name(), path)
}

// A logarithmic slider gives low sensitivities useful space on the track.
func SliderValue(position float64) float64 {
	if math.IsNaN(position) {
		position = 0
	}
	position = math.Max(0, math.Min(1, position))
	return math.Max(MinSensitivity, math.Min(MaxSensitivity, math.Exp(math.Log(MinSensitivity)+position*math.Log(MaxSensitivity/MinSensitivity))))
}
func SliderPosition(value float64) float64 {
	if !valid(value) {
		value = 1
	}
	return math.Max(0, math.Min(1, math.Log(value/MinSensitivity)/math.Log(MaxSensitivity/MinSensitivity)))
}

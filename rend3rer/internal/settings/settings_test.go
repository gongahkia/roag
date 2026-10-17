package settings

import (
	"math"
	"os"
	"path/filepath"
	"testing"
)

func TestPreferencesSurviveSavingAndRestart(t *testing.T) {
	path := filepath.Join(t.TempDir(), "rend3rer", "settings.json")
	p, err := Load(path)
	if err != nil || p != Default() {
		t.Fatalf("missing file: %+v %v", p, err)
	}
	p.MouseSensitivity = .37
	if err := p.Save(path); err != nil {
		t.Fatal(err)
	}
	got, err := Load(path)
	if err != nil || got != p {
		t.Fatalf("saved preference was lost: %+v %v", got, err)
	}
	p.MouseSensitivity = 2.3
	if err := p.Save(path); err != nil {
		t.Fatal(err)
	}
	got, err = Load(path)
	if err != nil || got != p {
		t.Fatal("could not replace an existing settings file")
	}
}
func TestInvalidSettingsFallBackWithoutBreakingStartup(t *testing.T) {
	path := filepath.Join(t.TempDir(), "settings.json")
	for _, data := range []string{`{"mouse_sensitivity":-1}`, `{"mouse_sensitivity":100}`, `{"mouse_sensitivity":"fast"}`, `broken`} {
		if err := os.WriteFile(path, []byte(data), 0644); err != nil {
			t.Fatal(err)
		}
		got, err := Load(path)
		if err == nil || got != Default() {
			t.Fatalf("invalid settings did not fall back: %s", data)
		}
	}
}
func TestSliderMappingIsMonotonicAndReversible(t *testing.T) {
	previous := 0.0
	for i := 0; i <= 100; i++ {
		position := float64(i) / 100
		value := SliderValue(position)
		if value <= previous {
			t.Fatal("slider moved backwards")
		}
		previous = value
		if math.Abs(SliderPosition(value)-position) > 1e-9 {
			t.Fatal("slider thumb does not match saved sensitivity")
		}
	}
	if math.Abs(SliderValue(-1)-MinSensitivity) > 1e-9 || math.Abs(SliderValue(2)-MaxSensitivity) > 1e-9 {
		t.Fatal("slider escaped bounds")
	}
}

package soft3d

import (
	"bytes"
	"reflect"
	"testing"

	"rend3rer/levels"
)

func TestLevelRoundTripAndCollision(t *testing.T) {
	level, err := DecodeLevel(levels.Default)
	if err != nil {
		t.Fatal(err)
	}
	encoded, err := level.Encode()
	if err != nil {
		t.Fatal(err)
	}
	got, err := DecodeLevel(encoded)
	if err != nil {
		t.Fatal(err)
	}
	if !reflect.DeepEqual(got, level) || !bytes.Contains(encoded, []byte(`"geometry"`)) {
		t.Fatal("level changed during encode/decode")
	}
	if !got.Walkable(0, -8) || got.Walkable(0, 6) || got.Walkable(3.3, 5.5) || got.Walkable(4, -8) {
		t.Fatal("walkable or blocker rectangles were not loaded")
	}
}

func TestEditedGeometryControlsScene(t *testing.T) {
	level, err := DecodeLevel(levels.Default)
	if err != nil {
		t.Fatal(err)
	}
	level.Geometry[0].Vertices[0][0] = -30
	level.Spawn.Position[2] = -6
	scene, err := level.BuildScene()
	if err != nil {
		t.Fatal(err)
	}
	if scene.Static[0].V[0].Pos.X != -30 || level.SpawnCamera().Position.Z != -6 {
		t.Fatal("JSON level values did not reach the scene or camera")
	}
}

func TestInvalidLevelRejected(t *testing.T) {
	bad := bytes.Replace(levels.Default, []byte(`"version": 1`), []byte(`"version": 99`), 1)
	if _, err := DecodeLevel(bad); err == nil {
		t.Fatal("unsupported level version accepted")
	}
	bad = bytes.Replace(levels.Default, []byte(`"material": "stone"`), []byte(`"material": "missing"`), 1)
	if _, err := DecodeLevel(bad); err == nil {
		t.Fatal("missing material accepted")
	}
	bad = bytes.Replace(levels.Default, []byte(`[0, 1.55, -8]`), []byte(`[0, 1.55]`), 1)
	if _, err := DecodeLevel(bad); err == nil {
		t.Fatal("incomplete 3D point accepted")
	}
}

func TestCameraLookMovesPitchAndClamps(t *testing.T) {
	c := Camera{FOV: 75}
	c.Look(0, -40)
	if c.Pitch <= 0 {
		t.Fatalf("looking up did not raise pitch: %f", c.Pitch)
	}
	c.Look(0, 1000)
	if c.Pitch != -1.45 {
		t.Fatalf("pitch clamp failed: %f", c.Pitch)
	}
	r := NewRenderer()
	r.Render(Camera{FOV: 75, Pitch: 0.4}, nil, 0)
	up := append([]byte(nil), r.Pixels...)
	r.Render(Camera{FOV: 75, Pitch: -0.4}, nil, 0)
	if bytes.Equal(up, r.Pixels) {
		t.Fatal("pitch did not change the rendered view")
	}
}

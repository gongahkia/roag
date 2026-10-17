package soft3d

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"math"
)

// Level is the editable, round-trippable description of a scene.
type Level struct {
	Version    int               `json:"version"`
	Name       string            `json:"name"`
	Spawn      CameraSpec        `json:"spawn"`
	Materials  map[string]string `json:"materials"`
	Geometry   []GeometrySpec    `json:"geometry"`
	Animations AnimationsSpec    `json:"animations"`
	Collision  CollisionSpec     `json:"collision"`
}

type Point [3]float64

func (p Point) vec() Vec3 { return Vec3{p[0], p[1], p[2]} }

func (p *Point) UnmarshalJSON(data []byte) error {
	var values []float64
	if err := json.Unmarshal(data, &values); err != nil {
		return err
	}
	if len(values) != 3 {
		return fmt.Errorf("3D point needs exactly three numbers")
	}
	copy(p[:], values)
	return nil
}

type CameraSpec struct {
	Position Point   `json:"position"`
	Yaw      float64 `json:"yaw"`
	Pitch    float64 `json:"pitch"`
	FOV      float64 `json:"fov"`
}

type GeometrySpec struct {
	Type     string     `json:"type"`
	Material string     `json:"material"`
	Min      *Point     `json:"min,omitempty"`
	Max      *Point     `json:"max,omitempty"`
	Vertices []Point    `json:"vertices,omitempty"`
	Tile     [2]float64 `json:"tile"`
	RepeatX  []float64  `json:"repeat_x,omitempty"`
	RepeatZ  []float64  `json:"repeat_z,omitempty"`
	TwoSided bool       `json:"two_sided,omitempty"`
}

type OrbitSpec struct {
	Center   Point   `json:"center"`
	Count    int     `json:"count"`
	Radius   float64 `json:"radius"`
	Speed    float64 `json:"speed"`
	Arc      float64 `json:"arc"`
	Height   float64 `json:"height"`
	Wobble   float64 `json:"wobble"`
	Material string  `json:"material"`
}

type ReflectionSpec struct {
	Center   Point   `json:"center"`
	Count    int     `json:"count"`
	Spacing  float64 `json:"spacing"`
	Width    float64 `json:"width"`
	Wobble   float64 `json:"wobble"`
	Speed    float64 `json:"speed"`
	Material string  `json:"material"`
}

type AnimationsSpec struct {
	Orbit      *OrbitSpec      `json:"orbit,omitempty"`
	Reflection *ReflectionSpec `json:"reflection,omitempty"`
}

type RectSpec struct {
	Min     [2]float64 `json:"min"`
	Max     [2]float64 `json:"max"`
	RepeatX []float64  `json:"repeat_x,omitempty"`
	RepeatZ []float64  `json:"repeat_z,omitempty"`
}

type CollisionSpec struct {
	Walkable []RectSpec `json:"walkable"`
	Blockers []RectSpec `json:"blockers"`
}

func DecodeLevel(data []byte) (*Level, error) {
	dec := json.NewDecoder(bytes.NewReader(data))
	dec.DisallowUnknownFields()
	var l Level
	if err := dec.Decode(&l); err != nil {
		return nil, fmt.Errorf("decode level: %w", err)
	}
	var extra any
	if err := dec.Decode(&extra); err != io.EOF {
		return nil, fmt.Errorf("decode level: trailing data")
	}
	if err := l.Validate(); err != nil {
		return nil, err
	}
	return &l, nil
}

func (l *Level) Encode() ([]byte, error) {
	if err := l.Validate(); err != nil {
		return nil, err
	}
	return json.MarshalIndent(l, "", "  ")
}

func (l *Level) SpawnCamera() Camera {
	return Camera{Position: l.Spawn.Position.vec(), Yaw: l.Spawn.Yaw, Pitch: l.Spawn.Pitch, FOV: l.Spawn.FOV}
}

func (l *Level) Validate() error {
	if l.Version != 1 {
		return fmt.Errorf("level version %d: only version 1 is supported", l.Version)
	}
	if l.Name == "" {
		return fmt.Errorf("level name is required")
	}
	if l.Spawn.FOV < 30 || l.Spawn.FOV > 120 {
		return fmt.Errorf("spawn fov must be between 30 and 120 degrees")
	}
	if math.Abs(l.Spawn.Pitch) > 1.45 {
		return fmt.Errorf("spawn pitch must be within ±1.45 radians")
	}
	if len(l.Collision.Walkable) == 0 {
		return fmt.Errorf("at least one walkable rectangle is required")
	}
	if len(l.Geometry) == 0 {
		return fmt.Errorf("at least one geometry entry is required")
	}
	for name, pattern := range l.Materials {
		if name == "" || !knownPattern(pattern) {
			return fmt.Errorf("material %q has unknown pattern %q", name, pattern)
		}
	}
	for i, g := range l.Geometry {
		if _, ok := l.Materials[g.Material]; !ok {
			return fmt.Errorf("geometry %d uses unknown material %q", i, g.Material)
		}
		if g.Tile[0] <= 0 || g.Tile[1] <= 0 {
			return fmt.Errorf("geometry %d has nonpositive tiling", i)
		}
		switch g.Type {
		case "box":
			if g.Min == nil || g.Max == nil || g.Min[0] >= g.Max[0] || g.Min[1] >= g.Max[1] || g.Min[2] >= g.Max[2] {
				return fmt.Errorf("geometry %d has invalid box bounds", i)
			}
		case "quad":
			if len(g.Vertices) != 4 {
				return fmt.Errorf("geometry %d needs four vertices", i)
			}
			if g.RepeatX != nil || g.RepeatZ != nil {
				return fmt.Errorf("geometry %d: repeated quads are not supported", i)
			}
		default:
			return fmt.Errorf("geometry %d has unknown type %q", i, g.Type)
		}
	}
	for i, r := range append(append([]RectSpec{}, l.Collision.Walkable...), l.Collision.Blockers...) {
		if r.Min[0] >= r.Max[0] || r.Min[1] >= r.Max[1] {
			return fmt.Errorf("collision rectangle %d has invalid bounds", i)
		}
	}
	if a := l.Animations.Orbit; a != nil {
		if a.Count < 1 || a.Count > 100 || a.Radius <= 0 || a.Height <= 0 || l.Materials[a.Material] == "" {
			return fmt.Errorf("invalid orbit animation")
		}
	}
	if a := l.Animations.Reflection; a != nil {
		if a.Count < 1 || a.Count > 100 || a.Width <= 0 || l.Materials[a.Material] == "" {
			return fmt.Errorf("invalid reflection animation")
		}
	}
	return nil
}

func knownPattern(p string) bool {
	switch p {
	case "stone", "dark", "gold", "rune", "water":
		return true
	}
	return false
}

func repeat(values []float64) []float64 {
	if len(values) == 0 {
		return []float64{0}
	}
	return values
}

func (l *Level) Walkable(x, z float64) bool {
	inside := false
	for _, rect := range l.Collision.Walkable {
		if rectContains(rect, x, z) {
			inside = true
			break
		}
	}
	if !inside {
		return false
	}
	for _, rect := range l.Collision.Blockers {
		if rectContains(rect, x, z) {
			return false
		}
	}
	return true
}

func rectContains(r RectSpec, x, z float64) bool {
	for _, ox := range repeat(r.RepeatX) {
		for _, oz := range repeat(r.RepeatZ) {
			if x >= r.Min[0]+ox && x <= r.Max[0]+ox && z >= r.Min[1]+oz && z <= r.Max[1]+oz {
				return true
			}
		}
	}
	return false
}

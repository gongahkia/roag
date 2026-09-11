package game

import "math"

type Vec struct{ X, Y float64 }

func (v Vec) Add(o Vec) Vec          { return Vec{v.X + o.X, v.Y + o.Y} }
func (v Vec) Sub(o Vec) Vec          { return Vec{v.X - o.X, v.Y - o.Y} }
func (v Vec) Mul(n float64) Vec      { return Vec{v.X * n, v.Y * n} }
func (v Vec) Len() float64           { return math.Hypot(v.X, v.Y) }
func (v Vec) Distance(o Vec) float64 { return v.Sub(o).Len() }

func (v Vec) Normalized() Vec {
	if n := v.Len(); n > 0 {
		return v.Mul(1 / n)
	}
	return Vec{}
}

type Point struct{ X, Y int }

func (v Vec) Point() Point { return Point{int(math.Floor(v.X)), int(math.Floor(v.Y))} }

func clamp(v, lo, hi float64) float64 {
	if v < lo {
		return lo
	}
	if v > hi {
		return hi
	}
	return v
}

func angleDelta(a, b float64) float64 {
	d := math.Mod(a-b+math.Pi, 2*math.Pi) - math.Pi
	return d
}

type Action int

const (
	MoveForward Action = iota
	MoveBackward
	StrafeLeft
	StrafeRight
	TurnLeft
	TurnRight
	ToggleCrouch
	ToggleSprint
	Interact
	ToggleFlashlight
	ThrowDecoy
	UseFlare
	ToggleTracker
	Fire
	Pause
)

type Input struct {
	Held    map[Action]bool
	Pressed map[Action]bool
	MoveX   float64
	MoveY   float64
	LookX   float64
}

func NewInput() Input { return Input{Held: map[Action]bool{}, Pressed: map[Action]bool{}} }

type AIState int

const (
	Dormant AIState = iota
	Stalk
	Investigate
	Hunt
	Search
	Retreat
	Stunned
)

func (s AIState) String() string {
	switch s {
	case Dormant:
		return "DORMANT"
	case Stalk:
		return "STALKING"
	case Investigate:
		return "LISTENING"
	case Hunt:
		return "HUNTING"
	case Search:
		return "SEARCHING"
	case Retreat:
		return "REPELLED"
	case Stunned:
		return "STUNNED"
	default:
		return "UNKNOWN"
	}
}

type SoundKind int

const (
	SoundStep SoundKind = iota
	SoundDoor
	SoundDecoy
	SoundGun
	SoundGenerator
)

type Sound struct {
	At        Vec
	Intensity float64
	Kind      SoundKind
	Age       float64
}

type ItemKind int

const (
	Fuse ItemKind = iota
	Decoy
	Flare
	Battery
	StunCharge
)

type Item struct {
	At    Vec
	Kind  ItemKind
	Taken bool
}

func (i ItemKind) Rune() rune {
	switch i {
	case Fuse:
		return 'F'
	case Decoy:
		return 'n'
	case Flare:
		return '*'
	case Battery:
		return 'b'
	default:
		return '+'
	}
}

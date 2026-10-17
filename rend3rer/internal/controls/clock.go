package controls

import (
	"time"

	"rend3rer/internal/gameplay"
)

// TickClock separates the simulation's fixed rate from input/display refresh.
// Fractions are kept in scaled nanoseconds to avoid a 60 Hz rounding drift.
type TickClock struct{ fraction int64 }

func (c *TickClock) Reset() { c.fraction = 0 }
func (c *TickClock) Advance(elapsed time.Duration) int {
	if elapsed <= 0 {
		return 0
	}
	// Bound work after a window drag or debugger stall; do not queue seconds of
	// stale local commands or spin in a catch-up loop before showing fresh input.
	if elapsed > 250*time.Millisecond {
		elapsed = 250 * time.Millisecond
	}
	c.fraction += elapsed.Nanoseconds() * gameplay.TickRate
	ticks := int(c.fraction / int64(time.Second))
	c.fraction %= int64(time.Second)
	return min(ticks, 8)
}

// Actions retains taps that happen between simulation ticks and consumes each
// one once, even if several ticks are needed for one display frame.
type Actions struct{ fire, dash, interact, jump bool }

func (a *Actions) Reset() { *a = Actions{} }
func (a *Actions) Remember(command gameplay.Command) {
	a.fire = a.fire || command.Fire
	a.dash = a.dash || command.Dash
	a.interact = a.interact || command.Interact
	a.jump = a.jump || command.Jump
}
func (a *Actions) Consume(command gameplay.Command) gameplay.Command {
	command.Fire = command.Fire || a.fire
	command.Dash = command.Dash || a.dash
	command.Interact = command.Interact || a.interact
	command.Jump = command.Jump || a.jump
	a.Reset()
	return command
}

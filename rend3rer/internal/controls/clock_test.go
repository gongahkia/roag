package controls

import (
	"testing"
	"time"

	"rend3rer/internal/gameplay"
)

func TestSimulationRateIndependentOfDisplayRefresh(t *testing.T) {
	for _, fps := range []int{30, 60, 120, 144, 240} {
		var clock TickClock
		ticks := 0
		for frame := 1; frame <= fps*5; frame++ {
			previous := time.Duration(frame-1) * time.Second / time.Duration(fps)
			now := time.Duration(frame) * time.Second / time.Duration(fps)
			ticks += clock.Advance(now - previous)
		}
		if ticks != 5*gameplay.TickRate {
			t.Fatalf("%d FPS produced %d ticks, want %d", fps, ticks, 5*gameplay.TickRate)
		}
	}
}

func TestFastTapSurvivesFramesWithoutSimulationAndIsConsumedOnce(t *testing.T) {
	var clock TickClock
	var actions Actions
	actions.Remember(gameplay.Command{Fire: true, Dash: true, Interact: true, Jump: true})
	if clock.Advance(3*time.Millisecond) != 0 {
		t.Fatal("unexpected simulation tick")
	}
	actions.Remember(gameplay.Command{}) // released before the next simulation tick
	if clock.Advance(30*time.Millisecond) != 1 {
		t.Fatal("missing simulation tick")
	}
	first := actions.Consume(gameplay.Command{})
	if !first.Fire || !first.Dash || !first.Interact || !first.Jump {
		t.Fatal("tap was lost between ticks")
	}
	second := actions.Consume(gameplay.Command{})
	if second.Fire || second.Dash || second.Interact || second.Jump {
		t.Fatal("tap replayed on catch-up tick")
	}
	if !actions.Consume(gameplay.Command{Fire: true}).Fire {
		t.Fatal("held fire was lost")
	}
}

func TestPauseResetsFractionAndPendingActions(t *testing.T) {
	var clock TickClock
	var actions Actions
	clock.Advance(15 * time.Millisecond)
	actions.Remember(gameplay.Command{Fire: true, Dash: true, Interact: true, Jump: true})
	clock.Reset()
	actions.Reset()
	if clock.Advance(3*time.Millisecond) != 0 {
		t.Fatal("paused time advanced simulation on resume")
	}
	c := actions.Consume(gameplay.Command{})
	if c.Fire || c.Dash || c.Interact || c.Jump {
		t.Fatal("paused action leaked into resumed game")
	}
	if got := clock.Advance(10 * time.Second); got > 8 {
		t.Fatal("frame stall caused an unbounded catch-up loop")
	}
}

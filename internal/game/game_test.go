package game

import (
	"math"
	"testing"
)

func TestWorldObjectivesAreReachable(t *testing.T) {
	g := New()
	goals := []Point{g.World.Generator, g.World.Exit}
	for _, it := range g.World.Items {
		if it.Kind == Fuse {
			goals = append(goals, it.At.Point())
		}
	}
	for _, goal := range goals {
		if p := g.World.Path(g.Player.Point(), goal, true); len(p) == 0 {
			t.Fatalf("objective at %+v is unreachable", goal)
		}
	}
}

func TestClosedDoorBlocksPlayerButMonsterCanPlanThrough(t *testing.T) {
	w := NewWorld()
	door := Point{5, 10}
	if !w.Blocked(door) {
		t.Fatal("closed door should block movement")
	}
	for _, step := range w.Path(Point{5, 9}, Point{5, 11}, false) {
		if step == door {
			t.Fatalf("player path crossed closed door: %v", door)
		}
	}
	if p := w.Path(Point{5, 9}, Point{5, 11}, true); len(p) == 0 {
		t.Fatal("monster should plan through doors it can open")
	}
}

func TestDecoysLoseEffectivenessAfterInvestigation(t *testing.T) {
	g := New()
	g.Time = 10
	g.Monster.State = Investigate
	g.Monster.Pos = Vec{10.5, 12.5}
	g.Monster.Target = g.Monster.Pos
	g.Sounds = []Sound{{At: g.Monster.Pos, Intensity: 10, Kind: SoundDecoy}}
	before := g.Monster.DecoyTrust
	g.Update(.05, NewInput())
	if g.Monster.DecoyTrust >= before {
		t.Fatalf("decoy trust did not decrease: before %.2f after %.2f", before, g.Monster.DecoyTrust)
	}
}

func TestRepeatedHidingBecomesUnsafe(t *testing.T) {
	g := New()
	p := Point{2, 7}
	g.Hidden = true
	g.HideAt = p
	g.HideUses[p] = 3
	g.Monster.State = Search
	g.Monster.Pos = Vec{2.5, 7.5}
	g.Monster.Target = g.Monster.Pos
	g.Update(.05, NewInput())
	if !g.Dead {
		t.Fatal("monster should search a repeatedly used hiding place")
	}
}

func TestStunnerRequiresAimAndBuildsResistance(t *testing.T) {
	g := New()
	g.Time = 10
	g.Monster.Pos = Vec{6.5, 12.5}
	g.Facing = 0
	g.Charges = 2
	in := NewInput()
	in.Pressed[Fire] = true
	g.Update(.01, in)
	if g.Monster.State != Stunned {
		t.Fatalf("expected stun, got %v", g.Monster.State)
	}
	if math.Abs(g.Monster.StunResistance-1) > .01 {
		t.Fatalf("expected resistance 1, got %.2f", g.Monster.StunResistance)
	}
}

func TestWinRequiresPower(t *testing.T) {
	g := New()
	g.Player = Vec{39.5, 12.5}
	g.interact()
	if g.Won {
		t.Fatal("unpowered exit should not win")
	}
	g.GeneratorOn = true
	g.interact()
	if !g.Won {
		t.Fatal("powered exit should win")
	}
}

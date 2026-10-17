package gameplay

import (
	"math"
	"testing"
)

func movementArena() *Run {
	r := testArena()
	r.Enemies[0].Position = Vec3{X: 8, Z: 18}
	return r
}

func TestGroundAccelerationAndFriction(t *testing.T) {
	r := movementArena()
	r.Step(Command{Forward: 1})
	if speed := r.Player.Velocity.Flat().Length(); speed <= 0 || speed >= GroundSpeed {
		t.Fatal("movement skipped acceleration")
	}
	for i := 0; i < 20; i++ {
		r.Step(Command{Forward: 1})
	}
	if math.Abs(r.Player.Velocity.Flat().Length()-GroundSpeed) > 1e-9 {
		t.Fatal("ground speed not reached")
	}
	start := r.Player.Position
	for i := 0; i < 45; i++ {
		r.Step(Command{})
	}
	if r.Player.Velocity.Flat().Length() > .001 || distance(start, r.Player.Position) > 1.2 {
		t.Fatal("ground friction did not stop player promptly")
	}
}

func TestJumpGravityLandingAndNoDoubleJump(t *testing.T) {
	r := movementArena()
	r.Step(Command{Jump: true})
	if r.Player.Grounded || r.Player.Position.Y <= 0 {
		t.Fatal("jump did not leave the floor")
	}
	vy := r.Player.Velocity.Y
	r.Step(Command{Jump: true})
	if r.Player.Velocity.Y >= vy {
		t.Fatal("holding jump produced a midair jump")
	}
	maxY := r.Player.Position.Y
	for i := 0; i < 100 && !r.Player.Grounded; i++ {
		r.Step(Command{})
		maxY = math.Max(maxY, r.Player.Position.Y)
	}
	if maxY < 1 || maxY > 1.4 {
		t.Fatalf("unexpected jump height: %f", maxY)
	}
	if !r.Player.Grounded || r.Player.Position.Y != 0 || r.Player.Velocity.Y != 0 {
		t.Fatal("gravity did not return player to the floor")
	}
}

func TestAirStrafingPreservesForwardMomentum(t *testing.T) {
	r := movementArena()
	r.Player.Position.Y = 1
	r.Player.Grounded = false
	r.Player.Velocity = Vec3{Z: 8}
	r.Step(Command{Strafe: 1})
	if r.Player.Velocity.X <= 0 || math.Abs(r.Player.Velocity.Z-8) > 1e-9 {
		t.Fatal("air strafe replaced forward momentum")
	}
	if r.Player.Velocity.Flat().Length() <= 8 {
		t.Fatal("air strafe cannot build speed")
	}
}

func TestHeldJumpBunnyHopsWithoutLandingFriction(t *testing.T) {
	r := movementArena()
	r.Player.Velocity = Vec3{Z: GroundSpeed}
	jumps := 0
	for i := 0; i < 100; i++ {
		r.Step(Command{Jump: true})
		for _, e := range r.Events {
			if e.Kind == Jumped {
				jumps++
			}
		}
	}
	if jumps < 3 {
		t.Fatalf("holding jump did not chain hops: %d", jumps)
	}
	if r.Player.Velocity.Flat().Length() < GroundSpeed-.01 {
		t.Fatal("landing consumed bunny-hop momentum")
	}
}

func TestMovementSlidesAlongWallsAndCannotJumpThroughGates(t *testing.T) {
	r := movementArena()
	r.Player.Position = Vec3{X: 8.5}
	for i := 0; i < 30; i++ {
		r.Step(Command{Forward: 1, Strafe: 1})
	}
	if r.Player.Position.X > 9-PlayerRadius || r.Player.Position.Z < 2 || math.Abs(r.Player.Velocity.X) > .01 {
		t.Fatal("wall collision failed to preserve sliding motion")
	}
	r = movementArena()
	r.World.Rooms[0].Gate = Solid{Bounds: Rect{-9, 3, 9, 3.2}, Height: 3.6}
	for i := 0; i < 90; i++ {
		r.Step(Command{Forward: 1, Jump: true, Dash: i == 0})
	}
	if r.Player.Position.Z > 3-PlayerRadius {
		t.Fatal("jump or dash passed through a closed gate")
	}
}

func TestLowCoverCanBeClearedAndLandedOn(t *testing.T) {
	r := movementArena()
	r.World.Solids = []Solid{{Bounds: Rect{-2, 2, 2, 3}, Height: .5}}
	r.Player.Position.Z = .5
	r.Player.Velocity.Z = GroundSpeed
	for i := 0; i < 40; i++ {
		r.Step(Command{Jump: i == 0, Forward: 1})
	}
	if r.Player.Position.Z < 3.5 {
		t.Fatal("jump could not clear low cover")
	}
	r.Player.Position = Vec3{Y: 1.2, Z: 2.5}
	r.Player.Velocity = Vec3{Y: -1}
	r.Player.Grounded = false
	for i := 0; i < 60 && !r.Player.Grounded; i++ {
		r.Step(Command{})
	}
	if !r.Player.Grounded || r.Player.Position.Y != .5 {
		t.Fatal("player could not land on top of cover")
	}
}

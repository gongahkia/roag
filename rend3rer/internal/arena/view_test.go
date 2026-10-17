package arena

import (
	"reflect"
	"testing"

	"rend3rer/internal/gameplay"
	"rend3rer/internal/soft3d"
)

func TestPresentationDoesNotChangeCombatState(t *testing.T) {
	r := gameplay.New(42)
	v := New(r.World)
	renderer := soft3d.NewRenderer()
	cam := soft3d.Camera{Position: soft3d.Vec3{Y: gameplay.EyeHeight, Z: r.Player.Position.Z}, FOV: 75}
	for i := 0; i < 90; i++ {
		r.Step(gameplay.Command{Fire: true})
		v.Update(r.Events)
	}
	before := r.Snapshot()
	for _, phase := range []gameplay.Phase{gameplay.Playing, gameplay.Choosing, gameplay.Dead, gameplay.Won} {
		r.Phase = phase
		for i := 0; i < 3; i++ {
			renderer.Render(cam, v.Quads(r, cam), 1)
			v.HUD(renderer.Pixels, r, true, false)
		}
		if renderer.Stats.Quads < 20 || renderer.Stats.Pixels < 1000 {
			t.Fatal("combat frame appears empty")
		}
	}
	r.Phase = before.Phase
	if !reflect.DeepEqual(before, r.Snapshot()) {
		t.Fatal("rendering changed simulation")
	}
}

func BenchmarkCombatFrame(b *testing.B) {
	r := gameplay.New(1)
	v := New(r.World)
	renderer := soft3d.NewRenderer()
	cam := soft3d.Camera{Position: soft3d.Vec3{Y: gameplay.EyeHeight, Z: r.Player.Position.Z}, FOV: 75}
	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		renderer.Render(cam, v.Quads(r, cam), 1)
		v.HUD(renderer.Pixels, r, true, false)
	}
}

func TestBacktrackingRendersTheChamberAroundCamera(t *testing.T) {
	r := gameplay.New(1)
	v := New(r.World)
	cam := soft3d.Camera{Position: soft3d.Vec3{Y: gameplay.EyeHeight, Z: r.Player.Position.Z}, FOV: 75}
	before := v.Quads(r, cam)
	r.Room = 4 // the encounter stays advanced when the player returns to room one
	after := v.Quads(r, cam)
	if !reflect.DeepEqual(before, after) {
		t.Fatal("encounter progress changed geometry at a fixed camera")
	}
}

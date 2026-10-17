package gameplay

import (
	"math"
	"reflect"
	"testing"
)

func testArena() *Run {
	r := New(7)
	r.World = &World{Floors: []Rect{{-9, -1, 9, 20}}, Rooms: []Room{{Bounds: Rect{-9, -1, 9, 20}, Gate: Solid{Bounds: Rect{-1.5, 19.8, 1.5, 20}, Height: 3.6}}}, Exit: Vec3{Z: 18}}
	r.Cleared = []bool{false}
	r.Room = 0
	r.Player.Position = Vec3{}
	r.Enemies = []Enemy{{ID: 1, Position: Vec3{Z: 8}, Health: 1000, Cooldown: 10000}}
	return r
}

func TestGeneratedWorldHasReachableRoomsAndValidSpawns(t *testing.T) {
	for seed := int64(0); seed < 100; seed++ {
		w := Generate(seed)
		cleared := make([]bool, len(w.Rooms))
		for i := range cleared {
			cleared[i] = true
		}
		if !w.Walkable(w.Spawn, PlayerRadius, cleared) {
			t.Fatalf("seed %d: invalid player spawn", seed)
		}
		for _, room := range w.Rooms {
			for _, s := range room.Spawns {
				if !w.Walkable(s.Position, .38, cleared) {
					t.Fatalf("seed %d: enemy in cover at %+v", seed, s.Position)
				}
			}
		}
		for z := w.Spawn.Z; z < w.Exit.Z; z += .2 {
			if !w.Walkable(Vec3{Z: z}, PlayerRadius, cleared) {
				t.Fatalf("seed %d: central route blocked at %f", seed, z)
			}
		}
	}
}

func TestCommandsReplayDeterministicallyAndSnapshotIsDetached(t *testing.T) {
	a, b := New(99), New(99)
	for tick := 0; tick < 1200; tick++ {
		c := Command{Forward: .5, Strafe: math.Sin(float64(tick) * .04), Yaw: math.Sin(float64(tick) * .01), Fire: tick%3 == 0, Dash: tick%130 == 0, Choice: tick%3 + 1, Jump: tick%60 < 10}
		a.Step(c)
		b.Step(c)
		if !reflect.DeepEqual(a.Events, b.Events) {
			t.Fatalf("event replay diverged at %d", tick)
		}
	}
	if !reflect.DeepEqual(a.Snapshot(), b.Snapshot()) {
		t.Fatal("identical commands and seed diverged")
	}
	s := a.Snapshot()
	s.Enemies[0].Health = -99
	s.Cleared[0] = !s.Cleared[0]
	if a.Enemies[0].Health == -99 || a.Cleared[0] == s.Cleared[0] {
		t.Fatal("snapshot aliases live state")
	}
	if reflect.DeepEqual(Generate(99), Generate(100)) {
		t.Fatal("different seeds generated identical worlds")
	}
}

func TestHitscanStopsAtNearestEnemyAndSolidCover(t *testing.T) {
	r := testArena()
	r.Enemies = []Enemy{{ID: 1, Position: Vec3{Z: 4}, Health: 48, Cooldown: 999}, {ID: 2, Position: Vec3{Z: 7}, Health: 48, Cooldown: 999}}
	r.Step(Command{Fire: true})
	if r.Enemies[0].Health != 23 || r.Enemies[1].Health != 48 {
		t.Fatal("shot did not hit only nearest enemy")
	}
	r.Player.Cooldown = 0
	r.World.Solids = []Solid{{Bounds: Rect{-1, 2, 1, 2.4}, Height: 3}}
	r.Step(Command{Fire: true})
	if r.Enemies[0].Health != 23 {
		t.Fatal("shot passed through cover")
	}
	r.World.Solids = nil
	r.Player.Cooldown = 0
	r.Step(Command{Fire: true, Pitch: 1})
	if r.Enemies[0].Health != 23 {
		t.Fatal("pitch ignored by hit detection")
	}
	r.Player.Cooldown = 0
	r.Step(Command{Fire: true})
	if r.Kills != 1 || r.Enemies[1].Health != 48 {
		t.Fatal("kill did not register or shot pierced corpse in same tick")
	}
}

func TestMovementDashAndShotsRespectClosedGate(t *testing.T) {
	r := testArena()
	r.World.Rooms[0].Gate = Solid{Bounds: Rect{-9, 3, 9, 3.2}, Height: 3.6}
	for i := 0; i < 80; i++ {
		r.Step(Command{Forward: 1, Dash: i%76 == 0, Fire: true})
	}
	if r.Player.Position.Z > 3-PlayerRadius || r.Enemies[0].Health != 1000 {
		t.Fatal("movement or shots tunneled through gate")
	}
	r.Cleared[0] = true
	for i := 0; i < 30; i++ {
		r.Step(Command{Forward: 1})
	}
	if r.Player.Position.Z < 3.3 {
		t.Fatal("opened gate still blocks movement")
	}
}

func TestNormalizeMovementAndRejectNonFiniteInputs(t *testing.T) {
	a, b := testArena(), testArena()
	a.Step(Command{Forward: 1})
	b.Step(Command{Forward: 1, Strafe: 1})
	if math.Abs(a.Player.Position.Length()-b.Player.Position.Length()) > 1e-9 {
		t.Fatal("diagonal movement is faster")
	}
	b.Step(Command{Forward: math.Inf(1), Strafe: math.NaN(), Yaw: math.NaN(), Pitch: math.Inf(-1)})
	if math.IsNaN(b.Player.Position.X) || b.Player.Pitch != 0 || b.Player.Yaw != 0 {
		t.Fatal("invalid command contaminated simulation")
	}
}

func TestActorsCannotWalkOrDashThroughEachOther(t *testing.T) {
	r := testArena()
	r.Enemies = []Enemy{{ID: 1, Kind: Caster, Position: Vec3{Z: 2}, Health: 1000, Cooldown: 10000}}
	for i := 0; i < 40; i++ {
		r.Step(Command{Forward: 1, Dash: i == 0})
	}
	if r.Player.Position.Z > 2-(PlayerRadius+.38) {
		t.Fatal("player passed through a live enemy")
	}
	r.Enemies[0].Kind = Charger
	r.Player.Position = Vec3{Z: 1.3}
	for i := 0; i < 60; i++ {
		r.Step(Command{})
	}
	if distance(r.Player.Position, r.Enemies[0].Position) < PlayerRadius+.38 {
		t.Fatal("enemy entered player's collision footprint")
	}
}

func TestChargerTelegraphsBeforeDealingDamage(t *testing.T) {
	r := testArena()
	r.Enemies = []Enemy{{ID: 1, Position: Vec3{Z: 3}, Health: 48}}
	r.Step(Command{})
	if r.Enemies[0].Windup == 0 {
		t.Fatal("charger skipped windup")
	}
	for i := 0; i < 40; i++ {
		r.Step(Command{})
		if r.Player.Health != 100 {
			t.Fatal("charger damaged player during windup")
		}
	}
	for i := 0; i < 30; i++ {
		r.Step(Command{})
	}
	if r.Player.Health >= 100 {
		t.Fatal("charger never landed its rush")
	}
}

func TestCasterBoltsCanHitAndBeBlocked(t *testing.T) {
	for _, cover := range []bool{false, true} {
		r := testArena()
		r.Enemies = []Enemy{{ID: 1, Kind: Caster, Position: Vec3{Z: 6}, Health: 40}}
		for i := 0; i < 49; i++ {
			r.Step(Command{})
		}
		if len(r.Projectiles) == 0 {
			t.Fatal("caster did not fire after telegraph")
		}
		if cover {
			r.World.Solids = []Solid{{Bounds: Rect{-1, 2, 1, 2.5}, Height: 3}}
		}
		for i := 0; i < 65; i++ {
			r.Step(Command{})
		}
		if cover && r.Player.Health != 100 {
			t.Fatal("bolt passed through wall")
		}
		if !cover && r.Player.Health >= 100 {
			t.Fatal("bolt did not hit player")
		}
	}
}

func TestClearUpgradeAndExitProgression(t *testing.T) {
	r := New(1)
	for room := range r.World.Rooms {
		r.Player.Position = Vec3{Z: r.World.Rooms[room].Bounds.MinZ + 2}
		for i := range r.Enemies {
			if r.Enemies[i].Room == room {
				r.Enemies[i].Health = 0
			}
		}
		r.Step(Command{})
		if !r.Cleared[room] {
			t.Fatalf("chamber %d did not clear", room)
		}
		if room < len(r.World.Rooms)-1 {
			if r.Phase != Choosing {
				t.Fatal("missing upgrade choice")
			}
			tick := r.Elapsed
			r.Step(Command{Forward: 1, Fire: true})
			if r.Elapsed != tick {
				t.Fatal("combat advanced during choice")
			}
			r.Step(Command{Choice: 2})
			if r.Player.Fork != room+1 || r.Phase != Playing {
				t.Fatal("upgrade did not apply")
			}
		}
	}
	r.Player.Position = r.World.Exit
	r.Step(Command{Interact: true})
	if r.Phase != Won {
		t.Fatal("cleared exit did not win run")
	}
	tick := r.Tick
	r.Step(Command{Fire: true, Forward: 1})
	if r.Tick != tick {
		t.Fatal("finished run still advances")
	}
}

func TestUpgradeEffectsPickupsAndDeath(t *testing.T) {
	r := testArena()
	r.Phase = Choosing
	r.Step(Command{Choice: 1})
	r.Step(Command{Fire: true})
	if r.Player.Cooldown != 15 {
		t.Fatal("storm heart did not speed up weapon")
	}
	r.Phase = Choosing
	r.Step(Command{Choice: 3})
	r.Player.Health = 80
	r.Enemies = []Enemy{{ID: 1, Position: Vec3{Z: 4}, Health: 25, Cooldown: 999}, {ID: 2, Position: Vec3{X: 6, Z: 9}, Health: 100, Cooldown: 999}}
	r.Player.Cooldown = 0
	r.Step(Command{Fire: true})
	if r.Player.MaxHealth != 120 || r.Player.Health != 84 {
		t.Fatal("blood pact did not heal on kill")
	}
	r.Pickups = append(r.Pickups, Pickup{Position: r.Player.Position})
	r.Step(Command{})
	if !r.Pickups[0].Taken || r.Player.Health != 106 {
		t.Fatal("health pickup was not consumed")
	}
	r.Player.Invulnerable = 0
	r.hurt(1000)
	if r.Phase != Dead || r.Player.Health != 0 {
		t.Fatal("lethal damage did not end run")
	}
	fresh := New(r.Seed)
	if fresh.Kills != 0 || fresh.Player.Health != 100 || fresh.Phase != Playing {
		t.Fatal("new run retained old progression")
	}
}

func TestTideDeadline(t *testing.T) {
	r := testArena()
	r.Elapsed = RunTicks - 1
	r.Step(Command{})
	if r.Phase != Dead || r.Player.Health != 0 {
		t.Fatal("deadline did not end run")
	}
}

func TestFlowRoutesAroundCover(t *testing.T) {
	r := testArena()
	r.Player.Position = Vec3{Z: 2}
	r.Enemies = []Enemy{{ID: 1, Position: Vec3{Z: 9}, Health: 48, Cooldown: 10000}}
	r.World.Solids = []Solid{{Bounds: Rect{-1, 4, 1, 6}, Height: 3}}
	for i := 0; i < 360; i++ {
		r.Step(Command{})
	}
	if distance(r.Enemies[0].Position, r.Player.Position) > 1.2 {
		t.Fatalf("enemy stuck behind cover: %+v", r.Enemies[0].Position)
	}
}

// Exercise a complete run through actual commands, without teleporting, editing
// health, or killing enemies directly. This is a playability smoke check, not a
// substitute for human feedback about difficulty or weapon feel.
func TestCommandDrivenRunCanExtract(t *testing.T) {
	for _, seed := range []int64{1, 7, 42} {
		r := New(seed)
		for tick := 0; tick < RunTicks && r.Phase != Dead && r.Phase != Won; tick++ {
			c := Command{Choice: 2}
			pos := r.Player.Position
			var aim, move Vec3
			if r.Cleared[r.Room] {
				dest := r.World.Exit
				if r.Room < len(r.World.Rooms)-1 {
					dest = Vec3{Z: r.World.Rooms[r.Room+1].Bounds.MinZ + 2}
				}
				move = dest.Sub(pos).Flat().Unit()
				aim = move
				c.Interact = true
			} else {
				nearest := math.Inf(1)
				var target Enemy
				for _, e := range r.Enemies {
					if e.Room == r.Room && e.Health > 0 {
						if d := distance(pos, e.Position); d < nearest {
							nearest = d
							target = e
						}
					}
				}
				aim = target.Position.Add(Vec3{Y: 1.15}).Sub(pos.Add(Vec3{Y: EyeHeight}))
				move = Vec3{X: math.Sin(float64(tick)*.025) * .5}
				if nearest > 9 {
					move = target.Position.Sub(pos).Flat().Unit().Scale(.5)
				}
				c.Fire = true
				c.Dash = tick%130 == 0
			}
			c.Yaw = math.Atan2(aim.X, aim.Z)
			c.Pitch = math.Atan2(aim.Y, aim.Flat().Length())
			c.Forward = move.X*math.Sin(c.Yaw) + move.Z*math.Cos(c.Yaw)
			c.Strafe = move.X*math.Cos(c.Yaw) - move.Z*math.Sin(c.Yaw)
			r.Step(c)
		}
		if r.Phase != Won {
			t.Fatalf("seed %d: bot failed in room %d, phase %d, health %d, kills %d, pos %+v", seed, r.Room, r.Phase, r.Player.Health, r.Kills, r.Player.Position)
		}
		if r.Kills != len(r.Enemies) {
			t.Fatal("extracted without defeating all guardians")
		}
	}
}

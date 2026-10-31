using Roag.Core.Dungeons;
using Roag.Core.Expeditions;
using Roag.Core.Content;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    public WorldRoomId? RoomAt(WorldPosition position) => State.Dungeon is { } run
        ? run.RoomAt(position) : Chunks.Generator.RoomAt(position);

    /// <summary>Create the bounded solo run on persistent world chunks and actors.</summary>
    public static WorldSession CreateDungeonRun(int seed, ClassKind kind = ClassKind.Adventurer)
    {
        var world = Create(seed, floorCount: 8);
        try
        {
            world.State.Dungeon = new();
            world.State.Inventory["gold"] = 0;
            var combat = world.CreateTacticalSimulation(new(EnableSurvival: false));
            combat.State.Chests.AvailableBoons = BoonCatalog.Default.NormalPool
                .Where(id => id is not ("core:boon/road_flask" or "core:boon/ration_reserve")).ToArray();
            var first = DungeonLayoutGenerator.Generate(seed, DungeonCatalog.Default.Biomes[0].Id);
            var second = DungeonLayoutGenerator.Generate(unchecked(seed + 1), first.Biome);
            var origin = new WorldPosition(128, 128, 1);
            var firstExit = new WorldPosition(origin.X + first.Elite.X, origin.Y + first.Elite.Y, 1);
            var nextOrigin = new WorldPosition(firstExit.X - 4 - second.Start.X, firstExit.Y - second.Start.Y, 2);
            var hero = world.Party.Single();
            hero.Position = world.State.Position = new(origin.X + first.Start.X, origin.Y + first.Start.Y, 1);
            hero.Body.Hunger = 0; hero.Body.Fatigue = 0; hero.Body.Wounds = 0;
            hero.CombatTrait = "";
            hero.Talents.Clear();
            combat.Register(hero, new() { Build = CharacterBuild.Create(kind, world.DungeonSeed), AutoEat = false });
            hero.SpriteId = NativeAppearance.ForClass(kind);
            world.PaintDungeonFloor(first, origin, combat, boss: false);
            world.PaintDungeonFloor(second, nextOrigin, combat, boss: true);
            var landing = new WorldPosition(firstExit.X - 4, firstExit.Y, 2);
            world.Chunks.Edit(firstExit, Material.StairsDown); world.Chunks.Edit(landing, Material.StairsUp);
            world.Chunks.Prefetch(hero.Position); world.Reveal();
            combat.AdvanceToPartyDecision();
            return world;
        }
        catch { world.Dispose(); throw; }
    }

    internal int DungeonSeed => (int)(WorldRandom.Hash(Seed, 0, salt: 9401) % (RoagRandom.Modulus - 1)) + 1;
    public TacticalCombat DungeonCombat => State.Dungeon == null
        ? throw new InvalidOperationException("No native dungeon run is loaded.") : new(this, AdvanceTacticalWorld);

    public ActionResult ChooseDungeonClass(ClassKind kind)
    {
        if (State.Dungeon == null || !Enum.IsDefined(kind) || State.Tactics is not { NextAction: 1 } tactics || tactics.Queue.Clock != 0)
            return new(false, "Choose a fixed starting class before the first paid action.");
        var hero = Party.Single();
        hero.Tactics!.Build = CharacterBuild.Create(kind, DungeonSeed);
        hero.Tactics.Resources = new(); hero.Tactics.Boons = new(); hero.Health = hero.MaxHealth;
        hero.SpriteId = NativeAppearance.ForClass(kind);
        return new(true, ClassCatalog.Get(kind).Name + " selected.");
    }

    public ActionResult SubmitDungeon(TacticalIntent intent)
    {
        if (State.Dungeon == null || State.AwaitingContinuation) return new(false, "This run has ended. Begin a fresh run.");
        var combat = DungeonCombat;
        var result = combat.Commit(intent);
        if (result.Success) combat.AdvanceToPartyDecision();
        return result.Success && State.AwaitingContinuation ? new(true, "You have fallen. Begin a fresh run.") : result;
    }

    public TacticalPreview PreviewDungeonAim(RoagAim aim) => TryDungeonAim(aim, out var intent)
        ? DungeonCombat.Preview(intent) : new(false, "Choose an available native action.", 0, []);

    private bool TryDungeonAim(RoagAim aim, out TacticalIntent intent)
    {
        Facing facing = DungeonCombat.Active?.Tactics?.Facing ?? Facing.South;
        if ((aim.Dx != 0 || aim.Dy != 0) && !AttackGeometry.TryFacing(aim.Dx, aim.Dy, out facing))
        { intent = new(TacticalAction.Wait); return false; }
        TacticalAction? action = aim.Kind switch
        {
            "move" => TacticalAction.Move, "basic_attack" => TacticalAction.BasicAttack,
            "sweeping_slash" => TacticalAction.Special, "dash" => TacticalAction.Dash,
            "wait" => TacticalAction.Wait, "guard" => TacticalAction.Guard, "revive" => TacticalAction.Revive,
            "use_stairs" => TacticalAction.UseStairs, "claim_chest" => TacticalAction.ClaimChest, "dungeon_route" => TacticalAction.DungeonRoute,
            _ => null
        };
        intent = new(action ?? TacticalAction.Wait, facing, aim.Target, aim.Vow, aim.Choice);
        return action != null;
    }

    private void PaintDungeonFloor(DungeonLayout layout, WorldPosition origin, TacticalCombat combat, bool boss)
    {
        WorldPosition At(RoomPoint point) => new(origin.X + point.X, origin.Y + point.Y, origin.Floor);
        for (int y = 0; y < layout.Height; y++) for (int x = 0; x < layout.Width; x++)
        {
            var material = layout.Rows[y][x] switch { '#' => Material.Wall, '~' => Material.Water, 'B' => Material.Barricade, _ => Material.Floor };
            Chunks.Edit(new(origin.X + x, origin.Y + y, origin.Floor), material);
            if (layout.Rows[y][x] is 'b' or 'g' or 'i' or 'm' or 'c')
                State.Dungeon!.Terrain[DungeonRunState.CellKey(new(origin.X + x, origin.Y + y, origin.Floor))] = layout.Rows[y][x];
        }
        var biome = DungeonCatalog.Default.Biomes.Single(b => b.Id == layout.Biome);
        var rooms = layout.Rooms.Select(r => new DungeonRoomSite(r.Index, r.Shape.Id, new(origin.X + r.X, origin.Y + r.Y, origin.Floor),
            r.Shape.Width, r.Shape.Height, r.Shape.Turns, r.Shape.Mirror, r.Role, layout.CriticalRooms.Contains(r.Index))).ToArray();
        var floor = new DungeonFloorState(State.Dungeon!.Stage, layout.Biome, origin, layout.Width, layout.Height, rooms, At(layout.Start), At(layout.Elite));
        State.Dungeon!.Floors.Add(floor);
        int serial = 0;
        var chestSites = layout.Rooms.Where(r => r.Role is not ("start" or "elite") && r.Shape.Markers.GetValueOrDefault("chest") is { Length: > 0 })
            .Select(r => (Room: r, Position: At(r.At(r.Shape.Markers["chest"][0])))).Where(p => CanEnter(p.Position)).ToArray();
        int chestCount = origin.Floor % 2 == 1 ? 2 : 3;
        if (chestSites.Length < chestCount) throw new InvalidDataException("Authored dungeon rooms lack the original chest budget.");
        for (int i = 0; i < chestCount; i++)
        {
            var site = chestSites[i * (chestSites.Length - 1) / (chestCount - 1)];
            combat.SpawnBoonChest("core:chest/standard", $"floor:{origin.Floor}/room:{site.Room.Index}/chest:0", site.Position);
        }
        foreach (var room in layout.Rooms)
        {
            if (room.Role is "start" or "elite" || !room.Shape.Markers.TryGetValue("enemy", out var enemies) || enemies.Length == 0) continue;
            var position = At(room.At(enemies[room.Index % enemies.Length]));
            if (CanEnter(position)) SpawnDungeonEnemy(combat, biome.EnemyIds[serial++ % biome.EnemyIds.Length], position);
        }
        PopulateDungeonEncounters(layout, origin, combat);
        if (boss)
        {
            var actor = SpawnDungeonEnemy(combat, biome.EliteId, floor.Exit);
            actor.Tactics!.Enemy!.Dormant = true;
            combat.State.Queue.CancelActor(actor.Id, ScheduledKind.ActorReady);
            int index = State.Dungeon.Floors.IndexOf(floor);
            State.Dungeon.Floors[index] = floor with { Boss = actor.Id };
        }
        PopulateDungeonOptionalTerrain(layout, origin, combat);
    }

    private Actor SpawnDungeonEnemy(TacticalCombat combat, string definition, WorldPosition position)
    {
        var actor = ActorFactory.Create(Content, Content.Game.Raider, Seed, State.NextActorId++, position, 0);
        actor.Name = EnemyCatalog.Default.Get(definition).Name;
        actor.CombatTrait = "";
        State.Actors.Add(actor);
        combat.RegisterEnemy(actor, definition, checked(combat.Clock + 100));
        actor.Health = actor.MaxHealth;
        return actor;
    }
}

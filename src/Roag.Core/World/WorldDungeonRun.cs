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
        var world = Create(seed);
        try
        {
            world.State.Dungeon = new();
            world.State.Inventory["gold"] = 0;
            var combat = world.CreateTacticalSimulation(new(EnableSurvival: false));
            var first = DungeonLayoutGenerator.Generate(seed, DungeonCatalog.Default.Biomes[0].Id);
            var second = DungeonLayoutGenerator.Generate(unchecked(seed + 1), first.Biome);
            var origin = new WorldPosition(128, 128, 1);
            var firstExit = new WorldPosition(origin.X + first.Elite.X, origin.Y + first.Elite.Y, 1);
            var nextOrigin = new WorldPosition(firstExit.X - 4 - second.Start.X, firstExit.Y - second.Start.Y, 2);
            var hero = world.Party.Single();
            hero.Position = world.State.Position = new(origin.X + first.Start.X, origin.Y + first.Start.Y, 1);
            hero.Body.Hunger = 0; hero.Body.Fatigue = 0; hero.Body.Wounds = 0;
            combat.Register(hero, new() { Build = CharacterBuild.Create(kind, world.DungeonSeed), AutoEat = false });
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

    private int DungeonSeed => (int)(WorldRandom.Hash(Seed, 0, salt: 9401) % (RoagRandom.Modulus - 1)) + 1;
    public TacticalCombat DungeonCombat => State.Dungeon == null
        ? throw new InvalidOperationException("No native dungeon run is loaded.") : new(this, AdvanceTacticalWorld);

    public ActionResult ChooseDungeonClass(ClassKind kind)
    {
        if (State.Dungeon == null || !Enum.IsDefined(kind) || State.Tactics is not { NextAction: 1 } tactics || tactics.Queue.Clock != 0)
            return new(false, "Choose a fixed starting class before the first paid action.");
        var hero = Party.Single();
        hero.Tactics!.Build = CharacterBuild.Create(kind, DungeonSeed);
        hero.Tactics.Resources = new(); hero.Tactics.Boons = new(); hero.Health = hero.MaxHealth;
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
            r.Shape.Width, r.Shape.Height, r.Shape.Turns, r.Shape.Mirror)).ToArray();
        var floor = new DungeonFloorState(1, layout.Biome, origin, layout.Width, layout.Height, rooms, At(layout.Start), At(layout.Elite));
        State.Dungeon!.Floors.Add(floor);
        int serial = 0;
        foreach (var room in layout.Rooms)
        {
            if (room.Shape.Markers.TryGetValue("enemy", out var enemies))
                foreach (var point in enemies)
                {
                    var position = At(room.At(point));
                    string definition = biome.EnemyIds[(int)(WorldRandom.Hash(Seed, room.Index, serial++, 9402 + origin.Floor) % (uint)biome.EnemyIds.Length)];
                    if (CanEnter(position)) SpawnDungeonEnemy(combat, definition, position);
                }
            if (room.Shape.Markers.TryGetValue("chest", out var chests))
                foreach (var point in chests)
                {
                    var position = At(room.At(point));
                    if (CanEnter(position)) combat.SpawnBoonChest("core:chest/standard", $"floor:{origin.Floor}/room:{room.Index}/chest:{serial++}", position);
                }
        }
        if (boss)
        {
            var actor = SpawnDungeonEnemy(combat, biome.EliteId, floor.Exit);
            int index = State.Dungeon.Floors.IndexOf(floor);
            State.Dungeon.Floors[index] = floor with { Boss = actor.Id };
        }
    }

    private Actor SpawnDungeonEnemy(TacticalCombat combat, string definition, WorldPosition position)
    {
        var actor = ActorFactory.Create(Content, Content.Game.Raider, Seed, State.NextActorId++, position, 0);
        actor.Name = EnemyCatalog.Default.Get(definition).Name;
        actor.SpriteId = "bandit"; // Stored Vagabonds mapping; final per-species bindings remain presentation work.
        State.Actors.Add(actor);
        combat.RegisterEnemy(actor, definition, checked(combat.Clock + 100));
        actor.Health = actor.MaxHealth;
        return actor;
    }
}

using Roag.Core.Dungeons;
using Roag.Core.Expeditions;
using Roag.Core.Content;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    public WorldRoomId? RoomAt(WorldPosition position) => SnapshotRoomAt(State, Chunks.Generator, position);

    /// <summary>Create the bounded solo run on persistent world chunks and actors.</summary>
    public static WorldSession CreateDungeonRun(int seed, ClassKind kind = ClassKind.Adventurer, bool survival = false)
    {
        var world = CreateWorld(seed, null, null, roagNative: false, floorCount: 8, populateWorld: false);
        try
        {
            world.State.Dungeon = new();
            world.State.Inventory["gold"] = 0;
            var combat = world.CreateTacticalSimulation(new(EnableSurvival: survival));
            combat.State.Chests.AvailableBoons = (survival ? BoonCatalog.Default : BoonCatalog.Original).NormalPool
                .Where(id => survival || id is not ("core:boon/road_flask" or "core:boon/ration_reserve")).ToArray();
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
            combat.Register(hero, new() { Build = CharacterBuild.Create(kind, world.DungeonSeed), AutoEat = survival });
            hero.SpriteId = NativeAppearance.ForClass(kind);
            world.PaintDungeonFloor(first, origin, combat, world.State.Dungeon, world.DungeonSeed, boss: false);
            world.PaintDungeonFloor(second, nextOrigin, combat, world.State.Dungeon, world.DungeonSeed, boss: true);
            var landing = new WorldPosition(firstExit.X - 4, firstExit.Y, 2);
            world.Chunks.Edit(firstExit, Material.StairsDown); world.Chunks.Edit(landing, Material.StairsUp);
            world.Chunks.Prefetch(hero.Position); world.Reveal();
            combat.AdvanceToPartyDecision();
            return world;
        }
        catch { world.Dispose(); throw; }
    }

    internal int DungeonSeed => (int)(WorldRandom.Hash(Seed, 0, salt: 9401) % (RoagRandom.Modulus - 1)) + 1;
    public TacticalCombat DungeonCombat => !HasNativeParty
        ? throw new InvalidOperationException("No native dungeon run is loaded.") : new(this, AdvanceTacticalWorld);

    public ActionResult ChooseDungeonClass(ClassKind kind)
    {
        if (!HasNativeParty || !Enum.IsDefined(kind) || State.Party.Count != 1
            || State.Tactics is not { NextAction: 1 } tactics
            || tactics.Queue.Clock != (State.NativeCampaign?.StartedAt ?? 0) * tactics.Tuning.UnitsPerWorldTick)
            return new(false, "Choose a fixed starting class before the first paid action.");
        var hero = Party.Single();
        hero.Tactics!.Build = CharacterBuild.Create(kind, DungeonSeed);
        hero.Tactics.Resources = new(); hero.Tactics.Boons = new(); hero.Health = hero.MaxHealth;
        hero.SpriteId = NativeAppearance.ForClass(kind);
        return new(true, ClassCatalog.Get(kind).Name + " selected.");
    }

    public ActionResult SubmitDungeon(TacticalIntent intent)
    {
        if (!HasNativeParty || State.AwaitingContinuation) return new(false, "This run has ended. Begin a fresh run.");
        if (!SelectedDungeonMemberReady) return new(false, "Select the ready member to act; inspecting another member does not change turn order.");
        var combat = DungeonCombat;
        var result = combat.Commit(intent);
        if (result.Success) combat.AdvanceToPartyDecision();
        if (result.Success && (State.AwaitingContinuation || intent.Action == TacticalAction.Abandon))
        {
            // Persist only after the complete action/effect transaction settles.
            // A failed death save stays in memory and blocks fresh-run creation.
            try { CommitDeaths(); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Diagnose("death-save-failed", new { e.Message });
                return new(true, State.AwaitingContinuation
                    ? "You have fallen. Death could not be saved. Use Save to retry before starting a fresh run."
                    : "Abandonment could not be saved. Use Save to record this death before starting a fresh run.");
            }
        }
        return result.Success && State.AwaitingContinuation ? new(true, "You have fallen. Begin a fresh run.") : result;
    }

    public TacticalPreview PreviewDungeonAim(RoagAim aim) => !SelectedDungeonMemberReady
        ? new(false, "Select the ready member to act.", 0, []) : TryDungeonAim(aim, out var intent)
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
            "use_utility" => TacticalAction.UseUtility,
            "interact" => TacticalAction.Interact,
            "open_door" => TacticalAction.OpenDoor,
            "open_vault" => TacticalAction.OpenVault,
            "recruit" => TacticalAction.Recruit,
            "abandon" => TacticalAction.Abandon,
            "cooperate" => TacticalAction.Cooperate,
            "recover_equipment" => TacticalAction.RecoverEquipment,
            "eat" => TacticalAction.Eat,
            "rest" => TacticalAction.Rest,
            "light_fire" => TacticalAction.LightCampFire,
            "extinguish_fire" => TacticalAction.ExtinguishCampFire,
            "watch" => TacticalAction.Watch,
            "use_medicine" => TacticalAction.UseMedicine,
            _ => null
        };
        intent = new(action ?? TacticalAction.Wait, facing, aim.Target, aim.Vow, aim.Choice, aim.ReplaceUtility);
        return action != null;
    }

    private void PaintDungeonFloor(DungeonLayout layout, WorldPosition origin, TacticalCombat combat, DungeonRunState run, int dungeonSeed, bool boss)
    {
        WorldPosition At(RoomPoint point) => new(origin.X + point.X, origin.Y + point.Y, origin.Floor);
        for (int y = 0; y < layout.Height; y++) for (int x = 0; x < layout.Width; x++)
        {
            var material = layout.Rows[y][x] switch { '#' => Material.Wall, '~' => Material.Water, 'B' => Material.Barricade, _ => Material.Floor };
            Chunks.Edit(new(origin.X + x, origin.Y + y, origin.Floor), material);
            if (layout.Rows[y][x] is 'b' or 'g' or 'i' or 'm' or 'c')
                run.Terrain[DungeonRunState.CellKey(new(origin.X + x, origin.Y + y, origin.Floor))] = layout.Rows[y][x];
        }
        var biome = DungeonCatalog.Default.Biomes.Single(b => b.Id == layout.Biome);
        var rooms = layout.Rooms.Select(r => new DungeonRoomSite(r.Index, r.Shape.Id, new(origin.X + r.X, origin.Y + r.Y, origin.Floor),
            r.Shape.Width, r.Shape.Height, r.Shape.Turns, r.Shape.Mirror, r.Role, layout.CriticalRooms.Contains(r.Index))).ToArray();
        var floor = new DungeonFloorState(run.Stage, layout.Biome, origin, layout.Width, layout.Height, rooms, At(layout.Start), At(layout.Elite));
        run.Floors.Add(floor);
        var population = new DungeonPopulation(run, floor, dungeonSeed);
        foreach (var door in layout.Doors) combat.SpawnDoor($"floor:{origin.Floor}/door:room:{door.Room}", At(door.Position));
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
        PopulateDungeonCorridors(layout, population, combat, biome.EnemyIds, serial);
        PopulateDungeonEncounters(layout, population, combat);
        if (boss)
        {
            var actor = SpawnDungeonEnemy(combat, biome.EliteId, floor.Exit);
            actor.Tactics!.Enemy!.Dormant = true;
            combat.State.Queue.CancelActor(actor.Id, ScheduledKind.ActorReady);
            int index = run.Floors.IndexOf(floor);
            run.Floors[index] = floor with { Boss = actor.Id };
        }
        PopulateDungeonSecrets(layout, population, combat);
        PopulateDungeonOptionalTerrain(layout, population, combat);
        PopulateDungeonServices(layout, population, combat);
        PopulateDungeonRoomFeatures(layout, population, combat);
        PopulateDungeonVault(layout, population, combat);
        PopulateDungeonRecruits(population);
    }

    // Every placement helper receives its physical floor and owning progression
    // explicitly; no helper infers a floor from whichever member is selected.
    private sealed record DungeonPopulation(DungeonRunState Run, DungeonFloorState Floor, int Seed);

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

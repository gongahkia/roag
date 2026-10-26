namespace Roag.Core.World;

public enum WorldSeason { Spring, Summer, Autumn, Winter }
public enum WeatherKind { Clear, Rain, Wind, Storm, Snow, Dry }
public readonly record struct ClimateConditions(WorldSeason Season, WeatherKind Weather, long Until, int GatheringPercent,
    int FoodYieldPercent, int RecoveryPercent, int ExposedRestPercent, double TravelFatigue, bool Underground)
{
    public bool Severe => !Underground && Weather is WeatherKind.Storm or WeatherKind.Snow;
}

public sealed partial class WorldSession
{
    public const int ClimateRegionSize = 512, WeatherPeriod = TicksPerHour * 6, SeasonDays = 30, TicksPerDay = TicksPerHour * 24;
    private readonly (bool Ready, Biome Biome, long Epoch, ClimateConditions Conditions)[] _climateRegions = new (bool, Biome, long, ClimateConditions)[64];
    private static ClimateConditions ClimateFor(WorldPlan plan, WorldPosition position, long tick, Biome biome)
    {
        var season = (WorldSeason)(tick / (TicksPerDay * SeasonDays) % 4);
        long epoch = tick / WeatherPeriod, until = (epoch + 1) * WeatherPeriod;
        if (position.Floor > 0) return new(season, WeatherKind.Clear, until, 100, 100, 100, 100, 0, true);
        int region = position.Y / ClimateRegionSize * 8 + position.X / ClimateRegionSize;
        uint roll = WorldRandom.Hash(plan.Seed, region, (int)epoch, 1729) % 100;
        WeatherKind weather = roll < 45 ? WeatherKind.Clear : roll < 65 ? WeatherKind.Wind : roll < 85 ? WeatherKind.Rain
            : roll < 95 ? WeatherKind.Storm : WeatherKind.Dry;
        if ((biome == Biome.Snow || season == WorldSeason.Winter && biome is not (Biome.Marsh or Biome.Badland)) && weather is WeatherKind.Rain or WeatherKind.Storm)
            weather = WeatherKind.Snow;
        if (biome == Biome.Badland && weather == WeatherKind.Rain) weather = WeatherKind.Dry;
        (int Gather, int Food, int Recovery, int Resting, double Travel) rule = weather switch
        {
            WeatherKind.Rain => (85, 120, 80, 70, .04),
            WeatherKind.Wind => (90, 90, 110, 80, .02), WeatherKind.Storm => (65, 80, 125, 50, .08),
            WeatherKind.Snow => (70, 50, 175, 50, .08), WeatherKind.Dry => (90, 60, 150, 80, .04),
            _ => (100, 100, 100, 100, 0.0)
        };
        int food = season switch { WorldSeason.Summer or WorldSeason.Autumn => 125, WorldSeason.Winter => 50, _ => 100 };
        int recovery = season switch { WorldSeason.Summer => 90, WorldSeason.Autumn => 110, WorldSeason.Winter => 175, _ => 100 };
        return new(season, weather, until, rule.Gather, food * rule.Food / 100, Math.Clamp(recovery * rule.Recovery / 100, 50, 300), rule.Resting, rule.Travel, false);
    }
    internal static ClimateConditions ClimateAt(WorldPlan plan, WorldPosition position, long tick)
    {
        int x = position.X / ClimateRegionSize, y = position.Y / ClimateRegionSize;
        var biome = plan.BiomeAt(x * ClimateRegionSize + ClimateRegionSize / 2, y * ClimateRegionSize + ClimateRegionSize / 2);
        return ClimateFor(plan, position, tick, biome);
    }
    public ClimateConditions WeatherAt(WorldPosition position) => WeatherAt(position, State.Tick);
    private ClimateConditions WeatherAt(WorldPosition position, long tick)
    {
        if (!Plan.Contains(position)) throw new ArgumentOutOfRangeException(nameof(position));
        int index = position.Y / ClimateRegionSize * 8 + position.X / ClimateRegionSize;
        ref var cached = ref _climateRegions[index]; long epoch = tick / WeatherPeriod;
        if (!cached.Ready)
        {
            cached.Biome = Plan.BiomeAt(position.X / ClimateRegionSize * ClimateRegionSize + ClimateRegionSize / 2,
                position.Y / ClimateRegionSize * ClimateRegionSize + ClimateRegionSize / 2); cached.Ready = true; cached.Epoch = -1;
        }
        if (cached.Epoch != epoch) { cached.Epoch = epoch; cached.Conditions = ClimateFor(Plan, position with { Floor = 0 }, tick, cached.Biome); }
        return position.Floor == 0 ? cached.Conditions : new(cached.Conditions.Season, WeatherKind.Clear, cached.Conditions.Until, 100, 100, 100, 100, 0, true);
    }
    public bool ShelteredAt(WorldPosition position)
    {
        if (!Plan.Contains(position)) throw new ArgumentOutOfRangeException(nameof(position));
        if (position.Floor > 0) return true;
        if (Chunks.KnownCampAt(position)) return true;
        foreach (var (x, y) in Directions) if (Chunks.KnownCampAt(position.Offset(x, y))) return true;
        return false;
    }
    public bool Sheltered(Actor actor) => ShelteredAt(actor.Position) || !actor.Recruited && actor.Routine is { Kind: RoutineKind.Shelter } routine
        && actor.Position.Distance(routine.Target) <= 1 && routine.Target.Distance(ResidentHomeAnchor(actor)) <= 6;
    private double ClimateGathering(WorldPosition position) => ShelteredAt(position) ? 1 : WeatherAt(position).GatheringPercent / 100.0;
    private void CommitClimateMove(Actor actor, WorldPosition next)
    {
        if (actor.Kind == ActorKind.Person && actor.Life == LifeState.Alive && actor.Position.Distance(next) == 1 && !ShelteredAt(next))
            { double exposure = WeatherAt(next).TravelFatigue; if (exposure > 0) actor.Body.Fatigue = Math.Min(100, actor.Body.Fatigue + exposure); }
        actor.Position = next;
    }
    private double ClimateRest(Actor actor, long tick) => Sheltered(actor) ? 1 : WeatherAt(actor.Position, tick).ExposedRestPercent / 100.0;
    public string WeatherDescription(WorldPosition position)
    {
        var view = WeatherAt(position);
        return view.Underground ? view.Season + "; underground cover protects travel and rest."
            : $"{view.Season} / {view.Weather}. Exposed gathering {view.GatheringPercent}%; food production {view.FoodYieldPercent}%; regrowth time {view.RecoveryPercent}%."
                + (view.TravelFatigue > 0 ? " Exposed travel is tiring; camp cover protects rest." : " Calm travel and rest.");
    }
    private static long RecoveryDelay(int ticks, int percent) => ((long)ticks * percent + 99) / 100;
}

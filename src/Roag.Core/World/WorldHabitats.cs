namespace Roag.Core.World;

/// <summary>Persistent wild-food stock; resource nodes keep their own authored stocks.</summary>
public sealed class HabitatState
{
    public int RecoveryVersion { get; set; }
    [System.Text.Json.Serialization.JsonRequired] public int Food { get; set; }
    [System.Text.Json.Serialization.JsonRequired] public int Pressure { get; set; }
    [System.Text.Json.Serialization.JsonRequired] public long Epoch { get; set; }
    [System.Text.Json.Serialization.JsonRequired] public Dictionary<int, int> LandLoss { get; set; } = [];
}
public readonly record struct HabitatConditions(ChunkKey Key, int Food, int Capacity, int Pressure, int ClearedTiles, long RecoverIn);

public sealed partial class WorldSession
{
    private readonly record struct HabitatReadKey(HabitatState State, long Epoch, long Settled, int Food, int Pressure, int Capacity, int Version);
    private (int Index, HabitatReadKey Key, int Food, int Pressure)[]? _habitatReads;
    private (int Food, int Pressure) CachedHabitatElapsed(ChunkKey chunk, HabitatState? saved, long epoch)
    {
        if (saved is not { RecoveryVersion: 1 } || epoch <= saved.Epoch) return HabitatElapsed(Plan, chunk, saved, epoch);
        _habitatReads ??= new (int, HabitatReadKey, int, int)[128]; int index = HabitatIndex(chunk);
        var key = new HabitatReadKey(saved, epoch, saved.Epoch, saved.Food, saved.Pressure, HabitatCapacity(Plan, chunk, saved), saved.RecoveryVersion);
        ref var slot = ref _habitatReads[index % _habitatReads.Length];
        if (slot.Index == index && slot.Key == key) return (slot.Food, slot.Pressure);
        var value = HabitatElapsed(Plan, chunk, saved, epoch); slot = (index, key, value.Food, value.Pressure); return value;
    }
    public const int HabitatPressureLimit = 1000, HabitatPressureRecovery = 20;
    public static int HabitatIndex(ChunkKey key) => key.Floor * 4096 + key.Y * 64 + key.X;
    private static bool HabitatKeyValid(ChunkKey key) => key.X is >= 0 and < 64 && key.Y is >= 0 and < 64 && key.Floor is >= 0 and < WorldPlan.Floors;
    private static int HabitatBaseCapacity(WorldPlan plan, ChunkKey key) => key.Floor > 0 ? 8 : plan.BiomeAt(key.X * 64 + 32, key.Y * 64 + 32) switch
    { Biome.Forest => 64, Biome.Marsh => 40, Biome.Highland => 24, Biome.Badland => 12, Biome.Snow => 16, _ => 48 };
    private static int HabitatCapacity(WorldPlan plan, ChunkKey key, HabitatState? state)
    {
        int loss = 0; if (state != null) foreach (int weight in state.LandLoss.Values) loss += weight;
        return Math.Max(0, HabitatBaseCapacity(plan, key) - (loss + 31) / 32);
    }
    private static (int Food, int Pressure) HabitatElapsed(WorldPlan plan, ChunkKey key, HabitatState? state, long epoch)
    {
        int capacity = HabitatCapacity(plan, key, state);
        if (state == null) return (capacity, 0);
        long elapsed = Math.Max(0, epoch - state.Epoch);
        if (elapsed == 0 || state.Food >= capacity) return (Math.Min(capacity, state.Food), (int)Math.Max(0, state.Pressure - elapsed * HabitatPressureRecovery));
        if (state.RecoveryVersion == 1)
        {
            // Pressure falls independently; food never decays here. Skip barren
            // hours, then visit at most Capacity hours before positive recovery
            // fills the stock, even after centuries away.
            long barrenHours = Math.Min(elapsed, Math.Max(0, (state.Pressure - 600) / HabitatPressureRecovery));
            int pressure = (int)Math.Max(0, state.Pressure - barrenHours * HabitatPressureRecovery), food = state.Food;
            int climaticRate = Math.Max(1, HabitatBaseCapacity(plan, key) / 8);
            var center = new WorldPosition(key.X * 64 + 32, key.Y * 64 + 32, key.Floor);
            int rx = center.X / ClimateRegionSize, ry = center.Y / ClimateRegionSize;
            var biome = plan.BiomeAt(rx * ClimateRegionSize + ClimateRegionSize / 2, ry * ClimateRegionSize + ClimateRegionSize / 2);
            for (long hour = state.Epoch + barrenHours + 1; hour <= epoch && food < capacity; hour++)
            {
                pressure = Math.Max(0, pressure - HabitatPressureRecovery);
                var climate = ClimateFor(plan, center, hour * TicksPerHour, biome);
                int recoveryRate = Math.Max(1, climaticRate * 100 / climate.RecoveryPercent);
                food = Math.Min(capacity, food + (pressure >= 200 ? Math.Max(1, recoveryRate / 2) : recoveryRate));
            }
            return (food, (int)Math.Max(0, state.Pressure - elapsed * HabitatPressureRecovery));
        }
        // Integrate the exact hourly bands without visiting every missed hour.
        long barren = Math.Min(elapsed, Math.Max(0, (state.Pressure - 600) / HabitatPressureRecovery));
        long reduced = Math.Min(elapsed, Math.Max(0, (state.Pressure - 200) / HabitatPressureRecovery));
        int rate = Math.Max(1, HabitatBaseCapacity(plan, key) / 8);
        long recovery = (reduced - barren) * Math.Max(1, rate / 2) + (elapsed - reduced) * rate;
        return ((int)Math.Min(capacity, state.Food + recovery), (int)Math.Max(0, state.Pressure - elapsed * HabitatPressureRecovery));
    }
    /// <summary>Pure state/coordinate query; no terrain generation, actor discovery or saved mutation.</summary>
    public HabitatConditions HabitatAt(WorldPosition position)
    {
        var key = position.Chunk; if (!Plan.Contains(position)) throw new ArgumentOutOfRangeException(nameof(position));
        State.Habitats.TryGetValue(HabitatIndex(key), out var saved);
        var (food, pressure) = CachedHabitatElapsed(key, saved, State.Tick / TicksPerHour);
        int capacity = HabitatCapacity(Plan, key, saved);
        long epochs = Math.Max(1, (pressure - 600) / HabitatPressureRecovery + 1);
        if (saved?.RecoveryVersion == 1 && food < 2 && capacity >= 2)
        {
            // Report actual usable stock, including a two-hour underground refill.
            while (HabitatElapsed(Plan, key, saved, State.Tick / TicksPerHour + epochs).Food < 2) epochs++;
        }
        long recovery = food >= 2 || capacity < 2 ? 0 : (State.Tick / TicksPerHour + epochs) * TicksPerHour - State.Tick;
        return new(key, food, capacity, pressure, saved?.LandLoss.Count ?? 0, recovery);
    }
    private HabitatState SettleHabitat(WorldPosition position)
    {
        var view = HabitatAt(position); int index = HabitatIndex(view.Key);
        if (!State.Habitats.TryGetValue(index, out var state)) State.Habitats.Add(index, state = new());
        state.Food = view.Food; state.Pressure = view.Pressure; state.Epoch = State.Tick / TicksPerHour; state.RecoveryVersion = 1; return state;
    }
    private bool TakeWildFood(WorldPosition position, int amount)
    {
        if (HabitatAt(position).Food < amount) return false;
        var state = SettleHabitat(position); state.Food -= amount; state.Pressure = Math.Min(HabitatPressureLimit, state.Pressure + amount * 4);
        if (state.Food < 2 || state.Pressure >= 600) WakeHabitatWildlife(position); return true;
    }
    private void PressureHabitat(WorldPosition position, int amount)
    { var state = SettleHabitat(position); state.Pressure = Math.Min(HabitatPressureLimit, state.Pressure + amount); if (state.Pressure >= 600) WakeHabitatWildlife(position); }
    private static int Vegetation(Material material) => material == Material.Tree ? 8 : material == Material.Grass ? 2 : 0;
    private void RecordHabitatClearing(WorldPosition position, Material before, Material after)
    {
        int change = Vegetation(before) - Vegetation(after); if (change == 0) return;
        var state = SettleHabitat(position); int index = position.LocalIndex;
        int loss = Math.Clamp(state.LandLoss.GetValueOrDefault(index) + change, 0, 8);
        if (loss == 0) state.LandLoss.Remove(index); else state.LandLoss[index] = loss;
        state.Food = Math.Min(state.Food, HabitatCapacity(Plan, position.Chunk, state));
        state.Pressure = Math.Min(HabitatPressureLimit, state.Pressure + Math.Max(0, change) * 2);
        WakeHabitatWildlife(position);
    }
    public string WildFoodUnavailable(WorldPosition position)
    {
        var view = HabitatAt(position); if (view.Food >= 2) return "";
        return view.Capacity < 2 ? "The cleared grounds cannot support wild food here. Seek another habitat."
            : $"Local wild food is depleted; recovery begins in {Math.Max(1, (view.RecoverIn + 4) / 5)} game minutes. Seek another habitat or use stored food.";
    }
    public string HabitatDescription(WorldPosition position)
    {
        var view = HabitatAt(position); string pressure = view.Pressure >= 600 ? "heavy" : view.Pressure >= 200 ? "moderate" : "low";
        return $"Wild food {view.Food}/{view.Capacity}; habitat pressure {pressure}." + (view.ClearedTiles == 0 ? "" : $" {view.ClearedTiles} cleared tiles reduce its capacity.");
    }
    private static void ValidateHabitats(WorldPlan plan, WorldState state)
    {
        if (state.Habitats == null || state.Habitats.Count > 16384) throw new InvalidDataException("Invalid habitat registry.");
        foreach (var (index, habitat) in state.Habitats)
        {
            var key = new ChunkKey(index % 64, index / 64 % 64, index / 4096);
            if (index is < 0 or >= 16384 || !HabitatKeyValid(key) || habitat == null || habitat.LandLoss == null
                || habitat.LandLoss.Count > 4096 || habitat.LandLoss.Any(p => p.Key is < 0 or >= 4096 || p.Value is < 1 or > 8)
                || habitat.RecoveryVersion is < 0 or > 1 || habitat.Epoch < 0 || habitat.Epoch > state.Tick / TicksPerHour || habitat.Pressure is < 0 or > HabitatPressureLimit
                || habitat.Food < 0 || habitat.Food > HabitatCapacity(plan, key, habitat)) throw new InvalidDataException("Invalid habitat stock, pressure or land use.");
        }
    }
}

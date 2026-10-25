namespace Vagabonds.Core.World;

/// <summary>Persistent wild-food stock; resource nodes keep their own authored stocks.</summary>
public sealed class HabitatState
{
    [System.Text.Json.Serialization.JsonRequired] public int Food { get; set; }
    [System.Text.Json.Serialization.JsonRequired] public int Pressure { get; set; }
    [System.Text.Json.Serialization.JsonRequired] public long Epoch { get; set; }
    [System.Text.Json.Serialization.JsonRequired] public Dictionary<int, int> LandLoss { get; set; } = [];
}
public readonly record struct HabitatConditions(ChunkKey Key, int Food, int Capacity, int Pressure, int ClearedTiles, long RecoverIn);

public sealed partial class WorldSession
{
    public const int HabitatPressureLimit = 1000, HabitatPressureRecovery = 20;
    public static int HabitatIndex(ChunkKey key) => key.Floor * 4096 + key.Y * 64 + key.X;
    private static bool HabitatKeyValid(ChunkKey key) => key.X is >= 0 and < 64 && key.Y is >= 0 and < 64 && key.Floor is >= 0 and < WorldPlan.Floors;
    private static int HabitatBaseCapacity(WorldPlan plan, ChunkKey key) => key.Floor > 0 ? 8 : plan.BiomeAt(key.X * 64 + 32, key.Y * 64 + 32) switch
    { Biome.Forest => 64, Biome.Marsh => 40, Biome.Highland => 24, Biome.Badland => 12, Biome.Snow => 16, _ => 48 };
    private static int HabitatCapacity(WorldPlan plan, ChunkKey key, HabitatState? state) => Math.Max(0,
        HabitatBaseCapacity(plan, key) - ((state?.LandLoss.Values.Sum() ?? 0) + 31) / 32);
    private static (int Food, int Pressure) HabitatElapsed(WorldPlan plan, ChunkKey key, HabitatState? state, long epoch)
    {
        int capacity = HabitatCapacity(plan, key, state);
        if (state == null) return (capacity, 0);
        long elapsed = Math.Max(0, epoch - state.Epoch);
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
        var (food, pressure) = HabitatElapsed(Plan, key, saved, State.Tick / TicksPerHour);
        int capacity = HabitatCapacity(Plan, key, saved);
        long epochs = Math.Max(1, (pressure - 600) / HabitatPressureRecovery + 1);
        long recovery = food >= 2 || capacity < 2 ? 0 : (State.Tick / TicksPerHour + epochs) * TicksPerHour - State.Tick;
        return new(key, food, capacity, pressure, saved?.LandLoss.Count ?? 0, recovery);
    }
    private HabitatState SettleHabitat(WorldPosition position)
    {
        var view = HabitatAt(position); int index = HabitatIndex(view.Key);
        if (!State.Habitats.TryGetValue(index, out var state)) State.Habitats.Add(index, state = new());
        state.Food = view.Food; state.Pressure = view.Pressure; state.Epoch = State.Tick / TicksPerHour; return state;
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
                || habitat.Epoch < 0 || habitat.Epoch > state.Tick / TicksPerHour || habitat.Pressure is < 0 or > HabitatPressureLimit
                || habitat.Food < 0 || habitat.Food > HabitatCapacity(plan, key, habitat)) throw new InvalidDataException("Invalid habitat stock, pressure or land use.");
        }
    }
}

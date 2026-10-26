using System.Diagnostics;
using Roag.Core.Expeditions;

namespace Roag.Core.World;

/// <summary>Optional, ephemeral observation. Never serialized into the world or used by its rules.</summary>
public sealed record WorldDiagnosticEvent(string Kind, long Tick, object? Data);

public sealed partial class WorldSession
{
    private Action<WorldDiagnosticEvent>? _diagnosticSink;
    public Action<WorldDiagnosticEvent>? DiagnosticSink
    {
        get => _diagnosticSink;
        set { _diagnosticSink = value; Chunks.DiagnosticSink = value; }
    }

    private void Diagnose(string kind, object? data)
    {
        try { _diagnosticSink?.Invoke(new(kind, State.Tick, data)); }
        catch (Exception) { /* Diagnostic failure cannot change a command's result or randomness. */ }
    }

    private ActionResult ObserveAction(string kind, object input, Func<ActionResult> run)
    {
        if (_diagnosticSink == null) return run();
        long started = Stopwatch.GetTimestamp(), tick = State.Tick;
        var position = State.Position;
        try
        {
            var result = run();
            Diagnose(kind, new { input, result.Success, result.Message, tickBefore = tick, positionBefore = position,
                position = State.Position, elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds });
            return result;
        }
        catch (Exception e)
        {
            Diagnose("action-exception", new { action = kind, input, type = e.GetType().Name, e.Message });
            throw;
        }
    }

    // This view copies player-owned data only. Inspection does not generate chunks,
    // discover cells, activate populations, consume time, or consume an RNG stream.
    public object DiagnosticSnapshot() => new
    {
        seed = Seed, contentHash = Content.Hash, generationVersion = WorldPlan.GenerationVersion,
        tick = State.Tick, position = State.Position, Paused, Frozen, Focused, ModalOpen,
        awaitingContinuation = State.AwaitingContinuation, victory = State.Endgame.Victory,
        party = Party.Select(a => new { a.Id, a.Name, a.Archetype, classId = Class(a).Id, a.Level, a.Experience,
            a.Health, a.MaxHealth, a.Mana, a.MaxMana, life = a.Life.ToString(), a.Position,
            a.Body.Hunger, a.Body.Fatigue, a.Body.Wounds, a.Stress, a.CombatTrait,
            talents = a.Talents.ToArray(), equipment = a.Equipment.ToDictionary(p => p.Key, p => p.Value) }).ToArray(),
        inventory = new Dictionary<string, int>(State.Inventory),
        work = State.Work.Select(o => new { o.Id, kind = o.Kind.ToString(), o.Target, o.Worker, o.Progress,
            o.Status, o.Priority, o.Suspended, supplies = o.Supplies?.Select(s => new { s.Stack, s.Count, phase = s.Phase.ToString(), s.Carrier, s.Storage }).ToArray(),
            o.DeliveryStorage, output = o.Output is { } output ? new { output.Carrier, output.ProducedTick, remaining = new Dictionary<string, int>(output.Remaining), delivered = new Dictionary<string, int>(output.Delivered) } : null }).ToArray(),
        knownDiscoveries = State.Discoveries.Count(d => d.Known), visitedDiscoveries = State.Discoveries.Count(d => d.Visited),
        visitedSites = State.VisitedSites.Count, residentChunks = Chunks.ResidentCount,
        modifiedChunks = State.Chunks.Count, cachedFootprints = Chunks.Generator.CachedFootprints,
        bodySimulation = BodySimulation,
        residentJourneys = ResidentJourneys,
        residentForaging = ResidentForaging,
        chunkPreparation = Chunks.PreparationStatus,
        journal = State.Journal.TakeLast(12).ToArray(),
        battle = State.Battle is not { } b ? null : new { b.Round, active = b.Active.Id, b.Active.Friendly,
            units = b.Units.Select(u => new { u.Id, actorId = b.ActorIds[u.Id], u.Name, u.Friendly, u.Health,
                u.MaxHealth, u.Mana, u.Burn, u.Bleed, u.Mark, u.Stun, u.Guarding }).ToArray() },
        reward = State.LastEncounterReward
    };
}

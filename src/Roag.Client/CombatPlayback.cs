using System.Text.Json;
using Roag.Core.Expeditions;
using Roag.Core.World;
using CombatEvent = Roag.Core.World.CombatEvent;

namespace Roag.Client;

/// <summary>A disposable view of an already resolved action. Playback never calls game rules.</summary>
internal sealed class CombatPlayback
{
    internal sealed record Beat(string AbilityId, string Label, string Effect, long Source, long[] Targets, long Helper, CombatEvent[] Events, CombatVisualProfile Profile);
    private readonly Queue<Beat> _beats = [];
    private readonly Queue<CombatEvent> _impacts = [];
    private WorldBattle? _after;
    private double _elapsed;
    private bool _impacted;
    public WorldBattle? View { get; private set; }
    public Beat? Current { get; private set; }
    public IReadOnlyList<Skill> Skills { get; private set; } = [];
    public bool Busy => Current != null;
    public bool EndingPending { get; private set; }
    public long Sequence { get; private set; }
    public bool Completed { get; private set; }
    public bool RewardPending { get; private set; }
    public double Progress => Current == null ? 0 : Math.Clamp(_elapsed / Duration, 0, 1);
    private double Duration => Current!.Profile.Duration + (EndingPending && _beats.Count == 0 ? .45 : 0);
    public string Phase => Current == null ? "idle" : _elapsed < Current.Profile.Anticipation ? "anticipation"
        : _elapsed < Current.Profile.Anticipation + Current.Profile.Travel ? "travel"
        : _elapsed < Current.Profile.Anticipation + Current.Profile.Travel + Current.Profile.Impact ? "impact" : "recovery";
    public double PhaseProgress
    {
        get
        {
            if (Current == null) return 0;
            var p = Current.Profile;
            return Math.Clamp(Phase switch { "anticipation" => _elapsed / p.Anticipation,
                "travel" => (_elapsed - p.Anticipation) / p.Travel,
                "impact" => (_elapsed - p.Anticipation - p.Travel) / p.Impact,
                _ => (_elapsed - p.Anticipation - p.Travel - p.Impact) / (Duration - p.Anticipation - p.Travel - p.Impact) }, 0, 1);
        }
    }
    internal static WorldBattle Copy(WorldBattle battle) => JsonSerializer.Deserialize<WorldBattle>(JsonSerializer.Serialize(battle))!;
    public ActionResult Resolve(WorldSession session, Skill skill, Func<ActionResult> action, int target = -1, int helper = -1)
    {
        if (Busy || session.State.Battle is not { } battle) return new(false, "Wait for the current action to finish.");
        var before = Copy(battle); var skills = session.ActiveSkills().ToArray(); long firstEvent = session.State.NextCombatEvent;
        try { return action(); }
        finally
        {
            var events = session.State.CombatEvents.Where(e => e.Sequence >= firstEvent).ToArray();
            // Also retain a terminal action when its mandatory death-save throws after resolving.
            if (events.Length > 0) Start(before, Copy(battle), events, skill, skills, target, helper, session.State.Battle == null);
        }
    }
    private void Start(WorldBattle before, WorldBattle after, CombatEvent[] events, Skill skill, Skill[] skills, int target, int helper, bool ended)
    {
        Reset(); Skills = skills; View = before; _after = after; EndingPending = ended; RewardPending = events.Any(e => e.Kind == "reward"); Sequence++;
        var primary = new List<CombatEvent>(); var subsequent = new List<List<CombatEvent>>();
        foreach (var e in events)
        {
            // Begin-turn damage is a separate beat, after the ability, even when both resolve in one call.
            if (e.Kind == "damage" && e.Source == 0) subsequent.Add([]);
            if (subsequent.Count > 0) subsequent[^1].Add(e); else primary.Add(e);
        }
        long source = before.ActorIds[before.Active.Id];
        var targets = skill.Target switch
        {
            TargetKind.AllEnemies => before.Units.Where(u => u.Alive && u.Friendly != before.Active.Friendly).Select(u => before.ActorIds[u.Id]).ToArray(),
            TargetKind.AllAllies => before.Units.Where(u => u.Alive && u.Friendly == before.Active.Friendly).Select(u => before.ActorIds[u.Id]).ToArray(),
            TargetKind.Self => [source],
            _ => target >= 0 && target < before.ActorIds.Count ? [before.ActorIds[target]] : [source]
        };
        targets = targets.Where(id => before.Units[before.ActorIds.IndexOf(id)].Alive).ToArray();
        string label = targets.Length == 0 ? skill.Name + " / no living target" : skill.Name;
        string effect = skill.Effect;
        var profile = CombatVisualProfiles.Default.Resolve(skill.Id, skill.Effect, skill.Cooperative);
        if (primary.FirstOrDefault(e => e.Source == source && e.Kind == "prepare") is { } preparation)
        {
            label = preparation.Label; effect = "guard"; targets = [source];
            profile = CombatVisualProfiles.Default.Resolve(null, "guard");
        }
        else if (!primary.Any(e => e.Source == source && e.Kind == "ability") && primary.FirstOrDefault(e => e.Source == source && e.Kind == "skip") is { } skipped)
        {
            label = skipped.Label; effect = "guard"; targets = [];
            profile = CombatVisualProfiles.Default.Resolve(null, "guard");
        }
        _beats.Enqueue(new(skill.Id, label, effect, source, targets,
            helper >= 0 ? before.ActorIds[helper] : 0, primary.ToArray(), profile));
        foreach (var group in subsequent)
        {
            var hit = group[0];
            _beats.Enqueue(new("", hit.Label, "status", hit.Target, [hit.Target], 0, group.ToArray(), CombatVisualProfiles.Default.Resolve(null, "dread")));
        }
        Next();
    }
    public void Advance(double seconds, double speed = 1, bool paused = false)
    {
        if (!Busy || paused || !double.IsFinite(seconds) || seconds <= 0) return;
        double remaining = Math.Min(seconds, .2) * Math.Clamp(double.IsFinite(speed) ? speed : 1, .25, 4);
        while (Busy && remaining > 0)
        {
            double used = Math.Min(remaining, Math.Max(0, Duration - _elapsed)); _elapsed += used; remaining -= used;
            if (!_impacted && _elapsed >= Current!.Profile.Anticipation + Current.Profile.Travel) Impact();
            if (_elapsed >= Duration) Next();
        }
    }
    private void Impact()
    {
        _impacted = true;
        foreach (var e in Current!.Events)
        {
            _impacts.Enqueue(e);
            int index = View!.ActorIds.IndexOf(e.Target); if (index < 0) continue;
            var u = View.Units[index];
            if (e.TargetState is { } state)
            {
                u.Health = state.Health; u.Mana = state.Mana; u.Stress = state.Stress;
                u.Guarding = state.Guarding; u.Burn = state.Burn; u.Bleed = state.Bleed; u.Mark = state.Mark; u.Stun = state.Stun;
            }
            else
            {
                if (e.Kind == "damage") u.Health = Math.Max(0, u.Health - e.Amount);
                if (e.Kind == "heal") u.Health = Math.Min(u.MaxHealth, u.Health + e.Amount);
            }
            if (e.Kind == "fall") { u.Health = 0; u.Burn = u.Bleed = u.Mark = u.Stun = 0; }
        }
        // Costs are paid by the single resolution. Conditions remain event ordered until recovery.
        foreach (var unit in View!.Units)
        {
            unit.Mana = _after!.Units[unit.Id].Mana;
        }
    }
    private void Next()
    {
        _elapsed = 0; _impacted = false;
        if (_beats.TryDequeue(out var beat)) { Current = beat; return; }
        Current = null; View = null; Completed = true;
    }
    public CombatEvent[] DrainImpacts() { var result = _impacts.ToArray(); _impacts.Clear(); return result; }
    public void Skip()
    {
        if (!Busy) return;
        _beats.Clear(); _impacts.Clear(); Current = null; View = null; Completed = true;
    }
    public void AcknowledgeCompletion() { Completed = false; RewardPending = EndingPending = false; }
    public void Reset()
    {
        _beats.Clear(); _impacts.Clear(); View = _after = null; Current = null; Skills = [];
        _elapsed = 0; _impacted = Completed = RewardPending = EndingPending = false;
    }
}

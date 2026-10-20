using System.Text.Json;

namespace Ithica.Core.Colony;

public sealed class IncidentChoice
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Description { get; set; } = "";
    public Stock Cost { get; set; }
}
public sealed class IncidentDefinition
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Family { get; set; } = "";
    public string Description { get; set; } = "";
    public int Weight { get; set; }
    public int Severity { get; set; }
    public int MinTick { get; set; }
    public int Warning { get; set; }
    public int Cooldown { get; set; }
    public string Default { get; set; } = "";
    public List<IncidentChoice> Choices { get; set; } = [];
}
public sealed class Incident
{
    public int Id { get; set; }
    public string DefinitionId { get; set; } = "";
    public long OpenedAt { get; set; }
    public long DueAt { get; set; }
    public int? Cell { get; set; }
    public int? MachineId { get; set; }
    public int? ResidentId { get; set; }
    public int? OtherId { get; set; }
}
public sealed class StoryFollowup
{
    public string Kind { get; set; } = "";
    public int IncidentId { get; set; }
    public long DueAt { get; set; }
    public int Cell { get; set; }
    public int? MachineId { get; set; }
}
public sealed record IncidentRecord(int Id, string DefinitionId, long Tick, string Outcome);
public sealed class StoryState
{
    public int Version { get; set; } = 1;
    public ulong RandomState { get; set; }
    public long NextBeat { get; set; }
    public long RecoveryUntil { get; set; }
    public int NextId { get; set; } = 1;
    public float Budget { get; set; } = 1;
    public int LastSeverity { get; set; }
    public int HostileStreak { get; set; }
    public List<Incident> Pending { get; set; } = [];
    public List<IncidentRecord> History { get; set; } = [];
    public List<StoryFollowup> Followups { get; set; } = [];
    public Dictionary<string, long> Cooldowns { get; set; } = [];
}
public sealed record PressureForecast(float Strain, float Exposure, bool Recovering, string Reason);

public sealed partial class Simulation
{
    public static IReadOnlyList<IncidentDefinition> IncidentLibrary { get; } = LoadIncidents();
    private static List<IncidentDefinition> LoadIncidents()
    {
        using var stream = typeof(Simulation).Assembly.GetManifestResourceStream("Ithica.Story.json")!;
        var definitions = JsonSerializer.Deserialize<List<IncidentDefinition>>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        if (definitions.Count == 0 || definitions.Select(d => d.Id).Distinct().Count() != definitions.Count
            || definitions.Any(d => d.Weight <= 0 || d.Severity is < 0 or > 2 || d.Warning < 24 || d.Cooldown < d.Warning
                || d.Choices.Count < 2 || d.Choices.Select(c => c.Id).Distinct().Count() != d.Choices.Count
                || d.Choices.Any(c => !c.Cost.Valid) || !d.Choices.Any(c => c.Id == d.Default && c.Cost == default)))
            throw new InvalidDataException("Invalid authored incident library.");
        return definitions;
    }
    public StoryState? Story { get; private set; }
    public bool StoryEnabled => Story is not null;
    public void EnableStory()
    {
        if (Story is null)
        {
            var rng = new RandomStream(unchecked((ulong)Seed) ^ 0x73746f7279UL);
            // PCG's first output reflects the initial high bits. Warm this dedicated
            // stream so nearby campaign seeds do not share their first incident.
            rng.NextUInt(); rng.NextUInt();
            Story = new() { NextBeat = Tick + 120, RandomState = rng.State };
        }
        foreach (var c in Crew) InitializeDisposition(c);
    }
    public PressureForecast Forecast
    {
        get
        {
            var living = Crew.Where(c => c.Alive).ToArray();
            float strain = Math.Clamp((.65f - Pressure) * 1.2f, 0, .7f)
                + (Resources.Water < 20 ? .25f : 0) + (Resources.Food < 15 ? .2f : 0)
                + (living.Length > 0 ? living.Average(c => (100 - c.Health + 100 - c.Morale) / 400) : .5f)
                + Math.Min(.4f, _machines.Count(m => m.Fault != MachineFault.None) * .12f)
                + (Generated < Demand && Charge < 10 ? .2f : 0)
                + Math.Min(.2f, living.Count(c => c.Location == ResidentLocation.Field) * .05f);
            float exposure = Math.Clamp((_machines.Count - 11) * .035f + living.Length * .08f + _jobs.Count * .015f, 0, 1);
            bool recovering = Story is { } s && (Tick < s.RecoveryUntil || strain > .65f);
            string reason = Pressure < .4f ? "Pressure is unsafe; restore the enclosure and life support."
                : Resources.Water < 20 ? "Water reserves are limiting recovery."
                : _machines.Any(m => m.Fault != MachineFault.None) ? "Unrepaired faults are increasing strain."
                : living.Any(c => c.Health < 45 || c.Morale < 35) ? "Crew injuries or stress need attention."
                : "Reserves and crew condition leave room to expand.";
            return new(Math.Clamp(strain, 0, 1), exposure, recovering, reason);
        }
    }

    private Incident? Candidate(IncidentDefinition d)
    {
        var home = Crew.Where(c => c.Alive && c.Location == ResidentLocation.Home && c.Expedition is null).OrderBy(c => c.Id).ToArray();
        var incident = new Incident { DefinitionId = d.Id, OpenedAt = Tick, DueAt = Tick + d.Warning };
        Machine? machine = null;
        switch (d.Id)
        {
            case "pump": machine = _machines.Where(m => m.Kind == ModuleKind.Oxygen && m.Fault == MachineFault.None && JobAt(m.Cell) is null).OrderBy(m => m.Condition).FirstOrDefault(); break;
            case "relay": machine = _machines.Where(m => Serviceable(m) && m.Kind != ModuleKind.Oxygen && m.Powered && m.Fault == MachineFault.None && JobAt(m.Cell) is null).OrderBy(m => m.Condition).ThenBy(m => m.Id).FirstOrDefault(); break;
            case "seed": machine = _machines.FirstOrDefault(m => m.Kind == ModuleKind.Farm && m.Powered && m.Fault == MachineFault.None && World.Breathable(m.Cell)); break;
            case "dust": if (Storm) return null; break;
            case "debris":
                int cx = World.Width / 2, cy = World.Height / 2;
                for (int x = cx - 7; x <= cx + 7; x++)
                {
                    int cell = World.Index(x, cy - 5);
                    if (World.Tiles[cell] == Terrain.Wall && World.Discovered[cell] > 0 && JobAt(cell) is null) { incident.Cell = cell; break; }
                }
                if (incident.Cell is null) return null;
                break;
            case "fatigue":
            case "workload":
                var tired = home.OrderByDescending(c => c.WorkTicks).ThenBy(c => c.Energy).FirstOrDefault(c => c.BreakUntil <= Tick
                    && (c.Energy < 40 || c.Memories.Any(m => m.Key == "overworked")));
                if (tired is null) return null;
                incident.ResidentId = tired.Id;
                if (d.Id == "workload")
                {
                    var other = home.Where(c => c.Id != tired.Id && tired.WorkTicks - c.WorkTicks >= 40).OrderBy(c => c.WorkTicks).FirstOrDefault();
                    if (other is null) return null;
                    incident.OtherId = other.Id;
                }
                break;
            case "meal":
            case "solidarity":
                var first = home.FirstOrDefault(c => d.Id == "meal" || c.Memories.Any(m => m.Key == "repair"));
                var second = home.FirstOrDefault(c => first is not null && c.Id != first.Id);
                if (first is null || second is null) return null;
                incident.ResidentId = first.Id; incident.OtherId = second.Id;
                break;
            case "cache":
            case "signal":
                int center = World.Center;
                int target = Enumerable.Range(0, World.Count).Where(i => World.Discovered[i] > 0 && World.Tiles[i] == Terrain.Vacuum
                    && World.Distance(center, i) is >= 8 and <= 22 && JobAt(i) is null && SurvivorAt(i) is null)
                    .OrderBy(i => World.Distance(center, i)).ThenBy(i => i).FirstOrDefault(-1);
                if (target < 0 || home.Length == 0) return null;
                incident.Cell = target;
                break;
        }
        if (d.Id is "pump" or "relay" or "seed")
        {
            if (machine is null) return null;
            incident.Cell = machine.Cell; incident.MachineId = machine.Id;
        }
        return incident;
    }

    private bool SubjectsPresent(Incident i) =>
        (i.MachineId is not int machine || i.Cell is int cell && MachineAt(cell)?.Id == machine)
        && (i.ResidentId is not int resident || Crew.Any(c => c.Id == resident && c.Alive))
        && (i.OtherId is not int other || Crew.Any(c => c.Id == other && c.Alive));

    private void Record(Incident i, string outcome)
    {
        var story = Story!;
        story.History.Add(new(i.Id, i.DefinitionId, Tick, outcome));
        if (story.History.Count > 64) story.History.RemoveAt(0);
        story.Pending.Remove(i);
        Event(IncidentLibrary.Single(d => d.Id == i.DefinitionId).Title + ": " + outcome);
    }

    public Result IncidentChoiceAvailable(int id, string choiceId)
    {
        var i = Story?.Pending.SingleOrDefault(i => i.Id == id);
        var d = IncidentLibrary.SingleOrDefault(d => d.Id == i?.DefinitionId);
        var choice = d?.Choices.SingleOrDefault(c => c.Id == choiceId);
        if (i is null || choice is null || GameOver) return new(false, "This decision is no longer available.");
        if (!SubjectsPresent(i)) return new(false, "The people or module are no longer available.");
        if (!Resources.Covers(choice.Cost)) return new(false, "The stockpile cannot cover this response.");
        if (choiceId is "mediate" or "rest" or "share" or "recognize"
            && new[] { i.ResidentId, i.OtherId }.Where(x => x is not null).Any(x => Crew.Single(c => c.Id == x).Location != ResidentLocation.Home))
            return new(false, "Bring the people involved home first.");
        if (choiceId == "service")
        {
            var m = MachineAt(i.Cell!.Value)!;
            if (JobAt(m.Cell) is not null || !Resources.Covers(MaintenancePrice(m))) return new(false, "Clear the existing order and reserve the service parts first.");
        }
        if (choiceId == "survey" && (JobAt(i.Cell!.Value) is not null || !World.Walkable(i.Cell.Value))) return new(false, "The survey location is blocked or already has an order.");
        return new(true, "");
    }

    public Result RespondToIncident(int id, string choiceId)
    {
        var available = IncidentChoiceAvailable(id, choiceId);
        if (!available.Success) return available;
        var i = Story!.Pending.Single(i => i.Id == id);
        var d = IncidentLibrary.Single(d => d.Id == i.DefinitionId);
        var choice = d.Choices.Single(c => c.Id == choiceId);
        ApplyIncident(i, choiceId, false);
        Resources -= choice.Cost;
        return new(true, d.Title + ": " + choice.Label);
    }

    private void ApplyIncident(Incident i, string response, bool expired)
    {
        var story = Story!;
        Machine? m = i.Cell is int cell ? MachineAt(cell) : null;
        Colonist? a = i.ResidentId is int id ? Crew.Single(c => c.Id == id) : null;
        Colonist? b = i.OtherId is int other ? Crew.Single(c => c.Id == other) : null;
        bool harmful = false;
        switch (i.DefinitionId, response)
        {
            case ("pump", "service"):
                OrderMaintenance(i.Cell!.Value);
                story.Followups.Add(new() { Kind = "pump", IncidentId = i.Id, Cell = m!.Cell, MachineId = m.Id, DueAt = i.DueAt });
                break;
            case ("pump", "defer"):
                m!.Fault = MachineFault.Leak; m.Condition = Math.Min(m.Condition, 45); harmful = true;
                Event("Life support seal failed. Water and local air are leaking; queue major service.", NoticeLevel.Danger);
                break;
            case ("relay", "isolate"): m!.Fault = MachineFault.Offline; harmful = true; break;
            case ("relay", "run"): m!.Fault = MachineFault.Overheating; m.Condition = Math.Min(m.Condition, 65); harmful = true; break;
            case ("dust", _): StormUntil = Math.Max(StormUntil, Tick + (response == "filter" ? 36 : 120)); harmful = response != "filter"; break;
            case ("debris", "impact"):
                if (World.Tiles[i.Cell!.Value] == Terrain.Wall) { Meteor(i.Cell); harmful = true; }
                break;
            case ("workload", "mediate"):
                GrantBreak(a!, 72); ChangeTrust(a!, b!, 8); ChangeTrust(b!, a!, 8);
                Remember(b!, "fairness", $"We settled the shift dispute with {a!.Name}.", 5, other: a.Id); break;
            case ("fatigue", "rest"): GrantBreak(a!, 72); break;
            case ("workload", "dismiss"):
            case ("fatigue", "dismiss"):
                Remember(a!, "dismissed", "My exhaustion was dismissed.", a!.Disposition!.Dislikes is CrewAversion.Overwork or CrewAversion.Conflict ? -14 : -9);
                if (b is not null) { ChangeTrust(a, b, -12); ChangeTrust(b, a, -5); Remember(a, "resentment", $"{b.Name} left the hard shifts to me.", -7, other: b.Id); }
                harmful = true; break;
            case ("meal", "share"):
                GrantBreak(a!, 24); GrantBreak(b!, 24); ChangeTrust(a!, b!, 10); ChangeTrust(b!, a!, 10);
                Remember(a!, "shared-meal", $"A meal shared with {b!.Name}.", a!.Disposition?.Likes == CrewValue.Fellowship ? 10 : 7, other: b.Id);
                Remember(b, "shared-meal", $"A meal shared with {a!.Name}.", b.Disposition?.Likes == CrewValue.Fellowship ? 10 : 7, other: a.Id); break;
            case ("hauler", "trade"): Resources += new Stock(0, 24, 0, 0); break;
            case ("seed", "cultivate"): story.Followups.Add(new() { Kind = "harvest", IncidentId = i.Id, Cell = m!.Cell, MachineId = m.Id, DueAt = Tick + 72 }); break;
            case ("cache", "survey"):
            case ("signal", "survey"):
                Designate(World.X(i.Cell!.Value), World.Y(i.Cell.Value), Tool.Survey);
                if (i.DefinitionId == "cache") story.Followups.Add(new() { Kind = "cache", IncidentId = i.Id, Cell = i.Cell.Value, DueAt = Tick + 480 });
                break;
            case ("solidarity", "recognize"):
                ChangeTrust(a!, b!, 8); ChangeTrust(b!, a!, 8);
                Remember(a!, "solidarity", $"{b!.Name} recognized my repairs.", 8, other: b.Id);
                Remember(b, "solidarity", $"We kept the shelter alive together with {a!.Name}.", 6, other: a.Id); break;
        }
        if (harmful) story.RecoveryUntil = Math.Max(story.RecoveryUntil, Tick + 240);
        UpdatePower();
        Record(i, (expired ? "No response; " : "") + IncidentLibrary.Single(d => d.Id == i.DefinitionId).Choices.Single(c => c.Id == response).Label + (response == "service" ? " / crew must finish before the deadline" : ""));
    }

    private void FollowupOutcome(StoryFollowup f, string text, NoticeLevel level)
    {
        var history = Story!.History;
        int index = history.FindIndex(h => h.Id == f.IncidentId);
        if (index >= 0) history[index] = history[index] with { Outcome = history[index].Outcome + " / " + text };
        Event(text, level);
    }

    private void StoryTick()
    {
        if (Story is not { } s) return;
        MaintenanceTick();
        CommunityTick();
        foreach (var f in s.Followups.Where(f => f.DueAt <= Tick).ToArray())
        {
            var m = MachineAt(f.Cell);
            bool same = m is not null && m.Id == f.MachineId;
            if (f.Kind == "pump" && same && (m!.Condition < 98 || m.Fault != MachineFault.None))
            {
                m.Fault = MachineFault.Leak; m.Condition = Math.Min(m.Condition, 45);
                s.RecoveryUntil = Math.Max(s.RecoveryUntil, Tick + 240);
                FollowupOutcome(f, "Preventive service missed its deadline. The pump seal is leaking; repair the fault.", NoticeLevel.Danger);
            }
            else if (f.Kind == "pump") FollowupOutcome(f, same ? "Preventive pump service held. The warning ended without a leak." : "The warned pump was removed before the deadline.", NoticeLevel.Good);
            else if (f.Kind == "harvest")
            {
                if (same && m!.Powered && World.Breathable(f.Cell) && World.Heat[f.Cell] is >= 0 and < 46)
                { Resources += new Stock(0, 0, 8, 0); FollowupOutcome(f, "The seed culture took: +8 food.", NoticeLevel.Good); }
                else FollowupOutcome(f, "The seed culture failed without a functioning, habitable farm.", NoticeLevel.Warning);
            }
            else if (f.Kind == "cache") FollowupOutcome(f, "The locker recovery window passed before the survey finished.", NoticeLevel.Warning);
            s.Followups.Remove(f);
        }
        foreach (var i in s.Pending.ToArray())
        {
            if (!SubjectsPresent(i)) { Record(i, "The target was lost; decision closed."); continue; }
            if (i.DefinitionId is "pump" or "relay" && MachineAt(i.Cell!.Value) is { Condition: >= 98, Fault: MachineFault.None })
            { Record(i, "Preventive service completed before the fault developed."); continue; }
            if (i.DueAt <= Tick)
                ApplyIncident(i, IncidentLibrary.Single(d => d.Id == i.DefinitionId).Default, true);
        }
        if (!IncidentsEnabled || Tick < s.NextBeat || GameOver) return;
        var rng = new RandomStream(0) { State = s.RandomState };
        var forecast = Forecast;
        s.Budget = Math.Min(3, s.Budget + .6f + forecast.Exposure * .4f);
        var options = new List<(IncidentDefinition Definition, Incident Candidate, int Weight)>();
        foreach (var d in IncidentLibrary)
        {
            if (Tick < d.MinTick || s.Cooldowns.GetValueOrDefault(d.Id) > Tick || s.Pending.Any(i => IncidentLibrary.Single(x => x.Id == i.DefinitionId).Family == d.Family)
                || s.Followups.Any(f => f.Kind == "pump" && d.Id == "pump" || f.Kind == "cache" && d.Id == "cache" || f.Kind == "harvest" && d.Id == "seed")
                || d.Severity > s.Budget || d.Severity > 0 && (forecast.Recovering || s.HostileStreak >= 2) || d.Severity == 2 && forecast.Exposure < .2f) continue;
            var candidate = Candidate(d);
            if (candidate is null) continue;
            int weight = d.Weight + (d.Severity == 0 && forecast.Recovering ? 8 : 0);
            if (d.Id == "workload" && candidate.ResidentId is int resident)
                weight += Crew.Single(c => c.Id == resident).Bonds.Any(b => b.OtherId == candidate.OtherId && b.Trust < 0) ? 6 : 0;
            options.Add((d, candidate, weight));
        }
        if (s.Pending.Count < 3 && options.Count > 0)
        {
            int roll = rng.Next(0, options.Sum(x => x.Weight));
            var selected = options[0];
            foreach (var option in options) { selected = option; roll -= option.Weight; if (roll < 0) break; }
            var d = selected.Definition; var i = selected.Candidate;
            i.Id = s.NextId++;
            if (d.Id is "pump" or "relay") MachineAt(i.Cell!.Value)!.Condition = Math.Min(90, MachineAt(i.Cell.Value)!.Condition);
            s.Pending.Add(i); s.Cooldowns[d.Id] = Tick + d.Cooldown; s.Budget -= d.Severity;
            s.LastSeverity = d.Severity; s.HostileStreak = d.Severity > 0 ? s.HostileStreak + 1 : 0;
            Event(d.Title + $". Response window: {d.Warning} ticks. Reports [I].", d.Severity > 0 ? NoticeLevel.Warning : NoticeLevel.Good);
        }
        s.NextBeat = Tick + rng.Next(144, 241) + (forecast.Recovering ? 96 : 0);
        s.RandomState = rng.State;
    }

    private void SurveyStoryReward(int cell)
    {
        if (Story is not { } s) return;
        var reward = s.Followups.FirstOrDefault(f => f.Kind == "cache" && f.Cell == cell);
        if (reward is null) return;
        s.Followups.Remove(reward);
        Resources += new Stock(8, 6, 0, 0);
        FollowupOutcome(reward, "Survey crew recovered the old locker: +8 metal, +6 water.", NoticeLevel.Good);
    }
}

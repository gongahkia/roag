using Microsoft.Xna.Framework;
using Ithica.Core;
using Ithica.Core.Colony;

namespace Ithica.Client;

public sealed partial class IthicaGame
{
    private int _incidentIndex;
    private void DrawStationModal(int width, int height)
    {
        Fill(new(0, 0, width, height), Color.Black * .8f);
        int w = Math.Min(900, width - 60), h = Math.Min(660, height - 50);
        var r = new Rectangle((width - w) / 2, (height - h) / 2, w, h);
        Fill(r, Surface); Outline(r, Accent * .4f);
        float x = r.X + 26, y = r.Y + 22, inner = r.Width - 52;
        if (_modal == "resident") DrawResidentFile(r, x, y, inner);
        else if (_modal == "work") DrawWorkRoster(r, x, y, inner);
        else if (_modal == "industry") DrawIndustry(r, x, y, inner);
        else if (_modal == "deposits") DrawDeposits(r, x, y, inner);
        else DrawIncidents(r, x, y, inner);
        if (_elapsed < _messageUntil) Text(Short(_message, (int)(inner / 7)), x, r.Bottom - 77, LevelColor(_messageLevel), .75f);
        if (Button(new(r.Right - 166, r.Bottom - 52, 140, 38), "CLOSE / ESC", modal: true)) _modal = "";
    }

    private void DrawIncidents(Rectangle r, float x, float y, float width)
    {
        var story = _sim.Story!;
        Text("STATION REPORTS", x, y, Ink, 1.5f); y += 34;
        y += Wrap(StationReports.Conditions(_sim), x, y, width, Accent, .85f) + 14;
        Text("PAUSED / MESSAGES, WARNINGS AND WORK IN PROGRESS", x, y, Muted, .75f); y += 28;
        if (story.Pending.Count == 0)
        {
            Text("NO MESSAGES AWAITING A REPLY", x, y, Accent); y += 30;
            foreach (var f in story.Followups)
            {
                y += Wrap($"{StationReports.Followup(f.Kind).ToUpperInvariant()} / {Math.Max(0, f.DueAt - _sim.Tick)} ticks / sector {_sim.World.X(f.Cell)},{_sim.World.Y(f.Cell)}", x, y, width, Amber, .9f) + 10;
            }
            Text("RECENT RECORDS", x, y + 12, Muted, .8f); y += 40;
            foreach (var entry in story.History.TakeLast(5).Reverse())
            {
                if (y > r.Bottom - 110) break;
                var d = Simulation.IncidentLibrary.Single(d => d.Id == entry.DefinitionId);
                y += Wrap($"{entry.Tick:00000} / {d.Title}: {StationReports.Outcome(entry)}", x, y, width, Ink, .9f) + 14;
            }
            return;
        }
        _incidentIndex = Math.Clamp(_incidentIndex, 0, story.Pending.Count - 1);
        var i = story.Pending[_incidentIndex];
        var definition = Simulation.IncidentLibrary.Single(d => d.Id == i.DefinitionId);
        Text($"REPORT {_incidentIndex + 1}/{story.Pending.Count} / RESPONSE WINDOW {i.DueAt - _sim.Tick} TICKS", x, y, Amber, .85f); y += 28;
        y += Wrap(definition.Title.ToUpperInvariant(), x, y, width, Ink, 1.3f) + 12;
        string targets = i.ResidentId is int id ? _sim.Crew.Single(c => c.Id == id).Name : "";
        if (i.OtherId is int other) targets += " / " + _sim.Crew.Single(c => c.Id == other).Name;
        if (i.Cell is int cell) targets += $"SECTOR {_sim.World.X(cell)},{_sim.World.Y(cell)}";
        if (targets.Length > 0) { Text(targets, x, y, Accent, .85f); y += 23; }
        y += Wrap(definition.Description, x, y, width, Ink, .95f) + 12;
        foreach (var residentId in new[] { i.ResidentId, i.OtherId }.Where(id => id is not null))
        {
            var resident = _sim.Crew.Single(c => c.Id == residentId);
            Text($"{resident.Name.ToUpperInvariant()} / ENERGY {resident.Energy:0}% / WORK {resident.WorkTicks} TICKS", x, y, Muted, .8f); y += 22;
        }
        if (_campaign.Phase != CampaignPhase.Colony) { Text("RETURN TO THE SHELTER TO ANSWER.", x, y, Amber, .8f); y += 25; }
        foreach (var choice in definition.Choices)
        {
            var available = _sim.IncidentChoiceAvailable(i.Id, choice.Id);
            bool enabled = available.Success && _campaign.Phase == CampaignPhase.Colony;
            var box = new Rectangle((int)x, (int)y, (int)width, 84);
            if (Button(box, "", enabled: enabled, modal: true, tooltip: available.Success ? null : available.Message))
            {
                int incidentId = i.Id; string response = choice.Id;
                QueueUI(() => Message(_campaign.RespondToIncident(incidentId, response)));
            }
            Text(choice.Label.ToUpperInvariant(), x + 12, y + 10, enabled ? Accent : Muted, .95f);
            Wrap(choice.Description + (!available.Success ? " " + available.Message : ""), x + 12, y + 33, width - 24, Muted, .85f);
            y += 94;
        }
        Text("IF UNANSWERED: " + definition.Choices.Single(c => c.Id == definition.Default).Label.ToUpperInvariant(), x, y + 2, Amber, .75f);
        if (Button(new(r.X + 26, r.Bottom - 52, 122, 38), "PREVIOUS", enabled: _incidentIndex > 0, modal: true)) _incidentIndex--;
        if (Button(new(r.X + 158, r.Bottom - 52, 122, 38), "NEXT", enabled: _incidentIndex + 1 < story.Pending.Count, modal: true)) _incidentIndex++;
        var targetResident = _sim.Crew.FirstOrDefault(c => c.Id == i.ResidentId && c.Location == ResidentLocation.Home);
        int? targetCell = i.Cell ?? targetResident?.Cell;
        if (Button(new(r.X + 290, r.Bottom - 52, 122, 38), "LOCATE", enabled: _campaign.Phase == CampaignPhase.Colony && targetCell is not null, modal: true))
        {
            _cursor = targetCell!.Value; _camera.Center = CellCenter(_cursor); _selectedCrew = targetResident?.Id; _selectedSurvivor = null; _panel = "colony"; _modal = "";
        }
    }

    private void DrawResidentFile(Rectangle r, float x, float y, float width)
    {
        var c = _sim.Crew.FirstOrDefault(c => c.Id == _selectedCrew);
        if (c is null) return;
        Text(c.Name.ToUpperInvariant() + " / PERSONAL FILE", x, y, Ink, 1.5f); y += 40;
        if (Button(new(r.Right - 245, r.Y + 18, 215, 40), "WORK ASSIGNMENTS", modal: true)) _modal = "work";
        Text(c.Role + " / " + c.Trait + " / " + c.Location, x, y, Muted, .9f); y += 28;
        Text($"LIKES {c.Disposition?.Likes.ToString().ToUpperInvariant()} / DISLIKES {c.Disposition?.Dislikes.ToString().ToUpperInvariant()}", x, y, Accent, .9f); y += 28;
        Text($"MORALE {c.Morale:0}% / WORK {c.WorkTicks} TICKS / BREAK {Math.Max(0, c.BreakUntil - _sim.Tick)} TICKS", x, y, Amber, .85f); y += 34;
        Text("RELATIONSHIPS", x, y, Muted, .8f); y += 24;
        foreach (var bond in c.Bonds.OrderByDescending(b => Math.Abs(b.Trust)).Take(3))
        {
            var other = _sim.Crew.Single(o => o.Id == bond.OtherId);
            Text($"{other.Name} / TRUST {bond.Trust:+0;-0;0}" + (!other.Alive ? " / LOST" : ""), x, y, bond.Trust < 0 ? Danger : Accent, .9f); y += 23;
        }
        if (c.Bonds.Count == 0) { Text("No established bonds yet.", x, y, Muted, .9f); y += 23; }
        Text("MEMORIES", x, y + 8, Muted, .8f); y += 34;
        foreach (var memory in c.Memories.TakeLast(6).Reverse())
        {
            if (y > r.Bottom - 100) break;
            y += Wrap($"{memory.Mood:+0;-0;0} / {memory.Text} ({Math.Max(0, memory.Until - _sim.Tick)} ticks)", x, y, width, memory.Mood < 0 ? Danger : Ink, .95f) + 12;
        }
        if (c.Memories.Count == 0) Wrap("Memories form from danger, hunger, work, rest, repairs and time spent with other residents.", x, y, width, Muted, .95f);
    }

    private void DrawWorkRoster(Rectangle r, float x, float y, float width)
    {
        var c = _sim.Crew.FirstOrDefault(c => c.Id == _selectedCrew);
        if (c is null) return;
        bool home = _campaign.Phase == CampaignPhase.Colony && c.Alive;
        Text(c.Name.ToUpperInvariant() + " / WORK", x, y, Ink, 1.5f); y += 34;
        Text($"PAUSED / ENERGY {c.Energy:0}% / WORK {c.WorkTicks} / BREAK {Math.Max(0, c.BreakUntil - _sim.Tick)} TICKS", x, y, Amber, .85f); y += 28;
        float shiftWidth = (width - 32) / 5;
        foreach (var shift in Enum.GetValues<CrewShift>())
        {
            var chosen = shift;
            if (Button(new((int)(x + (int)shift * (shiftWidth + 8)), (int)y, (int)shiftWidth, 40), shift.ToString().ToUpperInvariant(), selected: c.Work.Shift == shift, enabled: home, modal: true))
                QueueUI(() => Message(_campaign.SetShift(c.Id, chosen)));
        }
        y += 48;
        Text("DAY 000–239 / NIGHT 240–479 / EXTENDED 000–399 / FLEXIBLE: AS FIT", x, y, Muted, .78f); y += 28;
        if (Button(new((int)x, (int)y, 230, 40), "PROTECT 120 TICKS OF REST", enabled: home && !c.Incapacitated && c.CarryingResidentId is null, modal: true))
            QueueUI(() => Message(_campaign.AssignRest(c.Id)));
        y += 56;
        Text("PRIORITIES / 1 FIRST, 2 NORMAL, 3 LAST, 0 DISABLED", x, y, Accent, .85f); y += 28;
        foreach (var group in Enum.GetValues<WorkGroup>())
        {
            Text(group.ToString().ToUpperInvariant(), x, y + 12, Ink, .9f);
            for (int rank = 0; rank < 4; rank++)
            {
                var selectedGroup = group; int selectedRank = rank;
                if (Button(new((int)(x + width - 232 + rank * 58), (int)y, 50, 40), rank.ToString(), selected: c.Work.Priorities[(int)group] == rank, enabled: home, modal: true))
                    QueueUI(() => Message(_campaign.SetWorkPriority(c.Id, selectedGroup, selectedRank)));
            }
            y += 44;
        }
        y += 8;
        if (Button(new((int)x, (int)y, (int)width, 40), c.Work.AllowWornDrilling ? "WORN DRILLING ALLOWED / CHANGE TO CAUTIOUS" : "CAUTIOUS DRILLING / ALLOW WORN EQUIPMENT", selected: c.Work.AllowWornDrilling, enabled: home, modal: true))
            QueueUI(() => Message(_campaign.SetDrillingRisk(c.Id, !c.Work.AllowWornDrilling)));
        y += 48;
        Wrap("Cautious operators stop below 50%. Worn drilling risks injury at 25%, or sooner when tired.", x, y, width, Muted, .8f);
    }

    private void DrawIndustry(Rectangle r, float x, float y, float width)
    {
        var m = _sim.MachineAt(_cursor);
        if (m is null) { Text("SELECT A STATION MODULE", x, y, Ink, 1.4f); return; }
        int cell = m.Cell;
        Text(Blueprint.Get(m.Kind).Name.ToUpperInvariant() + " / OPERATIONS", x, y, Ink, 1.3f); y += 36;
        Text($"PAUSED / DEEP ORE {_sim.Resources.Ore:0} / COMPONENTS {_sim.Resources.Components:0}", x, y, Accent, .9f); y += 30;
        Text($"CONDITION {m.Condition:0}% / {m.Fault.ToString().ToUpperInvariant()} / DRAW {_sim.PowerDraw(m):0} KW", x, y, Amber, .85f); y += 28;
        if (_sim.DepositAt(cell) is { } d) { Text($"DEPTH {d.Depth}/3 / {d.Remaining} ORE LEFT / {d.Extracted} RECOVERED", x, y, Ink, .9f); y += 28; }
        bool industrial = m.Kind is ModuleKind.Drill or ModuleKind.Fabricator;
        if (industrial)
        {
            y += Wrap(_sim.IndustrialBlock(m) ?? (m.Condition < 50 ? "Worn equipment: cautious operators will stop for service." : "Support ready. An on-duty resident with the right work enabled must operate this module."), x, y, width, Amber, .9f) + 12;
        }
        void Action(string label, Func<Result> command, bool enabled = true)
        {
            if (Button(new((int)x, (int)y, (int)width, 40), label, enabled: enabled && _campaign.Phase == CampaignPhase.Colony, modal: true)) QueueUI(() => Message(command()));
            y += 46;
        }
        if (m.Kind == ModuleKind.Drill)
        {
            Action("START EXTRACTION / MINING PRIORITY", () => _campaign.OrderExtraction(cell), _sim.JobAt(cell) is null && _sim.DepositAt(cell)?.Remaining > 0);
            var deposit = _sim.DepositAt(cell)!;
            var cost = Simulation.DeepeningPrice(deposit);
            Action($"LOWER RIG / {Cost(cost)}", () => _campaign.OrderDeepening(cell), _sim.JobAt(cell) is null && deposit.Remaining == 0 && deposit.Depth < 3);
        }
        if (m.Kind == ModuleKind.Fabricator) Action("MAKE COMPONENT / " + Cost(Simulation.ComponentRecipe), () => _campaign.OrderComponent(cell), _sim.JobAt(cell) is null);
        if (industrial) Action(m.Enabled ? "SWITCH INDUSTRIAL POWER OFF" : "SWITCH INDUSTRIAL POWER ON", () => _campaign.SetIndustryEnabled(cell, !m.Enabled));
        Action("QUEUE SERVICE / " + Cost(Simulation.MaintenancePrice(m)), () => _campaign.OrderMaintenance(cell), _sim.JobAt(cell) is null or { Kind: WorkKind.Extract });
        Action(m.Reinforced ? "REINFORCED / HALF EQUIPMENT WEAR" : "REINFORCE / " + Cost(Simulation.ReinforcementPrice), () => _campaign.OrderReinforcement(cell), !m.Reinforced && _sim.JobAt(cell) is null);
        if (_sim.JobAt(cell) is { } job)
        {
            Action("CANCEL " + job.Kind.ToString().ToUpperInvariant() + " / REFUND RESERVED PARTS", () => _sim.Designate(_sim.World.X(cell), _sim.World.Y(cell), Tool.Cancel));
        }
        Wrap(m.Kind == ModuleKind.Oxygen ? "Reinforcement halves wear and reduces water use by 35%."
            : m.Kind == ModuleKind.Battery ? "Reinforcement halves wear and raises storage from 240 to 360."
            : m.Kind == ModuleKind.SuitRack ? "Reinforcement halves wear and equips departing suits to use 50% less oxygen."
            : "Reinforcement halves wear. Keep pressure, refuge and power available; maintenance interrupts extraction and requires real labor.", x, y + 4, width, Muted, .85f);
    }

    private void DrawDeposits(Rectangle r, float x, float y, float width)
    {
        Text("DEEP WORKINGS", x, y, Ink, 1.5f); y += 36;
        Text($"PAUSED / DEEP ORE {_sim.Resources.Ore:0} / COMPONENTS {_sim.Resources.Components:0}", x, y, Accent, .9f); y += 30;
        if (_sim.Extraction is null) { Wrap("Restore the pump at the signal below to resolve deep readings near the station.", x, y, width, Ink); return; }
        y += Wrap("Survey a reading, extend connected deck and an enclosed breathable refuge with a sleeping pod, then build a rig on the deposit. Depth raises power demand and wear. Ore reinforces machines and supplies component fabrication.", x, y, width, Muted, .9f) + 14;
        foreach (var d in _sim.Extraction.Deposits)
        {
            int cell = d.Cell;
            Text($"SECTOR {_sim.World.X(cell)},{_sim.World.Y(cell)} / " + (d.Surveyed ? $"DEPTH {d.Depth} / {d.Remaining} ORE" : "DEEP READING / UNSURVEYED"), x, y, Ink, .9f); y += 23;
            if (Button(new((int)x, (int)y, 138, 40), "LOCATE", modal: true)) { LocateWorking(cell); }
            if (!d.Surveyed && Button(new((int)x + 148, (int)y, 160, 40), "QUEUE SURVEY", enabled: _sim.JobAt(cell) is null, modal: true))
                QueueUI(() => Message(_sim.Designate(_sim.World.X(cell), _sim.World.Y(cell), Tool.Survey)));
            bool built = _sim.MachineAt(cell)?.Kind == ModuleKind.Drill;
            if (d.Surveyed && Button(new((int)x + 148, (int)y, 160, 40), built ? "OPERATIONS" : "BUILD RIG", modal: true))
            {
                LocateWorking(cell);
                if (built) _modal = "industry"; else ChooseBlueprint(ModuleKind.Drill);
            }
            y += 53;
        }
        foreach (var c in _sim.Crew.Where(c => c.Alive && c.Incapacitated).Take(2))
        {
            Text("STRANDED / " + c.Name.ToUpperInvariant(), x, y + 10, Danger, .9f);
            if (Button(new((int)(x + width - 210), (int)y, 210, 40), "QUEUE RESIDENT RESCUE", modal: true)) QueueUI(() => Message(_campaign.OrderResidentRescue(c.Id)));
            y += 46;
        }
        if (Button(new(r.X + 26, r.Bottom - 52, 230, 38), "BUILD COMPONENT FABRICATOR", modal: true)) ChooseBlueprint(ModuleKind.Fabricator);
    }

    private void LocateWorking(int cell)
    {
        _cursor = cell; _camera.Center = CellCenter(cell); _selectedCrew = null; _selectedSurvivor = null; _panel = "colony"; _modal = "";
    }
}

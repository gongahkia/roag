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
        else DrawIncidents(r, x, y, inner);
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
}

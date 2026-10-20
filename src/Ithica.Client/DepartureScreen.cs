using Ithica.Core;
using Ithica.Core.Colony;
using Microsoft.Xna.Framework;

namespace Ithica.Client;

public sealed partial class IthicaGame
{
    private int _departureScroll;
    private Rectangle _departureList;

    private void DrawDeparture(int width, int height)
    {
        Fill(new(0, 0, width, height), Color.Black * .8f);
        var r = new Rectangle((width - Math.Min(940, width - 64)) / 2, 30, Math.Min(940, width - 64), height - 60);
        Fill(r, Surface); Outline(r, Accent * .35f);
        Text("BEFORE YOU LEAVE", r.X + 24, r.Y + 24, Ink, 1.7f);
        Text((_campaign.SelectedSite?.Name ?? "A signal").ToUpperInvariant() + " / TIME IS HELD", r.X + 24, r.Y + 60, Accent, .9f);
        int leftWidth = (r.Width - 72) / 2, rightX = r.X + 48 + leftWidth;
        Text("CHOOSE 1-4 RESIDENTS", r.X + 24, r.Y + 102, Muted, .85f);
        _departureList = new(r.X + 24, r.Y + 130, leftWidth, r.Height - 232);
        var residents = _sim.Crew.Where(c => c.Alive).ToArray();
        int rows = Math.Max(1, _departureList.Height / 70);
        _departureScroll = Math.Clamp(_departureScroll, 0, Math.Max(0, residents.Length - rows));
        for (int n = _departureScroll; n < Math.Min(residents.Length, _departureScroll + rows); n++)
        {
            var c = residents[n]; bool selected = _departureIds.Contains(c.Id);
            bool ready = c.Location == ResidentLocation.Home && !c.Rest && c.Expedition is null && c.Health >= 25 && c.Energy >= 15 && c.Suit >= 25;
            var card = new Rectangle(_departureList.X, _departureList.Y + (n - _departureScroll) * 70, leftWidth, 62);
            if (Button(card, "", selected: selected, enabled: ready && (selected || _departureIds.Count < 4), modal: true))
            { if (!_departureIds.Remove(c.Id)) _departureIds.Add(c.Id); }
            Text((selected ? "+ " : "  ") + c.Name.ToUpperInvariant() + " / " + c.Role, card.X + 10, card.Y + 9, ready ? Ink : Muted, .9f);
            Text($"HP {c.Health:0}  O2 {c.Suit:0}  ENERGY {c.Energy:0}" + (ready ? "" : " / RECOVERING"), card.X + 10, card.Y + 36, ready ? Accent : Amber, .75f);
        }
        Text("WHEEL SCROLLS THE RESIDENT LIST", r.X + 24, r.Bottom - 94, Muted, .75f);
        Text("CARRIED SUPPLIES", rightX, r.Y + 102, Muted, .85f);
        string[] names = ["FOOD", "WATER", "MEDICAL", "FILTERS"];
        int[] amounts = [_departurePack.Food, _departurePack.Water, _departurePack.Medicine, _departurePack.Filters];
        for (int n = 0; n < 4; n++)
        {
            int y = r.Y + 132 + n * 52;
            Text(names[n], rightX, y + 14, Ink, .85f);
            Text(amounts[n].ToString("00"), rightX + leftWidth - 98, y + 14, Accent);
            if (Button(new(rightX + leftWidth - 150, y, 40, 40), "-", enabled: amounts[n] > 0, modal: true)) amounts[n]--;
            if (Button(new(rightX + leftWidth - 48, y, 40, 40), "+", enabled: amounts[n] < (n < 2 ? 100 : 20), modal: true)) amounts[n]++;
        }
        (_departurePack.Food, _departurePack.Water, _departurePack.Medicine, _departurePack.Filters) = (amounts[0], amounts[1], amounts[2], amounts[3]);
        float detailsY = r.Y + 352;
        var remaining = _sim.Resources - _departurePack.Cost;
        int homeWorkers = _sim.Crew.Count(c => c.Alive && c.Location == ResidentLocation.Home && !c.Rest && !_departureIds.Contains(c.Id));
        detailsY += Wrap($"HOME AFTER PACKING\n{remaining.Food:0} food / {remaining.Water:0} water / {remaining.Metal:0} metal\n{homeWorkers} available workers remain. Machines operate even with nobody home.", rightX, detailsY, leftWidth, remaining.Valid ? Ink : Danger, .85f) + 14;
        detailsY += Wrap($"TRAVEL 8 ticks each way. A step costs 1 tick. Rescues share 1 food and 1 water.\nSuit service: {(_campaign.SupportUpgrade ? "35% less field oxygen use" : "standard suits")}", rightX, detailsY, leftWidth, Muted, .8f) + 12;
        Wrap($"HOME O2 {_sim.Pressure * 100:0}% / POWER {_sim.Generated:0}/{_sim.Demand:0} kW\nYour actions advance home conditions and distress deadlines. Thinking costs no time.", rightX, detailsY, leftWidth, _sim.Pressure < .4f || remaining.Water < 15 || remaining.Food < 15 ? Amber : Muted, .8f);
        if (Button(new(r.X + 24, r.Bottom - 64, 180, 44), "BACK [ESC]", modal: true))
        { QueueUI(() => { Message(_campaign.CancelPreparation()); SyncCampaignUI(); }); }
        if (Button(new(r.Right - 228, r.Bottom - 64, 204, 44), "DEPART [ENTER]", selected: true, enabled: _departureIds.Count > 0 && remaining.Valid && _departurePack.Food > 0 && _departurePack.Water > 0, modal: true)) QueueUI(ConfirmDeparture);
        if (_elapsed < _messageUntil) Text(Short(_message, (r.Width - 48) / 8), r.X + 24, r.Bottom - 18, LevelColor(_messageLevel), .75f);
    }

    private void DrawArrival(int width, int height)
    {
        Fill(new(0, 0, width, height), Color.Black * .8f);
        var r = new Rectangle((width - 740) / 2, (height - 480) / 2, 740, 480);
        Fill(r, Surface); Outline(r, Accent * .35f);
        Text("BACK IN THE SHELTER", r.X + 28, r.Y + 28, Ink, 1.7f);
        float y = r.Y + 82;
        y += Wrap(_campaign.ArrivalReport, r.X + 28, y, r.Width - 56, Ink) + 24;
        y += Wrap($"{_sim.LivingCount} living residents / {_sim.RescuedCount} rescued\n{_sim.Resources.Components:0} recovered component(s) in storage", r.X + 28, y, r.Width - 56, Accent) + 22;
        y += Wrap(_campaign.Objective, r.X + 28, y, r.Width - 56, Amber) + 16;
        Wrap("Time is held while you read this. Returning people keep their condition; food, rest and medical care help them recover.", r.X + 28, y, r.Width - 56, Muted, .9f);
        if (Button(new(r.Right - 270, r.Bottom - 72, 242, 44), "RESUME COLONY [ENTER]", selected: true, modal: true))
        { QueueUI(() => { Message(_campaign.DismissArrival()); SyncCampaignUI(); }); }
    }
}

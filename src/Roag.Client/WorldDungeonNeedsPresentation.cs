using Microsoft.Xna.Framework;
using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Client;

internal sealed partial class WorldGame
{
    private void BuildDungeonNeedsControls(Rectangle box)
    {
        var member = _session.DungeonMember;
        int width = (box.Width - 72) / 2, right = box.X + 48 + width;
        var eating = _session.DungeonCombat.Preview(new(TacticalAction.Eat));
        var medicine = _session.DungeonCombat.Preview(new(TacticalAction.UseMedicine));
        AddDungeonPartyControl("dungeon:eat", "Eat / one provision", new(box.X + 24, box.Y + 236, width, 64),
            DungeonSelectionReady && eating.Valid, reason: DungeonSelectionReady ? eating.Reason : "Select the ready member first.",
            sprite: "ration", detail: "One paid action / restores hunger only");
        AddDungeonPartyControl("dungeon:medicine:" + member.Id, "Medicine / self", new(right, box.Y + 236, width, 64),
            DungeonSelectionReady && medicine.Valid, reason: DungeonSelectionReady ? medicine.Reason : "Select the ready member first.",
            sprite: "potion", detail: "One medicine and one action / conscious healing");
        AddDungeonPartyControl("dungeon:auto-eat", "Auto-eat: " + (member.Tactics!.AutoEat ? "On" : "Off"),
            new(box.X + 24, box.Y + 318, width, 44));
        AddDungeonPartyControl("dungeon:auto-eat-combat", "Auto-eat during combat: " + (member.Tactics.AutoEatInCombat ? "On" : "Off"),
            new(box.X + 24, box.Y + 378, width, 44));
        var others = DungeonRoster.Where(a => a.Id != member.Id).ToArray();
        for (int i = 0; i < others.Length; i++)
        {
            var target = others[i];
            var preview = _session.DungeonCombat.Preview(new(TacticalAction.UseMedicine, Target: target.Id));
            AddDungeonPartyControl("dungeon:medicine:" + target.Id, "Tend " + target.Name,
                new(right, box.Y + 318 + i * 60, width, 48), DungeonSelectionReady && preview.Valid,
                reason: DungeonSelectionReady ? preview.Reason : "Select the ready member first.", sprite: target.Sprite,
                detail: $"HP {target.Health}/{target.MaxHealth} / must be adjacent");
        }
    }

    private void DrawDungeonNeedsPanel(Rectangle box, int x, int y)
    {
        var member = _session.DungeonMember;
        Text(member.Name + " / " + DungeonReadiness(member), x, y, Gold); y += 32;
        Text($"Hunger {member.Body.Hunger:0.00}/100 / 0 is fed; 100 is empty", x, y); y += 26;
        Text($"Fatigue {member.Body.Fatigue:0.00}/100 / wounds {member.Body.Wounds}/10", x, y); y += 26;
        Text($"Shared provisions {_session.Count("food")} / medicine {_session.Count("medicine")}", x, y, Gold); y += 28;
        Wrap($"Food restores {_session.Simulation.MealReduction} hunger. Well-fed members gradually recover HP; starvation can kill. Automatic meals spend this member's own scheduled turns at {_session.Simulation.MealAt} hunger or above.", x, y, box.Width - 48, Muted);
        Text("Policy changes and inspection are free; eating and medicine spend actions.", x, box.Bottom - 28, Muted);
    }

    private void ToggleDungeonEating(bool combat)
    {
        var member = _session.DungeonMember; var tactics = member.Tactics!;
        _notice = _session.ConfigureDungeonEating(member.Id, combat ? tactics.AutoEat : !tactics.AutoEat,
            combat ? !tactics.AutoEatInCombat : tactics.AutoEatInCombat).Message;
    }
}

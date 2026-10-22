using Ithica.Core.Colony;
using Ithica.Core.Expeditions;

namespace Ithica.Core.Diagnostics;

// Replays ordinary player commands. Used by CLI checks and native preview/smoke
// scenes; no debug recruits, teleports, material grants or completed-job injection.
public static class CampaignScenarios
{
    public static void Require(Result result)
    {
        if (!result.Success) throw new InvalidOperationException(result.Message);
    }

    public static void Fight(Campaign game)
    {
        for (int i = 0; game.ActiveField?.Battle is { } b && i < 200; i++)
        {
            var actor = b.Active;
            var hurt = b.Units.Where(u => u.Friendly && u.Health < 35).OrderBy(u => u.Health).FirstOrDefault();
            if (hurt is not null && game.Pack.Medicine > 0) Require(game.Act(4, hurt.Id));
            else Require(game.Act(0, b.Units.First(u => !u.Friendly && u.Alive).Id));
        }
        if (game.ActiveField?.Battle is not null || game.Phase != CampaignPhase.Expedition)
            throw new InvalidOperationException("Encounter did not finish successfully.");
    }

    public static void Approach(Campaign game, string id, bool interact = true, bool fight = true)
    {
        for (int attempt = 0; attempt < 40; attempt++)
        {
            var f = game.ActiveField ?? throw new InvalidOperationException("No active field session.");
            if (f.Battle is not null) { if (!fight) return; Fight(game); }
            var obj = f.Zone.Objects.Single(o => o.Id == id);
            if (f.Position.Distance(obj.Position) <= 1)
            {
                if (interact) Require(game.Interact(id));
                if (fight && game.ActiveField?.Battle is not null) Fight(game);
                return;
            }
            var route = f.Route(obj.Position);
            if (route.Count == 0) throw new InvalidOperationException("No route to " + id);
            foreach (var p in route)
            {
                Require(game.Move(p.X - f.Position.X, p.Y - f.Position.Y));
                if (game.ActiveField?.Battle is not null)
                {
                    if (!fight) return;
                    Fight(game); break;
                }
                if (f.Position.Distance(obj.Position) <= 1) break;
            }
        }
        throw new InvalidOperationException("Approach exceeded command budget: " + id);
    }

    public static void Rescue(Campaign game, bool dismiss = true)
    {
        Require(game.Prepare("rescue-0")); Require(game.Depart([0]));
        Approach(game, "survivor"); Approach(game, "return");
        if (dismiss) Require(game.DismissArrival());
    }

    public static void FirstLoop(Campaign game)
    {
        Rescue(game); Require(game.StepColony(80));
        Require(game.Prepare("salvage")); Require(game.Depart([0]));
        Approach(game, "presence"); Approach(game, "component"); Approach(game, "return"); Require(game.DismissArrival());
        int cell = game.Home.World.Floors.First(i => game.Home.World.Breathable(i) && game.Home.MachineAt(i) is null && game.Home.JobAt(i) is null);
        Require(game.OrderSuitService(game.Home.World.X(cell), game.Home.World.Y(cell)));
        for (int i = 0; !game.SupportUpgrade && i < 240; i++) Require(game.StepColony());
        if (!game.SupportUpgrade || game.Home.RescuedCount < 1 || game.Sites.All(s => s.Id != "below"))
            throw new InvalidOperationException("Rescue/salvage/construction loop did not complete.");
    }

    public static void OpenScene(Campaign game, string scene)
    {
        if (scene is "colony" or "atlas" or "work") return;
        if (scene is "mining" or "industry") { ExtractionScenario.Establish(game); return; }
        if (scene == "incidents") { Require(game.StepColony(120)); return; }
        if (scene == "resident")
        {
            FirstLoop(game);
            var pump = game.Home.Machines.Single(m => m.Kind == ModuleKind.Oxygen);
            Require(game.OrderMaintenance(pump.Cell));
            for (int n = 0; game.Home.JobAt(pump.Cell) is not null && n < 160; n++) Require(game.StepColony());
            return;
        }
        if (scene == "arrival") { Rescue(game, false); return; }
        if (scene is "preparation" or "surface")
        { Require(game.Prepare("rescue-0")); if (scene == "surface") Require(game.Depart([0])); return; }
        if (scene == "battle")
        {
            Rescue(game); Require(game.StepColony(80)); Require(game.Prepare("salvage")); Require(game.Depart([0]));
            Approach(game, "presence", fight: false); return;
        }
        if (scene == "structure")
        {
            FirstLoop(game); Require(game.StepColony(80)); Require(game.Prepare("below"));
            var ready = game.Home.Crew.Where(c => c.Alive && !c.Rest && c.Health >= 25 && c.Energy >= 15 && c.Suit >= 25).Take(4).Select(c => c.Id).ToArray();
            Require(game.Depart(ready)); return;
        }
        throw new ArgumentException("Unknown scene: " + scene);
    }
}

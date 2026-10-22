using Ithica.Core.Colony;

namespace Ithica.Core.Diagnostics;

/// <summary>Actual expedition, survey, annex construction and industrial work. No state injection.</summary>
public static class ExtractionScenario
{
    public static Campaign Run(int seed = 42)
    {
        var game = new Campaign(seed);
        Establish(game);
        return game;
    }

    public static void Establish(Campaign game)
    {
        CampaignScenarios.FirstLoop(game);
        Advance(game, 100);
        for (int n = 0; n < 480 && game.Home.Crew.Take(2).Any(c => c.Rest || c.BreakUntil > game.CampaignTick || c.Health < 25 || c.Energy < 15 || c.Suit < 25); n++) Advance(game);
        CampaignScenarios.Require(game.Prepare("below"));
        CampaignScenarios.Require(game.Depart([0, 1], new() { Food = 8, Water = 8, Medicine = 4, Filters = 1 }));
        CampaignScenarios.Approach(game, "terminal"); CampaignScenarios.Approach(game, "seal");
        CampaignScenarios.Approach(game, "presence-heavy"); CampaignScenarios.Approach(game, "component");
        CampaignScenarios.Approach(game, "return"); CampaignScenarios.Require(game.DismissArrival());
        if (game.Home.Extraction is null) throw new InvalidOperationException("Restoring the pump did not reveal deposits.");
        int cx = game.Home.World.Width / 2, cy = game.Home.World.Height / 2;
        void Order(int x, int y, Tool tool, ModuleKind kind = ModuleKind.Wall)
        {
            if (tool == Tool.Build && kind == ModuleKind.Wall)
                for (int n = 0; n < 80 && game.Home.CrewAt(game.Home.World.Index(cx + x, cy + y)) is not null; n++) Advance(game);
            var result = game.Home.Designate(cx + x, cy + y, tool, kind);
            if (!result.Success) throw new InvalidOperationException($"{tool}/{kind} at {x},{y} on tick {game.CampaignTick}: {result.Message}");
        }
        // Mine actual starter seams, including the rock-free approaches, for construction and survival.
        foreach (var (x, y) in new[] { (-7, -8), (5, 8), (12, -5) })
            for (int dy = 0; dy < 3; dy++) for (int dx = 0; dx < 3; dx++) Order(x + dx, y + dy, Tool.Dig);
        // The east hull can extend beyond the guaranteed apron into seeded rock.
        for (int y = 4; y <= 8; y++) for (int x = 10; x <= 14; x++)
            if (World.Solid(game.Home.World.Tiles[game.Home.World.Index(cx + x, cy + y)])) Order(x, y, Tool.Dig);
        FinishOrders(game);
        for (int n = 0; n < 3; n++) Order(n, 1, Tool.Build, ModuleKind.Solar);
        var deposit = game.Home.Extraction.Deposits[0];
        Order(12, 6, Tool.Survey);
        FinishOrders(game);
        // A small independent pressure room, joined to the shelter's power through sealed doors.
        foreach (var (x, y) in new[] { (10, 0), (11, 0), (11, 1), (11, 2), (11, 3) }) Order(x, y, Tool.Build, ModuleKind.Floor);
        for (int y = 5; y <= 7; y++) for (int x = 11; x <= 13; x++) Order(x, y, Tool.Build, ModuleKind.Floor);
        FinishOrders(game);
        // Corners must be built while an orthogonal approach is still open beside exterior rock.
        foreach (var (x, y) in new[] { (10, 4), (14, 4), (10, 8), (14, 8) }) Order(x, y, Tool.Build, ModuleKind.Wall);
        FinishOrders(game);
        for (int y = 4; y <= 8; y++) for (int x = 10; x <= 14; x++)
            if ((x is 10 or 14 || y is 4 or 8) && !(x is 10 or 14 && y is 4 or 8)) Order(x, y, Tool.Build, x == 11 && y == 4 ? ModuleKind.Airlock : ModuleKind.Wall);
        FinishOrders(game);
        Order(13, 6, Tool.Build, ModuleKind.Oxygen);
        Order(11, 6, Tool.Build, ModuleKind.Bunk);
        Order(12, 6, Tool.Build, ModuleKind.Drill);
        FinishOrders(game);
        CampaignScenarios.Require(game.SetWorkPriority(0, WorkGroup.Maintenance, 1));
        CampaignScenarios.Require(game.SetWorkPriority(1, WorkGroup.Mining, 1));
        for (int n = 0; n < 800 && game.Home.IndustrialBlock(game.Home.MachineAt(deposit.Cell)!) is not null; n++) Advance(game);
        CampaignScenarios.Require(game.OrderExtraction(deposit.Cell));
        for (int n = 0; n < 1000 && game.Home.Resources.Ore < 8; n++) Advance(game);
        if (game.Home.Resources.Ore < 8) throw new InvalidOperationException("The supported extraction operation produced no useful ore: " + game.Home.IndustrialBlock(game.Home.MachineAt(deposit.Cell)!));
        CampaignScenarios.Require(game.Home.Designate(cx + 12, cy + 6, Tool.Cancel));
        var battery = game.Home.Machines.Single(m => m.Kind == ModuleKind.Battery);
        CampaignScenarios.Require(game.OrderReinforcement(battery.Cell));
        Order(3, 1, Tool.Build, ModuleKind.Fabricator);
        FinishOrders(game);
        var fabricator = game.Home.Machines.Single(m => m.Kind == ModuleKind.Fabricator);
        CampaignScenarios.Require(game.SetIndustryEnabled(deposit.Cell, false));
        CampaignScenarios.Require(game.OrderComponent(fabricator.Cell));
        FinishOrders(game);
        if (!battery.Reinforced || game.Home.Resources.Components < 1) throw new InvalidOperationException("Ore did not become station reinforcement and a fabricated component.");
        string snapshot = game.StateJson();
        if (Campaign.FromJson(snapshot).StateJson() != snapshot) throw new InvalidOperationException("The extraction campaign did not resume exactly.");
    }

    private static void FinishOrders(Campaign game)
    {
        for (int n = 0; game.Home.Jobs.Count > 0 && n < 1600; n++) Advance(game);
        if (game.Home.Jobs.Count > 0) throw new InvalidOperationException("Construction/work did not finish: " + string.Join(", ", game.Home.Jobs.Select(j => $"{j.Kind}@{game.Home.World.X(j.Cell)},{game.Home.World.Y(j.Cell)} {j.Progress:0}/{j.Required}")));
    }

    private static void Advance(Campaign game, int count = 1)
    {
        for (int t = 0; t < count; t++)
        {
            foreach (var i in game.Home.Story!.Pending.ToArray())
            {
                string choice = i.DefinitionId switch { "pump" => "service", "relay" => "isolate", "dust" => "filter", "debris" => "brace",
                    "fatigue" => "rest", "workload" => "mediate", "meal" => "share", "hauler" => "trade", "seed" => "decline", "solidarity" => "recognize", "signal" => "archive", _ => "decline" };
                if (game.Home.IncidentChoiceAvailable(i.Id, choice).Success) CampaignScenarios.Require(game.RespondToIncident(i.Id, choice));
            }
            foreach (var m in game.Home.Machines.Where(m => Simulation.Serviceable(m) && m.Kind != ModuleKind.Drill && (m.Fault != MachineFault.None || m.Condition < 45) && game.Home.JobAt(m.Cell) is null).ToArray())
                if (game.Home.Resources.Covers(Simulation.MaintenancePrice(m))) CampaignScenarios.Require(game.OrderMaintenance(m.Cell));
            CampaignScenarios.Require(game.StepColony());
            if (game.Home.GameOver) throw new InvalidOperationException("The extraction walkthrough lost the colony.");
        }
    }
}

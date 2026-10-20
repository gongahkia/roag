using Ithica.Core.Colony;

namespace Ithica.Core.Diagnostics;

/// <summary>A reproducible pressure/recovery walkthrough using only ordinary campaign commands.</summary>
public static class StationScenario
{
    public static Campaign Run()
    {
        var game = new Campaign(2);
        CampaignScenarios.Require(game.StepColony(120));
        var warning = game.Home.Story!.Pending.Single(i => i.DefinitionId == "pump");
        CampaignScenarios.Require(game.RespondToIncident(warning.Id, "defer"));
        var pump = game.Home.Machines.Single(m => m.Kind == ModuleKind.Oxygen);
        float water = game.Home.Resources.Water;
        CampaignScenarios.Require(game.StepColony(12));
        if (pump.Fault != MachineFault.Leak || game.Home.Resources.Water >= water - .4f) throw new InvalidOperationException("Pump fault had no physical cost.");
        CampaignScenarios.Require(game.OrderMaintenance(pump.Cell));
        for (int n = 0; game.Home.JobAt(pump.Cell) is not null && n < 160; n++) CampaignScenarios.Require(game.StepColony());
        if (pump.Fault != MachineFault.None || !game.Home.Crew[0].Memories.Any(m => m.Key == "repair")) throw new InvalidOperationException("Paid resident repair did not complete.");
        CampaignScenarios.Rescue(game);
        CampaignScenarios.Require(game.StepColony(80));
        // Three useful ordinary construction orders provide sustained engineering work.
        for (int n = 0; n < 3; n++)
        {
            int cell = game.Home.World.Floors.Where(i => game.Home.World.Breathable(i) && game.Home.MachineAt(i) is null && game.Home.JobAt(i) is null)
                .OrderBy(i => game.Home.World.Distance(i, pump.Cell)).First();
            CampaignScenarios.Require(game.Home.Designate(game.Home.World.X(cell), game.Home.World.Y(cell), Tool.Build, ModuleKind.Solar));
            for (int t = 0; game.Home.JobAt(cell) is not null && t < 160; t++) { Respond(game); CampaignScenarios.Require(game.StepColony()); }
        }
        for (int n = 0; n < 2600 && !game.Home.GameOver; n++)
        {
            if (Respond(game))
            {
                string snapshot = game.StateJson();
                if (Campaign.FromJson(snapshot).StateJson() != snapshot) throw new InvalidOperationException("Social outcome did not resume exactly.");
                return game;
            }
            foreach (var m in game.Home.Machines.Where(m => m.Fault != MachineFault.None && game.Home.JobAt(m.Cell) is null).ToArray())
                if (game.Home.Resources.Covers(Simulation.MaintenancePrice(m))) CampaignScenarios.Require(game.OrderMaintenance(m.Cell));
            CampaignScenarios.Require(game.StepColony());
        }
        throw new InvalidOperationException("The ordinary-command walkthrough did not reach a work-related social response.");
    }

    private static bool Respond(Campaign game)
    {
        bool recovered = false;
        foreach (var i in game.Home.Story!.Pending.ToArray())
        {
            string choice = i.DefinitionId switch
            {
                "pump" => "service", "relay" => "isolate", "dust" => "filter", "debris" => "brace",
                "fatigue" => "rest", "workload" => "mediate", "meal" => "share", "hauler" => "trade",
                "seed" => "cultivate", "solidarity" => "recognize", "signal" => "archive", _ => "decline"
            };
            if (!game.Home.IncidentChoiceAvailable(i.Id, choice).Success) continue;
            bool social = i.DefinitionId is "fatigue" or "workload" && i.ResidentId is int resident && game.Home.Crew[resident].WorkTicks > 0;
            CampaignScenarios.Require(game.RespondToIncident(i.Id, choice));
            if (social)
            {
                var c = game.Home.Crew[i.ResidentId!.Value];
                recovered |= c.BreakUntil > game.CampaignTick && c.Job is null && c.Memories.Any(m => m.Key == "heard");
            }
        }
        return recovered;
    }
}

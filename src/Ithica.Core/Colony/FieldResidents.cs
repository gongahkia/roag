namespace Ithica.Core.Colony;

public sealed partial class Simulation
{
    public void SendToField(Colonist resident, string siteId, float suitEfficiency)
    {
        if (!Crew.Contains(resident) || !resident.Alive || resident.Location != ResidentLocation.Home || resident.Expedition is not null)
            throw new InvalidOperationException("Resident is unavailable.");
        Release(resident);
        resident.Location = ResidentLocation.Field;
        resident.SiteId = siteId;
        resident.Rest = false;
        resident.SuitEfficiency = suitEfficiency;
    }

    public int ArrivalCell() => World.Floors.FirstOrDefault(i => World.Breathable(i, .5f), World.Center);

    public void BringHome(Colonist resident)
    {
        resident.Health = Math.Max(5, resident.Health);
        resident.Location = ResidentLocation.Home;
        resident.SiteId = null;
        resident.Cell = ArrivalCell();
        resident.Rest = resident.Health < 90 || resident.Energy < 60;
        resident.Activity = resident.Rest ? "Recovering" : "Home";
    }

    public bool RecruitFieldSurvivor(Survivor survivor)
    {
        if (survivor.State != ContactState.Escorted || survivor.Health <= 0) return false;
        int cell = ArrivalCell();
        survivor.Cell = cell;
        if (!World.Breathable(cell, .5f)) return false;
        var resident = AddColonist(survivor.Name, survivor.Role, survivor.Trait, cell);
        resident.Health = survivor.Health;
        resident.Suit = survivor.Suit;
        resident.Hunger = 45;
        resident.Energy = 25;
        resident.Morale = 55;
        resident.Rest = true;
        resident.FieldRole = survivor.Role switch { "Miner" => 1, "Botanist" => 2, "Medic" => 3, _ => 0 };
        survivor.State = ContactState.Rescued;
        survivor.RecruitedResidentId = resident.Id;
        survivor.FieldEscort = false;
        Event(survivor.Name + " is home. A sleeping pod is no longer empty.", NoticeLevel.Good);
        return true;
    }
}

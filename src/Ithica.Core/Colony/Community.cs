namespace Ithica.Core.Colony;

public enum CrewValue { Craft, Discovery, Fellowship, Security }
public enum CrewAversion { Overwork, Confinement, Conflict, Danger }
public sealed record CrewDisposition(CrewValue Likes, CrewAversion Dislikes);
public sealed record CrewMemory(string Key, string Text, long Tick, long Until, int Mood, int? OtherId = null);
public sealed class CrewBond
{
    public int OtherId { get; set; }
    public int Trust { get; set; }
}

public sealed partial class Simulation
{
    private void InitializeDisposition(Colonist c)
    {
        // Stable identity without consuming simulation, generation or director randomness.
        uint identity = unchecked((uint)(Seed * 397 + c.Id * 7919));
        c.Disposition ??= c.Trait switch
        {
            "Meticulous" => new(CrewValue.Craft, CrewAversion.Danger),
            "Restless" or "Stargazer" or "Curious" => new(CrewValue.Discovery, CrewAversion.Confinement),
            "Optimist" or "Gentle" => new(CrewValue.Fellowship, CrewAversion.Conflict),
            "Steadfast" or "Survivor" => new(CrewValue.Security, CrewAversion.Danger),
            "Patient" => new(CrewValue.Security, CrewAversion.Conflict),
            _ => new(c.Role switch { "Engineer" => CrewValue.Craft, "Miner" => CrewValue.Discovery,
            "Medic" => CrewValue.Fellowship, _ => CrewValue.Security }, (CrewAversion)(identity % 4))
        };
    }

    public void Remember(Colonist c, string key, string text, int mood, int duration = 480, int? other = null)
    {
        if (!c.Alive || c.Memories.Any(m => m.Key == key && m.OtherId == other && m.Until > Tick)) return;
        if (c.Memories.Count >= 12) c.Memories.RemoveAt(0);
        c.Memories.Add(new(key, text, Tick, Tick + duration, mood, other));
        c.Morale = Math.Clamp(c.Morale + mood * .4f, 0, 100);
    }

    private static void ChangeTrust(Colonist a, Colonist b, int amount)
    {
        var bond = a.Bonds.SingleOrDefault(x => x.OtherId == b.Id);
        if (bond is null) { bond = new() { OtherId = b.Id }; a.Bonds.Add(bond); }
        bond.Trust = Math.Clamp(bond.Trust + amount, -100, 100);
    }

    private void CommunityTick()
    {
        foreach (var c in Crew.Where(c => c.Alive))
        {
            InitializeDisposition(c);
            c.Memories.RemoveAll(m => m.Until <= Tick);
            if (Tick % 24 != 0) continue;
            if (c.Suit < 30 || c.Health < 35)
                Remember(c, "unsafe", "I was left in danger.", c.Disposition!.Dislikes == CrewAversion.Danger ? -12 : -7);
            if (c.WorkTicks >= 120 && c.Energy < 55)
                Remember(c, "overworked", "The work never seems to end.", c.Disposition!.Dislikes == CrewAversion.Overwork ? -12 : -7);
            if (c.Hunger > 65) Remember(c, "hungry", "There was not enough to eat.", -8);
            float target = Math.Clamp(72 + c.Memories.Sum(m => m.Mood), 10, 98);
            c.Morale += Math.Clamp(target - c.Morale, -.6f, .3f);
            // Conversation and recovery happen only between residents actually at home.
            if (c.Location != ResidentLocation.Home)
            {
                if (c.Disposition!.Dislikes == CrewAversion.Confinement && Tick % 120 == 0)
                    Remember(c, "outside", "There is room to breathe beyond the station walls.", 5, 240);
                continue;
            }
            if (c.Rest && c.Energy > 70) c.WorkTicks = Math.Max(0, c.WorkTicks - 24);
            if (Tick % 120 != 0 || c.Job is not null || c.Rest) continue;
            var other = Crew.FirstOrDefault(o => o.Id > c.Id && o.Alive && o.Location == ResidentLocation.Home
                && o.Job is null && !o.Rest && World.Breathable(o.Cell) && World.Breathable(c.Cell) && World.Distance(c.Cell, o.Cell) <= 6);
            if (other is null) continue;
            ChangeTrust(c, other, 2); ChangeTrust(other, c, 2);
            Remember(c, "company", $"A quiet watch with {other.Name}.", 3, 240, other.Id);
            Remember(other, "company", $"A quiet watch with {c.Name}.", 3, 240, c.Id);
        }
    }

    private void GrantBreak(Colonist c, int ticks)
    {
        Release(c); c.BreakUntil = Tick + ticks; c.Rest = true;
        Remember(c, "heard", "My request for rest was heard.", 8);
    }
}

using Ithica.Core.Colony;
using Ithica.Core.Expeditions;
using Ithica.Core.Generation;
using Field = Ithica.Core.Expeditions.Campaign;

namespace Ithica.Core;

public sealed class ExpeditionPack
{
    public int Food { get; set; } = 4;
    public int Water { get; set; } = 4;
    public int Medicine { get; set; } = 2;
    public int Filters { get; set; } = 1;
    public Stock Cost => new(Medicine * 2, Water, Food, Filters);
    public bool Valid => Food is >= 0 and <= 100 && Water is >= 0 and <= 100 && Medicine is >= 0 and <= 20 && Filters is >= 0 and <= 20;
    public ExpeditionPack Copy() => new() { Food = Food, Water = Water, Medicine = Medicine, Filters = Filters };
}

public sealed class Site
{
    public string Id { get; set; } = "";
    public int Seed { get; set; }
    public int MapCell { get; set; }
    public int? ContactId { get; set; }
    public bool Structure { get; set; }
    public bool Secured { get; set; }
    public Field? Session { get; set; }
    public string Name => ContactId is not null ? "Distress contact" : Structure ? "A signal below" : "Scattered salvage";
}

public sealed partial class Campaign
{
    private bool _returning; // Commands and return travel settle atomically before any save boundary.
    public Simulation Home { get; private set; }
    public CampaignClock Clock { get; private set; } = new();
    public long CampaignTick => Home.Tick;
    public CampaignPhase Phase { get; private set; }
    public List<Site> Sites { get; } = [];
    public string? SelectedSiteId { get; private set; }
    public Site? SelectedSite => Sites.SingleOrDefault(s => s.Id == SelectedSiteId);
    public Field? ActiveField => Phase == CampaignPhase.Expedition ? SelectedSite?.Session : null;
    public List<int> PartyIds { get; private set; } = [];
    public IEnumerable<Colonist> Party => PartyIds.Select(id => Home.Crew.Single(c => c.Id == id));
    public ExpeditionPack Pack { get; private set; } = new() { Food = 0, Water = 0, Medicine = 0, Filters = 0 };
    public Stock Cargo { get; private set; }
    public string ArrivalReport { get; private set; } = "";
    public bool SupportUpgrade => Home.Machines.Any(m => m.Kind == ModuleKind.SuitRack && m.Powered);
    public string Objective => Home.RescuedCount == 0 ? "Bring the person behind the distress signal home."
        : Home.Machines.Any(m => m.Kind == ModuleKind.SuitRack) ? SupportUpgrade ? "Investigate the signal below. Bring people home. Keep the shelter alive." : "Power the suit service."
        : Home.Resources.Components > 0 ? "Build a suit service with the recovered component."
        : "Recover a usable component from the scattered salvage.";

    public Campaign(int seed = 42, int width = 96, int height = 64) : this(new Simulation(seed, width, height))
    {
        Home.Log.RemoveAll(n => n.Text.StartsWith("Charter:", StringComparison.Ordinal));
        Home.Event(Objective);
    }
    private Campaign(Simulation home)
    {
        Home = home;
        DiscoverSites();
    }

    private void ChangePhase(CampaignPhase phase) { Phase = phase; Clock.Rebase(); }

    public void UpdateRealtime(double seconds)
    {
        int count = Clock.Elapse(seconds, Phase == CampaignPhase.Colony);
        for (int i = 0; i < count; i++) Tick();
    }

    public Result StepColony(int count = 1)
    {
        if (Phase != CampaignPhase.Colony || count is < 1 or > 10000) return new(false, "Colony time is unavailable here.");
        for (int i = 0; i < count; i++) Tick();
        return new(true, "");
    }

    private void DiscoverSites()
    {
        foreach (var contact in Home.Survivors.Where(s => s.State == ContactState.Stranded))
            AddSite("rescue-" + contact.Id, contact.Cell, contact.Id, false);
        if (Home.RescuedCount > 0)
            AddSite("salvage", Home.World.Index(Home.World.Width / 2 + 17, Home.World.Height / 2 - 4), null, false);
        if (SupportUpgrade)
            AddSite("below", Home.World.Index(Home.World.Width / 2 - 16, Home.World.Height / 2 + 6), null, true);
    }

    private void AddSite(string id, int cell, int? contact, bool structure)
    {
        if (Sites.Any(s => s.Id == id)) return;
        uint salt = contact is int i ? unchecked(100U + (uint)i) : structure ? 81U : 80U;
        Sites.Add(new() { Id = id, MapCell = cell, ContactId = contact, Structure = structure,
            Seed = unchecked((int)GenerationSeeds.Mix(unchecked((uint)Home.Seed), salt)) });
        Home.World.Reveal(cell, 1);
    }

    public Result Prepare(string siteId)
    {
        if (Phase != CampaignPhase.Colony || Home.GameOver) return new(false, "Return to the shelter first.");
        var site = Sites.SingleOrDefault(s => s.Id == siteId);
        if (site is null || site.ContactId is int id && Home.Survivors[id].State != ContactState.Stranded)
            return new(false, "That signal is no longer available.");
        site.Session ??= Field.CreateField(site.Seed, LunarGenerator.Create(site.Id, site.Seed, site.Structure, site.ContactId is not null));
        SelectedSiteId = site.Id;
        ChangePhase(CampaignPhase.Preparation);
        return new(true, "Choose residents and supplies. Time is paused.");
    }

    public Result CancelPreparation()
    {
        if (Phase != CampaignPhase.Preparation) return new(false, "No departure is being prepared.");
        SelectedSiteId = null;
        ChangePhase(CampaignPhase.Colony);
        return new(true, "Departure canceled; supplies remain at home.");
    }

    public Result Depart(IEnumerable<int> residentIds, ExpeditionPack? supplies = null)
    {
        if (Phase != CampaignPhase.Preparation || SelectedSite?.Session is not { } field) return new(false, "Select a signal first.");
        var ids = residentIds.ToList();
        var pack = (supplies ?? new()).Copy();
        if (ids.Count is < 1 or > 4 || ids.Distinct().Count() != ids.Count || !pack.Valid || pack.Food < 1 || pack.Water < 1 || !Home.Resources.Covers(pack.Cost))
            return new(false, "Choose one to four people and supplies the shelter can spare (at least 1 food and water).");
        var residents = ids.Select(id => Home.Crew.SingleOrDefault(c => c.Id == id)).ToList();
        if (residents.Any(c => c is null || !c.Alive || c.Location != ResidentLocation.Home || c.Expedition is not null || c.Rest || c.Health < 25 || c.Energy < 15 || c.Suit < 25))
            return new(false, "Someone is away or needs recovery. Select ready residents.");
        if (SelectedSite.ContactId is int contact && Home.Survivors[contact].State != ContactState.Stranded)
            return new(false, "The distress signal has gone silent.");
        PartyIds = ids;
        Pack = pack;
        Cargo = default;
        Home.Resources -= pack.Cost;
        foreach (var c in residents.Cast<Colonist>()) Home.SendToField(c, SelectedSite.Id, SupportUpgrade ? .65f : 1);
        field.EnterField(Party.Select(Project));
        field.SetFieldSupplies(Pack.Medicine);
        ChangePhase(CampaignPhase.Expedition);
        Charge(ActionCosts.Travel);
        Home.Event("Outside / " + string.Join(", ", Party.Select(c => c.Name)), NoticeLevel.Info);
        return new(true, "Approach complete. The world waits for your decision.");
    }

    private static Hero Project(Colonist c) => new() { Resident = true, Id = c.Id, Name = c.Name, Role = c.FieldRole,
        Health = (int)Math.Ceiling(c.Health), Mana = c.Charge, Stress = (int)Math.Clamp(100 - c.Morale, 0, 100), Level = c.Level, Experience = c.Experience };

    private void RefreshProjection()
    {
        if (ActiveField is not { } f) return;
        f.SetFieldSupplies(Pack.Medicine);
        foreach (var c in Party)
        {
            var h = f.Party.Single(h => h.Id == c.Id);
            h.Health = (int)Math.Ceiling(c.Health); h.Mana = c.Charge; h.Stress = (int)(100 - c.Morale);
            h.Level = c.Level; h.Experience = c.Experience;
            if (f.Battle?.Units.SingleOrDefault(u => u.HeroId == c.Id) is { } u)
            { u.Health = h.Health; u.Mana = h.Mana; u.Stress = h.Stress; }
        }
    }

    private Result FieldCommand(Func<Field, ActionResult> action, int ticks)
    {
        if (ActiveField is not { } field) return new(false, "No expedition is active.");
        var before = field.Party.ToDictionary(h => h.Id, h => h.Health);
        var result = action(field);
        if (!result.Success) return new(false, result.Message);
        foreach (var c in Party)
        {
            var h = field.Party.Single(h => h.Id == c.Id);
            var unit = field.Battle?.Units.SingleOrDefault(u => u.HeroId == c.Id);
            c.Health = Math.Clamp(c.Health + (unit?.Health ?? h.Health) - before[c.Id], 0, 100);
            c.Charge = unit?.Mana ?? h.Mana; c.Morale = 100 - (unit?.Stress ?? h.Stress);
            c.Level = h.Level; c.Experience = h.Experience;
        }
        Pack.Medicine = field.Potions;
        Charge(ticks);
        if (Phase != CampaignPhase.Expedition) return new(true, ArrivalReport);
        if (field.RetreatRequested) return FinishReturn(true);
        RefreshProjection();
        return new(true, result.Message);
    }

    public Result Move(int dx, int dy)
    {
        if (ActiveField is not { } f) return new(false, "No expedition is active.");
        if (Math.Abs(dx) + Math.Abs(dy) != 1) return new(false, "Move one tile at a time.");
        var next = f.Position + new Cell(dx, dy);
        if (f.Battle is null && !f.Walkable(next) && f.ObjectAt(next) is { Kind: ObjectKind.Gate } gate) return Interact(gate.Id);
        var result = FieldCommand(c => c.Move(dx, dy), ActionCosts.Move);
        if (result.Success) ResolveEnemies();
        return result;
    }

    public Result Wait() => FieldCommand(f => f.Wait(), ActionCosts.Wait);

    public Result Act(int skill, int? target = null)
    {
        var result = FieldCommand(f => f.Act(skill, target), ActionCosts.Combat);
        if (result.Success) ResolveEnemies();
        return result;
    }

    private void ResolveEnemies()
    {
        int guard = 0;
        while (ActiveField?.Battle is { Active.Friendly: false } && guard++ < 100)
            if (!FieldCommand(f => f.AdvanceEnemy(), ActionCosts.Combat).Success) break;
        if (guard >= 100) throw new InvalidOperationException("Encounter resolution did not reach a player decision.");
    }

    public Result Interact(string? id = null)
    {
        if (ActiveField is not { Battle: null } f) return new(false, "Finish or retreat from the encounter first.");
        var obj = f.Zone.Objects.Where(f.Present).Where(o => id is null || o.Id == id)
            .Where(o => o.Position.Distance(f.Position) <= 1).OrderBy(o => o.Position.Distance(f.Position)).FirstOrDefault();
        if (obj is null) return new(false, "Approach an object to interact.");
        switch (obj.Kind)
        {
            case ObjectKind.Return: return ReturnHome();
            case ObjectKind.Rescue:
                if (SelectedSite!.ContactId is not int contact || Home.Survivors[contact] is not { State: ContactState.Stranded } survivor)
                    return new(false, "There is no living contact here.");
                if (Pack.Water < 1 || Pack.Food < 1) return new(false, "Sharing oxygen and supplies needs 1 carried water and food.");
                Pack.Water--; Pack.Food--; survivor.Suit = Math.Max(survivor.Suit, 80);
                survivor.State = ContactState.Escorted; survivor.FieldEscort = true;
                f.Completed.Add(obj.Id); Charge(ActionCosts.Rescue);
                return new(true, survivor.Name + " is with you. Return to shelter.");
            case ObjectKind.Salvage:
                if (!f.Completed.Contains(obj.Detail)) return new(false, "Movement still blocks safe recovery. Resolve the encounter first.");
                f.Completed.Add(obj.Id); Cargo += new Stock(12, 0, 0, 2, 1); Charge(ActionCosts.Salvage);
                return new(true, "Recovered a component, 12 metal and 2 crystal. Bring them home.");
            case ObjectKind.Terminal:
                if (SelectedSite!.Secured) return new(false, "The pump is already operating.");
                if (Pack.Water < 2) return new(false, "The repair needs 2 carried water.");
                Pack.Water -= 2; SelectedSite.Secured = true; f.Completed.Add(obj.Id); Charge(ActionCosts.Repair);
                return new(true, "The pump starts. Water will reach the shelter; the seal can cycle.");
            case ObjectKind.Gate:
                if (!SelectedSite!.Secured) return new(false, obj.Detail);
                if (!f.Completed.Add(obj.Id)) return new(false, "The passage is open.");
                Charge(ActionCosts.Repair); return new(true, "The seal opens.");
            case ObjectKind.Encounter:
                var result = FieldCommand(c => c.Interact(obj.Id), ActionCosts.Combat); ResolveEnemies(); return result;
            default: return new(true, obj.Detail.Length > 0 ? obj.Detail : obj.Name);
        }
    }

    public Result Treat(int residentId)
    {
        if (ActiveField is not { Battle: null } || !PartyIds.Contains(residentId) || Pack.Medicine < 1)
            return new(false, "Treatment needs an available resident and a medical kit outside combat.");
        var c = Party.Single(c => c.Id == residentId);
        if (c.Health >= 100) return new(false, "No treatment is needed.");
        Pack.Medicine--; c.Health = Math.Min(100, c.Health + 24); Charge(ActionCosts.Treatment);
        return new(true, c.Name + " treated. +24 health.");
    }

    public Result Rest()
    {
        if (ActiveField is not { Battle: null } || Pack.Food < PartyIds.Count || Pack.Water < 1)
            return new(false, "Rest needs food for each resident and 1 water, outside combat.");
        Pack.Food -= PartyIds.Count; Pack.Water--;
        foreach (var c in Party) { c.Energy = Math.Min(100, c.Energy + 30); c.Charge = Math.Min(40, c.Charge + 8); c.Morale = Math.Min(100, c.Morale + 8); }
        Charge(ActionCosts.Rest); return new(true, "A short stop. Deadlines and the colony advance too.");
    }

    public Result Refill()
    {
        if (ActiveField is not { Battle: null } || Pack.Water < PartyIds.Count || Party.All(c => c.Suit >= 99))
            return new(false, "Refilling needs 1 carried water per resident and a depleted suit, outside combat.");
        Pack.Water -= PartyIds.Count;
        foreach (var c in Party) c.Suit = 100;
        Charge(ActionCosts.Refill); return new(true, "Suit reserves replenished.");
    }

    public Result ClearHazards()
    {
        if (ActiveField is not { Battle: null } f || Pack.Filters < 1 || !f.Zone.Environment.Nearby(f.Zone, f.Position, 3))
            return new(false, "Approach a hazard with a filter kit, outside combat.");
        Pack.Filters--; int cleared = f.Zone.Environment.ClearNear(f.Zone, f.Position, 3); Charge(ActionCosts.Clear);
        return new(true, "Suppressed " + cleared + " affected tiles.");
    }

    private void Charge(int ticks)
    {
        for (int i = 0; i < ticks; i++) Tick();
        RefreshProjection();
        if (!_returning && ActiveField is not null && Party.All(c => c.Health <= 5)) FinishReturn(true);
    }

    private void Tick()
    {
        if (Home.GameOver) return;
        Home.Step();
        if (ActiveField is { } f)
        {
            bool hazardTick = f.Zone.Environment.Advance(f.Zone, f.Seed);
            foreach (var c in Party)
            {
                if (c.Hunger >= 35 && Pack.Food > 0) { Pack.Food--; c.Hunger = Math.Max(0, c.Hunger - 27); }
                if (hazardTick && f.Zone.Environment.Cells[f.Zone.Index(f.Position)] != HazardKind.None)
                { c.Morale = Math.Max(0, c.Morale - 3); if (c.Health > 0) c.Health = Math.Max(1, c.Health - 1); }
            }
        }
        foreach (var s in Home.Survivors.Where(s => s.State == ContactState.Escorted && !s.FieldEscort)) Home.RecruitFieldSurvivor(s);
        if (Home.Tick % 32 == 0) Home.Resources += new Stock(0, Sites.Count(s => s.Secured), 0, 0);
        DiscoverSites();
    }

    public Result ReturnHome() => FinishReturn(false);

    private Result FinishReturn(bool emergency)
    {
        if (ActiveField is not { } f) return new(false, "No expedition is active.");
        var exit = f.Zone.Objects.Single(o => o.Kind == ObjectKind.Return);
        if (!emergency && (f.Battle is not null || f.Position.Distance(exit.Position) > 1)) return new(false, "Reach the return approach first.");
        _returning = true;
        try
        {
            Charge(ActionCosts.Travel);
            if (emergency) Cargo = default;
            Home.Resources += Cargo + Pack.Cost;
            string names = string.Join(", ", Party.Select(c => c.Name));
            foreach (var c in Party) Home.BringHome(c);
            foreach (var s in Home.Survivors.Where(s => s.FieldEscort))
            { s.FieldEscort = false; s.Cell = Home.ArrivalCell(); Home.RecruitFieldSurvivor(s); }
            ArrivalReport = (emergency ? "An injured return. Cargo was lost. " : "Home. ") + names
                + $" / recovered {Cargo.Components:0} component(s), {Cargo.Metal:0} metal. Residents retain their condition and may need recovery.";
            Home.Event(ArrivalReport, emergency ? NoticeLevel.Warning : NoticeLevel.Good);
            Cargo = default; Pack = new() { Food = 0, Water = 0, Medicine = 0, Filters = 0 };
            f.LeaveField(); PartyIds.Clear(); ChangePhase(CampaignPhase.Arrival); DiscoverSites();
            return new(true, ArrivalReport);
        }
        finally { _returning = false; }
    }

    public Result DismissArrival()
    {
        if (Phase != CampaignPhase.Arrival) return new(false, "No arrival report.");
        SelectedSiteId = null; ChangePhase(CampaignPhase.Colony); return new(true, Objective);
    }

    public Result OrderSuitService(int x, int y) => Phase == CampaignPhase.Colony
        ? Home.Designate(x, y, Tool.Build, ModuleKind.SuitRack) : new(false, "Build after returning home.");
}

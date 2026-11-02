using Roag.Core.Expeditions;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed record RoagAim(string Kind, int Dx = 0, int Dy = 0, long Target = 0, int Choice = 0, bool Vow = false, bool ReplaceUtility = false);
public sealed record RoagTelegraph(long Source, long Due, IReadOnlyList<WorldPosition> Cells);
public sealed record RoagFighter(long Id, string Name, string Class, WorldPosition Position, int Health, int MaxHealth, bool Friendly, bool Ready);

// The optional reference assembly preserves old integration tests without
// shipping their generated rules or fixed combat window in the playable game.
internal interface IRoagPrototypeCombat
{
    long RoagActiveActor { get; }
    void InitializeRoagParty();
    ActionResult ChooseRoagClass(long actor, string classId);
    bool RoagCombatActor(long actor);
    void BeginRoagCombat(long enemy);
    IReadOnlyList<RoagFighter> RoagFighters();
    IReadOnlyList<RoagTelegraph> RoagTelegraphs();
    IReadOnlyList<WorldPosition> RoagPreview(RoagAim aim);
    ActionResult SubmitRoag(RoagAim aim);
    void CaptureRoagRules();
}

public sealed partial class WorldSession
{
    internal static Func<WorldSession, IRoagPrototypeCombat>? PrototypeCombatFactory { get; set; }
    private IRoagPrototypeCombat? _prototypeCombat;
    private IRoagPrototypeCombat PrototypeCombat => _prototypeCombat ??= PrototypeCombatFactory?.Invoke(this)
        ?? throw new NotSupportedException("The generated combat prototype is reference-only. Start a native dungeon run.");

    public IReadOnlyList<string> RoagClasses => ClassCatalog.Ids;
    public string RoagClassName(string id) => ClassCatalog.Get(id).Name;
    public long RoagActiveActor => State.Tactics?.ActiveActor
        ?? (State.RoagCombat ? PrototypeCombat.RoagActiveActor : State.Party.FirstOrDefault());
    public void InitializeRoagParty() { if (Plan.RoagNative) PrototypeCombat.InitializeRoagParty(); }
    public ActionResult ChooseRoagClass(long actorId, string classId) => Plan.RoagNative
        ? PrototypeCombat.ChooseRoagClass(actorId, classId)
        : new(false, "Choose the starting class before taking your first action.");
    public bool RoagCombatActor(long id) => State.Tactics?.Participants.Contains(id)
        ?? (State.RoagCombat && PrototypeCombat.RoagCombatActor(id));
    private void BeginRoagCombat(long enemyId) => PrototypeCombat.BeginRoagCombat(enemyId);
    private void CaptureRoagRules() => _prototypeCombat?.CaptureRoagRules();

    public IReadOnlyList<RoagFighter> RoagFighters()
    {
        if (HasNativeParty && State.Tactics is { } native)
            return State.Actors.Where(a => native.Participants.Contains(a.Id) && a.Life != LifeState.Dead
                && (State.Party.Contains(a.Id) || Visible.Contains(a.Position)))
                .Select(a => new RoagFighter(a.Id, a.Name, a.Tactics!.Build is { } build ? ClassCatalog.Get(build.Class).Id : a.Tactics.DefinitionId,
                    a.Position, a.Health, a.MaxHealth, State.Party.Contains(a.Id) || !IsHostile(a), a.Id == native.ActiveActor)).ToArray();
        return State.RoagCombat ? PrototypeCombat.RoagFighters() : [];
    }

    public IReadOnlyList<RoagTelegraph> RoagTelegraphs() => HasNativeParty
        ? DungeonCombat.VisibleAttacks(Visible).Select(a => new RoagTelegraph(a.Source, a.Due, a.Cells)).ToArray()
        : State.RoagCombat ? PrototypeCombat.RoagTelegraphs() : [];

    public IReadOnlyList<WorldPosition> RoagPreview(RoagAim aim) => HasNativeParty
        ? PreviewDungeonAim(aim).Cells
        : State.RoagCombat ? PrototypeCombat.RoagPreview(aim) : [];

    public ActionResult SubmitRoag(RoagAim aim) => HasNativeParty
        ? TryDungeonAim(aim, out var intent) ? SubmitDungeon(intent) : new(false, "Choose a valid action and direction.")
        : State.RoagCombat && !State.AwaitingContinuation ? PrototypeCombat.SubmitRoag(aim) : new(false, "No active tile combat.");

    // Narrow bridge for the archived adapter; native tactics uses its own typed
    // world integration. These methods add no production runtime dependency.
    internal void PrototypeAdvanceWorldTick() => AdvanceWorldTick();
    internal void PrototypeCommitDeaths() => CommitDeaths();
    internal void PrototypeAdd(string item, int amount) => Add(item, amount);
    internal void PrototypeLog(string message) => Log(message);
    internal void PrototypeDiagnose(string kind, object data) => Diagnose(kind, data);
}

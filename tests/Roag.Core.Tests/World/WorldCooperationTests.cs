using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class WorldCooperationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-cooperation-" + Guid.NewGuid().ToString("N"));
    private string Slot { get { Directory.CreateDirectory(_directory); return Path.Combine(_directory, "world.json"); } }
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    private static WorldSession Ready(int seed = 42)
    { var s = WorldSession.Create(seed); CooperationPreview.Prepare(s); Assert.Equal(0, s.State.Battle!.Active.Id); return s; }
    private static string State(WorldSession s) => JsonSerializer.Serialize(s.State);
    private static void Step(WorldSession s)
    { Assert.True(s.State.Battle!.Active.Friendly ? s.Act(3).Success : s.AdvanceEnemy().Success); }

    [Theory]
    [InlineData(42)] [InlineData(1337)] [InlineData(-100)]
    public void JointStrikeUsesGeneratedCompanionsAndBothManaPoolsWithAnAccuratePurePreview(int seed)
    {
        var s = Ready(seed); var b = s.State.Battle!; var enemy = b.Units.First(u => !u.Friendly);
        string before = State(s); var preview = s.ActionPreview(6, enemy.Id, 3);
        Assert.Empty(preview.Reason); Assert.Equal(before, State(s));
        int casterMana = b.Units[0].Mana, helperMana = b.Units[3].Mana, hp = enemy.Health;
        Assert.True(s.Act(6, enemy.Id, 3).Success);
        Assert.Equal(casterMana - 3, b.Units[0].Mana); Assert.Equal(helperMana - 3, b.Units[3].Mana);
        Assert.InRange(hp - enemy.Health, preview.Minimum, preview.Maximum); Assert.Contains(3, b.Assisting);
        Assert.Equal(1, b.Active.Id); Assert.Equal(0, s.State.Tick);
        Assert.All(new[] { 0, 3 }, id => Assert.Equal(b.Units[id].Mana, s.State.Actors.Single(a => a.Id == b.ActorIds[id]).Mana));
        Assert.Contains(s.State.CombatEvents, e => e.Label == "Assists Twin strike" && e.Source == b.ActorIds[3]);
    }
    [Fact]
    public void HelperPowerActuallyChangesJointDamage()
    {
        int Damage(int extra)
        {
            var s = Ready(); var b = s.State.Battle!; b.Units[3].Power += extra;
            var enemy = b.Units.First(u => !u.Friendly); int hp = enemy.Health;
            Assert.True(s.Act(6, enemy.Id, 3).Success); return hp - enemy.Health;
        }
        Assert.Equal(Damage(0) + 5, Damage(5));
    }
    [Fact]
    public void CommittedHelperSkipsOnlyTheirActionTicksStatusesAndResumesExactlyAfterLoading()
    {
        var s = Ready(); var b = s.State.Battle!; var helper = b.Units[3]; helper.Burn = 2;
        Assert.True(s.Act(6, 4, 3).Success); int hp = helper.Health;
        s.Save(Slot); var loaded = WorldSession.Load(Slot); Assert.Equal(State(s), State(loaded));
        Assert.Contains("committed", s.CooperationPartnerReason(6, 3));
        Step(s); Step(loaded); Assert.Equal(2, b.Active.Id);
        Step(s); Step(loaded); Assert.False(b.Active.Friendly);
        Assert.Empty(b.Assisting); Assert.Equal(1, helper.Burn); Assert.True(helper.Health < hp);
        Assert.Equal(0, s.State.Tick); Assert.Equal(State(s), State(loaded));
        Assert.Contains(s.State.CombatEvents, e => e.Label == "Assisted" && e.Target == b.ActorIds[3]);
        Step(s); Step(loaded); Assert.Equal(2, b.Round); Assert.Equal(5, s.State.Tick);
        Assert.Empty(s.CooperationPartnerReason(6, 3)); Assert.Equal(State(s), State(loaded));
    }
    [Theory]
    [InlineData("self")] [InlineData("enemy")] [InlineData("downed")] [InlineData("stunned")]
    [InlineData("poor")] [InlineData("past")] [InlineData("committed")] [InlineData("missing")]
    public void InvalidHelpersCannotConsumeManaRandomnessTimeOrAnAction(string invalid)
    {
        var s = Ready(); var b = s.State.Battle!; int helper = 3;
        switch (invalid)
        {
            case "self": helper = 0; break;
            case "enemy": helper = 4; break;
            case "downed": b.Units[helper].Health = 0; break;
            case "stunned": b.Units[helper].Stun = 1; break;
            case "poor": b.Units[helper].Mana = 2; break;
            case "past": Step(s); helper = 0; break;
            case "committed": Assert.True(s.Act(6, 4, helper).Success); break;
            case "missing": helper = -1; break;
        }
        string before = State(s); Assert.NotEmpty(s.CooperationPartnerReason(6, helper));
        Assert.False(s.Act(6, 4, helper).Success); Assert.Equal(before, State(s));
    }
    [Fact]
    public void SharedAidCleansesAndHealsLivingPartyWithoutRevivingDownedMembers()
    {
        var s = Ready(); var b = s.State.Battle!;
        foreach (var unit in b.Units.Where(u => u.Friendly)) { unit.Burn = unit.Bleed = unit.Mark = 2; unit.Stress = 30; }
        b.Units[1].Stun = 2; b.Units[2].Health = 0;
        var downed = s.State.Actors.Single(a => a.Id == b.ActorIds[2]); downed.Health = 0; downed.Life = LifeState.Downed;
        int[] hp = b.Units.Select(u => u.Health).ToArray(); int casterMp = b.Units[0].Mana, helperMp = b.Units[3].Mana;
        var preview = s.ActionPreview(7, partnerIndex: 3); Assert.Empty(preview.Reason);
        Assert.True(s.Act(7, partnerIndex: 3).Success);
        foreach (int id in new[] { 0, 1, 3 })
        {
            var unit = b.Units[id]; Assert.InRange(unit.Health - hp[id], preview.Minimum, preview.Maximum);
            Assert.Equal(0, unit.Burn + unit.Bleed + unit.Mark + unit.Stun); Assert.Equal(20, unit.Stress);
        }
        Assert.Equal(0, b.Units[2].Health); Assert.Equal(casterMp - 4, b.Units[0].Mana); Assert.Equal(helperMp - 3, b.Units[3].Mana);
        Assert.Equal(1, b.Active.Id); Assert.Contains(3, b.Assisting);
    }
    [Theory]
    [InlineData(-1)] [InlineData(0)] [InlineData(1)] [InlineData(4)] [InlineData(99)]
    public void SaveRejectsImpossibleCommittedActions(int id)
    {
        var s = Ready(); Assert.True(s.Act(6, 4, 3).Success); s.State.Battle!.Assisting.Add(id);
        s.Save(Slot); Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
    }
    [Fact]
    public void DownedCommittedHelperStillLoadsAndConsumesNoSecondAction()
    {
        var s = Ready(); Assert.True(s.Act(6, 4, 3).Success); var b = s.State.Battle!;
        b.Units[3].Health = 0; var actor = s.State.Actors.Single(a => a.Id == b.ActorIds[3]); actor.Health = 0; actor.Life = LifeState.Downed;
        s.Save(Slot); var loaded = WorldSession.Load(Slot); Step(s); Step(loaded); Step(s); Step(loaded);
        Assert.Equal(State(s), State(loaded)); Assert.False(b.Active.Friendly); Assert.Empty(b.Assisting);
    }
    [Fact]
    public void ContentToolingPreviewsBothHandlersAndRejectsUnsupportedCooperation()
    {
        foreach (string id in ContentCatalog.Default.Game.CooperativeAbilities)
        {
            var preview = JsonSerializer.SerializeToNode(AbilityPreview.Run(ContentCatalog.Default, id, 42), ContentCatalog.Json)!;
            Assert.True(preview["result"]!["success"]!.GetValue<bool>());
            Assert.Single(preview["after"]!["assisting"]!.AsArray());
            Assert.Contains(ContentCatalog.Default.References, r => r.Source == "game.vagabonds" && r.Target == id);
        }
        var invalid = ContentCatalog.Load(ContentCatalog.Default.Entries.Values.Select(d =>
        {
            var node = JsonSerializer.SerializeToNode<ContentDefinition>(d, ContentCatalog.Json)!;
            if (d.Id == "ability.twin-strike") { node["effect"] = "flee"; node["cooperation"]!["partnerMana"] = 0; }
            return (File: d.Id, Text: node.ToJsonString());
        }));
        Assert.Contains(invalid.Issues, i => i.Field == "cooperation"); Assert.Contains(invalid.Issues, i => i.Field == "cooperation.partnerMana");
    }
}

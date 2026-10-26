using System.Text.Json;
using Roag.Core.Rules;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class RoagIntegrationTests
{
    private static WorldSession Ready(bool party=true)
    {
        var world=WorldSession.Create(42,roagNative:true);
        var hero=world.Party.First();hero.Position=world.State.Position=new(100,100);
        for(int y=95;y<=110;y++)for(int x=95;x<=110;x++)world.Chunks.Edit(new(x,y),Material.Floor);
        if(party)
        {
            var friend=world.State.Actors.First(a=>a.Kind==ActorKind.Person&&!a.Recruited);
            friend.Recruited=true;friend.Position=new(99,100);world.State.Party.Add(friend.Id);
        }
        world.InitializeRoagParty();world.Reveal();
        var enemy=world.State.Actors.First(a=>a.Kind==ActorKind.Raider&&a.EncounterLeader==0);
        enemy.Position=new(104,100);world.StartBattle(enemy.Id);
        Assert.True(world.State.RoagCombat);Assert.Null(world.State.Battle);
        return world;
    }
    [Fact]
    public void NativeWorldOnlyAdvancesFromCommittedActions()
    {
        using var world=WorldSession.Create(42,roagNative:true);world.Paused=false;
        string before=JsonSerializer.Serialize(world.State);
        for(int i=0;i<10;i++)Assert.Equal(0,world.AdvanceFrame(TimeSpan.FromSeconds(1)));
        Assert.Equal(before,JsonSerializer.Serialize(world.State));
        Assert.True(world.Submit(new(WorldCommandKind.Wait,Amount:5)).Success);Assert.Equal(5,world.State.Tick);
    }
    [Fact]
    public void PartyMembersHaveIndependentPositionsAndReadyDecisions()
    {
        using var world=Ready();long first=world.RoagActiveActor;
        var positions=world.Party.ToDictionary(a=>a.Id,a=>a.Position);
        var result=world.SubmitRoag(new("move",0,-1));Assert.True(result.Success,result.Message);
        Assert.NotEqual(first,world.RoagActiveActor);
        Assert.Equal(positions[first].Offset(0,-1),world.State.Actors.Single(a=>a.Id==first).Position);
        Assert.All(world.Party.Where(a=>a.Id!=first),a=>Assert.Equal(positions[a.Id],a.Position));
        Assert.True(world.SubmitRoag(new("wait")).Success);Assert.True(world.State.Tick>0);
    }
    [Fact]
    public void InvalidMoveAndPreviewDoNotSpendTimeOrChangeState()
    {
        using var world=Ready();string before=JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitRoag(new("move",1,1)).Success);
        _=world.RoagPreview(new("basic_attack",1,0));_=world.RoagTelegraphs();
        Assert.Equal(before,JsonSerializer.Serialize(world.State));
    }
    [Fact]
    public void NativeRuleCodecRoundTripsEveryScalarAndNumericKey()
    {
        var runtime=new NativeRuntime();var value=R.Table(new("line","a\nb\0"),new(2,-1.25),new("large",2147483646),new("flag",false));
        var encoded=runtime.Call("src.persistence","encode",value).First;
        var restored=runtime.Call("src.persistence","decode",encoded).First;
        Assert.Equal(encoded,runtime.Call("src.persistence","encode",restored).First);
    }
    [Fact]
    public void AllFifteenClassesUseCompiledOriginalAttackGeometry()
    {
        var runtime=new NativeRuntime();var classes=NativeRuntime.Entries(runtime.Require("src.classes")["order"]).ToArray();Assert.Equal(15,classes.Length);
        foreach(var id in classes)
        {
            var state=runtime.Call("src.game","new",42,V.Nil,R.Table(new Entry("class_id",id))).First;
            var result=runtime.Call("src.game","submit",state,R.Table(new("kind","basic_attack"),new("dx",1),new("dy",0)));
            Assert.True(R.Truth(result.First),id.Text+": "+result[1].Text);
        }
    }
    [Fact]
    public void ChallengeSelectionIsSeededAndDoesNotChangeOnInspection()
    {
        using var a=WorldSession.Create(42,roagNative:true);using var b=WorldSession.Create(42,roagNative:true);
        Assert.NotEmpty(a.State.RoagChallengeSites);Assert.True(a.State.RoagChallengeSites.Count<48);
        Assert.Equal(a.State.RoagChallengeSites.Order(),b.State.RoagChallengeSites.Order());
    }
    [Fact]
    public void EndedWorldCannotCreateAnotherTraveller()
    {
        using var world=WorldSession.Create(42,roagNative:true);world.State.AwaitingContinuation=true;
        string before=JsonSerializer.Serialize(world.State);
        Assert.False(world.Submit(new(WorldCommandKind.Continue)).Success);Assert.Equal(before,JsonSerializer.Serialize(world.State));
    }
    [Fact]
    public void CombatSaveResumesAtTheSamePartyDecision()
    {
        using var world=Ready();Assert.True(world.SubmitRoag(new("wait")).Success);
        string dir=Path.Combine(Path.GetTempPath(),"roag-native-"+Guid.NewGuid().ToString("N"));string slot=Path.Combine(dir,"world.json");
        try
        {
            world.Save(slot);Assert.Equal("roag-world",SessionStore.Format(slot));using var loaded=WorldSession.Load(slot);
            Assert.Equal(world.RoagActiveActor,loaded.RoagActiveActor);Assert.Equal(world.RoagFighters(),loaded.RoagFighters());
            Assert.True(world.SubmitRoag(new("wait")).Success);Assert.True(loaded.SubmitRoag(new("wait")).Success);
            Assert.Equal(world.State.RoagCombatState,loaded.State.RoagCombatState);Assert.Equal(world.State.Tick,loaded.State.Tick);
        }
        finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
}

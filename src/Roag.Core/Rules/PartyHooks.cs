namespace Roag.Core.Rules;

public sealed partial class NativeRuntime
{
    private void RegisterPartyHooks()
    {
        Fn("_roag_party_ready",a=>{
            V state=a[0],item=a[1];
            if(!R.Truth(state["roag_party"])||!R.Truth(item["is_player"]))return new(false);
            var actor=Call("src.game","entity",state,item["actor_id"]).First;
            if(actor.IsNil||actor["hp"].Number<=0)return new(false);
            R.Set(state,"next_player_id",item["actor_id"]);return new(true);
        });
        Fn("_roag_downed",a=>{
            V state=a[0],target=a[1];
            if(!R.Truth(state["roag_party"])||target["controller"].Text!="player")return new(false);
            R.Set(target,"downed",true);R.Set(target,"hp",0);
            Call("src.scheduler","remove_for_actor",state,target["id"]);
            bool survivor=Entries(state["entities"]).Any(e=>e["controller"].Text=="player"&&e["hp"].Number>0);
            if(!survivor)R.Set(state,"game_over",true);
            return new(true);
        });
        Fn("_roag_finish_action",a=>{
            var state=a[0];if(!R.Truth(state["roag_party"]))return Results.Empty;
            SaveMember(state);
            if(!state["next_player_id"].IsNil) {SelectMember(state,state["next_player_id"]);R.Set(state,"next_player_id",V.Nil);}
            return Results.Empty;
        });
    }
    public static IEnumerable<V> Entries(V table)=>table.Data is RuleTable t?Enumerable.Range(1,t.Length).Select(t.GetInt):[];
    public void SaveMember(V state)
    {
        var current=Call("src.game","player",state).First;
        foreach(string field in new[]{"class_special","progression"})R.Set(current,"party_"+field,state[field]);
    }
    public void SelectMember(V state,V id)
    {
        SaveMember(state);
        var member=Call("src.game","entity",state,id).First;
        if(member.IsNil||member["controller"].Text!="player"||member["hp"].Number<=0)throw new InvalidOperationException("Selected party member is not conscious.");
        R.Set(state,"player_id",id);
        foreach(string field in new[]{"class_special","progression"})
        {
            var value=member["party_"+field];
            if(value.IsNil)value=field=="progression"?R.Table(new("level",1),new("xp_total",0)):new RuleTable();
            R.Set(state,field,value);
        }
        // Several original dungeon rules intentionally use the observer in slot one.
        var entities=(RuleTable)state["entities"].Data!;
        for(int i=1;i<=entities.Length;i++)if(entities.Get(i)["id"]==id){var first=entities.Get(1);entities.Set(1,member);entities.Set(i,first);break;}
        Call("src.visibility","recompute",state);
    }
}

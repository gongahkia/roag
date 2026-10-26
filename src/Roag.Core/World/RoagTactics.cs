using Roag.Core.Content;
using Roag.Core.Expeditions;
using Roag.Core.Rules;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed record RoagAim(string Kind, int Dx = 0, int Dy = 0, long Target = 0, int Choice = 0, bool Vow = false);
public sealed record RoagTelegraph(long Source, long Due, IReadOnlyList<WorldPosition> Cells);
public sealed record RoagFighter(long Id, string Name, string Class, WorldPosition Position, int Health, int MaxHealth, bool Friendly, bool Ready);

public sealed partial class WorldSession
{
    private NativeRuntime? _roagRuntime;
    private V _roagState;
    private const int RoagMapSize = 65;
    private NativeRuntime Rules => _roagRuntime ??= new();
    public IReadOnlyList<string> RoagClasses => ClassCatalog.Ids;
    public string RoagClassName(string id) => ClassCatalog.Get(id).Name;
    public long RoagActiveActor => State.RoagCombat ? (long)RoagState["player_id"].Number : State.Party.FirstOrDefault();
    private V RoagState
    {
        get
        {
            if (_roagState.IsNil && State.RoagCombatState.Length > 0)
            {
                _roagState=Rules.Call("src.persistence","decode",State.RoagCombatState).First;
                ValidateRoagSnapshot(_roagState);
            }
            return _roagState;
        }
    }
    private int RuleSeed(long salt) => (int)(WorldRandom.Hash(Seed,unchecked((int)salt),salt:9302)%2147483646)+1;
    private static V Table(params (string Key,V Value)[] fields)=>R.Table(fields.Select(p=>new Entry(p.Key,p.Value)).ToArray());
    private static int Number(V table,string key,int fallback=0)=>table[key].IsNil?fallback:(int)table[key].Number;
    private WorldPosition RulePosition(V position)=>new(State.RoagOriginX+Number(position,"x")-1,State.RoagOriginY+Number(position,"y")-1,State.RoagFloor);
    private V ToRulePosition(WorldPosition p)=>Table(("floor_id",$"roag:world/{p.Floor}"),("x",p.X-State.RoagOriginX+1),("y",p.Y-State.RoagOriginY+1));
    public void InitializeRoagParty()
    {
        if(!Plan.RoagNative)return;
        foreach(var actor in Party)
        {
            if(actor.RoagClass.Length==0)actor.RoagClass=actor.Id==State.Party[0]?RoagClasses[0]:RoagClasses[(int)(WorldRandom.Hash(Seed,(int)actor.Id,salt:9303)%(uint)RoagClasses.Count)];
            if(actor.RoagCharacter.Length>0)continue;
            var fresh=Rules.Call("src.game","new",RuleSeed(actor.Id),V.Nil,Table(("class_id",actor.RoagClass))).First;
            var entity=Rules.Call("src.game","player",fresh).First;
            R.Set(entity,"id",actor.Id);R.Set(entity,"party_class_special",fresh["class_special"]);R.Set(entity,"party_progression",fresh["progression"]);
            double fraction=actor.Health/(double)Math.Max(1,actor.MaxHealth);
            actor.RoagMaxHealth=Number(entity,"max_hp");actor.Health=Math.Clamp((int)Math.Ceiling(actor.RoagMaxHealth*fraction),1,actor.RoagMaxHealth);
            R.Set(entity,"hp",actor.Health);actor.RoagCharacter=Rules.Call("src.persistence","encode",entity).First.Text;
        }
    }
    public ActionResult ChooseRoagClass(long actorId,string classId)
    {
        var actor=Party.FirstOrDefault(a=>a.Id==actorId);
        if(!Plan.RoagNative||IsInCombat||actor==null||!RoagClasses.Contains(classId)||State.Tick>0)
            return new(false,"Choose the starting class before taking your first action.");
        actor.RoagClass=classId;actor.RoagCharacter="";InitializeRoagParty();return new(true,RoagClassName(classId)+" selected.");
    }
    public bool RoagCombatActor(long id)=>!RoagState.IsNil&&NativeRuntime.Entries(RoagState["entities"]).Any(e=>e["id"]==(V)id);
    private void BeginRoagCombat(long enemyId)
    {
        if(IsInCombat||State.AwaitingContinuation||State.Endgame.Victory)return;
        var enemy=State.Actors.FirstOrDefault(a=>a.Id==enemyId&&a.Life==LifeState.Alive&&IsHostile(a));if(enemy==null)return;
        InitializeRoagParty();
        State.RoagOriginX=Math.Clamp(State.Position.X-RoagMapSize/2,0,WorldPlan.Size-RoagMapSize);
        State.RoagOriginY=Math.Clamp(State.Position.Y-RoagMapSize/2,0,WorldPlan.Size-RoagMapSize);State.RoagFloor=State.Position.Floor;
        var state=Rules.Call("src.game","new",RuleSeed(State.Tick+enemyId)).First;
        _roagState=state;R.Set(state,"roag_party",true);R.Set(state,"active_floor_id",$"roag:world/{State.RoagFloor}");
        R.Set(state,"clock",State.Tick*20);R.Set(state,"entities",new RuleTable());R.Set(state,"schedule",new RuleTable());
        R.Set(state,"available_boons",Rules.Call("src.boon_content","normal_order").First);
        R.Set(state,"currency",Count("gold"));R.Set(state,"next_entity_id",Math.Max(1000000,State.NextActorId+10000));
        RefreshRoagTerrain(state);
        foreach(var actor in Party.Where(a=>a.Life!=LifeState.Dead))
        {
            var entity=Rules.Call("src.persistence","decode",actor.RoagCharacter).First;
            R.Set(entity,"position",ToRulePosition(actor.Position));R.Set(entity,"hp",actor.Health);R.Set(entity,"max_hp",actor.MaxHealth);
            Append(state["entities"],entity);
            if(actor.Life==LifeState.Alive)EnqueueRoag(actor.Id,State.Tick*20,true);
        }
        var opponents=State.Actors.Where(a=>a.Life==LifeState.Alive&&IsHostile(a)&&a.Position.Floor==State.Position.Floor&&Party.Any(p=>p.Position.Distance(a.Position)<=16)).OrderBy(a=>a.Id).ToArray();
        var ids=NativeRuntime.Entries(Rules.Require("src.biomes")["order"]).ToArray();
        foreach(var actor in opponents)
        {
            var biome=Rules.Call("src.biomes","get",ids[(int)(WorldRandom.Hash(Seed,(int)actor.Id,salt:9304)%(uint)ids.Length)]).First;
            bool boss=Content.Entries.GetValueOrDefault(actor.Archetype) is BossDefinition;
            var choices=NativeRuntime.Entries(biome["enemy_ids"]).ToArray();
            string definition=boss?biome["elite_id"].Text:choices[(int)(WorldRandom.Hash(Seed,(int)actor.Id,salt:9305)%(uint)choices.Length)].Text;
            var pos=ToRulePosition(actor.Position);
            var entity=Rules.Call("src.game","spawn_enemy",state,definition,pos["x"],pos["y"],State.Tick*20+100).First;
            var oldId=entity["id"];Rules.Call("src.scheduler","remove_for_actor",state,oldId);R.Set(entity,"id",actor.Id);
            if(actor.RoagCharacter.Length>0)
            {
                var previous=Rules.Call("src.persistence","decode",actor.RoagCharacter).First;
                foreach(var pair in (RuleTable)previous.Data!)if(pair.Key.Text is not ("position" or "pending_attack" or "id"))R.Set(entity,pair.Key,pair.Value);
            }
            else {actor.RoagMaxHealth=Number(entity,"max_hp");actor.Health=Math.Min(actor.Health,actor.RoagMaxHealth);}
            R.Set(entity,"hp",actor.Health);EnqueueRoag(actor.Id,State.Tick*20+100,false);
        }
        var ready=NativeRuntime.Entries(state["entities"]).First(e=>e["controller"].Text=="player"&&Number(e,"hp")>0);
        R.Set(state,"player_id",ready["id"]);Rules.SelectMember(state,ready["id"]);
        State.RoagCombat=true;State.Battle=null;State.CombatEvents.Clear();State.LastEncounterReward=null;
        Diagnose("roag-combat-start",new { enemyId, active=RoagActiveActor, position=State.Position });
    }
    private static void Append(V array,V value)=>R.Set(array,R.Length(array)+1,value);
    private void EnqueueRoag(long id,long due,bool friendly)=>Rules.Call("src.scheduler","enqueue",_roagState,
        Table(("kind","actor_ready"),("actor_id",id),("due",due),("is_player",friendly)));
    private void RefreshRoagTerrain(V state)
    {
        V rows=new RuleTable(),art=new RuleTable();
        for(int y=0;y<RoagMapSize;y++)
        {
            var chars=new char[RoagMapSize];
            for(int x=0;x<RoagMapSize;x++)
            {
                var p=new WorldPosition(State.RoagOriginX+x,State.RoagOriginY+y,State.RoagFloor);
                var material=Chunks.At(p);
                chars[x]=material==Material.Water?'~':GroundOpen(p)?'.':'#';
            }
            R.Set(rows,y+1,new string(chars));R.Set(art,y+1,new string('s',RoagMapSize));
        }
        var map=Table(("id","roag:world"),("floor_id",$"roag:world/{State.RoagFloor}"),("width",RoagMapSize),("height",RoagMapSize),
            ("rows",rows),("visual_rows",art),("rooms",new RuleTable()),("placements",Table(("stairs",new RuleTable()),("secrets",new RuleTable()))),
            ("start_x",State.Position.X-State.RoagOriginX+1),("start_y",State.Position.Y-State.RoagOriginY+1));
        R.Set(state,"generated_map",map);
    }
    public IReadOnlyList<RoagFighter> RoagFighters()
    {
        if(!State.RoagCombat)return [];
        return NativeRuntime.Entries(RoagState["entities"]).Where(e=>!e["hp"].IsNil).Select(e=>{
            long id=(long)e["id"].Number;var actor=State.Actors.FirstOrDefault(a=>a.Id==id);
            return new RoagFighter(id,actor?.Name??e["definition_id"].Text,e["definition_id"].Text,RulePosition(e["position"]),Number(e,"hp"),Number(e,"max_hp"),e["controller"].Text=="player",id==RoagActiveActor);
        }).ToArray();
    }
    public IReadOnlyList<RoagTelegraph> RoagTelegraphs()=>!State.RoagCombat?[]:NativeRuntime.Entries(Rules.Call("src.game","pending_strikes",RoagState).First)
        .Select(p=>new RoagTelegraph((long)p["source_id"].Number,(long)p["due"].Number,NativeRuntime.Entries(p["target_cells"]).Select(RulePosition).ToArray())).ToArray();
    public IReadOnlyList<WorldPosition> RoagPreview(RoagAim aim)
    {
        if(!State.RoagCombat)return [];
        var preview=Rules.Call("src.game","preview",RoagState,aim.Kind,aim.Dx,aim.Dy).First;
        return NativeRuntime.Entries(preview["cells"]).Select(RulePosition).ToArray();
    }
    public ActionResult SubmitRoag(RoagAim aim)
    {
        if(!State.RoagCombat||State.AwaitingContinuation)return new(false,"No active tile combat.");
        var state=RoagState;var player=Rules.Call("src.game","player",state).First;
        if(aim.Kind=="revive")
        {
            var target=Rules.Call("src.game","entity",state,aim.Target).First;
            if(target.IsNil||target["controller"].Text!="player"||Number(target,"hp")>0||Count("medicine")<1||RulePosition(target["position"]).Distance(RulePosition(player["position"]))>1)
                return new(false,"Stand beside a downed companion and spend one medicine to rescue them.");
            Add("medicine",-1);R.Set(target,"hp",Math.Max(1,Number(target,"max_hp")/3));R.Set(target,"downed",false);
            EnqueueRoag(aim.Target,(long)state["clock"].Number+100,true);aim=aim with {Kind="wait"};
        }
        long before=(long)state["clock"].Number;
        var intent=Table(("kind",aim.Kind),("dx",aim.Dx),("dy",aim.Dy),("vow",aim.Vow),("chest_id",aim.Target),("offer_index",aim.Choice));
        var result=Rules.Call("src.game","submit",state,intent);
        if(!R.Truth(result[0]))return new(false,result[1].Text);
        Rules.SaveMember(state);
        SyncRoagActors(result[2]);
        long elapsed=(long)state["clock"].Number-before;
        for(long i=0;i<elapsed/20&&!State.AwaitingContinuation;i++)AdvanceWorldTick();
        State.Inventory["gold"]=Number(state,"currency");
        if(R.Truth(state["game_over"]))
        {
            foreach(var actor in Party){actor.Life=LifeState.Dead;actor.Health=0;}
            State.AwaitingContinuation=true;State.RoagCombat=false;CommitDeaths();
        }
        else if(!NativeRuntime.Entries(state["entities"]).Any(e=>e["controller"].Text=="ai"&&Number(e,"hp")>0))
        {
            State.RoagCombat=false;Log("Encounter won. Rescue fallen companions with medicine before leaving them behind.");
        }
        var active=State.Actors.FirstOrDefault(a=>a.Id==(long)state["player_id"].Number&&a.Life==LifeState.Alive);
        if(active!=null){State.Position=active.Position;State.Party.Remove(active.Id);State.Party.Insert(0,active.Id);}
        Reveal();CaptureRoagRules();
        return new(true,State.AwaitingContinuation?"The party has fallen. Begin a fresh world.":State.RoagCombat?active?.Name+" is ready.":"Encounter won.");
    }
    private void SyncRoagActors(V events)
    {
        foreach(var entity in NativeRuntime.Entries(_roagState["entities"]))
        {
            var actor=State.Actors.FirstOrDefault(a=>a.Id==(long)entity["id"].Number);if(actor==null||entity["hp"].IsNil)continue;
            var next=RulePosition(entity["position"]);if(actor.Position!=next)CommitClimateMove(actor,next);
            actor.RoagMaxHealth=Number(entity,"max_hp");actor.Health=Number(entity,"hp");
            actor.Life=actor.Health>0?LifeState.Alive:actor.Recruited?LifeState.Downed:LifeState.Dead;
            actor.RoagCharacter=Rules.Call("src.persistence","encode",entity).First.Text;
        }
        foreach(var ev in NativeRuntime.Entries(events))
        {
            string kind=ev["kind"].Text;
            if(kind=="EntityKilled")
            {
                var actor=State.Actors.FirstOrDefault(a=>a.Id==Number(ev,"target_id"));
                if(actor!=null&&!actor.Recruited){actor.Health=0;actor.Life=LifeState.Dead;}
            }
        }
    }
    private void ValidateRoagSnapshot(V state)
    {
        if(state.Data is not RuleTable||!R.Truth(state["roag_party"])||state["schema"]!=Rules.Require("src.game")["snapshot_schema"]||state["entities"].Data is not RuleTable||state["schedule"].Data is not RuleTable)
            throw new InvalidDataException("Invalid native Roag combat snapshot.");
        var ids=new HashSet<long>();
        foreach(var entity in NativeRuntime.Entries(state["entities"]))
        {
            long id=(long)entity["id"].Number;
            if(!ids.Add(id)||id<1||!Plan.Contains(RulePosition(entity["position"])))throw new InvalidDataException("Invalid native combat actor.");
        }
        if(!ids.Contains((long)state["player_id"].Number))throw new InvalidDataException("Missing native combat actor.");
    }
    private void CaptureRoagRules()
    {if(!_roagState.IsNil)State.RoagCombatState=Rules.Call("src.persistence","encode",_roagState).First.Text;}
}

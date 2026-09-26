"""Finite mechanical regional situations with selected-pack presentation."""
from __future__ import annotations
from dataclasses import dataclass, fields
from .catalog import CatalogError, load_catalog
from .state import GameState, Position, Region
from .visuals import SITE_SYMBOLS
from .situation_presentation import situation_format, situation_slot, situation_text

@dataclass(frozen=True)
class Situation:
    id: str
    region_id: str
    band: str
    anchor: str
    material_effects: tuple[str, ...]
    @property
    def name(self) -> str: return situation_slot(self.id, "title")
    @property
    def groups(self) -> tuple[str, str]: return (situation_slot(self.id, "group.0"), situation_slot(self.id, "group.1"))
    @property
    def duty(self) -> str: return situation_slot(self.id, "duty")
    @property
    def material(self) -> str: return situation_slot(self.id, "material")
    @property
    def answers(self) -> tuple[str, str, str]: return tuple(situation_slot(self.id, f"choice.{choice}") for choice in ("tool", "material", "account"))
    @property
    def consequence(self) -> str: return situation_slot(self.id, "consequence")

_CATALOG=load_catalog("situations.json",("situations","afterwork_samples")); _FIELDS={f.name for f in fields(Situation)}
_EFFECTS=frozenset({"water_shallow","water_deep","water_salt","heat_timber","heat_resin","smoke","unstable","lime","resin","soil"})
def _from_data(row: object)->Situation:
    if not isinstance(row,dict) or set(row)!=_FIELDS or not all(isinstance(row[key],str) and row[key] for key in ("id","region_id","band","anchor")) or not isinstance(row["material_effects"],list) or any(effect not in _EFFECTS for effect in row["material_effects"]): raise CatalogError("situations.json has an invalid mechanical situation record")
    return Situation(**{**row,"material_effects":tuple(row["material_effects"])})
if not isinstance(_CATALOG["situations"],list): raise CatalogError("situations.json must provide situation records")
SITUATIONS=tuple(_from_data(row) for row in _CATALOG["situations"])
if not isinstance(_CATALOG["afterwork_samples"],dict) or any(not isinstance(region,str) or not isinstance(sample,str) for region,sample in _CATALOG["afterwork_samples"].items()): raise CatalogError("situations.json has invalid afterwork samples")
AFTERWORK_SAMPLES=_CATALOG["afterwork_samples"]
BY_ID={row.id:row for row in SITUATIONS}; BY_REGION_BAND={(row.region_id,row.band):row for row in SITUATIONS}
def _key(row:Situation,part:str)->str:return f"micro-site:{part}:{row.id}"
def _choice_id(method:str,*,spent:str|None=None)->str:
    return {"t":"tool","a":"account","m":f"material_{spent}" if spent else "material"}[method]
def _outcome_text(row:Situation,outcome_id:object,outcome:object="")->str:
    if outcome_id=="tool": return situation_slot(row.id,"choice.tool")
    if outcome_id=="account": return situation_slot(row.id,"choice.account")
    if outcome_id in {"material_rope","material_oil"}: return situation_format("situation.outcome.material",choice=situation_slot(row.id,"choice.material"),spent=str(outcome_id).partition("_")[2])
    return str(outcome)
_CONDITION_EVENT_TEXT={
    "water and stone":"situation.condition.event.water_and_stone", "flood":"situation.condition.event.flood", "fire":"situation.condition.event.fire", "support loss":"situation.condition.event.support_loss", "shared repair":"situation.condition.event.shared_repair", "private advance":"situation.condition.event.private_advance", "contested occupation":"situation.condition.event.contested_occupation", "unsettled account":"situation.condition.event.unsettled_account", None:"situation.condition.event.none",
}
_CONDITION_AFTERMATH_TEXT={"shared":"situation.condition.aftermath.shared", "claimed":"situation.condition.aftermath.claimed", None:"situation.condition.aftermath.none"}
def _condition(state:GameState)->dict[str,str|None]:
    from .calendar import calendar_at
    return {"season":calendar_at(state).season,"event":state.region.regional_history[-1].kind if state.region.regional_history else None,"aftermath":state.region.changes.get("aftermath_configuration")}
def _condition_text(value:object)->str:
    if isinstance(value,dict) and set(value)=={"season","event","aftermath"}:
        event=value["event"] if isinstance(value["event"],str) else None; aftermath=value["aftermath"] if isinstance(value["aftermath"],str) else None
        return situation_format("situation.condition",season=str(value["season"]),event=situation_text(_CONDITION_EVENT_TEXT.get(event,_CONDITION_EVENT_TEXT[None])),aftermath=situation_text(_CONDITION_AFTERMATH_TEXT.get(aftermath,_CONDITION_AFTERMATH_TEXT[None])))
    return str(value)
def _site_point_for_region(region:Region,row:Situation)->Position:
    stored=region.changes.get(_key(row,"point"))
    if isinstance(stored,str): return Position(*map(int,stored.split(",")))
    anchor=region.landmarks[row.anchor]; forbidden={c.position for c in region.containers}|set(region.landmarks.values()); forbidden|={p for link in region.vertical_links for p in(link.first,link.second)}
    from collections import deque
    rows=region.levels[str(anchor.z)]; queue,seen,candidates=deque([anchor]),{anchor},[]
    while queue and not candidates:
        for _ in range(len(queue)):
            current=queue.popleft()
            if current not in forbidden and abs(current.x-anchor.x)+abs(current.y-anchor.y)>=3 and all(other.z!=current.z or max(abs(other.x-current.x),abs(other.y-current.y))>2 for other in region.landmarks.values()) and all(other.position.z!=current.z or max(abs(other.position.x-current.x),abs(other.position.y-current.y))>1 for other in region.containers): candidates.append(current)
            for dx,dy in ((0,-1),(1,0),(0,1),(-1,0)):
                point=Position(current.x+dx,current.y+dy,current.z)
                if point in seen or not(0<=point.y<len(rows) and 0<=point.x<len(rows[point.y])) or region.tile_changes.get(f"{point.x},{point.y},{point.z}",rows[point.y][point.x]) in {" ","#","~","T"}: continue
                seen.add(point); queue.append(point)
        if len(seen)>256: break
    if not candidates: raise RuntimeError(f"{row.id} has no safe local site near {row.anchor}")
    point=min(candidates,key=lambda p:(abs(p.x-anchor.x)+abs(p.y-anchor.y),(p.x*17+p.y*31+len(row.id))%11,p.y,p.x)); region.changes[_key(row,"point")]=f"{point.x},{point.y},{point.z}"
    from .world import position_key
    region.tile_changes[position_key(point)]="?"; return point
def site_point(state:GameState,row:Situation)->Position:return _site_point_for_region(state.region,row)
def initialise_region_sites(region:Region)->None:
    for row in SITUATIONS:
        if row.region_id==region.id:_site_point_for_region(region,row)
def site_at(state:GameState,point:Position,*,adjacent:bool=False)->Situation|None:
    if state.location!="region":return None
    return next((row for row in SITUATIONS if row.region_id==state.active_region_id and (site:=site_point(state,row)).z==point.z and max(abs(site.x-point.x),abs(site.y-point.y))<=int(adjacent)),None)
def site_glyph(state:GameState,point:Position)->str|None:
    row=site_at(state,point)
    if not row:return None
    return SITE_SYMBOLS["resolved"] if state.region.changes.get(_key(row,"resolved")) else SITE_SYMBOLS["active"] if state.region.changes.get("situation:active")==row.id else SITE_SYMBOLS["inactive"]
def _prepare_material(state:GameState,row:Situation,point:Position)->None:
    from .calendar import calendar_at
    from .materials import ensure_cell
    cell=ensure_cell(state,point)
    if cell is None:return
    effects=set(row.material_effects)
    if "water_deep" in effects or "water_shallow" in effects: cell.water=max(cell.water,2 if "water_deep" in effects else 1);cell.fluid="salt" if "water_salt" in effects else "fresh"
    if "heat_timber" in effects or "heat_resin" in effects:cell.material,cell.fuel,cell.fire=("timber" if "heat_timber" in effects else "resin"),3,1
    if "smoke" in effects:cell.smoke=max(cell.smoke,2)
    if "unstable" in effects:cell.support=min(cell.support,1)
    if "lime" in effects:cell.material,cell.coating="lime","lime"
    elif "resin" in effects:cell.material,cell.coating="resin","resin"
    elif "soil" in effects:cell.material="soil"
    if calendar_at(state).season=="winter" and cell.water and cell.fluid=="fresh":cell.ice=True
    if state.region.regional_history:
        last=state.region.regional_history[-1].kind
        if last=="fire":cell.smoke=max(cell.smoke,1)
        elif last=="flood":cell.water=min(3,cell.water+1)
    if state.region.changes.get("aftermath_configuration")=="shared":cell.support=min(3,cell.support+1)
    state.region.changes[_key(row,"material")]={"water":cell.water,"fire":cell.fire,"smoke":cell.smoke,"support":cell.support,"ice":cell.ice}
def activate_for_band(state:GameState,band:str)->Situation|None:
    if state.location!="region" or (state.active_region_id,band) not in BY_REGION_BAND:return None
    row=BY_REGION_BAND[state.active_region_id,band];point=site_point(state,row)
    if state.region.changes.get(_key(row,"resolved")):return row
    previous_id=state.region.changes.get("situation:active")
    if isinstance(previous_id,str) and previous_id in BY_ID and previous_id!=row.id:
        from .world import position_key
        previous=BY_ID[previous_id];state.region.tile_changes[position_key(site_point(state,previous))]="*" if state.region.changes.get(_key(previous,"resolved")) else "?"
    state.region.changes["situation:active"]=row.id
    from .world import position_key
    state.region.tile_changes[position_key(point)]="!";_prepare_material(state,row,point);state.region.changes.setdefault(_key(row,"condition"),_condition(state))
    candidates=sorted((a for a in state.combatants if a.status in {"watching","dormant"}),key=lambda a:(a.group,a.id));chosen=[]
    for actor in candidates:
        if not chosen or actor.group!=chosen[0].group:chosen.append(actor)
        if len(chosen)==2:break
    for index,actor in enumerate(chosen):
        actor.status,actor.objective_position="watching",point; actor.intent_id="situation.work";actor.intent=situation_format("situation.intent.work",title=row.name,material=row.material);actor.goal_reason=situation_format("situation.reason.stake",group=row.groups[index])
    state.region.changes[_key(row,"participants")]=",".join(actor.id for actor in chosen)
    if not state.region.changes.get(_key(row,"seen")):
        state.region.changes[_key(row,"seen")]=True;state.add_message(situation_format("situation.notice.activation",title=row.name,duty=row.duty,material=row.material),priority=3)
    return row
def inspect_lines(state:GameState,situation_id:str)->list[str]:
    row=BY_ID[situation_id];point=site_point(state,row);outcome=state.region.changes.get(_key(row,"outcome"));outcome_id=state.region.changes.get(_key(row,"outcome_id"));condition=_condition_text(state.region.changes.get(_key(row,"condition"),_condition(state)))
    if outcome or outcome_id:
        afterwork=state.region.changes.get(_key(row,"afterwork"));lines=[situation_format("situation.inspect.resolved.fact",title=row.name,x=point.x,y=point.y,z=f"{point.z:+d}"),situation_format("situation.inspect.resolved.answer",outcome=_outcome_text(row,outcome_id,outcome)),situation_format("situation.inspect.resolved.effect",consequence=row.consequence),situation_format("situation.inspect.resolved.return",condition=condition)]
        if afterwork:lines.append(situation_format("situation.inspect.afterwork",afterwork=afterwork))
        else:lines.extend((situation_text("situation.inspect.followup"),situation_format("situation.inspect.sample",sample=AFTERWORK_SAMPLES[row.region_id])))
        lines.append(situation_format("situation.inspect.report",report=state.region.changes.get(_key(row,"report"),situation_text("situation.report.unfiled"))))
        return lines+[situation_text("situation.inspect.time")]
    return [situation_format("situation.inspect.visible",title=row.name,x=point.x,y=point.y,z=f"{point.z:+d}"),situation_format("situation.inspect.groups",first=row.groups[0],second=row.groups[1]),situation_format("situation.inspect.duty",duty=row.duty),situation_format("situation.inspect.material",material=row.material),situation_format("situation.inspect.condition",condition=condition),situation_format("situation.inspect.choice.tool",choice=situation_slot(row.id,"choice.tool")),situation_format("situation.inspect.choice.material",choice=situation_slot(row.id,"choice.material")),situation_format("situation.inspect.choice.account",choice=situation_slot(row.id,"choice.account")),situation_text("situation.inspect.guidance")]
def choices(state:GameState,situation_id:str)->list[tuple[str,str,str,bool,str]]:
    row=BY_ID[situation_id];tool=state.gear=="repair tools" or state.weapon in {"spade","boat hook","billhook","hand axe","felling axe","war hammer","cudgel"} or bool(state.courier and state.courier.technique=="lever craft")
    if state.region.changes.get(_key(row,"resolved")):
        if state.region.changes.get(_key(row,"afterwork")):return []
        return [("T",situation_text("situation.choice.maintain"),"commitment",tool,situation_text("situation.choice.maintain.requirement")),("M",situation_format("situation.choice.sample",sample=AFTERWORK_SAMPLES[row.region_id]),"commitment",True,situation_text("situation.choice.sample.requirement"))]
    if state.region.changes.get("situation:active")!=row.id:return []
    material=state.rope_uses>0 or state.lamp_oil>0;account=state.contact.disposition>=0 or bool(state.carried_goods.get(state.region.objective_commodity))
    return [("T",situation_slot(row.id,"choice.tool").title(),"commitment",tool,situation_text("situation.choice.tool.requirement")),("M",situation_slot(row.id,"choice.material").title(),"commitment",material,situation_text("situation.choice.material.requirement")),("A",situation_slot(row.id,"choice.account").title(),"commitment",account,situation_text("situation.choice.account.requirement"))]
def resolve(state:GameState,situation_id:str,method:str)->tuple[bool,str,int]:
    row=BY_ID.get(situation_id)
    if not row or row.region_id!=state.active_region_id:return False,situation_text("situation.resolve.missing"),0
    available={key.lower():(ok,reason) for key,_,_,ok,reason in choices(state,situation_id)}
    if method not in available:return False,situation_text("situation.resolve.settled"),0
    okay,requirement=available[method]
    if not okay:return False,situation_format("situation.resolve.requirement",requirement=requirement),0
    point=site_point(state,row)
    if state.region.changes.get(_key(row,"resolved")):
        from .materials import ensure_cell
        if method=="m":
            from .inventory import InventoryTransaction,auto_place,create_item
            transaction=InventoryTransaction.begin(state);sample=AFTERWORK_SAMPLES[row.region_id];item=create_item(state,f"ingredient:{sample}",situation_format("situation.afterwork.provenance",title=row.name))
            if not auto_place(state,item.id,"pack",owner_id=state.active_courier_id):transaction.cancel(state);return False,situation_text("situation.resolve.pack"),0
            result,steps=situation_format("situation.afterwork.sample",sample=sample),1
        else:
            cell=ensure_cell(state,point)
            if cell:cell.support,cell.collapse_due,cell.fire,cell.smoke=3,0,0,0
            account=state.institutions.get(f"work:{row.region_id}")
            if account:account.confidence=min(3,account.confidence+1)
            result,steps=situation_text("situation.afterwork.maintenance"),2
        state.region.changes[_key(row,"afterwork")]="sample" if method=="m" else "maintenance";record=situation_format("situation.record.afterwork",title=row.name,courier=state.courier.name,result=result,outcome=_outcome_text(row,state.region.changes.get(_key(row,"outcome_id")),state.region.changes.get(_key(row,"outcome")))) ;state.remember(record);return True,record,steps
    from .materials import ensure_cell
    cell=ensure_cell(state,point);account=state.institutions.get(f"work:{row.region_id}");spent=None
    if method=="t":
        if cell:cell.support,cell.collapse_due,cell.fire,cell.fuel,cell.water=3,0,0,0,max(0,cell.water-1)
        for actor in state.combatants:
            if actor.objective_position==point:actor.morale-=1;actor.intent_id="situation.completed";actor.intent=situation_text("situation.intent.completed")
        outcome_id,steps="tool",2
    elif method=="m":
        if state.rope_uses:
            state.rope_uses-=1;spent="rope"
            if cell:cell.support,cell.coating=min(3,cell.support+1),"wet";cell.fire,cell.smoke=0,max(0,cell.smoke-2)
        else:
            state.lamp_oil-=1;spent="oil"
            if cell:cell.material,cell.coating,cell.fuel="resin","oil",max(2,cell.fuel);cell.water=max(0,cell.water-1)
        state.region.changes[f"population-shift:{row.id}"]="group.1";outcome_id,steps=_choice_id(method,spent=spent),1
    else:
        if account:account.trust=min(3,account.trust+1);account.witnessed_acts.append(situation_format("situation.record.resolved",title=row.name,outcome=situation_slot(row.id,"choice.account"),consequence=row.consequence));del account.witnessed_acts[:-12]
        state.contact.disposition=min(3,state.contact.disposition+1);state.market[state.region.objective_commodity].demand=max(0,state.market[state.region.objective_commodity].demand-1)
        for actor in state.combatants:
            if actor.objective_position==point and actor.profile!="animal":actor.status="negotiated";actor.intent_id="situation.negotiated";actor.intent=situation_text("situation.intent.negotiated")
        if cell:cell.fire,cell.smoke,cell.water,cell.support=0,0,min(1,cell.water),max(2,cell.support)
        outcome_id,steps="account",1
    outcome=_outcome_text(row,outcome_id);state.region.changes[_key(row,"resolved")]=True;state.region.changes[_key(row,"outcome_id")]=outcome_id;state.region.changes[_key(row,"outcome")]=outcome;state.region.changes[_key(row,"revisit")]=row.consequence;state.region.changes.pop("situation:active",None)
    from .world import position_key
    state.region.tile_changes[position_key(point)]="*";record=situation_format("situation.record.resolved",title=row.name,outcome=outcome,consequence=row.consequence);state.remember(record);state.contact.memories.append(record);del state.contact.memories[:-8]
    from .state import append_narrative_record
    append_narrative_record(state,event_id="situation.resolved",refs={"situation_id":row.id,"region_id":row.region_id,"outcome_id":outcome_id},params={"world_time":state.world_time,"steps":steps},rendered=record)
    return True,record,steps
def interaction(state:GameState)->str|None:
    row=site_at(state,state.position,adjacent=True);return f"situation:{row.id}" if row else None
def validate_situations()->None:
    if len(SITUATIONS)!=24 or len(BY_ID)!=24:raise ValueError("exactly 24 distinct situations are required")
    for region in {row.region_id for row in SITUATIONS}:
        if {row.band for row in SITUATIONS if row.region_id==region}!={"steady","strained","critical"}:raise ValueError(f"{region} needs all pressure bands")
    if len({row.region_id for row in SITUATIONS}) != 8: raise ValueError("situations need eight regions")
    from .production import SOURCES
    if set(AFTERWORK_SAMPLES)!=set(SOURCES) or any(sample not in SOURCES[region] for region,sample in AFTERWORK_SAMPLES.items()):raise ValueError("afterwork samples need a regional physical source")
def validate_situation_state(state:GameState)->None:
    for region_id,region in state.regions.items():
        active=region.changes.get("situation:active")
        if active is not None and (active not in BY_ID or BY_ID[str(active)].region_id!=region_id):raise ValueError("active situation does not belong to its region")
        for key_name,value in region.changes.items():
            for part,valid in (("afterwork",{"sample","maintenance"}),("report",{"public","private"}),("outcome_id",{"tool","account","material_rope","material_oil"})):
                prefix=f"micro-site:{part}:"
                if key_name.startswith(prefix):
                    situation_id=key_name[len(prefix):]
                    if situation_id not in BY_ID or BY_ID[situation_id].region_id!=region_id or not isinstance(value,str) or value not in valid or not region.changes.get(f"micro-site:resolved:{situation_id}"):raise ValueError("invalid changed-site follow-up")
            if key_name.startswith("micro-site:point:"):
                situation_id=key_name.split("micro-site:point:",1)[1]
                if situation_id not in BY_ID or BY_ID[situation_id].region_id!=region_id or not isinstance(value,str):raise ValueError("invalid mutable site identity")
                try:point=Position(*map(int,value.split(",")))
                except (TypeError,ValueError):raise ValueError("invalid mutable site position") from None
                if str(point.z) not in region.levels or not(0<=point.x<region.width and 0<=point.y<region.height):raise ValueError("mutable site lies outside its region")
def audit_situations(samples:int=200)->dict[str,object]:
    from collections import Counter
    from .state import stage_rng
    occurrences=Counter();signatures=set()
    for index in range(samples):
        rows=list(SITUATIONS);stage_rng(f"situation-audit-{index}","situation-order").shuffle(rows)
        for row in rows:occurrences[row.id]+=1;signatures.add((row.region_id,row.band,row.anchor,row.material_effects))
    total=sum(occurrences.values());share=max(occurrences.values(),default=0)/total if total else 0.;failures=[]
    if len(occurrences)!=24 or len(signatures)!=24:failures.append("not every mechanical situation occurred distinctly")
    if share>.10:failures.append("one situation exceeded ten percent of all opportunities")
    return {"samples":samples,"opportunities":total,"situations":len(occurrences),"distinct_signatures":len(signatures),"max_share":round(share,4),"failures":failures}

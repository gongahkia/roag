"""Immutable renderer-neutral projections of generic game state."""
from __future__ import annotations
from dataclasses import dataclass
from .catalog import selected_content_pack
from .state import Actor, GameState, Position
@dataclass(frozen=True)
class CellView: position:Position; terrain_id:str; visible:bool; remembered:bool; actor_ids:tuple[str,...]=()
@dataclass(frozen=True)
class ActorView: id:str; presentation_id:str; actor_kind:str; position:Position; health:int; maximum_health:int; alive:bool
@dataclass(frozen=True)
class ItemView: id:str; kind_id:str; display_name:str; description:str; quantity:int; equipped:bool; legal_operations:tuple[str,...]
@dataclass(frozen=True)
class QuestView: id:str; title:str; objective:str; state_id:str; progress:int
@dataclass(frozen=True)
class RouteView: id:str; display_name:str; turns:int; available:bool=True
@dataclass(frozen=True)
class RecipeView: id:str; display_name:str; input_id:str; output_id:str; available:bool=True
@dataclass(frozen=True)
class SetupOptionView: id:str; display_name:str
@dataclass(frozen=True)
class CharacterSetupView: crew:tuple[SetupOptionView,...]; ancestries:tuple[SetupOptionView,...]; origins:tuple[SetupOptionView,...]; traits:tuple[SetupOptionView,...]
@dataclass(frozen=True)
class WorldView: width:int; height:int; cells:tuple[CellView,...]; courier_position:Position

def _visible(state:GameState, point:Position)->bool:return abs(point.x-state.position.x)+abs(point.y-state.position.y)<=5
def world_view(state:GameState)->WorldView:
    cells=[]
    actors={actor.position:actor.id for actor in state.actors if actor.alive}
    for y,row in enumerate(state.rows):
      for x,tile in enumerate(row):
        p=Position(x,y); visible=_visible(state,p); remembered=p in state.remembered
        cells.append(CellView(p,"terrain.wall" if tile=="#" else "terrain.floor",visible,remembered,(actors[p],) if visible and p in actors else ()))
    return WorldView(len(state.rows[0]),len(state.rows),tuple(cells),state.position)
def actor_views(state:GameState)->tuple[ActorView,...]:
    rows=[state.courier,*state.actors]
    return tuple(ActorView(row.id,row.id,row.kind,row.position,row.health,row.maximum_health,row.alive) for row in rows)
def inventory_view(state:GameState)->tuple[ItemView,...]:
    pack=selected_content_pack(); definitions={row["id"]:row for row in pack.systems["items"]}
    return tuple(ItemView(item.id,item.kind,definitions.get(item.kind,{}).get("name",item.kind),definitions.get(item.kind,{}).get("description","") ,item.quantity,item.equipped,("unequip",) if item.equipped else ("equip",)) for item in state.items)
def quest_views(state:GameState)->tuple[QuestView,...]:
    rows={row["id"]:row for row in selected_content_pack().systems["quests"]}
    return tuple(QuestView(q.id,str(rows[q.id].get("title",q.id)),str(rows[q.id].get("objective","")),q.state,q.progress) for q in state.quests)
def travel_view(state:GameState)->tuple[RouteView,...]:
    return tuple(RouteView(row["id"],str(row.get("name",row["id"])),int(row.get("turns",1))) for row in selected_content_pack().systems["routes"])
def recipe_view(state:GameState)->tuple[RecipeView,...]:
    return tuple(RecipeView(row["id"],str(row.get("name",row["id"])),str(row["input"]),str(row["output"])) for row in selected_content_pack().systems["recipes"])
def character_setup_view()->CharacterSetupView:
    setup=selected_content_pack().systems["setup"]
    convert=lambda section:tuple(SetupOptionView(row["id"],row["name"]) for row in setup[section])
    return CharacterSetupView(convert("crew"),convert("ancestries"),convert("origins"),convert("traits"))

"""Generic deterministic state; no setting instances are defined here."""
from __future__ import annotations
from dataclasses import asdict, dataclass, field
from hashlib import sha256
from typing import Any
from .catalog import ContentError, mechanical_fingerprint, selected_content_pack

SAVE_FORMAT = 16
class StateError(ValueError): pass
class ContentUnavailable(StateError): pass
@dataclass(frozen=True)
class Position: x:int; y:int; z:int=0
@dataclass
class Item: id:str; kind:str; quantity:int=1; equipped:bool=False; power:int=0
@dataclass
class Actor: id:str; kind:str; position:Position; health:int; maximum_health:int; alive:bool=True
@dataclass
class Quest: id:str; state:str; progress:int=0
@dataclass
class GameState:
    seed:str; pack_id:str; fingerprint:str; rows:tuple[str,...]; position:Position
    courier:Actor; items:list[Item]; actors:list[Actor]; quests:list[Quest]
    routes:tuple[str,...]; recipes:tuple[str,...]; turn:int=0; setup:dict[str,str]=field(default_factory=dict)
    remembered:set[Position]=field(default_factory=set)
    def to_dict(self) -> dict[str,Any]:
        return {"format":SAVE_FORMAT,"seed":self.seed,"pack_id":self.pack_id,"fingerprint":self.fingerprint,
                "rows":list(self.rows),"position":asdict(self.position),"courier":asdict(self.courier),
                "items":[asdict(item) for item in self.items],"actors":[asdict(actor) for actor in self.actors],
                "quests":[asdict(quest) for quest in self.quests],"routes":list(self.routes),"recipes":list(self.recipes),
                "turn":self.turn,"setup":dict(self.setup),"remembered":[asdict(item) for item in sorted(self.remembered,key=lambda p:(p.y,p.x,p.z))]}

def _position(value: object, context:str) -> Position:
    if not isinstance(value, dict) or set(value)!={"x","y","z"} or any(type(value[k]) is not int for k in value): raise StateError(f"invalid {context}")
    return Position(**value)
def _actor(row:dict[str,Any]) -> Actor: return Actor(row["id"],row["kind"],_position(row["position"],"actor position"),row["health"],row["maximum_health"],row["alive"])
def create_world(seed:str) -> GameState:
    pack=selected_content_pack()
    if not pack.playable: raise ContentUnavailable("No playable content pack installed")
    world=pack.systems["world"]
    assert isinstance(world,dict)
    start=Position(*world["start"])
    items=[Item(row["id"],row["id"],1,False,int(row.get("power",0))) for row in pack.systems["items"]]
    actors=[Actor(row["id"],str(row.get("kind","neutral")),Position(*row["position"]),int(row.get("health",1)),int(row.get("health",1))) for row in pack.systems["actors"]]
    courier=Actor("courier","courier",start,10,10)
    state=GameState(seed,pack.id,mechanical_fingerprint(pack),tuple(world["rows"]),start,courier,items,actors,[Quest(row["id"],str(row.get("state","active"))) for row in pack.systems["quests"]],tuple(row["id"] for row in pack.systems["routes"]),tuple(row["id"] for row in pack.systems["recipes"]))
    state.remembered.add(start); return state
def game_state_from_dict(value:object) -> GameState:
    if not isinstance(value,dict) or value.get("format") != SAVE_FORMAT: raise StateError("Save belongs to pre-reset content baseline and cannot be loaded")
    pack=selected_content_pack()
    if value.get("pack_id") != pack.id or value.get("fingerprint") != mechanical_fingerprint(pack): raise StateError("save requires a different playable content pack")
    try:
        state=GameState(str(value["seed"]),str(value["pack_id"]),str(value["fingerprint"]),tuple(value["rows"]),_position(value["position"],"position"),_actor(value["courier"]),[Item(**row) for row in value["items"]],[_actor(row) for row in value["actors"]],[Quest(**row) for row in value["quests"]],tuple(value["routes"]),tuple(value["recipes"]),int(value["turn"]),dict(value["setup"]),{_position(row,"remembered") for row in value["remembered"]})
    except (KeyError,TypeError,StateError) as exc: raise StateError("invalid reset-baseline save") from exc
    return state

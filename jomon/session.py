"""The deterministic application boundary over generic content-pack systems."""
from __future__ import annotations
from dataclasses import dataclass
from pathlib import Path
from .catalog import selected_content_pack
from .commands import *
from .runtime_events import *
from .save import load_game,save_game
from .state import GameState,Item,Position,create_world
from .views import *
@dataclass(frozen=True)
class CommandOutcome:
 accepted:bool; changed:bool; time_advanced:bool; result_id:str; revision:int; events:tuple[RuntimeEvent,...]=()
class GameSession:
 def __init__(self,state:GameState):self._state=state;self._revision=0
 @classmethod
 def create(cls,seed:str)->"GameSession":return cls(create_world(seed))
 @classmethod
 def load(cls,path:Path)->"GameSession":return cls(load_game(path))
 @classmethod
 def pending_character_setup_view(cls,seed:str="")->CharacterSetupView:return character_setup_view()
 @classmethod
 def create_configured(cls,seed:str,command:CharacterSetupCommand):
  session=cls.create(seed); outcome=session.submit(command)
  if not outcome.accepted:raise ValueError("invalid initial character setup")
  return session,outcome
 def save(self,path:Path)->Path:return save_game(self._state,path)
 @property
 def revision(self):return self._revision
 def world_view(self):return world_view(self._state)
 def actor_views(self):return actor_views(self._state)
 def actor_view(self,identity:str):return next((row for row in self.actor_views() if row.id==identity),None)
 def inventory_view(self):return inventory_view(self._state)
 def quest_views(self):return quest_views(self._state)
 def travel_view(self):return travel_view(self._state)
 def recipe_view(self):return recipe_view(self._state)
 def _out(self,changed:bool,result:str,events=()):
  if changed:self._revision+=1
  return CommandOutcome(changed,changed,False,result,self._revision,tuple(events))
 def submit(self,command):
  state=self._state
  if isinstance(command,CharacterSetupCommand):
   view=character_setup_view(); choices=(view.crew,view.ancestries,view.origins,view.traits); values=(command.crew_id,command.ancestry_id,command.origin_id,command.trait_id)
   if not all(any(row.id==value for row in options) for options,value in zip(choices,values)):return self._out(False,"setup.rejected")
   state.setup=dict(zip(("crew","ancestry","origin","trait"),values));return self._out(True,"setup.completed")
  if isinstance(command,MoveCommand):
   target=Position(state.position.x+command.dx,state.position.y+command.dy)
   if not(0<=target.y<len(state.rows) and 0<=target.x<len(state.rows[0])) or state.rows[target.y][target.x]=="#":return self._out(False,"move.rejected")
   before=state.position;state.position=target;state.courier.position=target;state.remembered.add(target);state.turn+=1
   return self._out(True,"move.ok",(ActorMoved("movement.step","courier",before,target),))
  if isinstance(command,AttackCommand):
   target=next((row for row in state.actors if row.id==command.target_actor_id and row.alive),None)
   if target is None or abs(target.position.x-state.position.x)+abs(target.position.y-state.position.y)>1:return self._out(False,"attack.rejected")
   power=max((item.power for item in state.items if item.equipped),default=1);target.health-=power;target.alive=target.health>0;state.turn+=1
   return self._out(True,"attack.resolved",(AttackResolved("attack.resolved","courier",target.id,power),))
  if isinstance(command,EquipItemCommand):
   item=next((row for row in state.items if row.id==command.item_id),None)
   if item is None:return self._out(False,"item.rejected")
   item.equipped=True;return self._out(True,"item.equipped",(ItemEquipped("item.equipped",item.id),))
  if isinstance(command,UnequipItemCommand):
   item=next((row for row in state.items if row.id==command.item_id),None)
   if item is None or not item.equipped:return self._out(False,"item.rejected")
   item.equipped=False;return self._out(True,"item.unequipped")
  if isinstance(command,TravelCommand):
   if command.route_id not in state.routes:return self._out(False,"travel.rejected")
   state.turn+=1;return self._out(True,"travel.resolved",(TravelResolved("travel.resolved",command.route_id),))
  if isinstance(command,CraftCommand):
   recipe=next((row for row in recipe_view(state) if row.id==command.recipe_id),None)
   if recipe is None or not any(row.kind==recipe.input_id for row in state.items):return self._out(False,"craft.rejected")
   state.items.append(Item(f"crafted-{state.turn}",recipe.output_id));state.turn+=1
   return self._out(True,"craft.resolved",(CraftResolved("craft.resolved",recipe.id,recipe.output_id),))
  return self._out(False,"command.unsupported")

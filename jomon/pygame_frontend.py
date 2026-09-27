"""Pygame application shell and Debug renderer for the engine baseline."""
from __future__ import annotations
import argparse
from dataclasses import dataclass
from pathlib import Path
from typing import Any
from .app_settings import AppSettings,load_app_settings,resolve_renderer,save_app_settings
from .catalog import selected_content_pack
from .state import ContentUnavailable
from .commands import AttackCommand,MoveCommand
from .font_stack import FontStack
from .session import GameSession
from .state import Position

def _pygame():
 try:
  import pygame
  return pygame
 except ModuleNotFoundError as exc:raise RuntimeError("Jomon requires pygame-ce; run `uv sync`.") from exc
@dataclass(frozen=True)
class MenuItem: action:str; label:str; enabled:bool=True; detail:str=""
class PygameFrontend:
 renderer_id="debug"
 def __init__(self,session:GameSession|None,*,pygame:Any|None=None,size=(1100,760),shell_mode="game",settings:AppSettings|None=None,seed="jomon",**_:Any):
  self.pygame=pygame or _pygame();self.screen=self.pygame.display.set_mode(size,self.pygame.RESIZABLE);self.pygame.display.set_caption("JOMON")
  self.clock=self.pygame.time.Clock();self.font_stack=FontStack(self.pygame,20);self.font=self.font_stack.text;self.session=session;self.seed=seed;self.settings=settings or load_app_settings();self.panel="title" if session is None or shell_mode=="title" else None;self.running=True;self.selected:Position|None=None;self.last_result="ready";self.tile_size=26
 def _camera(self,view):
  w,h=self.screen.get_size();return w//2-view.courier_position.x*self.tile_size,h//2-view.courier_position.y*self.tile_size
 def _rect(self,p,c):return self.pygame.Rect(c[0]+p.x*self.tile_size,c[1]+p.y*self.tile_size,self.tile_size,self.tile_size)
 def _rows(self):
  playable=selected_content_pack().playable
  if self.panel=="title":return (MenuItem("join","JOIN GAME",playable,"No playable content pack installed" if not playable else ""),MenuItem("settings","SETTINGS"),MenuItem("quit","QUIT"))
  if self.panel=="settings":return (MenuItem("debug","DEBUG",self.renderer_id!="debug"),MenuItem("ascii","ASCII",self.renderer_id!="ascii"),MenuItem("back","BACK"))
  return ()
 def _activate(self,action):
  if action=="quit":self.running=False
  elif action=="settings":self.panel="settings"
  elif action=="back":self.panel="title"
  elif action in {"debug","ascii"}:
   self.settings=AppSettings(action);save_app_settings(self.settings);self.requested_renderer=action
  elif action=="join":
   try:self.session=GameSession.create(self.seed);self.panel=None
   except ContentUnavailable:self.last_result="content.unavailable"
 def submit(self,command):
  if self.session is None:raise RuntimeError("no active session")
  out=self.session.submit(command);self.last_result=out.result_id;return out
 def _select_at(self,pos):
  if self.session is None:return
  v=self.session.world_view();c=self._camera(v);x,y=(pos[0]-c[0])//self.tile_size,(pos[1]-c[1])//self.tile_size
  cell=next((row for row in v.cells if (row.position.x,row.position.y)==(x,y) and (row.visible or row.remembered)),None)
  if cell:self.selected=cell.position
 def handle_event(self,event,save_path=None):
  p=self.pygame
  if event.type==p.QUIT:self.running=False;return
  if event.type==p.KEYDOWN:
   if event.key==p.K_ESCAPE:self.panel="title" if self.session is None else None;return
   if self.panel:
    rows=self._rows()
    if event.key in {p.K_RETURN,p.K_KP_ENTER} and rows:self._activate(rows[0].action)
    return
   if self.session:
    moves={p.K_LEFT:(-1,0),p.K_RIGHT:(1,0),p.K_UP:(0,-1),p.K_DOWN:(0,1),p.K_a:(-1,0),p.K_d:(1,0),p.K_w:(0,-1),p.K_s:(0,1)}
    if event.key in moves:self.submit(MoveCommand(*moves[event.key]))
    elif event.key==p.K_f and self.selected:
     actor=next((row for row in self.session.actor_views() if row.position==self.selected and row.actor_kind!="courier"),None)
     if actor:self.submit(AttackCommand(actor.id))
  if event.type==p.MOUSEBUTTONDOWN and event.button==1:
   if self.panel:
    rows=self._rows(); index=(event.pos[1]-self.screen.get_height()//2)//36
    if 0<=index<len(rows) and rows[index].enabled:self._activate(rows[index].action)
   else:self._select_at(event.pos)
 def _draw_shell(self):
  self.screen.fill((8,12,20));w,h=self.screen.get_size();title=self.font_stack.render("JOMON",(130,195,246));self.screen.blit(title,(w//2-title.get_width()//2,h//4))
  for i,row in enumerate(self._rows()):
   colour=(225,228,235) if row.enabled else (105,115,128);s=self.font_stack.render(row.label,colour);self.screen.blit(s,(w//2-s.get_width()//2,h//2+i*36))
   if row.detail and not row.enabled:
    d=self.font_stack.render(row.detail,(230,180,90));self.screen.blit(d,(w//2-d.get_width()//2,h//2+(i+1)*36))
 def draw(self):
  if self.panel:self._draw_shell();self.pygame.display.flip();return
  assert self.session is not None;self.screen.fill((8,12,20));v=self.session.world_view();c=self._camera(v)
  for cell in v.cells:
   if not(cell.visible or cell.remembered):continue
   rect=self._rect(cell.position,c);colour=(76,94,111) if cell.terrain_id=="terrain.floor" else (126,105,90)
   if not cell.visible:colour=tuple(x//2 for x in colour)
   self.pygame.draw.rect(self.screen,colour,rect)
  for actor in self.session.actor_views():
   if actor.actor_kind=="courier" or not actor.alive:continue
   rect=self._rect(actor.position,c);self.pygame.draw.circle(self.screen,(230,90,90),rect.center,7)
  rect=self._rect(v.courier_position,c);self.pygame.draw.circle(self.screen,(230,245,255),rect.center,7)
  if self.selected:self.pygame.draw.rect(self.screen,(255,220,100),self._rect(self.selected,c),2)
  s=self.font_stack.render(f"JOMON  result:{self.last_result}  arrows move · click select · F attack",(220,230,240));self.screen.blit(s,(10,8));self.pygame.display.flip()
 def run(self):
  while self.running:
   for event in self.pygame.event.get():self.handle_event(event)
   self.draw();self.clock.tick(60)
  self.pygame.quit()
def create_frontend(session,*,renderer="debug",**kwargs):
 if renderer in {"ascii"}:
  from .pygame_ascii import AsciiPygameFrontend
  return AsciiPygameFrontend(session,**kwargs)
 return PygameFrontend(session,**kwargs)
def main(argv=None):
 parser=argparse.ArgumentParser();parser.add_argument("--renderer",choices=("debug","ascii","graphical"));parser.add_argument("--new",action="store_true");parser.add_argument("--load",type=Path);parser.add_argument("--seed",default="jomon");args=parser.parse_args(argv)
 renderer=resolve_renderer(args.renderer,load_app_settings());session=None
 try:
  if args.load:session=GameSession.load(args.load)
  elif args.new:session=GameSession.create(args.seed)
 except (ContentUnavailable,ValueError) as exc:raise SystemExit(str(exc))
 create_frontend(session,renderer=renderer,shell_mode="title" if session is None else "game",seed=args.seed).run()

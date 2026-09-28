from __future__ import annotations
import os, unittest
from pathlib import Path
os.environ.setdefault('SDL_VIDEODRIVER','dummy');os.environ.setdefault('SDL_AUDIODRIVER','dummy')
import pygame
from jomon.catalog import select_content_pack,template_root
ROOT=Path(__file__).parent;PACK=ROOT.parents[0]/'jomon'/'content_packs'/'first-playable'
class ContinuityPygameTests(unittest.TestCase):
 @classmethod
 def setUpClass(c):pygame.init()
 @classmethod
 def tearDownClass(c):pygame.quit()
 def setUp(self):select_content_pack(PACK)
 def tearDown(self):select_content_pack(template_root())
 def key(self,a,k):a.handle_event(pygame.event.Event(pygame.KEYDOWN,key=k,mod=0))
 def start(self,renderer):
  from jomon.pygame_frontend import create_frontend
  a=create_frontend(None,renderer=renderer,pygame=pygame,seed='input-continuity');self.key(a,pygame.K_RETURN);self.key(a,pygame.K_RETURN);return a
 def test_both_renderer_input_paths_select_successor_after_real_death(self):
  for renderer in ('debug','ascii'):
   a=self.start(renderer)
   for _ in range(7):self.key(a,pygame.K_RIGHT)
   while a.session.world_view().courier_alive:
    self.key(a,pygame.K_LEFT);self.key(a,pygame.K_RIGHT)
   self.assertEqual(a.panel,'continuation')
   self.key(a,pygame.K_DOWN);self.key(a,pygame.K_RETURN)
   self.assertIsNone(a.panel);self.assertEqual(a.session.world_view().courier_position.x,1)
   a.draw()
 def click(self,a,x,y):
  v=a.session.world_view();a.draw();p=next(c.position for c in v.cells if c.position.x==x and c.position.y==y)
  a.handle_event(pygame.event.Event(pygame.MOUSEBUTTONDOWN,button=1,pos=a._rect(p,a._camera(v)).center))
 def moves(self,a,key,n):
  for _ in range(n):self.key(a,key)
 def test_both_renderer_inputs_recover_objective_then_deliver(self):
  for renderer in ('debug','ascii'):
   a=self.start(renderer)
   self.key(a,pygame.K_i);self.key(a,pygame.K_e);self.key(a,pygame.K_i)
   self.moves(a,pygame.K_UP,2);self.moves(a,pygame.K_RIGHT,6);self.click(a,9,1);self.key(a,pygame.K_e)
   self.moves(a,pygame.K_RIGHT,8);self.moves(a,pygame.K_DOWN,2);self.click(a,16,3);self.key(a,pygame.K_e)
   self.moves(a,pygame.K_UP,2);self.moves(a,pygame.K_LEFT,7);self.moves(a,pygame.K_DOWN,2)
   while a.session.world_view().courier_alive:self.key(a,pygame.K_LEFT);self.key(a,pygame.K_RIGHT)
   self.key(a,pygame.K_DOWN);self.key(a,pygame.K_RETURN)
   self.moves(a,pygame.K_RIGHT,8);self.moves(a,pygame.K_DOWN,1);self.click(a,9,3);self.key(a,pygame.K_r)
   # Choose the same semantic recovery action even as authored carried items
   # are added before the objective; input still travels through the menu.
   objective_action='recover:crew.initial-operative:objective.operation.recover-diagnostic'
   for _ in range(next(index for index,row in enumerate(a._rows()) if row.action==objective_action)):
    self.key(a,pygame.K_DOWN)
   self.key(a,pygame.K_RETURN)
   self.moves(a,pygame.K_UP,2);self.moves(a,pygame.K_LEFT,7);self.moves(a,pygame.K_DOWN,2);self.click(a,2,3);self.key(a,pygame.K_e)
   self.assertEqual(a.session.operation_views()[0].state_id,'returned');a.draw()

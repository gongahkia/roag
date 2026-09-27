from __future__ import annotations
import os,unittest
from pathlib import Path
os.environ.setdefault('SDL_VIDEODRIVER','dummy');os.environ.setdefault('SDL_AUDIODRIVER','dummy')
import pygame
from jomon.catalog import select_content_pack
from jomon.session import GameSession
ROOT=Path(__file__).parent
class RendererTests(unittest.TestCase):
 @classmethod
 def setUpClass(cls):pygame.init()
 @classmethod
 def tearDownClass(cls):pygame.quit()
 def tearDown(self):select_content_pack(ROOT.parents[0]/'jomon'/'content_packs'/'template')
 def test_template_title_disables_join_in_both_renderers(self):
  from jomon.pygame_frontend import create_frontend
  template=ROOT.parents[0]/'jomon'/'content_packs'/'template';select_content_pack(template)
  for renderer in ('debug','ascii'):
   app=create_frontend(None,renderer=renderer,pygame=pygame);app.draw();self.assertFalse(app._rows()[0].enabled)
 def test_settings_selects_a_renderer_without_creating_a_session(self):
  from jomon.pygame_frontend import create_frontend
  select_content_pack(ROOT.parents[0]/'jomon'/'content_packs'/'template')
  app=create_frontend(None,renderer='debug',pygame=pygame);app._activate('settings');app._activate('ascii')
  self.assertIsNone(app.session);self.assertEqual(app.requested_renderer,'ascii')
 def test_renderer_replacement_keeps_the_live_session(self):
  from jomon.pygame_frontend import create_frontend
  select_content_pack(ROOT/'fixtures'/'synthetic_content_pack');session=GameSession.create('switch')
  app=create_frontend(session,renderer='debug',pygame=pygame);app.requested_renderer='ascii';replacement=app.replacement_renderer()
  self.assertEqual(replacement.renderer_id,'ascii');self.assertIs(replacement.session,session)
 def test_synthetic_pack_renders_same_world_in_both_skins(self):
  from jomon.pygame_frontend import create_frontend
  select_content_pack(ROOT/'fixtures'/'synthetic_content_pack');session=GameSession.create('renderer')
  for renderer in ('debug','ascii'):
   app=create_frontend(session,renderer=renderer,pygame=pygame);app.draw();cell=next(row for row in session.world_view().cells if row.position==session.world_view().courier_position);app.handle_event(pygame.event.Event(pygame.MOUSEBUTTONDOWN,button=1,pos=app._rect(cell.position,app._camera(session.world_view())).center));self.assertEqual(app.selected,cell.position)

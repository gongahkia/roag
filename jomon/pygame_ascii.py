"""ASCII skin over the same session, views, commands, and tile grid."""
from __future__ import annotations
from .pygame_frontend import PygameFrontend
from .assets import ascii_glyph
class AsciiPygameFrontend(PygameFrontend):
 renderer_id="ascii"
 def draw(self):
  if self.panel:return super().draw()
  assert self.session is not None;self.screen.fill((5,10,19));v=self.session.world_view();c=self._camera(v)
  for cell in v.cells:
   if not(cell.visible or cell.remembered):continue
   rect=self._rect(cell.position,c);glyph=ascii_glyph(cell.terrain_id,"#" if cell.terrain_id=="terrain.wall" else ".");colour=(154,132,111) if glyph=="#" else (130,157,179)
   if not cell.visible:colour=tuple(max(22,x*3//5) for x in colour)
   surface=self.font_stack.render(glyph,colour);self.screen.blit(surface,surface.get_rect(center=rect.center))
  for actor in self.session.actor_views():
   if actor.actor_kind=="courier" or not actor.alive:continue
   rect=self._rect(actor.position,c);surface=self.font_stack.render(self.font_stack.icon("threat"),(242,111,105),icon=True);self.screen.blit(surface,surface.get_rect(center=rect.center))
  rect=self._rect(v.courier_position,c);surface=self.font_stack.render(self.font_stack.icon("courier"),(238,249,255),icon=True);self.screen.blit(surface,surface.get_rect(center=rect.center))
  if self.selected:self.pygame.draw.rect(self.screen,(255,242,166),self._rect(self.selected,c),1)
  s=self.font_stack.render(f"JOMON  result:{self.last_result}  arrows move · click select · F attack",(160,210,235));self.screen.blit(s,(10,8));self.pygame.display.flip()

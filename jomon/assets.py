"""Presentation-only logical asset resolution."""
from .catalog import AssetResource, selected_content_pack
def ascii_glyph(identity:str,fallback:str)->str:
    try:return selected_content_pack().assets.glyph(identity)
    except KeyError:return fallback
def asset_binding(category:str,identity:str)->dict[str,str]: return selected_content_pack().assets.binding(category,identity)
def asset_resource(identity:str)->AssetResource|None:return selected_content_pack().assets.resource(identity)
def actor_assets(identity:str)->dict[str,str]:return asset_binding("actors",identity)
def terrain_assets(identity:str)->dict[str,str]:return asset_binding("terrain",identity)
def event_assets(identity:str)->dict[str,str]:return asset_binding("events",identity)

"""Selected-pack presentation for Jomon's small tavern games."""
from __future__ import annotations
from .catalog import selected_content_pack

def tavern_text(semantic_id: str) -> str:
    return selected_content_pack().tavern_games.text(semantic_id)

def tavern_format(semantic_id: str, /, **values: object) -> str:
    return tavern_text(semantic_id).format(**values)

def draw_rank_name(rank: int) -> str:
    return selected_content_pack().tavern_games.rank_labels[rank]

def draw_phase_name(phase: str) -> str:
    return tavern_text(f"draw.phase.{phase}")

def tavern_cards() -> dict[str, object]:
    value = selected_content_pack().tavern_games
    return {"ranks": value.card_ranks, "suits": value.card_suits, "edge": value.card_edge,
            "selected_edge": value.card_selected_edge, "frame": value.card_frame}

def tavern_dice() -> dict[int, tuple[str, ...]]:
    return dict(selected_content_pack().tavern_games.dice_faces)

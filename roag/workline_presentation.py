"""Resolve selected-pack wording for stable regional workline mechanics."""
from __future__ import annotations

from .catalog import selected_content_pack


def workline_text(semantic_id: str) -> str:
    return selected_content_pack().workline_presentation(semantic_id).text


def workline_format(semantic_id: str, /, **values: object) -> str:
    return workline_text(semantic_id).format(**values)


def workline_title(region_id: str) -> str:
    return workline_text(f"workline.{region_id}.title")


def workline_evidence_name(region_id: str) -> str:
    return workline_text(f"workline.{region_id}.evidence")


def workline_reward_name(region_id: str) -> str:
    return workline_text(f"workline.{region_id}.reward")


def workline_branch_label(region_id: str, branch_id: str) -> str:
    return workline_text(f"workline.{region_id}.branch.{branch_id}")

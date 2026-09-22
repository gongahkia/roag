"""Resolve selected-pack progression wording from stable identities."""
from .catalog import selected_content_pack

def progression_text(semantic_id: str) -> str:
    return selected_content_pack().progression_presentation(semantic_id).text

def progression_format(semantic_id: str, **values: object) -> str:
    return progression_text(semantic_id).format(**values)

def branch_display_name(branch_id: str) -> str:
    return progression_text(f"progression.branch.{branch_id}.name")

def node_display_name(node_id: str) -> str:
    return progression_text(f"progression.node.{node_id}.name")

def node_description(node_id: str) -> str:
    return progression_text(f"progression.node.{node_id}.description")

def practice_display_name(practice_id: str) -> str:
    return progression_text(f"progression.practice.{practice_id.removeprefix('practice.')}.name")

def practice_description(practice_id: str) -> str:
    return progression_text(f"progression.practice.{practice_id.removeprefix('practice.')}.description")

def manoeuvre_display(manoeuvre_id: str, field: str) -> str:
    return progression_text(f"progression.manoeuvre.{manoeuvre_id}.{field}")


def technique_display_name(value: str) -> str:
    from .practices import stable_practice_id
    value = stable_practice_id(value)
    if value.startswith("practice.personal:"):
        from .character import role_display_name
        return progression_format("progression.personal.name", role=role_display_name(value.split(":", 1)[1]))
    if value.startswith("technique."):
        return progression_text(f"progression.{value}.name")
    if value.startswith("practice."):
        return practice_display_name(value)
    return value

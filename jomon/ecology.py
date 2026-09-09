"""Local work, wildlife and rival interests; no distant food-chain simulation."""

from __future__ import annotations

from .state import GameState, Position, Threat
from .world import base_tile, distance, is_walkable, line_of_sight, position_key

ACTIVE_RADIUS = 24
ACTOR_BUDGET = 24
DUTIES = {"", "quench", "kindle", "brace", "drain", "heal", "scavenge", "hunt", "rally", "cut support", "escort"}
ECOLOGIES = {"", "prey", "predator", "scavenger", "worker", "warden", "raider", "territorial"}


def perceives_point(state: GameState, actor: Threat, point: Position) -> bool:
    from .enemy_ai import effective_vision

    return distance(actor.position, point) <= effective_vision(state, actor) and line_of_sight(state, actor.position, point)


def opposed(first: Threat, second: Threat) -> bool:
    if first.id == second.id or first.allegiance and first.allegiance == second.allegiance:
        return False
    if first.ecology == "predator":
        return second.ecology == "prey"
    if first.ecology == "territorial":
        return second.ecology in {"predator", "raider"} and first.home_position is not None and distance(first.home_position, second.position) <= 5
    return {first.ecology, second.ecology} == {"warden", "raider"}


def active_actors(state: GameState) -> list[Threat]:
    """A sorted, bounded active neighbourhood; far populations retain state."""
    signature = tuple((a.id, a.position, a.status) for a in state.threats)
    cached = getattr(state, "_actor_chunks", None)
    if cached is None or cached[0] != signature:
        chunks: dict[tuple[int, int, int], list[Threat]] = {}
        for actor in state.threats:
            if actor.status in {"watching", "engaged"}:
                chunks.setdefault((actor.position.x // 8, actor.position.y // 8, actor.position.z), []).append(actor)
        cached = (signature, chunks)
        state._actor_chunks = cached
    origin = state.position
    candidates = []
    for (x, y, z), actors in cached[1].items():
        if abs(x - origin.x // 8) <= 3 and abs(y - origin.y // 8) <= 3 and abs(z - origin.z) <= 1:
            candidates.extend(a for a in actors if distance(a.position, origin) <= ACTIVE_RADIUS)
    return sorted(candidates, key=lambda actor: (distance(actor.position, origin), actor.id))[:ACTOR_BUDGET]


def safe_step(state: GameState, actor: Threat, danger: Position) -> Position:
    from .enemy_ai import _neighbours
    from .materials import fields

    candidates = [point for point in _neighbours(state, actor.position, actor) if point != state.position]
    def safety(point):
        cell = fields(state).get(position_key(point))
        risk = (cell.fire * 4 + cell.smoke + cell.water + bool(cell.collapse_due) * 5) if cell else 0
        habitat = {
            "dunmire": {'"', ";", "m"}, "rillscar": {"r", "q"},
            "marlbank": {";", ","}, "frostmere": {"r", "_"},
        }.get(actor.region_id, set())
        shelter = int(actor.ecology == "prey" and base_tile(state, point) in habitat)
        return (-risk, distance(point, danger), shelter, -point.y, -point.x)
    return max(candidates, key=safety, default=actor.position)


def world_options(state: GameState, actor: Threat, courier_visible: bool):
    from .enemy_ai import EnemyDecision
    from .materials import fields, point_at

    options = []
    def offer(goal, action, score, reason, target=None):
        options.append(EnemyDecision(goal, action, reason, target, score))

    own = fields(state).get(position_key(actor.position))
    if own and (own.fire or own.collapse_due):
        offer("leave material danger", "avoid hazard", 125, "flame or a creaking support threatens its present footing", actor.position)
    neighbours = [other for other in state.threats if other.status in {"watching", "engaged"} and perceives_point(state, actor, other.position)]
    if actor.ecology == "prey":
        dangers = [other.position for other in neighbours if other.ecology in {"predator", "raider"}]
        if courier_visible:
            dangers.append(state.position)
        if dangers:
            nearest = min(dangers, key=lambda point: distance(actor.position, point))
            offer("avoid hunters", "avoid hunter", 115, "a visible hunter or approaching courier disturbed its feeding", nearest)
        else:
            offer("graze within shelter", "graze", 85, "no hunter is currently perceived", actor.home_position)
        return options

    if actor.duty in {"quench", "brace", "drain"} and actor.supplies > 0:
        targets = []
        for coordinate, cell in fields(state).items():
            relevant = cell.fire if actor.duty == "quench" else cell.water >= 2 if actor.duty == "drain" else cell.support < 2 or cell.collapse_due
            point = point_at(coordinate)
            if relevant and perceives_point(state, actor, point):
                targets.append(point)
        if targets:
            target = min(targets, key=lambda point: (distance(actor.position, point), point.z, point.y, point.x))
            offer("preserve working ground", actor.duty, 107, f"visible material damage calls for its finite {actor.duty} supplies", target)
    if actor.duty in {"kindle", "cut support"} and actor.supplies > 0 and actor.objective_position:
        target = actor.objective_position
        if perceives_point(state, actor, target) and (courier_visible or actor.alarmed or actor.reaction):
            offer("alter the working route", actor.duty, 102, "its assigned material target is in sight and the dispute is active", target)
    if actor.duty == "heal" and actor.supplies > 0:
        wounded = [other for other in neighbours if other.id != actor.id and actor.group and other.group == actor.group and 0 < other.health <= other.max_health // 2]
        if wounded:
            target = min(wounded, key=lambda other: (other.health, distance(actor.position, other.position), other.id))
            offer("rescue wounded ally", "treat ally", 109, "an observed ally needs its last field dressing", target.position)
    if actor.duty == "rally" and actor.supplies > 0:
        wavering = [other for other in neighbours if other.id != actor.id and actor.group and other.group == actor.group and other.morale <= 1]
        if wavering:
            target = min(wavering, key=lambda other: other.id)
            offer("cover an ally's retreat", "rally ally", 106, "an ally's visible distress takes priority over pursuit", target.position)
    if actor.duty == "scavenge" and not actor.carrying_item_id:
        items = [item for item in state.items if item.location == "ground" and item.region_id == state.active_region_id and item.ground_position and perceives_point(state, actor, item.ground_position)]
        if items:
            item = min(items, key=lambda item: (distance(actor.position, item.ground_position), item.id))
            offer("retrieve abandoned property", "take ground item", 104, "a physical abandoned item is visible, not an unopened reward", item.ground_position)
    rivals = [other for other in neighbours if opposed(actor, other)]
    if rivals:
        target = min(rivals, key=lambda other: (distance(actor.position, other.position), other.id))
        offer("hunt prey" if actor.ecology == "predator" else "oppose rival claim", "engage rival", 100, f"{target.name} is visibly intruding on its {actor.ecology} interest", target.position)
    if actor.duty == "escort":
        carrier = next((other for other in neighbours if other.id != actor.id and other.group == actor.group and (other.carrying_item_id or other.health <= other.max_health // 2)), None)
        if carrier:
            offer("escort vulnerable ally", "escort ally", 103, "a visible carrier or wounded ally needs a physical escort", carrier.position)
    return options


def _move(state: GameState, actor: Threat, target: Position, stop=1) -> bool:
    from .enemy_ai import next_path_step

    limit = 120 if actor.ecology in {"prey", "predator", "territorial"} else 350
    step = next_path_step(state, actor, target, stop_distance=stop, limit=limit)
    if step == actor.position or step == state.position:
        return False
    if base_tile(state, step) == "+":
        if actor.profile == "animal":
            return False
        state.region.tile_changes[position_key(step)] = "."
        actor.intent = "opens a working door before passing"
        return True
    actor.position = step
    return True


def resolve_world_action(state: GameState, actor: Threat, decision) -> str | None:
    from .inventory import item_spec, release_enemy_possession
    from .materials import ensure_cell

    action, point = decision.action, decision.target
    if action not in {"avoid hazard", "avoid hunter", "graze", "quench", "brace", "drain", "kindle", "cut support", "treat ally", "rally ally", "take ground item", "engage rival", "escort ally"}:
        return None
    if action in {"avoid hazard", "avoid hunter"}:
        old = actor.position
        actor.position = safe_step(state, actor, point)
        actor.aimed_at = actor.marked_position = None
        actor.reaction = ""
        actor.intent = decision.reason
        return f"The {actor.name} leaves {('dangerous material' if action == 'avoid hazard' else 'the visible hunter')}." if actor.position != old else ""
    if action == "graze":
        if point and distance(actor.position, point) > 3:
            _move(state, actor, point, stop=3)
        actor.intent = "feeds within its shelter; not hunting the courier"
        return ""
    if point is None:
        return ""
    if distance(actor.position, point) > (2 if action == "engage rival" and actor.profile == "reach" else 1):
        moved = _move(state, actor, point, stop=1)
        actor.intent = f"moves to {decision.goal}; {decision.reason}"
        return f"The {actor.name} moves to {decision.goal}." if moved else ""
    if action == "engage rival":
        rival = next((other for other in state.threats if other.position == point and other.status in {"watching", "engaged"} and opposed(actor, other)), None)
        if not rival:
            actor.target_actor_id = None
            return ""
        if actor.reaction != "rival strike" or actor.target_actor_id != rival.id:
            actor.reaction, actor.marked_position, actor.target_actor_id = "rival strike", point, rival.id
            actor.intent = f"turns a warned strike on {rival.name}; a retreat breaks the attack"
            return f"The {actor.name} {actor.intent}."
        actor.reaction = ""
        if actor.marked_position != rival.position:
            return f"The {actor.name}'s rival strike meets empty ground."
        actor.marked_position = None
        rival.health = max(0, rival.health - 2)
        rival.morale -= 1
        rival.intent = f"disrupted by {actor.name}'s attack"
        if rival.health == 0:
            rival.status = "defeated"
            lost = release_enemy_possession(state, rival)
            state.region.changes[f"population:{rival.id}"] = "killed by a local rival"
            state.remember(f"{actor.name} killed {rival.name} at {position_key(point)} over {actor.ecology} interests.")
            return f"The {actor.name} defeats the {rival.name}.{lost}"
        return f"The {actor.name} strikes its rival {rival.name}; {rival.health} health remains."
    if action in {"treat ally", "rally ally", "escort ally"}:
        ally = next((other for other in state.threats if other.id != actor.id and other.group == actor.group and other.position == point and other.status in {"watching", "engaged"}), None)
        if ally is None:
            return ""
        if action == "escort ally":
            ally.morale = min(3, ally.morale + 1)
            actor.intent = f"holds beside {ally.name}; guarding the retreat"
            return ""
        actor.supplies -= 1
        ally.health = min(ally.max_health, ally.health + (2 if action == "treat ally" else 0))
        ally.morale = min(3, ally.morale + 1)
        ally.intent = "disrupted while an ally tends the withdrawal"
        actor.intent = f"spends one finite {'dressing' if action == 'treat ally' else 'signal'} on {ally.name}"
        return f"The {actor.name} {actor.intent}."
    if action == "take ground item":
        item = next((item for item in state.items if item.location == "ground" and item.region_id == state.active_region_id and item.ground_position == point), None)
        if item is None:
            return ""
        actor.carrying_item_id = item.id
        item.location, item.owner_id, item.ground_position = "enemy", None, None
        actor.intent = f"carries the observed {item_spec(item.kind).name} toward shelter"
        return f"The {actor.name} {actor.intent}; the item can be recovered before escape."
    cell = ensure_cell(state, point)
    if cell is None or actor.supplies <= 0:
        return ""
    if action in {"kindle", "cut support"}:
        if actor.reaction != action or actor.marked_position != point:
            actor.reaction, actor.marked_position = action, point
            actor.intent = f"prepares to {action} at {position_key(point)}; water, bracing or interruption can answer"
            return f"The {actor.name} {actor.intent}."
        actor.reaction, actor.marked_position = "", None
        if action == "kindle":
            if cell.water or cell.ice:
                actor.supplies -= 1
                return f"Water defeats the {actor.name}'s finite fire preparation."
            cell.material, cell.fuel, cell.fire = "resin", max(cell.fuel, 3), max(cell.fire, 1)
        else:
            cell.support = max(0, cell.support - 2)
            if cell.support == 0:
                cell.collapse_due = state.world_time + 3
    elif action == "quench":
        cell.water, cell.fire, cell.coating = min(3, cell.water + 1), 0, "wet"
    elif action == "brace":
        cell.support, cell.collapse_due = 3, 0
    elif action == "drain":
        cell.water = max(0, cell.water - 2)
        state.water.pop(position_key(point), None)
    actor.supplies -= 1
    actor.intent = f"uses finite {action} supplies at {position_key(point)}"
    state.region.changes[f"work:{actor.id}"] = f"{action} at {position_key(point)}"
    return f"The {actor.name} {actor.intent}; the physical change remains."


def validate_ecology(state: GameState) -> None:
    for region_id, actors in state.region_threats.items():
        if len(actors) > 48:
            raise ValueError("regional actor budget exceeded")
        for actor in actors:
            if actor.duty not in DUTIES or actor.ecology not in ECOLOGIES or not 0 <= actor.supplies <= 8:
                raise ValueError("invalid material duty or finite actor supply")
            if actor.reaction not in {"", "rival strike", "kindle", "cut support"}:
                raise ValueError("unknown staged actor reaction")
            if actor.glyph and (len(actor.glyph) != 1 or not actor.glyph.isascii() or not actor.glyph.isalnum()):
                raise ValueError("invalid actor glyph")
            if actor.marked_position and (str(actor.marked_position.z) not in state.regions[region_id].levels or not 0 <= actor.marked_position.x < state.regions[region_id].width or not 0 <= actor.marked_position.y < state.regions[region_id].height):
                raise ValueError("material duty marks an invalid position")

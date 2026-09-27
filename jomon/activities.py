"""Renderer-neutral ordinary-game activity choices.

The legacy terminal has many overlays for the same underlying pattern: expose
the currently legal semantic choices, then invoke an existing reducer.  This
module centralises that pattern without importing curses or teaching a
frontend any mechanics.
"""
from __future__ import annotations

from dataclasses import dataclass

from .state import GameState, Position
from .views import ActivityOptionView, ActivityView


def _label(identity: str) -> str:
    return identity.replace(".", " ").replace("_", " ").replace("-", " ").title()


@dataclass(frozen=True)
class ActivityResolution:
    changed: bool
    time_advanced: bool
    message: str


def _option(action_id: str, label: str, description: str = "", *, available: bool = True,
            reason_id: str | None = None, target_kind_id: str | None = None) -> ActivityOptionView:
    return ActivityOptionView(action_id, label, description, available, reason_id, target_kind_id)


def activity_view(state: GameState, context_id: str) -> ActivityView:
    """Return current semantic options without mutating simulation state."""
    options: list[ActivityOptionView] = []
    title = _label(context_id)
    if context_id == "production":
        from .production import RECIPES, at_shore_site, recipe_status, stations_here
        from .production_presentation import production_recipe_name
        stations = stations_here(state)
        for recipe_id, recipe in sorted(RECIPES.items()):
            if recipe.station not in stations:
                continue
            available, reason = recipe_status(state, recipe_id)
            name = production_recipe_name(recipe_id, recipe.output)
            options.append(_option(f"production.make:{recipe_id}", name, recipe.output, available=available,
                                   reason_id=None if available else "production.unavailable"))
            if at_shore_site(state) and not recipe.contents:
                options.append(_option(f"production.delegate:{recipe_id}", f"Delegate {name}", recipe.output,
                                       available=available, reason_id=None if available else "production.unavailable"))
        if at_shore_site(state):
            from .production import SOURCES
            for index, source in enumerate(SOURCES[state.active_region_id]):
                options.append(_option(f"production.gather:{index}", f"Gather {_label(source)}"))
        title = "Production"
    elif context_id == "preparation":
        from .chemistry import carried_flasks, carried_ingredients
        from .preparations import carried_preparations, preparation_status
        from .preparation_presentation import preparation_display_name
        for preparation_id in carried_preparations(state):
            available, _ = preparation_status(state, preparation_id)
            options.append(_option(f"preparation.use:{preparation_id}", preparation_display_name(preparation_id),
                                   available=available, reason_id=None if available else "preparation.unavailable"))
        for flask in carried_flasks(state):
            for ingredient in carried_ingredients(state):
                options.append(_option(f"preparation.fill:{flask.id}:{ingredient.id}",
                                       f"Fill {flask.id} with {_label(ingredient.kind)}"))
            for reagent in sorted(flask.contents):
                options.append(_option(f"preparation.distill:{flask.id}:{reagent}",
                                       f"Distil {_label(reagent)} from {flask.id}"))
        title = "Preparation"
    elif context_id == "progression":
        from .skill_tree import NODES
        from .progression_presentation import branch_display_name
        courier = state.courier
        if courier:
            for node_id, node in sorted(NODES.items()):
                if node_id not in courier.skill_nodes:
                    options.append(_option(f"skill.buy:{node_id}", node.name, branch_display_name(node.branch)))
        title = "Progression"
    elif context_id == "magic":
        from .magic import SPELLS
        from .magic_presentation import spell_description, spell_display_name
        courier = state.courier
        if courier:
            for spell_id in courier.known_spells:
                spell = SPELLS[spell_id]
                target = "actor" if spell.target == "enemy" else "cell" if spell.target == "cell" else "self"
                options.append(_option(f"magic.cast:{spell_id}", spell_display_name(spell_id), spell_description(spell),
                                       target_kind_id=target))
        title = "Spellcraft"
    elif context_id == "materials":
        from .materials import VERBS
        from .material_presentation import material_verb_display_name
        for dx, dy in ((0, 0), (0, -1), (1, 0), (0, 1), (-1, 0)):
            point = Position(state.position.x + dx, state.position.y + dy, state.position.z)
            for verb in VERBS:
                options.append(_option(f"material.{verb}:{point.x},{point.y},{point.z}", material_verb_display_name(verb),
                                       f"{point.x}, {point.y}, {point.z:+d}", target_kind_id="cell"))
        title = "Field materials"
    elif context_id == "vessel":
        from .vessel_refits import STATION_REFITS, installation_status, refit_station_at
        from .vessel_presentation import refit_display_name
        from .magic import rest_at_berths
        station = refit_station_at(state)
        for refit_id in STATION_REFITS.get(station or "", ()):
            available, _ = installation_status(state, refit_id)
            options.append(_option(f"vessel.refit:{refit_id}", refit_display_name(refit_id),
                                   available=available, reason_id=None if available else "vessel.refit.unavailable"))
        if station == "berths":
            available, _ = rest_at_berths(state)
            options.append(_option("vessel.rest", "Rest at berths", available=available,
                                   reason_id=None if available else "vessel.rest.unavailable"))
        title = "Vessel"
    elif context_id == "sanctum":
        for choice, name in (("o", "Offering"), ("b", "Breach"), ("s", "Study"), ("h", "Shelter")):
            # The established reducer owns availability.  It is deliberately
            # not probed here because offering is itself state-changing.
            options.append(_option(f"sanctum.choice:{choice}", name))
        title = "Sanctum"
    elif context_id.startswith("situation:"):
        from .situations import choices
        situation_id = context_id.split(":", 1)[1]
        for key, label, _semantic, available, _reason in choices(state, situation_id):
            options.append(_option(f"situation.choice:{situation_id}:{key.lower()}", label,
                                   available=available, reason_id=None if available else "situation.unavailable"))
        title = "Situation"
    elif context_id.startswith("ship-work:"):
        task = context_id.split(":", 1)[1]
        options.append(_option(f"ship.work:{task}", f"Resolve {_label(task)}"))
        title = "Ship crisis"
    elif context_id.startswith("voyage"):
        from .ship_crises import choices
        for key, label, _semantic in choices(state):
            options.append(_option(f"voyage.choice:{key.lower()}", label))
        title = "Voyage crisis"
    elif context_id == "workline":
        from .worklines import options as workline_options
        for key, label, _semantic, available, _reason in workline_options(state):
            options.append(_option(f"workline.choice:{key.lower()}", label,
                                   available=available, reason_id=None if available else "workline.unavailable"))
        title = "Workline"
    elif context_id == "mastery":
        from .manoeuvres import known, status
        for row in known(state):
            available, _ = status(state, row.id, None)
            options.append(_option(f"mastery.perform:{row.id}", row.name,
                                   available=available, reason_id=None if available else "mastery.unavailable",
                                   target_kind_id="actor"))
        title = "Manoeuvres"
    return ActivityView(context_id, title, tuple(options))


def resolve_activity(state: GameState, context_id: str, action_id: str,
                     target_position: Position | None = None,
                     target_actor_id: str | None = None) -> ActivityResolution:
    """Dispatch one published action to its existing authoritative reducer."""
    published = {row.action_id: row for row in activity_view(state, context_id).options}
    option = published.get(action_id)
    if option is None or not option.available:
        return ActivityResolution(False, False, "")
    before_time = state.world_time
    changed = False
    message = ""
    if action_id.startswith("production.make:"):
        from .production import make
        changed, message = make(state, action_id.split(":", 1)[1])
    elif action_id.startswith("production.delegate:"):
        from .production import delegate
        changed, message = delegate(state, action_id.split(":", 1)[1])
    elif action_id.startswith("production.gather:"):
        from .production import gather
        changed, message = gather(state, int(action_id.rsplit(":", 1)[1]))
    elif action_id.startswith("preparation.use:"):
        from .actions import use_gear
        result = use_gear(state, action_id.split(":", 1)[1]); changed, message = result.changed, result.message
    elif action_id.startswith("preparation.fill:"):
        from .chemistry import fill_flask
        _, flask_id, ingredient_id = action_id.split(":", 2); changed, message = fill_flask(state, flask_id, ingredient_id)
    elif action_id.startswith("preparation.distill:"):
        from .chemistry import distill_flask
        _, flask_id, reagent = action_id.split(":", 2); changed, message = distill_flask(state, flask_id, reagent)
    elif action_id.startswith("skill.buy:"):
        from .skill_tree import buy_node
        changed, message = buy_node(state, action_id.split(":", 1)[1])
    elif action_id.startswith("magic.cast:"):
        from .magic import SPELLS, cast
        spell_id = action_id.split(":", 1)[1]
        spell = SPELLS[spell_id]
        point = state.position if spell.target == "self" else target_position
        if point is None:
            return ActivityResolution(False, False, "")
        changed, message = cast(state, spell_id, point)
    elif action_id.startswith("material."):
        from .materials import handle_material
        head, point_text = action_id.split(":", 1)
        verb = head.split(".", 1)[1]
        point = Position(*map(int, point_text.split(",")))
        changed, message = handle_material(state, verb, point)
    elif action_id.startswith("vessel.refit:"):
        from .vessel_refits import install_refit
        changed, message = install_refit(state, action_id.split(":", 1)[1])
    elif action_id == "vessel.rest":
        from .magic import rest_at_berths
        changed, message = rest_at_berths(state)
    elif action_id.startswith("sanctum.choice:"):
        from .sanctums import shrine_choice
        changed, message, steps = shrine_choice(state, action_id.rsplit(":", 1)[1])
        if changed:
            from .actions import advance_world
            advance_world(state, steps=steps)
    elif action_id.startswith("situation.choice:"):
        from .situations import resolve
        _, situation_id, choice = action_id.split(":", 2)
        changed, message, steps = resolve(state, situation_id, choice)
        if changed:
            from .actions import advance_world
            advance_world(state, steps=steps)
    elif action_id.startswith("ship.work:"):
        from .ship_crises import work
        changed, message = work(state, action_id.split(":", 1)[1])
    elif action_id.startswith("voyage.choice:"):
        from .travel import resolve_voyage
        changed, message = resolve_voyage(state, action_id.split(":", 1)[1])
    elif action_id.startswith("workline.choice:"):
        from .worklines import resolve
        result = resolve(state, action_id.split(":", 1)[1]); changed, message = result.changed, result.message
    elif action_id.startswith("mastery.perform:"):
        from .manoeuvres import perform
        changed, message, steps = perform(state, action_id.split(":", 1)[1], target_actor_id)
        if changed:
            from .actions import advance_world
            advance_world(state, steps=steps)
    else:
        return ActivityResolution(False, False, "")
    return ActivityResolution(changed, state.world_time != before_time, message)

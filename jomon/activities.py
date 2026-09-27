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
        for relic_id in sorted(state.relics):
            options.append(_option(f"relic.select:{relic_id}", f"Select {_label(relic_id)}"))
        for flask in carried_flasks(state):
            options.append(_option(f"chemistry.pour:{flask.id}", f"Pour {flask.id}", target_kind_id="cell"))
            options.append(_option(f"chemistry.drink:{flask.id}", f"Drink from {flask.id}"))
        options.append(_option("preparation.use-readied", "Use selected relic, bottle, or readied gear"))
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
        options.append(_option("magic.restore", "Restore at shrine"))
        title = "Spellcraft"
    elif context_id == "materials":
        from .materials import VERBS
        from .material_presentation import material_verb_display_name
        for dx, dy in ((0, 0), (0, -1), (1, 0), (0, 1), (-1, 0)):
            point = Position(state.position.x + dx, state.position.y + dy, state.position.z)
            for verb in VERBS:
                options.append(_option(f"material.{verb}:{point.x},{point.y},{point.z}", material_verb_display_name(verb),
                                       f"{point.x}, {point.y}, {point.z:+d}"))
        from .worklines import WORKLINES
        if state.location == "region" and state.active_region_id in WORKLINES:
            options.append(_option("workline.open", "Open local workline"))
        from .aftermath import contracts_for, near_contract_site
        if any(contract.stage == 1 and near_contract_site(state, contract) for contract in contracts_for(state)):
            options.append(_option("aftermath.open", "Inspect accepted aftermath work"))
        title = "Field materials"
    elif context_id == "vessel":
        from .vessel_refits import STATION_REFITS, installation_status, refit_station_at
        from .vessel_presentation import refit_display_name
        station = refit_station_at(state)
        for refit_id in STATION_REFITS.get(station or "", ()):
            available, _ = installation_status(state, refit_id)
            options.append(_option(f"vessel.refit:{refit_id}", refit_display_name(refit_id),
                                   available=available, reason_id=None if available else "vessel.refit.unavailable"))
        if station == "berths":
            # ``rest_at_berths`` advances the simulation when it succeeds.  A
            # view must never probe a reducer, so publish the semantic action
            # and let the authoritative reducer validate it on submission.
            options.append(_option("vessel.rest", "Rest at berths"))
        title = "Vessel"
    elif context_id == "circuits":
        from .circuits import PARTS, cell_at, item_count, space_id
        if space_id(state):
            for kind, row in sorted(PARTS.items()):
                if row["behavior"] != "fuel" and item_count(state, kind):
                    options.append(_option(f"circuit.place:{kind}", f"Place {_label(kind)}",
                                           target_kind_id="cell"))
            options.extend((
                _option("circuit.operate:primary", "Operate selected circuit", target_kind_id="cell"),
                _option("circuit.operate:secondary", "Adjust selected circuit", target_kind_id="cell"),
                _option("circuit.reclaim:surface", "Reclaim selected circuit", target_kind_id="cell"),
            ))
        title = "Circuits"
    elif context_id in {"vehicle", "gangplank"}:
        options.extend((
            _option("vehicle.depart", "Depart for the active region"),
            _option("vehicle.board-tug", "Board the harbour tug"),
            _option("vehicle.disembark", "Disembark active vehicle"),
        ))
        title = "Vehicle"
    elif context_id == "field-traveller":
        options.extend((_option("traveller.choice:a", "Ask for a route clue"),
                        _option("traveller.choice:b", "Buy the offered lot")))
        title = "Field traveller"
    elif context_id == "station:workshop" or context_id.startswith("workshop"):
        from .workshop import FITTINGS, SLOTS, attached, can_fit
        from .equipment_presentation import fitting_name
        from .inventory import equipped_item
        for fitting_id in FITTINGS:
            options.append(_option(f"workshop.buy:{fitting_id}", f"Buy {fitting_name(fitting_id)} fitting"))
        for slot in SLOTS:
            item = equipped_item(state, slot)
            if item is None:
                continue
            if item.condition < 100:
                options.append(_option(f"workshop.repair:{item.id}", f"Repair {_label(item.kind)}"))
            for fitting_id in FITTINGS:
                if not any(part.kind == f"fitting:{fitting_id}" for part in attached(state, item)):
                    available, _ = can_fit(state, item, fitting_id)
                    options.append(_option(f"workshop.install:{item.id}:{fitting_id}",
                                           f"Fit {fitting_name(fitting_id)} to {_label(item.kind)}",
                                           available=available,
                                           reason_id=None if available else "workshop.unavailable"))
            for part in attached(state, item):
                socket = FITTINGS[part.kind.split(":", 1)[1]].slot
                options.append(_option(f"workshop.remove:{item.id}:{socket}", f"Remove {socket} fitting"))
        title = "Workshop"
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
    elif context_id == "objective":
        from .actions import can_alter_objective
        options.extend((
            _option("objective.decide:accept", "Accept objective"),
            _option("objective.decide:refuse", "Refuse objective"),
            _option("objective.decide:alter", "Alter objective", available=can_alter_objective(state),
                    reason_id=None if can_alter_objective(state) else "objective.alter.unavailable"),
        ))
        title = "Objective"
    elif context_id == "quest:regional":
        from .quests import regional_resolution_options
        for key, label, _semantic, available, _reason in regional_resolution_options(state):
            options.append(_option(f"quest.regional:{key.lower()}", label, available=available,
                                   reason_id=None if available else "quest.unavailable"))
        title = "Regional quest"
    elif context_id == "quest:arc":
        from .quests import arc_options
        for key, label, _semantic, available, _reason in arc_options(state):
            options.append(_option(f"quest.arc:{key.lower()}", label, available=available,
                                   reason_id=None if available else "quest.unavailable"))
        title = "Cross-region quest"
    elif context_id.startswith("contact-service:"):
        from .quests import secondary_service_options
        contact_id = context_id.split(":", 1)[1]
        for key, label, _semantic, available, _reason in secondary_service_options(state, contact_id):
            options.append(_option(f"contact.service:{contact_id}:{key.lower()}", label, available=available,
                                   reason_id=None if available else "contact.unavailable"))
        title = "Contact service"
    elif context_id.startswith("aftermath-contract:"):
        from .aftermath import contract_options
        contract_id = context_id.split(":", 1)[1]
        for key, label, _semantic, available, _reason in contract_options(state, contract_id):
            options.append(_option(f"aftermath.contract:{contract_id}:{key.lower()}", label, available=available,
                                   reason_id=None if available else "aftermath.unavailable"))
        title = "Aftermath contract"
    elif context_id == "aftermath":
        from .aftermath import contracts_for
        for contract in contracts_for(state):
            options.append(_option(f"aftermath.open:{contract.id}", contract.title,
                                   contract.status))
        title = "Aftermath"
    elif context_id == "merchant":
        from .actions import MERCHANT_ITEMS
        from .item_presentation import item_display_name_or_legacy
        for item_id in state.merchant_stock:
            options.append(_option(f"merchant.buy:{item_id}", item_display_name_or_legacy(item_id)))
        title = "Merchant"
    elif context_id == "bartender":
        from .vessel import DRINKS
        from .vessel_presentation import drink_display_name
        for drink_id in DRINKS:
            options.append(_option(f"bar.drink:{drink_id}", f"Drink {drink_display_name(drink_id)}"))
            options.append(_option(f"bar.bottle:{drink_id}", f"Bottle {drink_display_name(drink_id)}"))
        options.append(_option("support.open", "Choose household support"))
        options.append(_option("passives.open", "Choose carried discoveries"))
        title = "Bar"
    elif context_id == "incident":
        options.extend((_option("incident.resolve:mediate", "Mediate"),
                        _option("incident.resolve:side-first", "Support first speaker"),
                        _option("incident.resolve:let-fight", "Let the fight continue")))
        title = "Tavern incident"
    elif context_id == "route-stop":
        options.extend((_option("route.stop:resupply", "Resupply"),
                        _option("route.stop:trade", "Trade"),
                        _option("route.stop:sound", "Take soundings")))
        title = "Route stop"
    elif context_id.startswith("person:"):
        person_id = context_id.split(":", 1)[1]
        options.extend((_option(f"person.courier:{person_id}", "Select courier"),
                        _option(f"person.recruit:{person_id}", "Offer berth"),
                        _option(f"person.defer:{person_id}", "Defer invitation")))
        if state.courier and "teaching" in state.courier.skill_nodes:
            options.append(_option(f"teach.open:{person_id}", "Teach a known skill"))
        title = "Person"
    elif context_id.startswith("household-story:"):
        from .household_stories import story_choices
        story_id = context_id.split(":", 1)[1]
        for key, label, _semantic, available, _reason in story_choices(state, story_id):
            options.append(_option(f"story.resolve:{story_id}:{key.lower()}", label, available=available,
                                   reason_id=None if available else "story.unavailable"))
        title = "Household story"
    elif context_id == "station:gathering":
        from .household_stories import station_choices
        from .household_stories import STORIES
        from .skill_tree import NODES, journals_at_hand
        for story, (_key, label, _semantic, available, _reason) in zip(STORIES, station_choices(state)):
            options.append(_option(f"story.open:{story.id}", label, available=available,
                                   reason_id=None if available else "story.unavailable"))
        if state.courier:
            for node_id in state.courier.skill_nodes:
                if node_id not in state.courier.journal_nodes:
                    options.append(_option(f"journal.write:{node_id}", f"Write {NODES[node_id].name}"))
        for journal in journals_at_hand(state):
            options.append(_option(f"journal.study:{journal.id}", f"Study {journal.id}"))
        title = "Gathering"
    elif context_id == "support":
        from .actions import SUPPORTS
        for support_id, values in SUPPORTS.items():
            options.append(_option(f"support.select:{support_id}", _label(support_id), str(values[1])))
        title = "Household support"
    elif context_id == "passives":
        from .item_presentation import item_display_name_or_legacy
        for passive_id in sorted(state.owned_passives):
            options.append(_option(f"passive.toggle:{passive_id}", item_display_name_or_legacy(passive_id)))
        title = "Carried discoveries"
    elif context_id.startswith("teaching:"):
        from .skill_tree import NODES
        recipient_id = context_id.split(":", 1)[1]
        if state.courier:
            for node_id in state.courier.skill_nodes:
                options.append(_option(f"teach.node:{recipient_id}:{node_id}", f"Teach {NODES[node_id].name}"))
        title = "Teaching"
    elif context_id.startswith("station:") or context_id.startswith("vessel-refits:"):
        # Station interaction is a contextual entry into the same vessel
        # mechanics; the options retain stable refit IDs.
        return activity_view(state, "vessel")
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
    elif action_id == "preparation.use-readied":
        from .actions import use_gear
        result = use_gear(state); changed, message = result.changed, result.message
    elif action_id.startswith("relic.select:"):
        from .actions import choose_relic
        result = choose_relic(state, action_id.split(":", 1)[1]); changed, message = result.changed, result.message
    elif action_id.startswith("skill.buy:"):
        from .skill_tree import buy_node
        changed, message = buy_node(state, action_id.split(":", 1)[1])
    elif action_id.startswith("chemistry.pour:"):
        from .chemistry import pour_flask
        if target_position is None:
            return ActivityResolution(False, False, "")
        changed, message = pour_flask(state, action_id.split(":", 1)[1], target_position)
    elif action_id.startswith("chemistry.drink:"):
        from .chemistry import drink_flask
        changed, message = drink_flask(state, action_id.split(":", 1)[1])
    elif action_id.startswith("magic.restore"):
        from .magic import restore_at_shrine
        changed, message = restore_at_shrine(state)
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
    elif action_id.startswith("workshop.buy:"):
        from .workshop import buy_kit
        changed, message = buy_kit(state, action_id.split(":", 1)[1])
    elif action_id.startswith("workshop.repair:"):
        from .workshop import repair
        changed, message = repair(state, action_id.split(":", 1)[1])
    elif action_id.startswith("workshop.install:"):
        from .workshop import install
        _, item_id, fitting_id = action_id.split(":", 2)
        changed, message = install(state, item_id, fitting_id)
    elif action_id.startswith("workshop.remove:"):
        from .workshop import remove
        _, item_id, socket = action_id.split(":", 2)
        changed, message = remove(state, item_id, socket)
    elif action_id.startswith("circuit.place:"):
        from .circuits import place
        if target_position is None:
            return ActivityResolution(False, False, "")
        changed, message = place(state, target_position, "surface", action_id.split(":", 1)[1])
    elif action_id.startswith("circuit.operate:"):
        from .circuits import operate
        if target_position is None:
            return ActivityResolution(False, False, "")
        changed, message = operate(state, target_position, "surface", action_id.split(":", 1)[1])
    elif action_id.startswith("circuit.reclaim:"):
        from .circuits import reclaim
        if target_position is None:
            return ActivityResolution(False, False, "")
        changed, message = reclaim(state, target_position, action_id.split(":", 1)[1])
    elif action_id == "vehicle.depart":
        from .actions import depart
        result = depart(state); changed, message = result.changed, result.message
    elif action_id == "vehicle.board-tug":
        from .vehicles import board_tug
        result = board_tug(state); changed, message = result.changed, result.message
    elif action_id == "vehicle.disembark":
        from .vehicles import disembark
        result = disembark(state); changed, message = result.changed, result.message
    elif action_id.startswith("traveller.choice:"):
        from .landscape_variation import traveller_choice
        result = traveller_choice(state, action_id.split(":", 1)[1]); changed, message = result.changed, result.message
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
    elif action_id.startswith("objective.decide:"):
        from .actions import decide_objective
        result = decide_objective(state, action_id.split(":", 1)[1]); changed, message = result.changed, result.message
    elif action_id.startswith("quest.regional:"):
        from .actions import resolve_regional_quest_choice
        result = resolve_regional_quest_choice(state, action_id.split(":", 1)[1]); changed, message = result.changed, result.message
    elif action_id.startswith("quest.arc:"):
        from .actions import resolve_cross_region_choice
        result = resolve_cross_region_choice(state, action_id.split(":", 1)[1]); changed, message = result.changed, result.message
    elif action_id.startswith("contact.service:"):
        from .actions import use_contact_service
        _, contact_id, choice = action_id.split(":", 2)
        result = use_contact_service(state, choice, contact_id); changed, message = result.changed, result.message
    elif action_id.startswith("aftermath.contract:"):
        from .actions import use_aftermath_contract
        _, contract_id, choice = action_id.split(":", 2)
        result = use_aftermath_contract(state, contract_id, choice); changed, message = result.changed, result.message
    elif action_id.startswith("merchant.buy:"):
        from .actions import purchase_merchant_item
        result = purchase_merchant_item(state, action_id.split(":", 1)[1]); changed, message = result.changed, result.message
    elif action_id.startswith("bar.drink:") or action_id.startswith("bar.bottle:"):
        from .actions import purchase_bar_drink
        prefix, drink_id = action_id.split(":", 1)
        result = purchase_bar_drink(state, drink_id, bottle=prefix == "bar.bottle"); changed, message = result.changed, result.message
    elif action_id.startswith("incident.resolve:"):
        from .actions import intervene_socially
        result = intervene_socially(state, action_id.split(":", 1)[1]); changed, message = result.changed, result.message
    elif action_id.startswith("route.stop:"):
        from .actions import use_route_stop
        result = use_route_stop(state, action_id.split(":", 1)[1]); changed, message = result.changed, result.message
    elif action_id.startswith("person.courier:"):
        from .actions import choose_courier
        result = choose_courier(state, action_id.split(":", 1)[1]); changed, message = result.changed, result.message
    elif action_id.startswith("person.recruit:"):
        from .actions import recruit_person
        result = recruit_person(state, action_id.split(":", 1)[1]); changed, message = result.changed, result.message
    elif action_id.startswith("person.defer:"):
        from .actions import defer_recruit
        result = defer_recruit(state, action_id.split(":", 1)[1]); changed, message = result.changed, result.message
    elif action_id.startswith("journal.write:"):
        from .skill_tree import write_journal
        changed, message = write_journal(state, action_id.split(":", 1)[1])
    elif action_id.startswith("journal.study:"):
        from .skill_tree import study_journal
        changed, message = study_journal(state, action_id.split(":", 1)[1])
    elif action_id.startswith("teach.node:"):
        from .skill_tree import teach_node
        _, recipient_id, node_id = action_id.split(":", 2)
        changed, message = teach_node(state, recipient_id, node_id)
    elif action_id.startswith("support.select:"):
        from .actions import choose_support
        result = choose_support(state, action_id.split(":", 1)[1]); changed, message = result.changed, result.message
    elif action_id.startswith("passive.toggle:"):
        from .actions import choose_passive
        result = choose_passive(state, action_id.split(":", 1)[1]); changed, message = result.changed, result.message
    elif action_id.startswith("story.resolve:"):
        from .household_stories import resolve
        _, story_id, choice = action_id.split(":", 2)
        changed, message, steps = resolve(state, story_id, choice)
        if changed:
            from .actions import advance_world
            advance_world(state, steps=steps)
    else:
        return ActivityResolution(False, False, "")
    return ActivityResolution(changed, state.world_time != before_time, message)

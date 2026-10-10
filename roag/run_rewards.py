"""Run-local experience, boon offers, and inspectable build descriptions."""

from __future__ import annotations

from .run_items import RUN_ITEMS, can_collect_run_item, stack_value
from .state import GameState, stage_rng


# These effects use an ordinary attack, movement, defense, terrain, defeat, or
# visible encounter rule every class starts with.  The remaining legacy run
# items stay valid saved content and boss rewards, but never appear in the
# shared three-choice XP catalogue when their old wording only suits one weapon.
UNIVERSAL_BOON_IDS = frozenset(
    item_id for item_id, definition in RUN_ITEMS.items()
    if definition.family != "circuit"
    and definition.effect not in {"ranged_damage", "close_damage", "damage_charge"}
)


def eligible_boons(state: GameState, tier: str | None = None) -> tuple[str, ...]:
    run = state.run
    if run is None:
        return ()
    rows = [
        item_id for item_id in UNIVERSAL_BOON_IDS
        if item_id in (set(run.allowed_item_ids) or set(RUN_ITEMS))
        and (tier is None or RUN_ITEMS[item_id].tier == tier)
        and can_collect_run_item(state, item_id)
    ]
    return tuple(sorted(rows))


def _offer_choices(state: GameState, source: str, level: int) -> list[str]:
    tiers = ("common", "common", "uncommon") if level < 4 else (
        "common", "uncommon", "rare",
    )
    rng = stage_rng(
        state.seed,
        f"run-xp-offer:{state.run.run_id}:{level}:{source}:{len(state.run.pending_rewards)}",
    )
    choices: list[str] = []
    for index, tier in enumerate(tiers):
        pool = list(eligible_boons(state, tier)) or list(eligible_boons(state))
        # Prefer new behaviour without forbidding deliberately useful stacks.
        pool.sort(key=lambda item_id: (
            state.run.item_stacks.get(item_id, 0) > 0,
            state.run.item_stacks.get(item_id, 0), item_id,
        ))
        window = pool[:max(3, len(pool) // 2)] or pool
        candidate = window[rng.randrange(len(window))]
        if candidate in choices:
            alternatives = [row for row in pool if row not in choices]
            if alternatives:
                candidate = alternatives[rng.randrange(len(alternatives))]
        choices.append(candidate)
    return choices


def award_experience(state: GameState, amount: int, source: str) -> int:
    """Queue every crossed threshold; menus never consume a world turn."""
    run = state.run
    if run is None or run.status != "active" or amount <= 0:
        return 0
    run.experience += amount
    awarded = 0
    while run.experience >= run.next_experience:
        if not eligible_boons(state):
            # Do not fabricate a choice whose every stack is already capped.
            # Keep progress just below the next threshold so later catalogue
            # breadth can still be picked up without a silent lost reward.
            run.experience = run.next_experience - 1
            state.add_message("Every available boon is at its disclosed limit.", priority=2)
            break
        run.experience -= run.next_experience
        run.level += 1
        run.pending_rewards.append({
            "source": source,
            "level": run.level,
            "choices": _offer_choices(state, source, run.level),
        })
        run.next_experience = min(12, 3 + run.level * 2)
        awarded += 1
    if awarded:
        state.add_message(
            f"Power threshold reached: choose {awarded} boon"
            f"{'s' if awarded != 1 else ''} before acting again.",
            priority=3,
        )
    return awarded


def choose_pending_reward(state: GameState, choice_index: int) -> tuple[bool, str]:
    run = state.run
    if (
        run is None or not run.pending_rewards or type(choice_index) is not int
        or not 0 <= choice_index < 3
    ):
        return False, "No pending boon choice accepts that input."
    offer = run.pending_rewards.pop(0)
    item_id = offer["choices"][choice_index]
    from .run_items import collect_run_item

    before = run.item_stacks.get(item_id, 0)
    after = collect_run_item(state, item_id)
    definition = RUN_ITEMS[item_id]
    message = (
        f"{definition.name} {before}->{after}: {boon_effect_summary(item_id, after)}"
    )
    state.add_message(message, priority=3)
    return True, message


def boon_effect_summary(item_id: str, stacks: int) -> str:
    definition = RUN_ITEMS[item_id]
    current = stack_value(definition, stacks)
    following = stack_value(definition, stacks + 1)
    if following == current:
        next_text = "next copy is at this effect's limit"
    else:
        next_text = f"next copy {following}"
    return f"{definition.description} Current {current}; {next_text}."


def reward_lines(state: GameState) -> list[str]:
    run = state.run
    if run is None or not run.pending_rewards:
        return ["No boon choice is pending."]
    offer = run.pending_rewards[0]
    lines = [
        f"LEVEL {offer['level']}  Source: {offer['source'].replace('_', ' ')}.",
        "Choose one. This pauses the world; a fresh key selects it.",
    ]
    for index, item_id in enumerate(offer["choices"], start=1):
        current = run.item_stacks.get(item_id, 0)
        lines.append(
            f"{index}. {RUN_ITEMS[item_id].name} ({current}->{current + 1}) — "
            f"{boon_effect_summary(item_id, current + 1)}"
        )
    if len(run.pending_rewards) > 1:
        lines.append(f"{len(run.pending_rewards) - 1} further threshold choice(s) remain queued.")
    return lines


def build_lines(state: GameState) -> list[str]:
    run = state.run
    if run is None:
        return ["No active run build."]
    from .run_classes import active_class

    definition = active_class(state)
    lines = [
        f"{definition.name.upper() if definition else 'UNKNOWN'} RUN BUILD",
        f"Objective: defeat five regional claimants; the fifth is the final boss.",
        f"XP {run.experience}/{run.next_experience}; level {run.level}; "
        f"stage {run.stage_index}/5.",
        f"[A] attack  [B] {definition.movement_name if definition else 'movement'} "
        f"({run.movement_cooldown})  [X] {definition.signature_name if definition else 'signature'} "
        f"({run.signature_cooldown})",
        "",
    ]
    if not run.item_stacks:
        lines.append("No boons yet. XP thresholds, scattered chests, elites, and bosses grant them.")
        return lines
    for item_id, stacks in sorted(run.item_stacks.items()):
        lines.append(
            f"{RUN_ITEMS[item_id].name} x{stacks}: {boon_effect_summary(item_id, stacks)}"
        )
    return lines

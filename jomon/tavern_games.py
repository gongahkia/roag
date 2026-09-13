"""Shared tavern-game sessions, invitations, seats, and counted NPC credit."""

from __future__ import annotations

from .state import GameState, Person, Position

STARTING_NPC_CREDIT = 12
GAME_SLOTS = {
    "dullest": ("tabletop", "active_match"),
    "draw": ("tavern_draw", "active_hand"),
    "dice": ("tavern_dice", "active_match"),
}


def active_games(state: GameState) -> tuple[str, ...]:
    return tuple(game for game, (ledger, field) in GAME_SLOTS.items()
                 if getattr(state, ledger)[field] is not None)


def another_game_active(state: GameState, requested: str) -> bool:
    return any(game != requested for game in active_games(state))


def validate_game_occupancy(state: GameState) -> None:
    if len(active_games(state)) > 1:
        raise ValueError("more than one tavern game is active")


def available_opponents(state: GameState) -> list[Person]:
    people = [*state.household, *state.visitors, state.bartender]
    return [person for person in people if person.id != state.active_courier_id
            and person.alive and person.available
            and (schedule := state.actor_schedules.get(person.id)) is not None
            and schedule.area == "tavern"]


def invited_opponents(state: GameState, opponents: list[str], chairs: tuple[Position, ...], table: str) -> dict[str, Person]:
    if len(opponents) != 3 or len(set(opponents)) != 3:
        raise ValueError("invite three distinct tavern adults")
    available = {person.id: person for person in available_opponents(state)}
    if any(identity not in available for identity in opponents):
        raise ValueError("all three opponents must be in the tavern")
    occupied = {schedule.position for identity, schedule in state.actor_schedules.items()
                if schedule.area == "tavern" and identity not in opponents}
    occupied.add(state.position)
    if sum(chair not in occupied for chair in chairs) < 3:
        raise ValueError(f"the {table} table needs three free opponent chairs")
    return available


def seat_opponents(state: GameState, opponents: list[str], chairs: tuple[Position, ...], activity: str, table: str) -> None:
    occupied = {schedule.position for identity, schedule in state.actor_schedules.items()
                if schedule.area == "tavern" and identity not in opponents}
    occupied.add(state.position)
    free = [chair for chair in chairs if chair not in occupied]
    if len(free) < len(opponents):
        raise ValueError(f"the {table} table has no three free chairs")
    named = {person.id for person in [*state.household, *state.visitors]}
    for identity, chair in zip(opponents, free):
        schedule = state.actor_schedules[identity]
        schedule.area = schedule.destination_area = "tavern"
        schedule.position = schedule.destination = chair
        schedule.activity = activity
        schedule.next_boundary = state.world_time + 6
        if identity in named:
            state.tavern_positions[identity] = chair


def seated_game_opponents(state: GameState) -> set[str]:
    players = set()
    for ledger, field in (GAME_SLOTS["draw"], GAME_SLOTS["dice"]):
        match = getattr(state, ledger)[field]
        if match is not None and match["phase"] != "complete":
            players.update(match["players"][1:])
    return players


def npc_credit(state: GameState, identity: str, *, open_account: bool = False) -> int:
    # The existing draw ledger owns this persisted account until a save-format migration is warranted.
    balances = state.tavern_draw["bankrolls"]
    return balances.setdefault(identity, STARTING_NPC_CREDIT) if open_account else balances.get(identity, STARTING_NPC_CREDIT)


def change_npc_credit(state: GameState, identity: str, amount: int) -> int:
    updated = npc_credit(state, identity) + amount
    if not 0 <= updated <= 100000:
        raise ValueError("NPC tavern credit would leave its bounded account")
    state.tavern_draw["bankrolls"][identity] = updated
    return updated


def validate_npc_accounts(state: GameState) -> None:
    balances = state.tavern_draw["bankrolls"]
    named = {person.id for person in [*state.household, *state.visitors, state.bartender]}
    if not isinstance(balances, dict) or any(identity not in named or type(amount) is not int or not 0 <= amount <= 100000
                                              for identity, amount in balances.items()):
        raise ValueError("invalid shared tavern NPC credit")

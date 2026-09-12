"""Deterministic legal-command fuzzer with replayable failure transcripts."""

from __future__ import annotations

from dataclasses import asdict
import json
import random
from typing import Any

from .engine import GameEngine, RuleError
from .policies import Command, Policy, execute_command, legal_plays


class FuzzFailure(AssertionError):
    pass


def _commands(engine: GameEngine, policy: Policy) -> list[Command]:
    state = engine.state
    if state.phase in {"victory", "defeat"}:
        return []
    if state.phase == "combat":
        return [*legal_plays(engine), Command("end_turn")]
    if state.phase == "exploration":
        return [
            Command("step_exploration", position)
            for position in engine._neighbors((state.party_x, state.party_y))
        ]
    if state.phase == "reward":
        return [Command("choose_reward", (index,)) for index in range(len(state.rewards))] + [
            Command("choose_reward", (None,))
        ]
    if state.phase == "objective":
        objective = engine.current_objective()
        if objective.approach is not None:
            return [Command("advance_objective")]
        result = [Command("leave_objective")]
        for approach in engine.mission_definition(objective.biome_id)["approaches"]:
            resource = approach["cost"]["resource"]
            if resource not in {"light", "supplies"} or getattr(state, resource) >= approach["cost"]["amount"]:
                result.append(Command("begin_objective", (approach["id"],)))
        return result
    if state.phase == "event":
        return [
            Command("choose_event", (index,))
            for index, choice in enumerate(engine.catalog.events[state.current_event]["choices"])
            if choice.get("cost_supplies", 0) <= state.supplies
        ]
    if state.phase == "post_victory":
        return [Command("extract"), Command("descend_again")]
    command = policy.next_command(engine)
    return [] if command is None else [command]


def fuzz_expedition(catalog, fuzz_seed: int, *, steps: int = 120) -> dict[str, Any]:
    if type(fuzz_seed) is not int or type(steps) is not int or not 1 <= steps <= 5000:
        raise ValueError("fuzz seed must be an integer and steps must be 1..5000")
    chooser = random.Random(fuzz_seed)
    game_seed = chooser.getrandbits(32)
    engine = GameEngine.new(catalog, game_seed)
    policy = Policy(chooser.choice(Policy.NAMES))
    transcript: list[dict[str, Any]] = []
    for sequence in range(steps):
        choices = _commands(engine, policy)
        if not choices:
            break
        # A loop is sampled at most once; otherwise terminal victories terminate
        # promptly enough for broad seed cohorts.
        if engine.state.phase == "post_victory" and engine.state.loop_depth:
            choices = [Command("extract")]
        command = chooser.choice(choices)
        transcript.append({"sequence": sequence, **asdict(command)})
        try:
            execute_command(engine, command)
            snapshot = json.loads(json.dumps(engine.snapshot(), allow_nan=False))
            engine = GameEngine.from_snapshot(engine.catalog, snapshot)
        except (AssertionError, KeyError, RuleError, TypeError, ValueError) as exc:
            raise FuzzFailure(
                f"fuzz seed {fuzz_seed}, game seed {game_seed}, transcript "
                f"{json.dumps(transcript, separators=(',', ':'))}: {exc}"
            ) from exc
    return {
        "fuzz_seed": fuzz_seed,
        "game_seed": game_seed,
        "policy": policy.name,
        "commands": transcript,
        "outcome": engine.state.phase,
        "loop_depth": engine.state.loop_depth,
        "final": engine.snapshot(),
    }

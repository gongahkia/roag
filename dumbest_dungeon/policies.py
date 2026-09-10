"""Deterministic legal-command regression players, never a substitute for playtesting.

Policies read the hand, public definitions, displayed intents and known map sites.
They never inspect shuffled draw order, future random state or hidden discoveries.
Only execute_command mutates the simulation, through an explicit public allowlist.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import time
from collections import Counter
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Any

from .content import load_catalog
from .engine import Actor, GameEngine, RuleError
from .save import write_save

POLICY_VERSION = 1
COMMANDS = frozenset({
    "select_curated_squad", "begin_expedition", "step_exploration", "play_card",
    "end_turn", "use_supply", "begin_objective", "advance_objective",
    "leave_objective", "resolve_facility", "leave_facility", "finish_hazard",
    "choose_reward", "choose_event", "service", "boon_pickup_options",
    "resolve_boon_pickup", "resolve_item_pickup", "bargain_options",
    "resolve_bargain", "resolve_hidden_trap",
    "extract", "descend_again",
})


@dataclass(frozen=True)
class Command:
    method: str
    args: tuple[Any, ...] = ()


def canonical_hash(engine: GameEngine) -> str:
    encoded = json.dumps(engine.snapshot(), sort_keys=True, separators=(",", ":"), allow_nan=False)
    return hashlib.sha256(encoded.encode("utf-8")).hexdigest()


def execute_command(engine: GameEngine, command: Command) -> Any:
    if command.method not in COMMANDS:
        raise RuleError(f"not a regression-player command: {command.method}")
    return getattr(engine, command.method)(*command.args)


def legal_plays(engine: GameEngine) -> list[Command]:
    if engine.state.phase != "combat":
        return []
    owners = {hero.id: hero for hero in engine.living_heroes()}
    plays = []
    for index, card in enumerate(engine.state.hand):
        definition = engine.catalog.cards.get(card.card_id)
        if definition is None:
            continue
        owner = owners.get(definition["hero"])
        if (owner is None or owner.rank not in definition["from_ranks"]
                or owner.statuses.get("stun") or engine.card_cost(card) > engine.state.energy):
            continue
        plays.extend(Command("play_card", (index, target)) for target in engine.valid_targets(index))
    return plays


def incoming_damage(engine: GameEngine) -> dict[str, int]:
    """Estimate the displayed phase; conditional setup remains an approximation."""
    amounts: Counter[str] = Counter()
    actors = {actor.id: actor for actor in engine.living_heroes() + engine.living_enemies()}
    for intent in engine.state.intents:
        enemy = actors.get(intent["enemy_id"])
        if enemy is None or enemy.statuses.get("stun"):
            continue
        action = next(x for x in engine.catalog.enemies[enemy.definition_id]["actions"]
                      if x["name"] == intent["action"])
        for effect in action["effects"]:
            if effect["op"] != "damage":
                continue
            for target_id in intent["target_ids"]:
                target = actors.get(target_id)
                if target is None or target.side != "hero":
                    continue
                target = actors.get(target.guarded_by, target)
                value = int(effect["amount"])
                if effect.get("bonus_status") in target.statuses:
                    value += int(effect.get("bonus", 0))
                value = engine._outgoing_damage(enemy, value, target)
                if target.statuses.get("vulnerable"):
                    value = round(value * 1.5)
                amounts[target.id] += value
    return dict(amounts)


class Policy:
    """Myopic policies with disclosed route appetite and combat valuation."""

    NAMES = ("rusher", "explorer", "greedy", "offense", "defense", "synergy", "position")

    def __init__(self, name: str = "explorer"):
        if name not in self.NAMES:
            raise ValueError("unknown policy")
        self.name = name
        self.destination: tuple[int, int] | None = None
        self.visited: set[tuple[int, int]] = set()

    def _rank_value(self, engine: GameEngine, hero: Actor, rank: int) -> int:
        return sum(
            (3 if card in engine.state.hand else 1)
            for card in engine.state.deck + engine.state.hand
            if card.card_id in engine.catalog.cards
            and engine.catalog.cards[card.card_id]["hero"] == hero.id
            and rank in engine.catalog.cards[card.card_id]["from_ranks"]
        )

    def play_value(self, engine: GameEngine, command: Command) -> float:
        index, target_id = command.args
        card = engine.state.hand[index]
        definition = engine.catalog.cards[card.card_id]
        actor = engine._actor(definition["hero"])
        main = engine._card_targets(definition["target"], target_id, actor)
        incoming = incoming_damage(engine)
        value = 0.0
        effects = definition["upgrade_effects"] if card.upgraded else definition["effects"]
        defensive = 1.25 if self.name == "defense" else 0.65 if self.name == "offense" else 1.0
        for effect in effects:
            if not engine._card_effect_condition(effect, actor, main):
                continue
            op, amount = effect["op"], int(effect.get("amount", 0))
            if op in {"damage", "heal", "block"} and definition.get("biome") == engine.current_biome():
                amount += definition.get("biome_bonus", 0)
            if op == "draw":
                value += amount * (3 if engine.state.energy > engine.card_cost(card) else 0.5)
                continue
            if op == "energy":
                value += amount * 7
                continue
            if op == "discard":
                value -= min(amount, max(0, len(engine.state.hand) - 1))
                continue
            for target in engine._effect_targets(effect.get("target"), main, actor):
                risk = max(0, incoming.get(target.id, 0) - target.block)
                if op == "damage":
                    hit = amount + (int(effect.get("bonus", 0))
                                    if effect.get("bonus_status") in target.statuses else 0)
                    hit = engine._outgoing_damage(actor, hit, target)
                    if target.statuses.get("vulnerable"):
                        hit = round(hit * 1.5)
                    if target.statuses.get("dodge"):
                        value += 2
                    else:
                        dealt = max(0, hit - target.block)
                        value += min(dealt, target.hp) + min(hit, target.block) * 0.2
                        if dealt >= target.hp:
                            value += 10
                elif op == "heal":
                    restored = min(amount, target.max_hp - target.hp)
                    value += restored * defensive * (2.5 if target.hp <= risk + 3 else 0.8)
                    if target.deaths_door and restored:
                        value += 20
                elif op == "block":
                    value += min(amount, risk) * defensive
                    if target.deaths_door and amount >= risk > 0:
                        value += 16
                elif op == "stress":
                    value += min(-amount, target.stress) * 0.25 if amount < 0 else -amount * 0.35
                elif op == "cleanse":
                    value += sum({"wound": 5, "stun": 12, "vulnerable": 5, "weak": 4,
                                  "marked": 3}.get(status, 0) for status in target.statuses)
                elif op == "status":
                    status = effect["status"]
                    if target.statuses.get(status, 0) >= amount:
                        continue
                    value += {"stun": 10, "weak": 4, "wound": 2 * min(3, amount),
                              "marked": 3, "vulnerable": 5, "focus": 3,
                              "riposte": min(6, risk), "dodge": min(9, risk)}.get(status, 0)
                elif op == "guard" and target.id != actor.id:
                    value += min(risk, actor.block) * defensive
                elif op == "move" and target.side == "hero":
                    party = engine.living_heroes()
                    new_rank = max(1, min(len(party), target.rank + amount))
                    delta = self._rank_value(engine, target, new_rank) - self._rank_value(engine, target, target.rank)
                    for other in party:
                        if min(new_rank, target.rank) <= other.rank <= max(new_rank, target.rank) and other != target:
                            shifted = other.rank + (1 if new_rank < target.rank else -1)
                            delta += self._rank_value(engine, other, shifted) - self._rank_value(engine, other, other.rank)
                    value += delta * (1.2 if self.name == "position" else 0.7)
        return value / max(1, engine.card_cost(card))

    def _travel(self, engine: GameEngine) -> Command:
        state = engine.state
        origin = (state.party_x, state.party_y)
        self.visited.add(origin)
        if state.supplies:
            if state.light < 30:
                return Command("use_supply", ("light",))
            if any(hero.hp < 8 for hero in engine.living_heroes()):
                return Command("use_supply", ("heal",))
        if self.destination == origin:
            self.destination = None
        if self.destination is None:
            costs = engine._travel_costs_from(origin)
            objectives = [o for o in state.objectives if not o.completed]
            active = [o for o in objectives if o.approach is not None]
            if engine.boss_unlocked():
                destination = engine.core_position()
            else:
                objective = min(active or objectives,
                                key=lambda o: (costs[engine.objective_position(o)], o.id))
                destination = engine.objective_position(objective)
            if self.name != "rusher":
                allowance = 80 if self.name == "greedy" else 12
                sites = [(p.x, p.y) for p in state.pickups
                         if not p.resolved and not p.hidden and engine.feature_is_known(p.id)]
                sites += [engine.room_position(r.id) for r in state.rooms
                          if not r.resolved and r.kind in {"camp", "upgrade", "cache"}]
                sites += [(f.x, f.y) for f in state.facilities
                          if not f.used and engine.feature_is_known(f.id)]
                candidates = [p for p in sites if p not in self.visited
                              and costs[p] <= allowance and costs[p] <= costs[destination] + 6]
                if candidates:
                    destination = min(candidates, key=lambda p: (costs[p], p))
            self.destination = destination
        path = engine._find_path(origin, self.destination)
        if not path:
            raise RuleError("policy has no advancing route")
        engine.path_to(*path[0])
        return Command("step_exploration", path[0])

    def next_command(self, engine: GameEngine) -> Command | None:
        state = engine.state
        if state.phase in {"victory", "defeat"}:
            return None
        if state.phase == "post_victory":
            return Command("extract")
        if state.phase == "exploration":
            return self._travel(engine)
        if state.phase == "combat":
            plays = legal_plays(engine)
            if plays:
                best = max(plays, key=lambda command: self.play_value(engine, command))
                if self.play_value(engine, best) > 0:
                    return best
            return Command("end_turn")
        if state.phase == "hazard":
            return Command("finish_hazard")
        if state.phase == "objective":
            self.destination = None
            objective = engine.current_objective()
            if objective.approach is not None:
                return Command("advance_objective")
            approaches = engine.mission_definition(objective.biome_id)["approaches"]
            affordable = [a for a in approaches if a["cost"]["resource"] not in {"supplies", "light"}
                          or getattr(state, a["cost"]["resource"]) >= a["cost"]["amount"]]
            if not affordable:
                return Command("leave_objective")
            if self.name == "rusher":
                approach = min(affordable, key=lambda a: engine.objective_approach_projection(objective, a["id"])["ticks"])
            else:
                approach = affordable[0]
            return Command("begin_objective", (approach["id"],))
        if state.phase == "facility":
            facility = engine.current_facility()
            options = [o for o in engine.facility_definition(facility)["options"]
                       if engine.facility_option_available(facility, o["id"])[0]]
            return Command("resolve_facility", (options[0]["id"],)) if options else Command("leave_facility")
        if state.phase == "event":
            choices = engine.catalog.events[state.current_event]["choices"]
            index = next(i for i, c in enumerate(choices) if c.get("cost_supplies", 0) <= state.supplies)
            return Command("choose_event", (index,))
        if state.phase == "service":
            if state.service_type == "camp":
                return Command("service", ("recover",))
            candidates = [(i, c) for i, c in enumerate(state.deck)
                          if not c.upgraded and c.card_id in engine.catalog.cards]
            if not candidates:
                return Command("service", ("remove", 0))
            index, _ = max(candidates, key=lambda pair: self.draft_value(engine, pair[1].card_id))
            return Command("service", ("upgrade", index))
        if state.phase == "reward":
            if not state.rewards:
                return Command("choose_reward", (None,))
            index = max(range(len(state.rewards)), key=lambda i: self.draft_value(engine, state.rewards[i]))
            value = self.draft_value(engine, state.rewards[index])
            return Command("choose_reward", (index if value >= 5 and len(state.deck) < 28 else None,))
        if state.phase == "discovery":
            pickup = engine.current_pickup()
            hero = engine.living_heroes()[0]
            if pickup.kind == "item":
                return Command("resolve_item_pickup")
            if pickup.kind == "trap":
                return Command("resolve_hidden_trap")
            if pickup.kind == "bargain":
                return Command("resolve_bargain", (hero.id, None))
            if pickup.payload.get("hero_id") != hero.id:
                return Command("boon_pickup_options", (hero.id,))
            options = pickup.payload["options"]
            order = ("quick_hands", "hunters_rhythm", "field_rations", "second_wind", "vigilance")
            choice = min(options, key=lambda b: order.index(b) if b in order else len(order))
            return Command("resolve_boon_pickup", (hero.id, choice))
        raise RuleError(f"unsupported policy phase: {state.phase}")

    def draft_value(self, engine: GameEngine, card_id: str) -> float:
        card = engine.catalog.cards[card_id]
        owner = next(h for h in engine.living_heroes() if h.id == card["hero"])
        tags = set(card["tags"])
        deck_tags = {t for c in engine.state.deck if c.card_id in engine.catalog.cards
                     for t in engine.card_tags(c.card_id)}
        value = sum({"damage": 3, "recovery": 5, "energy": 6, "draw": 3,
                     "cleanse": 2, "mobility": 2, "control": 3}.get(t, 0) for t in tags)
        value += sum(4 for t in tags if t.startswith("payoff:")
                     and "setup:" + t.split(":", 1)[1] in deck_tags)
        value -= sum(c.card_id == card_id for c in engine.state.deck) * 3
        value -= 8 if owner.rank not in card["from_ranks"] else 0
        return value


def run_policy(engine: GameEngine, policy: Policy, *, limit: int = 3000,
               checkpoint_every: int = 0) -> dict[str, Any]:
    started = time.monotonic()
    records = []
    encounters = []
    active = None
    for sequence in range(limit):
        command = policy.next_command(engine)
        if command is None:
            break
        previous_phase = engine.state.phase
        previous_round = engine.state.round
        offered = list(engine.state.rewards) if command.method == "choose_reward" else []
        execute_command(engine, command)
        if previous_phase != "combat" and engine.state.phase == "combat":
            active = {"kind": engine.state.combat_kind, "biome": engine.current_biome(),
                      "enemies": [e.definition_id for e in engine.state.enemies],
                      "at_tick": engine.state.travel_ticks}
        if previous_phase == "combat" and engine.state.phase != "combat":
            if active is not None:
                encounters.append({**active, "rounds": previous_round, "result": engine.state.phase})
            active = None
        record = {"sequence": sequence, "command": asdict(command), "phase": engine.state.phase,
                  "hash": canonical_hash(engine)}
        if offered:
            record["offered"] = offered
        records.append(record)
        if checkpoint_every and (sequence + 1) % checkpoint_every == 0:
            snapshot = json.loads(json.dumps(engine.snapshot()))
            restored = GameEngine.from_snapshot(engine.catalog, snapshot)
            if canonical_hash(restored) != record["hash"]:
                raise AssertionError(f"checkpoint differs at command {sequence}")
            engine = restored
    return {"policy_version": POLICY_VERSION, "policy": policy.name,
            "seed": engine.state.seed, "outcome": engine.state.phase,
            "bounded_stop": engine.state.phase not in {"victory", "defeat"},
            "elapsed_seconds": round(time.monotonic() - started, 3),
            "layout": engine.catalog.worlds[engine.state.world_id]["layout"],
            "biomes": engine.state.biome_ids, "crew": [asdict(h) for h in engine.state.heroes],
            "ticks": engine.state.travel_ticks, "light": engine.state.light,
            "supplies": engine.state.supplies, "objectives": [asdict(o) for o in engine.state.objectives],
            "deck": [asdict(c) for c in engine.state.deck], "items": engine.state.items,
            "boons": engine.state.boons, "curses": engine.state.curses,
            "encounters": encounters, "commands": records, "final": engine.snapshot()}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--seed", type=int, required=True)
    parser.add_argument("--squad", default="bulkhead_basics")
    parser.add_argument("--policy", choices=Policy.NAMES, default="explorer")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--limit", type=int, default=3000)
    parser.add_argument("--checkpoint-every", type=int, default=0)
    args = parser.parse_args()
    if not 1 <= args.limit <= 10000 or args.checkpoint_every < 0:
        parser.error("limit must be 1..10000 and checkpoint interval nonnegative")
    engine = GameEngine.new(load_catalog(), args.seed, start_in_hub=True)
    engine.select_curated_squad(args.squad)
    engine.begin_expedition()
    report = run_policy(engine, Policy(args.policy), limit=args.limit,
                        checkpoint_every=args.checkpoint_every)
    report["squad"] = args.squad
    write_save(args.output, report)
    print(json.dumps({k: report[k] for k in ("seed", "squad", "policy", "outcome", "bounded_stop",
                                             "ticks", "light", "supplies", "elapsed_seconds", "encounters")}))


if __name__ == "__main__":
    main()

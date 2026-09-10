"""Local morgue summaries and explicitly requested NDJSON exports."""

from __future__ import annotations

import argparse
import hashlib
import json
from collections import Counter, defaultdict
from dataclasses import asdict
from pathlib import Path
from typing import Any

from .manifest import canonical_bytes
from .save import SaveError, default_save_path, read_save, write_save, write_text_atomic
from .telemetry import decision_counts
from .versions import ENGINE_VERSION, PROFILE_SCHEMA, RNG_ARCHITECTURE, RUN_SAVE_SCHEMA, TELEMETRY_SCHEMA

HISTORY_SCHEMA = 1


def run_report(engine, *, elapsed_seconds: int | None = None, outcome: str | None = None) -> dict[str, Any]:
    state = engine.state
    sources = defaultdict(Counter)
    encounters = []
    decisions = []
    activations = Counter()
    for row in state.ledger.records:
        data = row.data
        if row.kind == "damage":
            for key in ("requested", "absorbed", "amount", "hp_loss", "overkill"):
                sources[row.source_id][f"damage_{key}"] += data[key]
        elif row.kind in {"healing", "block", "block_expired", "stress"}:
            sources[row.source_id][row.kind] += data["amount"]
        elif row.kind == "control_skip":
            sources[row.source_id]["control_skips"] += 1
        elif row.kind == "payoff":
            activations[data["mechanic"]] += 1
        elif row.kind == "condition" and data["activated"]:
            activations.update(data["conditions"].values())
        if row.kind == "encounter_start":
            encounters.append({"id": row.source_id, "at_tick": row.tick, **data})
        elif row.kind == "encounter_end":
            if encounters and "result" not in encounters[-1]:
                encounters[-1].update(result=data["result"], rounds=data["rounds"])
        if row.kind.startswith(("card_", "boon_", "curse_", "item_", "bargain_", "objective_", "facility_", "guardian_", "finale_")) or row.kind in {"crew_death", "deaths_door_check", "travel"}:
            decisions.append(asdict(row))
    departure = next((row.data for row in state.ledger.records if row.kind == "departure"), None)
    turns = [row.data for row in state.ledger.records if row.kind == "turn_start"]
    roots = [row.data for row in state.ledger.records if row.kind == "resolution_root"]
    result = outcome or state.phase
    digest = hashlib.sha256(canonical_bytes(engine.snapshot())).hexdigest()
    return {
        "history_schema": HISTORY_SCHEMA, "id": digest, "seed": state.seed,
        "versions": {"engine": ENGINE_VERSION, "content": engine.catalog.raw["schema_version"],
                     "save": RUN_SAVE_SCHEMA, "profile": PROFILE_SCHEMA, "telemetry": TELEMETRY_SCHEMA,
                     "rng": RNG_ARCHITECTURE},
        "manifest": engine.catalog.manifest.snapshot(), "outcome": result,
        "elapsed_seconds": elapsed_seconds, "duration_basis": "session wall time including menus" if elapsed_seconds is not None else "unmeasured",
        "world": state.world_id, "layout": engine.catalog.worlds[state.world_id]["layout"],
        "biomes": list(state.biome_ids), "travel_ticks": state.travel_ticks,
        "ladder_rank": state.ladder_rank,
        "objectives": [asdict(item) for item in state.objectives],
        "facilities": [asdict(item) for item in state.facilities],
        "starting_formation": departure["formation"] if departure else None,
        "crew": [asdict(hero) for hero in state.heroes], "deck": [asdict(card) for card in state.deck],
        "items": dict(state.items), "boons": state.boons, "curses": state.curses,
        "light": state.light, "supplies": state.supplies,
        "cards": decision_counts(state.ledger), "sources": {key: dict(value) for key, value in sorted(sources.items())},
        "resolution": {"roots": len(roots), "max_depth": max((step.get("depth", 0) for root in roots for step in root["trace"]), default=0),
                       "chain_seals": engine.resolution.state.seals},
        "encounters": encounters, "decisions": decisions, "payoff_activations": dict(sorted(activations.items())),
        "rank_invalid_cards": sum(row["rank_invalid"] for row in turns),
        "owner_disabled_cards": sum(row["owner_disabled"] for row in turns),
        "hands_sampled": len(turns), "cause": state.log[-1] if state.log else "unknown",
        "incomplete_before_tick": state.ledger.incomplete_before_tick,
    }


def write_run(directory: Path, engine, *, elapsed_seconds: int | None = None,
              outcome: str | None = None, detailed: bool = False) -> Path:
    report = run_report(engine, elapsed_seconds=elapsed_seconds, outcome=outcome)
    path = directory / f"{report['id']}.json"
    write_save(path, report)
    if detailed:
        header = {"telemetry_schema": TELEMETRY_SCHEMA, "run_id": report["id"], "seed": report["seed"], "manifest": report["manifest"]}
        rows = [canonical_bytes(header)] + [canonical_bytes(asdict(row)) for row in engine.state.ledger.records]
        write_text_atomic(directory / "telemetry" / f"{report['id']}.ndjson", b"\n".join(rows).decode("ascii") + "\n")
    return path


def read_history(directory: Path) -> tuple[list[dict], list[str]]:
    records, errors = [], []
    for path in sorted(directory.glob("*.json")):
        try:
            record = read_save(path)
            fields = {"history_schema": int, "id": str, "seed": int, "outcome": str,
                      "world": str, "layout": str, "travel_ticks": int, "duration_basis": str,
                      "cause": str, "crew": list, "encounters": list, "decisions": list,
                      "cards": dict, "sources": dict}
            if (any(type(record.get(key)) is not expected for key, expected in fields.items())
                or record.get("history_schema") != HISTORY_SCHEMA or record.get("id") != path.stem
                or any(key not in record for key in ("elapsed_seconds", "incomplete_before_tick"))):
                raise SaveError("invalid history schema or identity")
            for section, required in (("crew", {"rank", "id", "hp", "max_hp", "stress"}),
                                      ("encounters", {"id", "plan"}),
                                      ("decisions", {"tick", "round", "kind", "source_id", "data"})):
                if any(not isinstance(row, dict) or not required <= row.keys() for row in record[section]):
                    raise SaveError(f"invalid history {section}")
            records.append(record)
        except SaveError as exc:
            errors.append(f"{path.name}: {exc}")
    return records, errors


def history_lines(record: dict) -> list[str]:
    lines = [f"Seed {record['seed']} | {record['outcome'].upper()}",
             f"{record['world']} / {record['layout']} | {record['travel_ticks']} travel ticks",
             f"Duration: {record['elapsed_seconds']} seconds ({record['duration_basis']})",
             f"Cause: {record['cause']}", "Crew:"]
    lines.extend(f"  R{hero['rank']} {hero['id']} HP {hero['hp']}/{hero['max_hp']} stress {hero['stress']}" for hero in record["crew"])
    if record["incomplete_before_tick"] is not None:
        lines.append(f"History before tick {record['incomplete_before_tick']} was not recorded.")
    lines.append("Encounters:")
    lines.extend(f"  {item['id']} / {item['plan']} / {item.get('rounds', '?')} rounds / {item.get('result', 'unfinished')}" for item in record["encounters"])
    lines.append("Cards: offered / picked / skipped / played / edited")
    lines.extend(f"  {identity}: {json.dumps(counts, sort_keys=True)}" for identity, counts in record["cards"].items())
    lines.append("Arithmetic by source:")
    lines.extend(f"  {identity}: {json.dumps(counts, sort_keys=True)}" for identity, counts in record["sources"].items())
    lines.append("Decisions and route:")
    lines.extend(f"  t{row['tick']} r{row['round']} {row['kind']} {row['source_id']}: {json.dumps(row['data'], sort_keys=True)}" for row in record["decisions"])
    return lines


def main() -> None:
    parser = argparse.ArgumentParser(description="Read the local Dullest Dungeon morgue; no networking.")
    parser.add_argument("--directory", type=Path, default=default_save_path().parent / "history")
    parser.add_argument("--filter", default="", help="case-insensitive seed, crew, outcome or content text")
    parser.add_argument("--detail", action="store_true")
    args = parser.parse_args()
    records, errors = read_history(args.directory)
    for record in records:
        if args.filter.casefold() not in json.dumps(record).casefold():
            continue
        if args.detail:
            print("\n".join(history_lines(record)))
        else:
            print(f"{record['seed']:>12} {record['outcome']:<10} {record['layout']:<10} {'/'.join(hero['id'] for hero in record['crew'])}")
    for error in errors:
        print(f"History read error: {error}")


if __name__ == "__main__":
    main()

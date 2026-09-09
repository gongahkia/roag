"""Read-only terminal text for recorded combat arithmetic and trigger ordering."""

from collections import defaultdict


def resolution_lines(engine) -> list[str]:
    records = engine.state.ledger.records
    start = max((index for index, row in enumerate(records) if row.kind == "encounter_start"), default=0)
    arithmetic = defaultdict(list)
    for row in records[start:]:
        data = row.data
        identity = data.get("event_id")
        if identity is None:
            continue
        target = data.get("target", "crew")
        if row.kind == "damage":
            text = (f"{target}: request {data['requested']}; after modifiers {data.get('modified', 'unrecorded')}; "
                    f"block {data['absorbed']}; deflected {data.get('deflected', 'unrecorded')}; "
                    f"HP lost {data['hp_loss']}; overkill {data['overkill']}")
            if data["dodged"]:
                text += "; DODGED"
        elif row.kind == "healing":
            text = f"{target}: heal request {data['requested']}; restored {data['amount']}; excess {data['overheal']}"
        elif row.kind == "block":
            text = f"{target}: block +{data['amount']}"
        elif row.kind == "status":
            text = f"{target}: {data['status']} {data['previous']} -> {data['result']} (application {data['amount']})"
        elif row.kind == "stress":
            text = f"{target}: stress {data['amount']:+}"
        elif row.kind == "deaths_door_check":
            text = f"{target}: death chance {data['chance_bp']}/10000; roll {data['roll']}; died {data['died']}"
        elif row.kind == "owner_disabled_event":
            text = f"{data['owner']}: owner dead; effect skipped"
        else:
            continue
        arithmetic[identity].append(f"  {row.source_id} | {text}")
    lines = ["ORDER: replace/prevent, before, primary, after, death, cleanup.",
             "Each root is one initiated action; indented results identify its source."]
    traces = [row.data for row in records[start:] if row.kind == "resolution_root"]
    if engine.resolution.state.root_id is not None:
        traces.append({"root": engine.resolution.state.root_id, "trace": engine.resolution.state.trace})
    for root in traces:
        lines.append(f"ROOT {root['root']}")
        for step in root["trace"]:
            if step.get("message") == "CHAIN SEALED":
                lines.append(f"CHAIN SEALED {step['chain_id']}; ancestry {step['ancestry']}")
                continue
            identity = step["event_id"]
            lines.append(f"#{identity} {step['phase']} {step['source_id']} depth {step['depth']}"
                         + (f" parent {step['parent_event_id']}" if step.get("parent_event_id") else ""))
            if step["phase"] == "PRIMARY":
                lines.extend(arithmetic[identity])
    if not traces:
        lines.append("No queued resolution recorded in this encounter yet.")
    return lines

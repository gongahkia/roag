#!/usr/bin/env python3
"""Exercise actual Godot ENet processes through the same room APIs as the UI."""
import json
import os
from pathlib import Path
import socket
import subprocess
import tempfile
import time

ROOT = Path(__file__).resolve().parent.parent


def main():
    processes = {}
    logs = {}
    sequences = {}
    with tempfile.TemporaryDirectory(prefix="redline-network-") as temp:
        folder = Path(temp)
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.bind(("127.0.0.1", 0))
        port = sock.getsockname()[1]
        sock.close()

        def start(tag, role="client"):
            log = open(folder / f"{tag}.log", "w")
            logs[tag] = log
            command = [str(ROOT / "scripts/godot.sh"), "--headless", "--path", str(ROOT / "speedrunner"),
                       "--max-fps", "60", "--script", "res://tests/network_peer.gd", "--",
                       f"--role={role}", f"--tag={tag}", f"--directory={folder}", f"--port={port}"]
            processes[tag] = subprocess.Popen(command, cwd=ROOT, stdout=log, stderr=log)

        def report(tag):
            try:
                return json.loads((folder / f"{tag}.json").read_text())
            except (OSError, ValueError):
                return {}

        def wait(description, predicate, seconds=12):
            deadline = time.monotonic() + seconds
            while time.monotonic() < deadline:
                if predicate():
                    return
                for tag, process in processes.items():
                    if process.poll() is not None:
                        raise AssertionError(f"{tag} exited during {description}: " + (folder / f"{tag}.log").read_text()[-3000:])
                time.sleep(0.05)
            raise AssertionError(f"Timed out: {description}; server={report('server')}")

        def send(tag, action, **fields):
            sequences[tag] = sequences.get(tag, 0) + 1
            path = folder / f"{tag}-command.json"
            temporary = path.with_suffix(".tmp")
            temporary.write_text(json.dumps(dict(sequence=sequences[tag], action=action, **fields)))
            temporary.replace(path)

        def converged(*tags):
            reports = [report(tag) for tag in tags]
            return all(r.get("hash") for r in reports) and len({r["hash"] for r in reports}) == 1

        def identities_match(*tags):
            identities = [report(tag).get("identities", {}) for tag in tags]
            return bool(identities[0]) and all(item == identities[0] for item in identities[1:])

        def unique_colors(tag):
            identities = report(tag).get("identities", {})
            return bool(identities) and len({p["color"] for p in identities.values()}) == len(identities)

        def cell(x, expected):
            return report("server").get("cells", {}).get(f"{x}:5") == expected

        try:
            start("server", "server")
            wait("dedicated host", lambda: report("server").get("ready"))
            start("Alice")
            wait("first player/owner", lambda: report("Alice").get("active") and report("Alice").get("ready"))
            start("Bob")
            wait("two networked players", lambda: all(report(t).get("count") == 2 for t in ("server", "Alice", "Bob")))
            assert report("Alice")["owner"] == report("Alice")["local_id"]
            wait("initial identities match including shader tints and nameplates", lambda: identities_match("server", "Alice", "Bob") and unique_colors("server"))
            bob_color = report("server")["people"]["Bob"]["color"]
            send("Bob", "rename", username=" Alice\n")
            wait("server assigns a distinct username and updates every nameplate", lambda: all("Alice 2" in report(t).get("people", {}) for t in ("server", "Alice", "Bob")) and identities_match("server", "Alice", "Bob"))
            assert report("server")["people"]["Alice 2"]["color"] == bob_color
            rename_after = time.monotonic() + 1.05
            wait("rename rate window", lambda: time.monotonic() >= rename_after)
            send("Bob", "rename", username="Bob")
            wait("client renames without reconnecting", lambda: all("Bob" in report(t).get("people", {}) for t in ("server", "Alice", "Bob")) and identities_match("server", "Alice", "Bob"))
            send("Alice", "edit", x=5, y=5, tile=4)
            send("Bob", "edit", x=6, y=5, tile=7)
            wait("simultaneous edits", lambda: cell(5, 4) and cell(6, 7) and converged("server", "Alice", "Bob"))
            send("Alice", "edit", x=5, y=5, tile=29, layer=1)
            wait("background layer", lambda: report("server").get("back") == 29 and converged("server", "Alice", "Bob"))
            send("Alice", "raw_edit", x=7, y=5, tile=4, expected=0)
            send("Bob", "raw_edit", x=7, y=5, tile=3, expected=0)
            wait("same-cell conflict", lambda: cell(7, 4) or cell(7, 3))
            wait("stale writer rejection", lambda: any("Cell changed" in n for t in ("Alice", "Bob") for n in report(t).get("notices", [])))
            wait("conflict convergence", lambda: converged("server", "Alice", "Bob"))
            for tile in (4, 3):
                send("Alice", "edit", x=9, y=5, tile=tile)
                wait("own edit", lambda tile=tile: cell(9, tile) and converged("server", "Alice", "Bob"))
            for action, tile in (("undo", 4), ("undo", 0), ("redo", 4), ("redo", 3)):
                send("Alice", action)
                wait(action, lambda tile=tile: cell(9, tile) and converged("server", "Alice", "Bob"))
            send("Bob", "edit", x=9, y=5, tile=9)
            wait("another builder writes", lambda: cell(9, 9) and converged("server", "Alice", "Bob"))
            send("Alice", "undo")
            wait("non-destructive undo conflict", lambda: any("Another builder" in n for n in report("Alice").get("notices", [])))
            assert cell(9, 9)
            send("Alice", "lock", locked=True)
            wait("host locks editing", lambda: report("Bob").get("editing") is False)
            send("Bob", "edit", x=10, y=5, tile=4)
            wait("guest edit rejected", lambda: any("Editing is locked" in n for n in report("Bob").get("notices", [])))
            assert cell(10, 0)
            send("Bob", "phase", mode="race")
            wait("guest cannot start a race", lambda: any("Only the host" in n for n in report("Bob").get("notices", [])))
            assert report("server")["phase"] == "build"
            send("Alice", "lock", locked=False)
            wait("edits reopened", lambda: report("Bob").get("editing") is True)
            send("Alice", "save")
            wait("host saves the world", lambda: (folder / "saved-world.json").exists() and any("World saved" in n for n in report("Alice").get("notices", [])))
            saved_hash = report("server")["hash"]
            send("Alice", "edit", x=11, y=5, tile=4)
            wait("edit before reload", lambda: cell(11, 4) and converged("server", "Alice", "Bob"))
            send("Alice", "load")
            wait("owner reloads shared world", lambda: cell(11, 0) and report("server").get("hash") == saved_hash and converged("server", "Alice", "Bob"))
            start("Charlie")
            wait("late join gets complete edited world", lambda: report("Charlie").get("count") == 3 and converged("server", "Alice", "Bob", "Charlie"))
            send("Bob", "editor", enabled=True)
            wait("editor state synchronized", lambda: all(report(t).get("people", {}).get("Bob", {}).get("editor") is True for t in ("server", "Bob")))
            send("Bob", "editor", enabled=False)
            wait("return to play", lambda: all(report(t).get("people", {}).get("Bob", {}).get("editor") is False for t in ("server", "Bob")))
            previous_generation = report("server")["people"]["Bob"]["generation"]
            send("Bob", "restart")
            wait("respawn input generation", lambda: report("server").get("people", {}).get("Bob", {}).get("generation", 0) > previous_generation)
            send("Bob", "move", axis=1)
            wait("movement continues after edit/respawn", lambda: report("server").get("people", {}).get("Bob", {}).get("x", 0) > 100)
            send("Bob", "move", axis=0)
            send("Bob", "disconnect")
            wait("disconnected player removed", lambda: "Bob" not in report("server").get("people", {}))
            send("Bob", "reconnect")
            wait("same client reconnects with a fresh input sequence", lambda: report("Bob").get("active") and report("server").get("count") == 3)
            wait("rejoining reuses a free tint without duplicating a remaining player", lambda: unique_colors("server") and identities_match("server", "Alice", "Bob", "Charlie"))
            assert report("server")["people"]["Bob"]["color"] == bob_color
            send("Bob", "move", axis=1)
            wait("reconnected player moves", lambda: report("server").get("people", {}).get("Bob", {}).get("x", 0) > 100)
            send("Bob", "disconnect")
            wait("reconnected player leaves cleanly", lambda: "Bob" not in report("server").get("people", {}))
            send("Alice", "phase", mode="race")
            wait("synchronized race countdown", lambda: all(report(t).get("phase") == "race" for t in ("server", "Alice", "Charlie")))
            assert len({report(t)["start"] for t in ("server", "Alice", "Charlie")}) == 1
            revision = report("server")["revision"]
            send("Charlie", "edit", x=11, y=5, tile=4)
            wait("race edit lock", lambda: any("Editing is locked" in n for n in report("Charlie").get("notices", [])))
            send("Alice", "move", axis=1)
            send("Charlie", "move", axis=1)
            wait("server validates both finishes", lambda: len(report("server").get("results", {})) == 2, seconds=15)
            wait("results reach all racers", lambda: all(len(report(t).get("results", {})) == 2 for t in ("Alice", "Charlie")))
            assert report("server")["revision"] == revision
            assert all(result["ticks"] > 0 and result["revision"] == revision for result in report("server")["results"].values())
            send("Alice", "rename", username="Speedster")
            wait("renaming after finishing updates synchronized results", lambda: all(any(result["name"] == "Speedster" for result in report(t).get("results", {}).values()) for t in ("server", "Alice", "Charlie")) and identities_match("server", "Alice", "Charlie"))
            start("Dave")
            wait("late racer is a spectator", lambda: report("server").get("people", {}).get("Dave", {}).get("spectator") is True)
            send("Alice", "phase", mode="build")
            wait("return to collaborative building", lambda: report("Dave").get("phase") == "build" and report("Dave").get("people", {}).get("Dave", {}).get("spectator") is False)
            print("Multiplayer integration: movement, edit conflicts, layers, undo/redo, permissions, save, late join, respawn, disconnect, race and results passed", flush=True)
            # A dedicated server admits 16 real simultaneous clients.
            tags = ["Alice", "Charlie", "Dave"]
            for number in range(4, 17):
                tag = f"Player{number:02d}"
                start(tag)
                tags.append(tag)
            wait("sixteen simultaneous clients", lambda: report("server").get("count") == 16, seconds=25)
            wait("sixteen clients share one world", lambda: all(report(tag).get("count") == 16 for tag in tags) and converged("server", *tags), seconds=25)
            wait("sixteen distinct colors and consistent rendered identities", lambda: unique_colors("server") and identities_match("server", *tags), seconds=25)
            print("Identity integration: usernames, live rename, unique colors after reconnect and all 16 shader tints/nameplates synchronized", flush=True)
            print("Capacity integration: 16 simultaneous clients admitted and synchronized", flush=True)
            send("Alice", "kick", target="Player16")
            wait("owner can remove a guest", lambda: report("server").get("count") == 15 and "Player16" not in report("server").get("people", {}))
            for tag in tags:
                log_text = (folder / f"{tag}.log").read_text()
                assert "SCRIPT ERROR" not in log_text and "ERROR:" not in log_text, f"Runtime error in {tag}: {log_text[-2000:]}"
            for process in processes.values():
                process.terminate()
            for process in processes.values():
                process.wait(timeout=3)
            processes.clear()
            start("ListenHost", "listen")
            wait("listen host starts as a player", lambda: report("ListenHost").get("count") == 1)
            start("Guest")
            wait("guest joins a listen host", lambda: report("Guest").get("count") == 2 and report("ListenHost").get("count") == 2)
            assert report("ListenHost")["owner"] == 1
            send("ListenHost", "phase", mode="race")
            wait("listen-host countdown", lambda: report("Guest").get("phase") == "race")
            send("ListenHost", "move", axis=1)
            send("Guest", "move", axis=1)
            wait("host and guest both finish", lambda: len(report("ListenHost").get("results", {})) == 2, seconds=15)
            wait("listen-host results synchronize", lambda: len(report("Guest").get("results", {})) == 2)
            for tag in ("ListenHost", "Guest"):
                log_text = (folder / f"{tag}.log").read_text()
                assert "SCRIPT ERROR" not in log_text and "ERROR:" not in log_text, f"Runtime error in {tag}: {log_text[-2000:]}"
            print("Listen-host integration: host and guest play and finish together", flush=True)
            print("All real-process multiplayer checks passed", flush=True)
        except Exception:
            destination = ROOT / ".race-data/network-test-failure"
            destination.mkdir(parents=True, exist_ok=True)
            for path in folder.iterdir():
                if path.is_file():
                    (destination / path.name).write_bytes(path.read_bytes())
            raise
        finally:
            for process in processes.values():
                if process.poll() is None:
                    process.terminate()
            for process in processes.values():
                try:
                    process.wait(timeout=3)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait()
            for log in logs.values():
                log.close()


if __name__ == "__main__":
    main()

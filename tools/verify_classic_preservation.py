#!/usr/bin/env python3
"""Verify original catalogue/artwork in place and retired Kenney files in Git."""
import hashlib
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MANIFEST_SHA = '815151b0371eea5f992cd3e25d821401364710963830f15e2fa36e6273e56e2f'


def main():
    raw = (ROOT / 'docs/colony-web/preservation-manifest.json').read_bytes()
    if hashlib.sha256(raw).hexdigest() != MANIFEST_SHA:
        raise SystemExit('Original preservation manifest changed')
    manifest = json.loads(raw)
    retired = json.loads((ROOT / 'docs/classic-restoration/retired-paths.json').read_text())
    allowed = set(retired['paths'])
    failures, live, archived = [], 0, 0
    for entry in manifest['files']:
        name = entry['path']
        path = ROOT / name
        if path.is_file() and not path.is_symlink():
            data = path.read_bytes()
            live += 1
        elif name.startswith('assets/kenney/') and name in allowed:
            result = subprocess.run(['git', 'show', f"{manifest['baseline_commit']}:{name}"], cwd=ROOT, capture_output=True)
            if result.returncode:
                failures.append({'path': name, 'reason': 'retired original not recoverable from Git'})
                continue
            data = result.stdout
            archived += 1
        else:
            failures.append({'path': name, 'reason': 'missing or symlinked original'})
            continue
        if len(data) != entry['bytes'] or hashlib.sha256(data).hexdigest() != entry['sha256']:
            failures.append({'path': name, 'reason': 'original bytes changed'})
    print(json.dumps({'passed': not failures, 'checked': len(manifest['files']), 'in_place': live,
                      'retired_kenney_recoverable_in_git': archived, 'failures': failures}, indent=2))
    return bool(failures)


if __name__ == '__main__':
    raise SystemExit(main())

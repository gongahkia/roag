#!/usr/bin/env python3
"""Read-only verification of the immutable pre-web preservation manifest."""
import argparse
import hashlib
import json
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--manifest', type=Path)
    args = parser.parse_args()
    root = args.root.resolve()
    manifest_path = args.manifest or root / 'docs/colony-web/preservation-manifest.json'
    manifest = json.loads(manifest_path.read_text())
    if manifest.get('schema_version') != 1 or not manifest.get('files'):
        raise SystemExit('Invalid or empty preservation manifest')
    failures, seen = [], set()
    for entry in manifest['files']:
        relative = entry['path']
        path = root / relative
        if relative in seen or Path(relative).is_absolute() or '..' in Path(relative).parts:
            raise SystemExit('Invalid or repeated manifest path: ' + relative)
        seen.add(relative)
        if path.is_symlink() or not path.is_file():
            failures.append({'path': relative, 'reason': 'missing, symlink, or not a file'})
            continue
        try:
            path.resolve().relative_to(root)
        except ValueError:
            failures.append({'path': relative, 'reason': 'outside preservation root'})
            continue
        data = path.read_bytes()
        if len(data) != entry['bytes'] or hashlib.sha256(data).hexdigest() != entry['sha256']:
            failures.append({'path': relative, 'reason': 'content changed'})
    print(json.dumps({'passed': not failures, 'baseline_commit': manifest['baseline_commit'],
                      'checked_files': len(seen), 'failures': failures}, indent=2))
    return 1 if failures else 0


if __name__ == '__main__':
    raise SystemExit(main())

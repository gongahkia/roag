#!/usr/bin/env python3
"""Build a reproducible .love archive containing only the current game artwork."""
import argparse
import hashlib
import json
import zipfile
from pathlib import Path
from catalog_io import load_catalog
from build_classic_assets import build

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--out', type=Path, default=ROOT / 'dist/roag.love')
    args = parser.parse_args()
    build(ROOT / 'assets/classic-bindings.json', ROOT / 'src/classic_sprite_data.lua', check=True, root=ROOT)
    selection = json.loads((ROOT / 'assets/classic-bindings.json').read_text())
    records = {s['id']: s for s in load_catalog()['sprites']}
    files = {'main.lua', 'conf.lua', 'src/classic_sprite_data.lua', 'assets/CREDITS.txt', 'assets/classic-bindings.json'}
    files.update(p.relative_to(ROOT).as_posix() for p in (ROOT / 'src').glob('*.lua'))
    files.update('assets/' + records[b['sprite']]['source'] for b in selection['bindings'].values())
    files.update(p.relative_to(ROOT).as_posix() for p in (ROOT / 'assets/fonts').iterdir() if p.is_file())
    if any(p.startswith(('assets/kenney/', 'web/', 'src/Roag.')) for p in files):
        raise SystemExit('Retired runtime or artwork entered the package')
    args.out.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(args.out, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for name in sorted(files):
            info = zipfile.ZipInfo(name, date_time=(1980, 1, 1, 0, 0, 0))
            info.external_attr = 0o100644 << 16
            info.compress_type = zipfile.ZIP_DEFLATED
            archive.writestr(info, (ROOT / name).read_bytes(), compresslevel=9)
    digest = hashlib.sha256(args.out.read_bytes()).hexdigest()
    receipt = {'archive': args.out.name, 'bytes': args.out.stat().st_size, 'sha256': digest, 'files': sorted(files)}
    args.out.with_suffix('.manifest.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps({k: v for k, v in receipt.items() if k != 'files'}))


if __name__ == '__main__':
    main()

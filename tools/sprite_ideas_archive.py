"""Verify the exact user-reviewed snapshot independently of newer live art."""
import hashlib
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
RECORD = ROOT/'docs/sprite-audit/ideas-decisions.json'


def verify(include_surplus=False):
    record = json.loads(RECORD.read_bytes())
    assert record['schema'] == 'roag-sprite-ideas-decisions/1'
    paths = {**record['review_files'], **record['sources']}
    inventory = record['review_inventory']
    paths[inventory['path']] = inventory['sha256']
    paths[record['feedback_file']] = record['feedback_sha256']
    if include_surplus:
        paths.update(record['superseded_reports'])
    for name, checksum in paths.items():
        path = (ROOT/name).resolve()
        assert path.is_relative_to(ROOT) and path.is_file()
        assert hashlib.sha256(path.read_bytes()).hexdigest() == checksum, \
            'User-reviewed archive/source changed: '+name
    original = json.loads((ROOT/inventory['path']).read_bytes())
    assert original['mapping_checksum'] == record['review_mapping_checksum']
    result = json.loads(subprocess.check_output([
        'node','tools/sprite_ideas_feedback.js',record['feedback_file']],cwd=ROOT))
    assert result['proposal_fingerprint'] == record['proposal_fingerprint']
    progress = result['progress']
    for key, value in [('total','total'),('approved','approve'),('rejected','reject'),('discussed','discuss')]:
        assert progress[key] == record['counts'][value]
    print('PASS preserved user-reviewed archive: 55 approve / 30 reject / 4 redirect; live audit remains separate')
    return record

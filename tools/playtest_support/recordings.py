"""Read bounded session evidence. Never follow selectedSave into player data."""
import json
from pathlib import Path
from agent_support.workspace import identifier

DEFAULT_ROOT = Path.home() / '.vagabonds/sessions'


def read(path, default=None, limit=1048576):
    path = Path(path)
    if not path.exists():
        return default
    if path.is_symlink() or path.stat().st_size > limit:
        raise ValueError('Diagnostic file is a link or exceeds its size bound: ' + str(path))
    return json.loads(path.read_text())


def folder(root, session_id):
    path = Path(root).resolve() / identifier(session_id)
    if path.is_symlink():
        raise ValueError('Session directory must not be a symbolic link.')
    metadata = read(path / 'session.json')
    if not metadata or metadata.get('schemaVersion') != 1 or metadata.get('id') != session_id:
        raise ValueError('Not a recognized session recording: ' + session_id)
    return path


def sessions(root=DEFAULT_ROOT, limit=20):
    result = []
    if not Path(root).is_dir():
        return result
    for path in sorted(Path(root).iterdir(), reverse=True):
        if len(result) >= limit:
            break
        try:
            location = folder(root, path.name)
            meta = read(location / 'session.json')
            summary = read(location / 'summary.json', {})
            latest = read(location / 'latest.json', {})
            world = latest.get('state', {}).get('world', latest.get('state', {}))
            result.append({'id': path.name, 'started': meta['started'], 'status': summary.get('status', 'unfinalized'),
                           'seed': world.get('seed', (summary.get('worldIdentity') or {}).get('seed', meta.get('identity', {}).get('requestedSeed'))),
                           'markers': len(summary.get('markers', [])), 'droppedEvents': summary.get('droppedEvents', 0),
                           'folder': str(location)})
        except (ValueError, OSError, KeyError, TypeError):
            continue
    return result


def events(root, session_id, after=0, limit=40, kind=None):
    if not 1 <= limit <= 200 or after < 0:
        raise ValueError('Use --limit 1..200 and a nonnegative --after sequence.')
    path = folder(root, session_id)
    found, malformed = [], 0
    for event_file in sorted(path.glob('events-*.jsonl'))[:16]:
        if event_file.is_symlink() or event_file.stat().st_size > 4194304:
            raise ValueError('Invalid event file size or link.')
        for line in event_file.read_text().splitlines():
            try:
                event = json.loads(line)
                if event.get('sequence', 0) > after and (kind is None or event.get('kind') == kind):
                    found.append(event)
            except (ValueError, TypeError):
                malformed += 1  # A crash may leave a partial final line.
    found.sort(key=lambda e: e['sequence'])
    selected = found[:limit] if after else found[-limit:]
    return {'id': session_id, 'events': selected, 'nextAfter': selected[-1]['sequence'] if selected else after,
            'availableMatchingEvents': len(found), 'malformedLines': malformed, 'boundedHistory': True}


def brief_snapshot(state):
    world = state.get('world', state)
    result = {key: world[key] for key in ['seed', 'contentHash', 'tick', 'position', 'paused', 'frozen', 'awaitingContinuation', 'victory',
        'knownDiscoveries', 'visitedDiscoveries', 'visitedSites', 'residentChunks', 'modifiedChunks', 'inventory'] if key in world}
    if 'party' in world:
        result['party'] = [{k: a[k] for k in ['id', 'name', 'classId', 'level', 'health', 'maxHealth', 'mana', 'maxMana', 'life', 'position', 'hunger', 'fatigue', 'wounds'] if k in a} for a in world['party']]
    result['workCount'] = len(world.get('work', []))
    if state.get('ui'):
        result['ui'] = {key: state['ui'][key] for key in ['panel', 'notice', 'routeCount'] if key in state['ui']}
    return result


def report(root, session_id, full=False):
    path = folder(root, session_id)
    meta = read(path / 'session.json')
    summary = read(path / 'summary.json', {})
    latest = read(path / 'latest.json', {})
    recent = events(root, session_id, limit=25)
    if not full:
        summary = {key: value for key, value in summary.items() if key != 'state'}
        if latest.get('state'):
            latest = {**latest, 'state': brief_snapshot(latest['state'])}
        for event in recent['events']:
            if event.get('kind') == 'snapshot':
                event['data'] = brief_snapshot(event['data']); event['payloadSummarized'] = True
            if event.get('kind') == 'marker':
                event['data']['state'] = brief_snapshot(event['data']['state']); event['payloadSummarized'] = True
    warnings = []
    writer_error = read(path / 'recording-error.json')
    if writer_error:
        warnings.append('The diagnostic writer stopped: ' + writer_error.get('message', 'unknown error'))
    if summary.get('status') == 'running':
        warnings.append('Unfinalized recording: the process may still be running or may have been interrupted; this is not proof of liveness.')
    if summary.get('droppedEvents', 0):
        warnings.append('Some events were dropped; totals and history may be incomplete.')
    metrics = summary.get('metrics', {})
    signals = []
    crash = read(path / 'crash.json', summary.get('crash'))
    if crash:
        signals.append({'kind': 'managed-exception', 'evidence': 'crash.json', 'detail': crash})
    if metrics.get('commandsRejected', 0) >= 3:
        signals.append({'kind': 'repeated-command-rejections', 'count': metrics['commandsRejected'], 'interpretation': 'Inspect the event reasons; intentional invalid actions are also counted.'})
    if metrics.get('latestFrameP95Ms', 0) > 50:
        signals.append({'kind': 'slow-frame-sample', 'milliseconds': metrics['latestFrameP95Ms'], 'interpretation': 'Measured locally; software rendering and debug/test conditions affect performance.'})
    return {'id': session_id, 'folder': str(path), 'metadata': meta, 'summary': summary,
            'latest': latest, 'recentEvents': recent, 'signals': signals, 'warnings': warnings,
            'limits': ['Bounded event history; this is not a complete deterministic replay.',
                       'Native crashes and forced termination may omit final crash context.',
                       'No player save or chunk file was opened by this report.']}
